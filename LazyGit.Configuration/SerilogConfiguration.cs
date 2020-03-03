using System;
using System.IO;
using LazyGit.Common.Pocos;
using LazyGit.Configuration.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace LazyGit.Configuration
{
	public class SerilogConfiguration : ISerilogConfiguration
	{
		private readonly string _nodeUri;

		public SerilogConfiguration(IOptions<SerilogConfig> serilogConfig)
		{
			_nodeUri = serilogConfig.Value.NodeUri;
		}

		public Logger InitialiseLogger()
		{
			var config = new LoggerConfiguration();

			config.MinimumLevel.Debug();

			if (!string.IsNullOrEmpty(_nodeUri))
				ConfigureElasticSearchSink(config);

			ConfigureFileSink(config);

			config.WriteTo.Console();

			return config.CreateLogger();
		}

		/// <summary>
		/// Configure the elastic search logs
		/// </summary>
		/// <param name="config"></param>
		private void ConfigureElasticSearchSink(LoggerConfiguration config)
		{
			const string appName = "lazygit";

			var indexFormat = $"{appName}-{{0:yyyy-MM}}";

			var sinkOptions = new ElasticsearchSinkOptions(new Uri(_nodeUri))
			{
				MinimumLogEventLevel = LogEventLevel.Debug,
				IndexFormat = indexFormat
			};

			config.WriteTo.Elasticsearch(sinkOptions);
		}

		/// <summary>
		/// Configure the file logs
		/// </summary>
		/// <param name="config"></param>
		private static void ConfigureFileSink(LoggerConfiguration config)
		{
			var baseDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\"));

			var logStorageFolder = Path.Combine(baseDirectory, "logs");

			config
				.WriteTo
				.File(Path.Combine(logStorageFolder, "LazyGit_.txt"),
					fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
					retainedFileCountLimit: 10,
					rollOnFileSizeLimit: true,
					rollingInterval: RollingInterval.Day,
					shared: true);
		}
	}
}
