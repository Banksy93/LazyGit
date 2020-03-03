namespace LazyGit.Common.Pocos
{
	public class BitbucketConfig
	{
		public string Username { get; set; }
		public string RepoSlug { get; set; }
		public string Secret { get; set; }
		public string ClientId { get; set; }
		public string PullRequestUrl { get; set; }
	}
}
