namespace LazyGit.Logic.Interfaces
{
	public interface IGitReader
	{
		/// <summary>
		/// If there are pending changes in the local repository we need to stash them first
		/// </summary>
		/// <returns></returns>
		bool IsRepositoryDirty();

		/// <summary>
		/// Fetch the latest code from the repository
		/// </summary>
		void FetchLatest();
	}
}
