using System.Linq;
using System.Threading.Tasks;
using LazyGit.Common.Pocos;
using LazyGit.Logic.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;

namespace LazyGit.Logic
{
	public class Startup
	{
		private readonly ILazyGitLogic _lazyGitLogic;

		private readonly string _baseTicketUrl;

		public Startup(ILazyGitLogic lazyGitLogic, IOptions<JiraConfig> config)
		{
			_lazyGitLogic = lazyGitLogic;
			_baseTicketUrl = config.Value.TicketUrl;
		}

		public async Task Run()
		{
			var jiraResponse = await _lazyGitLogic.GetJiraTickets();
			if (!jiraResponse.Issues.Any())
			{
				Log.Debug("No Jira tickets to process, terminating application.");
				return;
			}

			var ticketList = (await _lazyGitLogic.GetPullRequestInfoForJiraTickets(jiraResponse.Issues.Select(i => i.Key)))
				.ToList();

			if (!ticketList.Any())
			{
				Log.Debug("No pull requests found for Jira tickets, terminating application.");
				return;
			}

			foreach (var ticket in ticketList.Where(t => t.PullRequestId != 0))
			{
				ticket.Summary = jiraResponse.Issues
					.First(jt => jt.Key == ticket.JiraKey).Fields.Summary
					 ?? string.Empty;

				var rebaseResult = _lazyGitLogic.RebaseTicket(ticket);

				ticket.RebaseStatus = rebaseResult.Status;
				ticket.JiraKey = _baseTicketUrl + ticket.JiraKey;

				if (!rebaseResult.RebaseSuccess)
					continue;

				// Don't delete the branch or update the Jira ticket if the merge failed
				if (!_lazyGitLogic.MergePullRequest(ticket.PullRequestId))
					continue;

				_lazyGitLogic.DeleteBranch(rebaseResult.SourceBranch);

				var updateSuccess = await _lazyGitLogic.UpdateJiraTicket(ticket.JiraKey);

				if (!updateSuccess)
					Log.Warning("Failed to update Jira ticket with key: {JiraKey}", ticket.JiraKey);
			}

			_lazyGitLogic.ProcessEmails(ticketList);
		}
	}
}
