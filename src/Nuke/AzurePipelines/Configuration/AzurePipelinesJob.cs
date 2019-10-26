using System;
using System.Linq;
using Nuke.Common.Execution;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.CI.AzurePipelines;

namespace Rocket.Surgery.Nuke.AzurePipelines.Configuration
{
    public class AzurePipelinesJob : AzurePipelinesConfigurationEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public AzurePipelinesImage? Image { get; set; }
        public AzurePipelinesJob[] Dependencies { get; set; }
        public int Parallel { get; set; }
        public string PartitionName { get; set; }
        public string ScriptPath { get; set; }
        public string[] InvokedTargets { get; set; }
        public string[] DownloadArtifacts { get; set; }
        public string[] PublishArtifacts { get; set; }

        public override void Write(CustomFileWriter writer)
        {
            using (writer.WriteBlock($"- job: {Name}"))
            {
                writer.WriteLine($"displayName: {DisplayName.SingleQuote()}");
                writer.WriteLine($"dependsOn: [ {Dependencies.Select(x => x.Name).JoinComma()} ]");

                if (Image != null)
                {
                    using (writer.WriteBlock("pool:"))
                    {
                        writer.WriteLine($"vmImage: {Image.Value.GetValue().SingleQuote().SingleQuote()}");
                    }
                }

                if (Parallel > 1)
                {
                    using (writer.WriteBlock("strategy:"))
                    {
                        writer.WriteLine($"parallel: {Parallel}");
                    }
                }

                using (writer.WriteBlock("steps:"))
                {
                    WriteSteps(writer);
                }
            }
        }

        protected virtual void WriteSteps(CustomFileWriter writer)
        {
            // using (writer.WriteBlock("- task: DownloadBuildArtifacts@0"))
            // {
            //     // writer.WriteLine("displayName: Download Artifacts");
            //     using (writer.WriteBlock("inputs:"))
            //     {
            //         writer.WriteLine($"artifactName: {Name}");
            //         writer.WriteLine($"downloadPath: {Path.SingleQuote()}");
            //     }
            // }

            var arguments = $"{InvokedTargets.JoinSpace()} --skip";
            if (PartitionName != null)
                arguments += $" --{ParameterService.GetParameterDashedName(PartitionName)} $(System.JobPositionInPhase)";
            using (writer.WriteBlock($"- pwsh: .\\build.ps1 {arguments}")) { }

            PublishArtifacts.ForEach(x =>
            {
                using (writer.WriteBlock($"- publish: {StringExtensions.SingleQuote(x)}"))
                {
                    writer.WriteLine($"artifact: {Name}");
                }
            });
        }
    }
}
