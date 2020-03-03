namespace LazyGit.Common.Enums
{
	public enum RebaseStatus
	{
		Success = 1,
		Conflicts = 2,
		FailedToFindSourceBranch = 3,
		FailedToFindTargetBranch = 4,
		Failed = 5,
		FailedToFindPullRequest = 6
	}
}
