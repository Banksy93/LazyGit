using LazyGit.Common.Enums;

namespace LazyGit.Common.Pocos
{
	public class RebaseResult
	{
		public string SourceBranch { get; set; }
		public RebaseStatus Status { get; set; }
		public bool RebaseSuccess => (int)Status == 1;
	}
}
