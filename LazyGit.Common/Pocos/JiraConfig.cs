namespace LazyGit.Common.Pocos
{
	public class JiraConfig
	{
		public string Url { get; set; }
		public string AuthorizationHeader { get; set; }
		public string FixVersion { get; set; }
		public string TicketStatus { get; set; }
		public string Project { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Comment { get; set; }
		public string TransitionId { get; set; }
		public string TicketUrl { get; set; }
	}
}
