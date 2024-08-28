﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using MigrationTools._EngineV1.Configuration;
using MigrationTools._EngineV1.Containers;
using MigrationTools.Options;
using MigrationTools.Processors;
using MigrationTools.Processors.Infrastructure;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MigrationTools.Host.Commands
{
    internal class UpgradeConfigCommand : AsyncCommand<UpgradeConfigCommandSettings>
    {
        private IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly ITelemetryLogger Telemetery;
        private readonly IHostApplicationLifetime _appLifetime;

        private static Dictionary<string, string> classNameMappings = new Dictionary<string, string>();

        public UpgradeConfigCommand(
            IServiceProvider services,
            ILogger<InitMigrationCommand> logger,
            ITelemetryLogger telemetryLogger,
            IHostApplicationLifetime appLifetime)
        {
            _services = services;
            _logger = logger;
            Telemetery = telemetryLogger;
            _appLifetime = appLifetime;
        }


      


        public override async Task<int> ExecuteAsync(CommandContext context, UpgradeConfigCommandSettings settings)
        {
            int _exitCode;

            try
            {
                Telemetery.TrackEvent(new EventTelemetry("UpgradeConfigCommand"));
                string configFile = settings.ConfigFile;
                if (string.IsNullOrEmpty(configFile))
                {
                    configFile = "configuration.json";
                }
                _logger.LogInformation("ConfigFile: {configFile}", configFile);

                // Load configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile(configFile, optional: true, reloadOnChange: true)
                    .Build();

                classNameMappings.Add("WorkItemMigrationContext", "TfsWorkItemMigrationProcessor");
                classNameMappings.Add("TfsTeamProjectConfig", "TfsTeamProjectEndpoint");

                switch (VersionOptions.ConfigureOptions.GetMigrationConfigVersion(configuration).schema)
                {
                    case MigrationConfigSchema.v1:

                        // Find all options
                        List<IOptions> options = new List<IOptions>();

                        var AllOptionsObjects = AppDomain.CurrentDomain.GetMigrationToolsTypes().WithInterface<IOptions>();

                        // ChangeSetMappingFile
                       
                        options.Add(GetTfsChangeSetMappingToolOptions(configuration));

                        var sourceConfig = configuration.GetSection("Source");
                        var sourceType = sourceConfig.GetValue<string>("$type");
                        if (classNameMappings.ContainsKey(sourceType))
                        {
                            sourceType = classNameMappings[sourceType];
                        }
                        var type = AppDomain.CurrentDomain.GetMigrationToolsTypes().WithInterface<IOptions>().FirstOrDefault(t => t.Name == sourceType);
                        var sourceOptions = (IOptions)Activator.CreateInstance(type);
                        sourceConfig.Bind(sourceOptions);





                        //field mapping
                        //options.Enabled = true;
                        //options.FieldMaps = _configuration.GetSection("FieldMaps")?.ToMigrationToolsList(child => child.GetMigrationToolsOption<IFieldMapOptions>("$type"));


                        // Tools
                        //context.AddSingleton<IStringManipulatorTool, StringManipulatorTool>().AddSingleton<IOptions<StringManipulatorToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<StringManipulatorToolOptions>()));
                        //context.AddSingleton<IWorkItemTypeMappingTool, WorkItemTypeMappingTool>().AddSingleton<IOptions<WorkItemTypeMappingToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<WorkItemTypeMappingToolOptions>()));

                        //TFS Tools
                        //context.AddSingleton<GitRepoMappingTool>().AddSingleton<IOptions<GitRepoMappingToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<GitRepoMappingToolOptions>()));
                        //context.AddSingleton<TfsAttachmentTool>().AddSingleton<IOptions<TfsAttachmentToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsAttachmentToolOptions>()));

                        //context.AddSingleton<TfsUserMappingTool>().AddSingleton<IOptions<TfsUserMappingToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsUserMappingToolOptions>()));

                        //context.AddSingleton<TfsValidateRequiredFieldTool>().AddSingleton<IOptions<TfsValidateRequiredFieldToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsValidateRequiredFieldToolOptions>()));

                        //context.AddSingleton<TfsWorkItemLinkTool>().AddSingleton<IOptions<TfsWorkItemLinkToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsWorkItemLinkToolOptions>()));

                        //context.AddSingleton<TfsWorkItemEmbededLinkTool>().AddSingleton<IOptions<TfsWorkItemEmbededLinkToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsWorkItemEmbededLinkToolOptions>()));

                        //context.AddSingleton<TfsEmbededImagesTool>().AddSingleton<IOptions<TfsEmbededImagesToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsEmbededImagesToolOptions>()));

                        //context.AddSingleton<TfsGitRepositoryTool>().AddSingleton<IOptions<TfsGitRepositoryToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsGitRepositoryToolOptions>()));

                        //context.AddSingleton<TfsNodeStructureTool>().AddSingleton<IOptions<TfsNodeStructureToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsNodeStructureToolOptions>()));

                        //context.AddSingleton<TfsRevisionManagerTool>().AddSingleton<IOptions<TfsRevisionManagerToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsRevisionManagerToolOptions>()));

                        //context.AddSingleton<TfsTeamSettingsTool>().AddSingleton<IOptions<TfsTeamSettingsToolOptions>>(Microsoft.Extensions.Options.Options.Create(configuration.GetSectionCommonEnrichers_v15<TfsTeamSettingsToolOptions>()));

                        break;
                    case MigrationConfigSchema.v160:

                        break;
                }

                _exitCode = 0;
            }
            catch (Exception ex)
            {
                Telemetery.TrackException(ex, null, null);
                _logger.LogError(ex, "Unhandled exception!");
                _exitCode = 1;
            }
            finally
            {
                // Stop the application once the work is done
                _appLifetime.StopApplication();
            }
            return _exitCode;
        }

        //private static void AddConfiguredEndpointsV1(IServiceCollection services, IConfiguration configuration)
        //{
        //    var nodes = new List<string> { "Source", "Target" };
        //    foreach (var node in nodes)
        //    {
        //        var endpointsSection = configuration.GetSection(node);
        //        var endpointType = endpointsSection.GetValue<string>("$type").Replace("Options", "").Replace("Config", "");
        //        AddEndPointSingleton(services, configuration, endpointsSection, node, endpointType);
        //    }
        //}

        private IOptions GetTfsChangeSetMappingToolOptions(IConfiguration configuration)
        {
            var changeSetMappingOptions = configuration.GetValue<string>("ChangeSetMappingFile");
            var properties = new Dictionary<string, object>
                        {
                            { "ChangeSetMappingFile", changeSetMappingOptions }
                        };
            return (IOptions)OptionsBinder.BindToOptions("TfsChangeSetMappingToolOptions", properties, classNameMappings);
        }


    }
}
