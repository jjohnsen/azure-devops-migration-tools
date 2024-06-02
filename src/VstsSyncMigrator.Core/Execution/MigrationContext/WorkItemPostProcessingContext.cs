﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using MigrationTools;
using MigrationTools._EngineV1.Clients;
using MigrationTools._EngineV1.Configuration;
using MigrationTools._EngineV1.Configuration.Processing;
using MigrationTools._EngineV1.DataContracts;
using MigrationTools._EngineV1.Processors;
using MigrationTools.DataContracts;
using MigrationTools.Enrichers;

namespace VstsSyncMigrator.Engine
{
    /// <summary>
    /// Reapply field mappings after a migration. Does not migtate Work Items, only reapplied changes to filed mappings.
    /// </summary>
    /// <status>preview</status>
    /// <processingtarget>Work Items</processingtarget>
    public class WorkItemPostProcessingContext : MigrationProcessorBase
    {
        private WorkItemPostProcessingConfig _config;
        private TfsWorkItemEmbededLinkEnricher _workItemEmbeddedLinkEnricher;
        private TfsEmbededImagesEnricher _workItemEmbededImagesEnricher;

        public WorkItemPostProcessingContext(
            IMigrationEngine engine,
            IServiceProvider services,
            ITelemetryLogger telemetry,
            TfsWorkItemEmbededLinkEnricher workItemEmbeddedLinkEnricher,
            TfsEmbededImagesEnricher embededImagesEnricher,
            ILogger<WorkItemPostProcessingContext> logger)
            : base(engine, services, telemetry, logger)
        {
            _workItemEmbeddedLinkEnricher = workItemEmbeddedLinkEnricher;
            _workItemEmbededImagesEnricher = embededImagesEnricher;
        }

        public override string Name
        {
            get
            {
                return "WorkItemPostProcessingContext";
            }
        }

        public override void Configure(IProcessorConfig config)
        {
            _config = (WorkItemPostProcessingConfig)config;
        }

        protected override void InternalExecute()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            //////////////////////////////////////////////////
            var wiqbFactory = Services.GetRequiredService<IWorkItemQueryBuilderFactory>();
            var wiqb = wiqbFactory.Create();
            //Builds the constraint part of the query
            wiqb.Query = _config.WIQLQuery;

            List<WorkItemData> sourceWIS = Engine.Source.WorkItems.GetWorkItems(wiqb);
            Log.LogInformation("Migrate {0} work items?", sourceWIS.Count);
            //////////////////////////////////////////////////
            ProjectData destProject = Engine.Target.WorkItems.GetProject();
            Log.LogInformation("Found target project as {0}", destProject.Name);

            int current = sourceWIS.Count;
            int count = 0;
            long elapsedms = 0;
            foreach (WorkItemData sourceWI in sourceWIS)
            {
                Stopwatch witstopwatch = Stopwatch.StartNew();
                WorkItemData targetFound;
                targetFound = Engine.Target.WorkItems.FindReflectedWorkItem(sourceWI, false);
                Log.LogInformation("{0} - Updating: {1}-{2}", current, sourceWI.Id, sourceWI.Type);
                if (targetFound == null)
                {
                    Log.LogWarning("{0} - WARNING: does not exist {1}-{2}", current, sourceWI.Id, sourceWI.Type);
                }
                else
                {
                    Log.LogInformation("...Exists");
                    TfsExtensions.ToWorkItem(targetFound).Open();
                    Engine.FieldMaps.ApplyFieldMappings(sourceWI, targetFound);
                    _workItemEmbeddedLinkEnricher.Enrich(null, targetFound);
                    _workItemEmbededImagesEnricher.Enrich(sourceWI, targetFound);
                    if (TfsExtensions.ToWorkItem(targetFound).IsDirty)
                    {
                        try
                        {
                            targetFound.ToWorkItem().Fields["System.ChangedBy"].Value = "Migration";
                            TfsExtensions.SaveToAzureDevOps(targetFound);
                            Log.LogInformation("          Updated");
                        }
                        catch (ValidationException ve)
                        {
                            Log.LogError(ve, "          [FAILED] {0}", ve.ToString());
                        }
                    }
                    else
                    {
                        Log.LogInformation("          No changes");
                    }
                    TfsExtensions.ToWorkItem(sourceWI).Close();
                }
                witstopwatch.Stop();
                elapsedms = elapsedms + witstopwatch.ElapsedMilliseconds;
                current--;
                count++;
                TimeSpan average = new TimeSpan(0, 0, 0, 0, (int)(elapsedms / count));
                TimeSpan remaining = new TimeSpan(0, 0, 0, 0, (int)(average.TotalMilliseconds * current));
                Log.LogInformation("Average time of {0} per work item and {1} estimated to completion", string.Format(@"{0:s\:fff} seconds", average), string.Format(@"{0:%h} hours {0:%m} minutes {0:s\:fff} seconds", remaining));
            }
            //////////////////////////////////////////////////
            stopwatch.Stop();
            Log.LogInformation("DONE in {Elapsed}", stopwatch.Elapsed.ToString("c"));
        }

    }
}