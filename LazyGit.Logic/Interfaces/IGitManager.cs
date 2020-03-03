using LazyGit.Common.Pocos;

namespace LazyGit.Logic.Interfaces
{
	public interface IGitManager
	{
		/// <summary>
		/// Stash any pending changes before attempting a rebase
		/// </summary>
		void StashPendingChanges();

		/// <summary>
		/// Attempt to rebase the source branch onto the target branch
		/// </summary>
		/// <param name="sourceBranch"></param>
		/// <param name="targetBranch"></param>
		/// <returns></returns>
		RebaseResult RebaseBranch(string sourceBranch, string targetBranch);

		/// <summary>
		/// Push the rebased branch up to the remote ready for merging
		/// </summary>
		/// <param name="sourceBranch"></param>
		void PushBranch(string sourceBranch);

		/// <summary>
		/// If a merge has been successful, delete the source branch as it is no longer needed
		/// </summary>
		/// <param name="sourceBranch"></param>
		bool DeleteBranch(string sourceBranch);
	}
}
