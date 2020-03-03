namespace LazyGit.Common.Pocos
{
	public class UpdatePullRequest
	{
		public string title { get; set; }
		public Destination destination { get; set; }
		public Branch branch { get; set; }
	}

	public class Destination
	{
		public Commit commit { get; set; }
		public Branch branch { get; set; }
	}

	public class Commit
	{
		public string hash { get; set; }
	}

	public class Branch
	{
		public string name { get; set; }
	}
}
