using System.Threading.Tasks;
using LazyGit.Configuration;
using LazyGit.Configuration.Interfaces;
using LazyGit.Logic;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LazyGit.Core
{
	public class Program
	{
		private static readonly IServiceConfiguration _serviceConfiguration = new ServiceConfiguration();

		public static async Task Main(string[] args)
		{
			var serviceProvider = ConfigureServiceProvider();

			Log.Logger = InitialiseLogger(serviceProvider.GetService<ISerilogConfiguration>());

			Log.Debug("Application started.");

			var startup = serviceProvider.GetService<Startup>();

			await startup.Run();

			Log.Debug("Application finished.");

			// Dispose of services
			await serviceProvider.DisposeAsync();
			Log.CloseAndFlush();
		}

		/// <summary>
		/// Configure the service provider for LazyGit
		/// </summary>
		/// <returns></returns>
		private static ServiceProvider ConfigureServiceProvider()
		{
			var services = _serviceConfiguration.ConfigureServiceCollection();
			var config = _serviceConfiguration.GetConfigurationBuilder().Build();

			_serviceConfiguration.ConfigureServiceOptions(services, config);

			return services.BuildServiceProvider();
		}

		/// <summary>
		/// Initialise the logger
		/// </summary>
		/// <param name="serilogConfig"></param>
		/// <returns></returns>
		private static ILogger InitialiseLogger(ISerilogConfiguration serilogConfig)
		{
			return serilogConfig.InitialiseLogger();
		}
	}
}
