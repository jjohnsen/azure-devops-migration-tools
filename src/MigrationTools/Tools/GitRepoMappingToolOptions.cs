﻿using System;
using System.Collections.Generic;
using MigrationTools.Enrichers;
using MigrationTools.Options;
using MigrationTools.Tools.Infrastructure;

namespace MigrationTools.Tools
{
    public class GitRepoMappingToolOptions : ToolOptions
    {

        /// <summary>
        /// List of work item mappings. 
        /// </summary>
        /// <default>{}</default>
        public Dictionary<string, string> Mappings { get; set; }
    }

}