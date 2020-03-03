using System;
using System.Linq;
using LazyGit.Common.Pocos;
using LazyGit.Logic.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Options;
using Serilog;
using Branch = LibGit2Sharp.Branch;

namespace LazyGit.Logic
{
	public class GitManager : IGitManager
	{
		private readonly string _repoPath;
		private readonly string _username;
		private readonly string _password;
		private readonly string _email;

		private const string _origin = "origin";
		private const string _refsHead = "refs/heads/";

		public GitManager(IOptions<Git> gitConfig)
		{
			_repoPath = gitConfig.Value.RepositoryPath;
			_username = gitConfig.Value.Username;
			_password = gitConfig.Value.Password;
			_email = gitConfig.Value.Email;
		}

		public void StashPendingChanges()
		{
			if (!AreConfigValuesSet())
				throw new Exception("Config values are not set for Git, please set them before re-running.");

			var identity = new Identity(_username, _email);

			using (var repository = new Repository(_repoPath))
			{
				repository.Stashes.Add(new Signature(identity, DateTimeOffset.Now));
			}
		}

		public Common.Pocos.RebaseResult RebaseBranch(string sourceBranch, string targetBranch)
		{
			var mergeResult = new Common.Pocos.RebaseResult();

			if (!AreConfigValuesSet())
			{
				mergeResult.Status = Common.Enums.RebaseStatus.Failed;
				return mergeResult;
			}

			const string refsRemotes = "refs/remotes/origin/";

			using (var repository = new Repository(_repoPath))
			{
				try
				{
					var source = CheckoutSourceBranch(repository, sourceBranch);
					if (source == null)
					{
						source = GetBranchFromRemote(repository, sourceBranch);
						if (source == null)
						{
							Log.Warning("Could not find source branch: {SourceBranch}.", sourceBranch);

							mergeResult.Status = Common.Enums.RebaseStatus.FailedToFindSourceBranch;
							return mergeResult;
						}
					}

					var target = repository.Branches[_origin + "/" + targetBranch];
					if (target == null)
					{
						target = repository.Branches[refsRemotes + targetBranch];
						if (target == null)
						{
							Log.Warning("Could not find target branch: {TargetBranch}", targetBranch);

							mergeResult.Status = Common.Enums.RebaseStatus.FailedToFindTargetBranch;
							return mergeResult;
						}
					}

					var rebaseResult = repository.Rebase.Start(source, target, target, GetIdentity(), null);

					switch (rebaseResult.Status)
					{
						case RebaseStatus.Complete when rebaseResult.CompletedStepCount == rebaseResult.TotalStepCount:
							mergeResult.Status = Common.Enums.RebaseStatus.Success;
							return mergeResult;
						case RebaseStatus.Conflicts:
							repository.Rebase.Abort();
							mergeResult.Status = Common.Enums.RebaseStatus.Conflicts;
							return mergeResult;
					}

					while (rebaseResult.CompletedStepCount < rebaseResult.TotalStepCount)
					{
						rebaseResult = repository.Rebase.Continue(GetIdentity(), null);
					}

					if (rebaseResult.Status == RebaseStatus.Complete)
					{
						mergeResult.Status = Common.Enums.RebaseStatus.Success;
						return mergeResult;
					}

					repository.Rebase.Abort();

					mergeResult.Status = Common.Enums.RebaseStatus.Failed;
					return mergeResult;
				}
				catch (Exception ex)
				{
					Log.Error(ex.Message);
					throw;
				}
			}
		}

		public void PushBranch(string sourceBranch)
		{
			try
			{
				using (var repository = new Repository(_repoPath))
				{
					var remote = repository.Network.Remotes[_origin];

					repository.Branches.Update(repository.Head,
						b => b.Remote = remote.Name,
						b => b.UpstreamBranch = repository.Head.CanonicalName);

					var pushOptions = GetPushOptions();

					var pushRefSpec = $"+{repository.Head.CanonicalName}";
					repository.Network.Push(remote, pushRefSpec, pushOptions);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				throw;
			}
		}

		public bool DeleteBranch(string sourceBranch)
		{
			try
			{
				using (var repository = new Repository(_repoPath))
				{
					var remote = repository.Network.Remotes[_origin];
					var pushRefSpecs = "+:" + _refsHead + sourceBranch;

					repository.Network.Push(remote, pushRefSpecs, GetPushOptions());

					Log.Debug("Successfully deleted branch: {SourceBranch} after it has been merged.", sourceBranch);

					return true;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception thrown on attempt to delete source branch: {Branch}", sourceBranch);

				return false;
			}
		}

		/// <summary>
		/// Check config values are set, if not terminate the program
		/// </summary>
		/// <returns></returns>
		private bool AreConfigValuesSet()
		{
			return !string.IsNullOrWhiteSpace(_repoPath)
				&& !string.IsNullOrWhiteSpace(_username)
				&& !string.IsNullOrWhiteSpace(_password)
				&& !string.IsNullOrWhiteSpace(_email);
		}

		/// <summary>
		/// Get push options for remote
		/// </summary>
		/// <returns></returns>
		private PushOptions GetPushOptions()
		{
			return new PushOptions
			{
				CredentialsProvider = (url, fromUrl, types) => GetCredentials()
			};
		}

		/// <summary>
		/// Try and checkout the source branch, return null if it failed
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="sourceBranch"></param>
		/// <returns></returns>
		private static Branch CheckoutSourceBranch(IRepository repository, string sourceBranch)
		{
			try
			{
				var branch = repository.Branches.First(b => b.FriendlyName == sourceBranch);

				var localBranch = Commands.Checkout(repository, branch);

				repository.Branches.Update(localBranch, b => b.UpstreamBranch = branch.CanonicalName, b => b.Remote = _origin);

				return localBranch;
			}
			catch (Exception ex)
			{
				Log.Error(ex.Message);
				return null;
			}
		}

		/// <summary>
		/// Checkout local branch failed so try and checkout from remote
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="sourceBranch"></param>
		/// <returns></returns>
		private static Branch GetBranchFromRemote(IRepository repository, string sourceBranch)
		{
			var branch = repository.Branches[_origin + "/" + sourceBranch];

			if (branch == null)
				return null;

			if (!branch.IsRemote)
				return branch;

			var localBranch = repository.CreateBranch(sourceBranch, branch.Tip);
			repository.Branches.Update(localBranch, b => b.UpstreamBranch = _refsHead + sourceBranch, b => b.Remote = _origin);

			return Commands.Checkout(repository, sourceBranch);
		}

		/// <summary>
		/// Get the identity of the bitbucket user
		/// </summary>
		/// <returns></returns>
		private Identity GetIdentity()
		{
			return new Identity(_username, _email);
		}

		/// <summary>
		/// Get credentials of the bitbucket user
		/// </summary>
		/// <returns></returns>
		private UsernamePasswordCredentials GetCredentials()
		{
			return new UsernamePasswordCredentials
			{
				Username = _username,
				Password = _password
			};
		}
	}
}
