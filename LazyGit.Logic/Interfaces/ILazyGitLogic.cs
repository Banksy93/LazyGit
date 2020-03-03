using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LazyGit.Common.Pocos;
using LazyGit.Services.Interfaces;

namespace LazyGit.Logic.Interfaces
{
	public interface ILazyGitLogic
	{
		/// <summary>
		/// Get jira tickets that have pull requests that are ready to be merged
		/// </summary>
		/// <returns></returns>
		Task<JiraIssueResponse> GetJiraTickets();

		/// <summary>
		/// Get pull requests from bitbucket using the Jira keys
		/// </summary>
		/// <param name="bitbucketService"></param>
		/// <param name="issueKeys"></param>
		/// <returns></returns>
		Task<IEnumerable<TicketInformation>> GetPullRequestInfoForJiraTickets(IEnumerable<string> issueKeys);

		/// <summary>
		/// Attempt to rebase a ticket's source branch over it's target branch
		/// </summary>
		/// <param name="ticket"></param>
		/// <returns></returns>
		RebaseResult RebaseTicket(TicketInformation ticket);

		/// <summary>
		/// Send an email report with an overview of merged tickets, failed to merge tickets and tickets which had any other issues during processing
		/// Also send out emails to user's who are required to manually rebase their branches due to merge conflicts
		/// </summary>
		/// <param name="tickets"></param>
		void ProcessEmails(IList<TicketInformation> tickets);

		/// <summary>
		/// Attempt to merge the pull request
		/// If the target branch has changed since we've been processing this ticket
		/// Re-fetch from the remote and then attempt to complete the merge
		/// </summary>
		/// <param name="pullRequestId"></param>
		/// <returns></returns>
		bool MergePullRequest(int pullRequestId);

		/// <summary>
		/// Once the rebase and merge has been successful
		/// Update the status and add a comment to the Jira ticket
		/// </summary>
		/// <param name="jiraKey"></param>
		/// <returns></returns>
		Task<bool> UpdateJiraTicket(string jiraKey);

		/// <summary>
		/// If the rebase and merge of the source branch has been successful, delete the branch
		/// </summary>
		/// <param name="sourceBranch"></param>
		/// <returns></returns>
		bool DeleteBranch(string sourceBranch);
	}
}
