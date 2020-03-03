using LazyGit.Common.Enums;

namespace LazyGit.Common.Pocos
{
	public class TicketInformation
	{
		public string JiraKey { get; set; }
		public string JiraUrl { get; set; }
		public int PullRequestId { get; set; }
		public string PullRequestUrl { get; set; }
		public string Summary { get; set; }
		public RebaseStatus RebaseStatus { get; set; }
		public PullRequestAuthor Author { get; set; }
	}

	public class PullRequestAuthor
	{
		public string Name { get; set; }
		public string Email { get; set; }
	}
}
