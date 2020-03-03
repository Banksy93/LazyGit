using System.IO;
using LazyGit.Common.Pocos;
using LazyGit.Configuration.Interfaces;
using LazyGit.Logic;
using LazyGit.Logic.Interfaces;
using LazyGit.Services;
using LazyGit.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LazyGit.Configuration
{
	public class ServiceConfiguration : IServiceConfiguration
	{
		public IServiceCollection ConfigureServiceCollection()
		{
			return new ServiceCollection()
				.AddSingleton<IGitReader, GitReader>()
				.AddSingleton<IGitManager, GitManager>()
				.AddSingleton<IJiraService, JiraService>()
				.AddSingleton<IBitbucketService, BitbucketService>()
				.AddSingleton<IEmailService, EmailService>()
				.AddSingleton<ISerilogConfiguration, SerilogConfiguration>()
				.AddSingleton<IBitbucketResource, BitbucketResource>()
				.AddSingleton<ILazyGitLogic, LazyGitLogic>()
				.AddSingleton<Startup>()
				.AddLogging()
				.AddOptions();
		}

		public IConfigurationBuilder GetConfigurationBuilder()
		{
			return new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json");
		}

		public void ConfigureServiceOptions(IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<JiraConfig>(configuration.GetSection("Jira"));
			services.Configure<BitbucketConfig>(configuration.GetSection("Bitbucket"));
			services.Configure<Git>(configuration.GetSection("Git"));
			services.Configure<EmailConfig>(configuration.GetSection("Email"));
			services.Configure<SerilogConfig>(configuration.GetSection("Serilog"));
		}
	}
}
