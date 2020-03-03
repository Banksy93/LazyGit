using System.Collections.Generic;
using System.Threading.Tasks;
using LazyGit.Common.Pocos;
using SharpBucket.V2.Pocos;

namespace LazyGit.Services.Interfaces
{
	public interface IBitbucketService
	{
		/// <summary>
		/// Get the pull request ID associated with issue keys
		/// </summary>
		/// <param name="issueKeys"></param>
		/// <returns></returns>
		Task<IEnumerable<TicketInformation>> GetPullRequestInfoForIssueKeys(IEnumerable<string> issueKeys);

		/// <summary>
		/// Get a pull request by Id
		/// </summary>
		/// <param name="pullRequestId"></param>
		/// <returns></returns>
		PullRequest GetPullRequestById(int pullRequestId);

		/// <summary>
		/// Attempt to merge a pull request by it's Id
		/// </summary>
		/// <param name="pullRequestId"></param>
		void MergePullRequest(int pullRequestId);
	}
}
