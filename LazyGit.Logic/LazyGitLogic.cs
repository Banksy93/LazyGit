using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyGit.Common.Enums;
using LazyGit.Common.Pocos;
using LazyGit.Logic.Interfaces;
using LazyGit.Services.Interfaces;
using Serilog;
using SharpBucket.V2.Pocos;

namespace LazyGit.Logic
{
	public class LazyGitLogic : ILazyGitLogic
	{
		private readonly IJiraService _jiraService;
		private readonly IBitbucketService _bitbucketService;
		private readonly IGitReader _gitReader;
		private readonly IGitManager _gitManager;
		private readonly IEmailService _emailService;

		public LazyGitLogic(IJiraService jiraService, IBitbucketService bitbucketService, IGitReader gitReader,
			IGitManager gitManager, IEmailService emailService)
		{
			_jiraService = jiraService;
			_bitbucketService = bitbucketService;
			_gitReader = gitReader;
			_gitManager = gitManager;
			_emailService = emailService;
		}

		public async Task<JiraIssueResponse> GetJiraTickets()
		{
			return await _jiraService.GetReadyToMergeJiraTicketsAsync();
		}

		public async Task<IEnumerable<TicketInformation>> GetPullRequestInfoForJiraTickets(IEnumerable<string> issueKeys)
		{
			return await _bitbucketService.GetPullRequestInfoForIssueKeys(issueKeys);
		}

		public RebaseResult RebaseTicket(TicketInformation ticket)
		{
			var rebaseResult = new RebaseResult();

			var pullRequest = GetPullRequestById(ticket.PullRequestId);

			if (pullRequest.id != null)
				return AttemptRebase(pullRequest.source.branch.name, pullRequest.destination.branch.name);

			Log.Debug("Pull request for ticket: {Key} could not be found.", ticket.JiraKey);

			rebaseResult.Status = RebaseStatus.FailedToFindPullRequest;
			return rebaseResult;
		}

		public bool MergePullRequest(int pullRequestId)
		{
			try
			{
				_bitbucketService.MergePullRequest(pullRequestId);

				Log.Debug("Successfully merged pull request with Id: {PullRequestId}", pullRequestId);

				return true;
			}
			catch (Exception ex)
			{
				const string fastForward = "Unable to fast forward";

				if (ex.Message.Contains(fastForward))
				{
					return ReAttemptMerge(pullRequestId);
				}

				Log.Error(ex.Message);

				return false;
			}
		}

		public async Task<bool> UpdateJiraTicket(string jiraKey)
		{
			return await _jiraService.UpdateJiraTicketAsync(jiraKey);
		}

		public bool DeleteBranch(string sourceBranch)
		{
			return _gitManager.DeleteBranch(sourceBranch);
		}

		public void ProcessEmails(IList<TicketInformation> tickets)
		{
			_emailService.SendEmailReport(tickets);

			var mergeConflicts = tickets.Where(t => t.RebaseStatus == RebaseStatus.Conflicts).ToList();
			if (mergeConflicts.Any())
			{
				mergeConflicts.GroupBy(m => m.Author.Name).ToList()
					.ForEach(_emailService.SendMergeConflictEmails);
			}
		}

		/// <summary>
		/// Get a pull request by Id
		/// </summary>
		/// <param name="pullRequestId"></param>
		/// <returns></returns>
		private PullRequest GetPullRequestById(int pullRequestId)
		{
			return _bitbucketService.GetPullRequestById(pullRequestId);
		}

		/// <summary>
		/// Attempt to rebase the source branch over the target branch
		/// If the rebase encountered merge conflicts, return a failed merge result
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		private RebaseResult AttemptRebase(string source, string target)
		{
			var rebaseResult = new RebaseResult();

			try
			{
				if (_gitReader.IsRepositoryDirty())
				{
					_gitManager.StashPendingChanges();
					Log.Debug("Successfully stashed pending changes.");
				}

				_gitReader.FetchLatest();

				var result = _gitManager.RebaseBranch(source, target);
				if (!result.RebaseSuccess)
				{
					Log.Warning("Rebase unsuccessful for branch: {Branch}. Status: {Status}.", source, result.Status);

					rebaseResult.Status = result.Status;
					return rebaseResult;
				}

				_gitManager.PushBranch(source);

				Log.Debug("Successfully rebased and pushed branch: {SourceBranch}.", source);

				rebaseResult.Status = result.Status;
				rebaseResult.SourceBranch = source;

				return rebaseResult;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);

				rebaseResult.Status = RebaseStatus.Failed;
				return rebaseResult;
			}
		}

		/// <summary>
		/// If we get here, rebase has been successful but we need to fetch from remote and attempt the merge again
		/// as the target branch has changed since we began processing the ticket
		/// </summary>
		/// <param name="pullRequestId"></param>
		/// <returns></returns>
		private bool ReAttemptMerge(int pullRequestId)
		{
			try
			{
				Log.Debug("Re-attempting fetch and merge for pull request: {PullRequestId} as there have been changes in the destination branch.",
					pullRequestId);

				_gitReader.FetchLatest();

				_bitbucketService.MergePullRequest(pullRequestId);

				Log.Debug("Successfully merged pull request: {PullRequestId}", pullRequestId);

				return true;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				return false;
			}
		}
	}
}
