using System;
using System.Linq;
using LazyGit.Common.Pocos;
using LazyGit.Logic.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Options;
using Serilog;

namespace LazyGit.Logic
{
	public class GitReader : IGitReader
	{
		private readonly string _repoPath;
		private readonly string _username;
		private readonly string _password;

		private const string _origin = "origin";

		public GitReader(IOptions<Git> gitConfig)
		{
			_repoPath = gitConfig.Value.RepositoryPath;
			_username = gitConfig.Value.Username;
			_password = gitConfig.Value.Password;
		}

		public bool IsRepositoryDirty()
		{
			if (!AreConfigValuesSet())
				throw new Exception("Config values must be configured to check for pending changes, please configure them.");

			using (var repository = new Repository(_repoPath))
			{
				return repository.RetrieveStatus().IsDirty;
			}
		}

		public void FetchLatest()
		{
			if (!AreConfigValuesSet())
				throw new Exception("Config values must be configured to fetch from remote, please configure them.");

			const string logMessage = "LazyGit fetching.";

			try
			{
				using (var repository = new Repository(_repoPath))
				{
					var fetchOptions = new FetchOptions
					{
						CredentialsProvider = (url, UsernamePasswordCredentials, types) =>
							new UsernamePasswordCredentials
							{
								Username = _username,
								Password = _password
							}
					};

					var remote = repository.Network.Remotes[_origin];
					var refSpecs = remote.FetchRefSpecs.Select(rs => rs.Specification);

					Commands.Fetch(repository, remote.Name, refSpecs, fetchOptions, logMessage);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception thrown when attempting to fetch latest from remote.");
				throw;
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
				&& !string.IsNullOrWhiteSpace(_password);
		}
	}
}
