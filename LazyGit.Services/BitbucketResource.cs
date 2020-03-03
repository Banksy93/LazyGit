using LazyGit.Common.Pocos;
using LazyGit.Services.Interfaces;
using Microsoft.Extensions.Options;
using SharpBucket.V2;
using SharpBucket.V2.EndPoints;

namespace LazyGit.Services
{
	public class BitbucketResource : IBitbucketResource
	{
		private readonly SharpBucketV2 _sharpBucketV2 = new SharpBucketV2();

		private readonly string _username;
		private readonly string _repoSlug;

		public BitbucketResource(IOptions<BitbucketConfig> config)
		{
			_username = config.Value.Username;
			_repoSlug = config.Value.RepoSlug;
		}

		public RepositoriesEndPoint Authenticate(string clientId, string secret)
		{
			_sharpBucketV2.OAuth2ClientCredentials(clientId, secret);

			return _sharpBucketV2.RepositoriesEndPoint();
		}

		public PullRequestsResource GetPullRequestsResource(RepositoriesEndPoint repo)
		{
			return repo.PullRequestsResource(_username, _repoSlug);
		}

		public BranchResource GetBranchResource(RepositoriesEndPoint repo)
		{
			return repo.BranchResource(_username, _repoSlug);
		}

		public RepositoryResource GetRepositoryResource(RepositoriesEndPoint repo)
		{
			return repo.RepositoryResource(_username, _repoSlug);
		}

		public PullRequestResource GetPullRequestResourceByPullRequestId(RepositoriesEndPoint repo, int pullRequestId)
		{
			var pullRequestsResource = GetPullRequestsResource(repo);
			return pullRequestsResource.PullRequestResource(pullRequestId);
		}
	}
}
