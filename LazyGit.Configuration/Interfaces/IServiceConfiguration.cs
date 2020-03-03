using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LazyGit.Configuration.Interfaces
{
	public interface IServiceConfiguration
	{
		/// <summary>
		/// Configure the service collection for the application
		/// </summary>
		/// <returns></returns>
		IServiceCollection ConfigureServiceCollection();

		/// <summary>
		/// Configure the configuration builder
		/// </summary>
		/// <returns></returns>
		IConfigurationBuilder GetConfigurationBuilder();

		/// <summary>
		/// Configure options for LazyGit
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configuration"></param>
		void ConfigureServiceOptions(IServiceCollection services, IConfiguration configuration);
	}
}
