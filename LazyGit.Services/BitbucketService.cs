using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyGit.Common.Enums;
using LazyGit.Common.Pocos;
using LazyGit.Services.Interfaces;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using Serilog;
using SharpBucket.V2.EndPoints;
using SharpBucket.V2.Pocos;

namespace LazyGit.Services
{
	public class BitbucketService : IBitbucketService
	{
		private readonly IBitbucketResource _bitbucketResource;

		#region App Settings

		// Bitbucket
		private readonly string _username;
		private readonly string _repoSlug;
		private readonly string _clientId;
		private readonly string _secret;
		private readonly string _pullRequestUrl;
		private readonly bool _checkTargetBranch;

		// Jira
		private readonly string _fixVersion;
		private readonly string _baseJiraUrl;

		// Git
		private readonly string _gitUser;
		private readonly string _gitPassword;

		// Email
		private readonly string _emailDomain;

		#endregion

		#region Consts

		private const string _open = "OPEN";
		private const string _release = "Release";
		private const string _baseUrl = "https://api.bitbucket.org/2.0/";

		#endregion

		public BitbucketService(IOptions<BitbucketConfig> bitbucketConfig, IOptions<JiraConfig> jiraConfig, IOptions<Git> gitConfig,
			IOptions<EmailConfig> emailConfig, IBitbucketResource bitbucketResource)
		{
			_bitbucketResource = bitbucketResource;

			_username = bitbucketConfig.Value.Username;
			_repoSlug = bitbucketConfig.Value.RepoSlug;
			_clientId = bitbucketConfig.Value.ClientId;
			_secret = bitbucketConfig.Value.Secret;
			_pullRequestUrl = bitbucketConfig.Value.PullRequestUrl;
			_checkTargetBranch = bitbucketConfig.Value.CheckTargetBranch;

			_fixVersion = jiraConfig.Value.FixVersion;
			_baseJiraUrl = jiraConfig.Value.TicketUrl;

			_gitUser = gitConfig.Value.Username;
			_gitPassword = gitConfig.Value.Password;

			_emailDomain = emailConfig.Value.EmailDomain;
		}

		public async Task<IEnumerable<TicketInformation>> GetPullRequestInfoForIssueKeys(IEnumerable<string> issueKeys)
		{
			if (!AreConfigValuesSet())
			{
				Log.Warning("Some or all of the application settings aren't configure, please configure them.");
				return new List<TicketInformation>();
			}

			var ticketList = new List<TicketInformation>();
			var branchesNotFound = new List<string>();

			try
			{
				var repository = _bitbucketResource.Authenticate(_clientId, _secret);

				var branchResource = _bitbucketResource.GetBranchResource(repository);
				var branches = branchResource.ListBranches();

				issueKeys = issueKeys.ToList();

				foreach (var issueKey in issueKeys)
				{
					var issueBranch = GetBranchByIssueKey(issueKey, branches);
					if (issueBranch == null)
					{
						branchesNotFound.Add(issueKey);
						ticketList.Add(new TicketInformation
						{
							JiraKey = issueKey,
							RebaseStatus = RebaseStatus.FailedToFindSourceBranch,
							JiraUrl = _baseJiraUrl + issueKey
						});

						continue;
					}

					var listParams = GetPullRequestListParameters(issueBranch.name);

					var prResource = _bitbucketResource.GetPullRequestsResource(repository);
					var branchPullRequest = prResource.ListPullRequests(listParams);

					if (!branchPullRequest.Any())
						continue;

					if (branchPullRequest.Count > 1)
					{
						var openPullRequest = branchPullRequest.Where(b => b.state == _open).ToList().Count;
						if (openPullRequest == 1)
							branchPullRequest = branchPullRequest.Where(b => b.state == _open).ToList();
					}

					var pullRequestDetails = branchPullRequest.First();
					if (pullRequestDetails.state != _open)
						continue;

					var pullRequestId = pullRequestDetails.id ?? 0;

					// If the target branch for the PR doesn't match the fix version, attempt to re-target it
					var targetBranch = pullRequestDetails.destination.branch.name;
					if (_checkTargetBranch && !targetBranch.Contains(_fixVersion))
					{
						bool updateSuccess;

						if (targetBranch.Contains(_release) && pullRequestDetails.state == _open)
							updateSuccess = await UpdatePullRequestTarget(pullRequestId, pullRequestDetails, repository);
						else
							continue;

						if (!updateSuccess)
							continue;
					}

					ticketList.Add(new TicketInformation
					{
						PullRequestId = pullRequestId,
						JiraKey = issueKey,
						Author = GetPullRequestAuthor(pullRequestDetails.author.display_name),
						PullRequestUrl = _pullRequestUrl + pullRequestId
					});
				}

				if (branchesNotFound.Any())
					Log.Debug("Branches not found for the following Jira tickets: {Keys}", branchesNotFound);

				return ticketList;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				throw;
			}
		}

