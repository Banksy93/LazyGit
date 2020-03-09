using System.Collections.Generic;

namespace LazyGit.Common.Pocos
{
	public class JiraTransitionRequest
	{
		public Update update { get; set; }
		public Transition transition { get; set; }
	}

	public class Update
	{
		public IEnumerable<Comment> comment { get; set; }
	}

	public class Comment
	{
		public CommentContent add { get; set; }
	}

	public class CommentContent
	{
		public string body { get; set; }
	}

	public class Transition
	{
		public string id { get; set; }
	}
}
