using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.Tools.GitVersion;
using static Rocket.Surgery.Nuke.LoggingExtensions;

namespace Rocket.Surgery.Nuke.AzurePipelines
{
    public class AzurePipelinesTasks
    {
        /// <summary>
        /// Gets a value that determines if the build is running on Azure DevOps.
        /// </summary>
        public static Expression<Func<bool>> IsRunningOnAzurePipelines => () =>
            NukeBuild.Host == HostType.AzurePipelines || Environment.GetEnvironmentVariable("LOGNAME") == "vsts";

        Target PrintAzurePipelinesEnvironment => _ => _
            .OnlyWhenStatic(IsRunningOnAzurePipelines)
            .Executes(() =>
            {
                Information("AGENT_ID: {0}", EnvironmentVariable("AGENT_ID"));
                Information("AGENT_NAME: {0}", EnvironmentVariable("AGENT_NAME"));
                Information("AGENT_VERSION: {0}", EnvironmentVariable("AGENT_VERSION"));
                Information("AGENT_JOBNAME: {0}", EnvironmentVariable("AGENT_JOBNAME"));
                Information("AGENT_JOBSTATUS: {0}", EnvironmentVariable("AGENT_JOBSTATUS"));
                Information("AGENT_MACHINE_NAME: {0}", EnvironmentVariable("AGENT_MACHINE_NAME"));
                Information("\n");

                Information("BUILD_BUILDID: {0}", EnvironmentVariable("BUILD_BUILDID"));
                Information("BUILD_BUILDNUMBER: {0}", EnvironmentVariable("BUILD_BUILDNUMBER"));
                Information("BUILD_DEFINITIONNAME: {0}", EnvironmentVariable("BUILD_DEFINITIONNAME"));
                Information("BUILD_DEFINITIONVERSION: {0}", EnvironmentVariable("BUILD_DEFINITIONVERSION"));
                Information("BUILD_QUEUEDBY: {0}", EnvironmentVariable("BUILD_QUEUEDBY"));
                Information("\n");

                Information("BUILD_SOURCEBRANCHNAME: {0}", EnvironmentVariable("BUILD_SOURCEBRANCHNAME"));
                Information("BUILD_SOURCEVERSION: {0}", EnvironmentVariable("BUILD_SOURCEVERSION"));
                Information("BUILD_REPOSITORY_NAME: {0}", EnvironmentVariable("BUILD_REPOSITORY_NAME"));
                Information("BUILD_REPOSITORY_PROVIDER: {0}", EnvironmentVariable("BUILD_REPOSITORY_PROVIDER"));
            });

        Target UploadAzurePipelinesArtifacts => _ => _
            .Before(PublishAzurePipelinesTestResults)
            .OnlyWhenStatic(IsRunningOnAzurePipelines)
            .Executes(() => { });

        Target PublishAzurePipelinesTestResults => _ => _
            .Before(PublishAzurePipelinesCodeCoverage)
            .OnlyWhenStatic(IsRunningOnAzurePipelines)
            .Executes(() => { });

        Target PublishAzurePipelinesCodeCoverage => _ => _
            .OnlyWhenStatic(IsRunningOnAzurePipelines)
            .Executes(() => { });

        Target AzurePipelines => _ => _
            .DependsOn(PrintAzurePipelinesEnvironment)
            .DependsOn(UploadAzurePipelinesArtifacts)
            .DependsOn(PublishAzurePipelinesTestResults)
            .DependsOn(PublishAzurePipelinesCodeCoverage);
    }
}