		public PullRequest GetPullRequestById(int pullRequestId)
		{
			var listParams = new ListParameters
			{
				Filter = $"id={pullRequestId}"
			};

			try
			{
				var respository = _bitbucketResource.Authenticate(_clientId, _secret);
				var prResource = _bitbucketResource.GetPullRequestsResource(respository);

				var pullRequest = prResource.ListPullRequests(listParams);

				return pullRequest.First();
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				return null;
			}
		}

		public void MergePullRequest(int pullRequestId)
		{
			try
			{
				var repository = _bitbucketResource.Authenticate(_clientId, _secret);

				var pullRequestResource = _bitbucketResource.GetPullRequestResourceByPullRequestId(repository, pullRequestId);
				if (pullRequestResource == null)
					return;

				var merge = pullRequestResource.AcceptAndMergePullRequest();
				merge.close_source_branch = true;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				throw;
			}
		}

		/// <summary>
		/// If no config values are set, terminate the program
		/// </summary>
		/// <returns></returns>
		private bool AreConfigValuesSet()
		{
			return !string.IsNullOrWhiteSpace(_username)
				&& !string.IsNullOrWhiteSpace(_repoSlug)
				&& !string.IsNullOrWhiteSpace(_clientId)
				&& !string.IsNullOrWhiteSpace(_secret)
				&& !string.IsNullOrWhiteSpace(_pullRequestUrl)
				&& !string.IsNullOrWhiteSpace(_fixVersion)
				&& !string.IsNullOrWhiteSpace(_gitUser)
				&& !string.IsNullOrWhiteSpace(_gitPassword)
				&& !string.IsNullOrWhiteSpace(_emailDomain);
		}

		/// <summary>
		/// Build the author object for a pull request
		/// </summary>
		/// <param name="authorName"></param>
		/// <returns></returns>
		private PullRequestAuthor GetPullRequestAuthor(string authorName)
		{
			var emailName = authorName.Replace(' ', '.');

			return new PullRequestAuthor
			{
				Name = authorName,
				Email = emailName + _emailDomain
			};
		}

		/// <summary>
		/// Try and get a branch using an issue key
		/// </summary>
		/// <param name="issueKey"></param>
		/// <param name="branches"></param>
		/// <returns></returns>
		private static SharpBucket.V2.Pocos.Branch GetBranchByIssueKey(string issueKey, IEnumerable<SharpBucket.V2.Pocos.Branch> branches)
		{
			return branches.FirstOrDefault(b => b.name.Contains(issueKey));
		}

		/// <summary>
		/// List parameters to filter when searching for a pull request
		/// </summary>
		/// <param name="branch"></param>
		/// <returns></returns>
		private static ListParameters GetPullRequestListParameters(string branch)
		{
			return new ListParameters
			{
				Filter = $"source.branch.name = \"{branch}\""
			};
		}

		/// <summary>
		/// Attempt to update the target branch of the pull request
		/// </summary>
		/// <param name="pullRequestId"></param>
		/// <param name="pullRequest"></param>
		/// <param name="repo"></param>
		/// <returns></returns>
		private async Task<bool> UpdatePullRequestTarget(int pullRequestId, PullRequest pullRequest, RepositoriesEndPoint repo)
		{
			var releaseBranch = $"Releases/{_fixVersion}";

			var client = new RestClient(_baseUrl)
			{
				Authenticator = new HttpBasicAuthenticator(_gitUser, _gitPassword)
			};

			var request = new RestRequest($"repositories/{_username}/{_repoSlug}/pullrequests/{pullRequestId}", Method.PUT);

			var repositoryResource = _bitbucketResource.GetRepositoryResource(repo);
			var latestCommit = repositoryResource.ListCommits(releaseBranch, 1);

			var updateRequest = ConfigureUpdatePullRequest(pullRequest.title, latestCommit.First().hash, releaseBranch,
				pullRequest.source.branch.name);

			request.AddHeader("Accept", "application/json");
			request.AddJsonBody(updateRequest);

			var response = await client.ExecuteTaskAsync(request);

			return response.IsSuccessful;
		}

		/// <summary>
		/// Build the model to update a pull request
		/// </summary>
		/// <param name="title"></param>
		/// <param name="hash"></param>
		/// <param name="releaseBranch"></param>
		/// <param name="sourceBranch"></param>
		/// <returns></returns>
		private static UpdatePullRequest ConfigureUpdatePullRequest(string title, string hash, string releaseBranch, string sourceBranch)
		{
			return new UpdatePullRequest
			{
				title = title,
				destination = new Destination
				{
					commit = new Common.Pocos.Commit
					{
						hash = hash
					},
					branch = new Common.Pocos.Branch
					{
						name = releaseBranch
					}
				},
				branch = new Common.Pocos.Branch
				{
					name = sourceBranch
				}
			};
		}
	}
}
