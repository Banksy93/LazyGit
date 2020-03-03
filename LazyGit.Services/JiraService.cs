using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyGit.Common.Pocos;
using LazyGit.Services.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Serilog;

namespace LazyGit.Services
{
	public class JiraService : IJiraService
	{
		#region App Settings

		private readonly string _baseUrl;
		private readonly string _authorizationHeader;
		private readonly string _ticketStatus;
		private readonly string _project;
		private readonly string _username;
		private readonly string _password;
		private readonly string _comment;
		private readonly string _transitionId;
		private readonly string _fixVersion;

		#endregion

		public JiraService(IOptions<JiraConfig> config)
		{
			_baseUrl = config.Value.Url;
			_authorizationHeader = config.Value.AuthorizationHeader;
			_ticketStatus = config.Value.TicketStatus;
			_project = config.Value.Project;
			_username = config.Value.Username;
			_password = config.Value.Password;
			_comment = config.Value.Comment;
			_transitionId = config.Value.TransitionId;
			_fixVersion = config.Value.FixVersion;
		}

		public async Task<JiraIssueResponse> GetReadyToMergeJiraTicketsAsync()
		{
			if (!AreConfigValuesSet())
			{
				Log.Warning("Some or all of the Jira application settings aren't configured. Please configure them.");
				return new JiraIssueResponse
				{
					Issues = new List<JiraTicket>()
				};
			}

			try
			{
				var client = new RestClient(_baseUrl)
				{
					Authenticator = new HttpBasicAuthenticator(_username, _password)
				};

				var restRequest = new RestRequest("2/search", Method.POST);

				var model = new JiraSearchRequest
				{
					jql = $"project = {_project} AND status = \"{_ticketStatus}\" AND fixVersion = \"{_fixVersion}\" order by updated DESC"
				};

				restRequest.AddHeader("Accept", "application/json");
				restRequest.AddJsonBody(model);

				var response = await client.ExecuteTaskAsync(restRequest);

				var deserializedResult = JsonConvert.DeserializeObject<JiraIssueResponse>(response.Content);

				Log.Debug("Successfully retrieved: {IssueCount} issues set to Ready to Merge.",
						deserializedResult.Issues.Count());

				return deserializedResult;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				throw;
			}
		}

		public async Task<bool> UpdateJiraTicketAsync(string issueKey)
		{
			try
			{
				var client = new RestClient(_baseUrl)
				{
					Authenticator = new HttpBasicAuthenticator(_username, _password)
				};

				var restRequest = new RestRequest($"2/issue/{issueKey}/transitions?expand=transitions.fields", Method.POST);
				restRequest.AddHeader("Accept", "application/json");
				restRequest.AddJsonBody(BuildTransitionRequest(_transitionId, _comment));

				var response = await client.ExecuteTaskAsync(restRequest);

				Log.Debug("Successfully updated Jira ticket with key: {Key}", issueKey);

				return response.IsSuccessful;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				throw;
			}
		}

		/// <summary>
		/// If any of the config values aren't set, terminate the program
		/// </summary>
		/// <returns></returns>
		private bool AreConfigValuesSet()
		{
			return !string.IsNullOrWhiteSpace(_baseUrl)
				&& !string.IsNullOrWhiteSpace(_authorizationHeader)
				&& !string.IsNullOrWhiteSpace(_ticketStatus)
				&& !string.IsNullOrWhiteSpace(_project)
				&& !string.IsNullOrWhiteSpace(_username)
				&& !string.IsNullOrWhiteSpace(_password)
				&& !string.IsNullOrWhiteSpace(_comment)
				&& !string.IsNullOrWhiteSpace(_transitionId);
		}

		/// <summary>
		/// Build transition request to update Jira tickets status to 'Done'
		/// </summary>
		/// <param name="transitionId">The Id of the transition to set the Jira ticket to 'Done'</param>
		/// <param name="comment">The comment to add to the Jira ticket</param>
		/// <returns></returns>
		private static JiraTransitionRequest BuildTransitionRequest(string transitionId, string comment)
		{
			return new JiraTransitionRequest
			{
				update = new Update
				{
					comment = new Comment
					{
						add = new CommentContent
						{
							body = comment
						}
					}
				},
				transition = new Transition
				{
					id = transitionId
				}
			};
		}
	}
}
