using SharpBucket.V2.EndPoints;

namespace LazyGit.Services.Interfaces
{
	public interface IBitbucketResource
	{
		/// <summary>
		/// Authenticate with the Bitbucket service and return the repository endpoint
		/// </summary>
		/// <param name="clientId"></param>
		/// <param name="secret"></param>
		/// <returns></returns>
		RepositoriesEndPoint Authenticate(string clientId, string secret);

		/// <summary>
		/// Get the pull request's resource using the repositories end point
		/// </summary>
		/// <param name="repo"></param>
		/// <returns></returns>
		PullRequestsResource GetPullRequestsResource(RepositoriesEndPoint repo);

		/// <summary>
		/// Get branch resource using the repositories end point
		/// </summary>
		/// <param name="repo"></param>
		/// <returns></returns>
		BranchResource GetBranchResource(RepositoriesEndPoint repo);

		/// <summary>
		/// Get repository resource using the repositories end point
		/// </summary>
		/// <param name="repo"></param>
		/// <returns></returns>
		RepositoryResource GetRepositoryResource(RepositoriesEndPoint repo);

		/// <summary>
		/// Get a pull request resource using the pull requestId
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="pullRequestId"></param>
		/// <returns></returns>
		PullRequestResource GetPullRequestResourceByPullRequestId(RepositoriesEndPoint repo, int pullRequestId);
	}
}
