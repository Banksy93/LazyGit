using Serilog.Core;

namespace LazyGit.Configuration.Interfaces
{
	public interface ISerilogConfiguration
	{
		/// <summary>
		/// Configure logging for LazyGit
		/// </summary>
		/// <returns></returns>
		Logger InitialiseLogger();
	}
}
