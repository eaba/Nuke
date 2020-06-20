using System;
using Nuke.Common;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Octokit;
using Octokit.Reactive;

namespace Nuke.GitHub
{

    /// <summary>
    /// Xamarin test run
    /// </summary>
    public interface IHaveRepositoryDispatch
    {
        public const string OWNER = nameof(OWNER);
        public const string NAME = nameof(NAME);
        /// <summary>
        /// test
        /// </summary>
        public new Target NotifyRelease => _ => _
           .Executes(
                () =>
                {
                    var repositoryUri = "http://github.com/RocketSurgeonsGuild/Nuke";
                    GitHubClient client = new GitHubClient(new Connection(ProductHeaderValue.Parse(""), new Uri(repositoryUri)));
                    ObservableGitHubClient observableClient = new ObservableGitHubClient(client);

                    observableClient.Repository.Release.GetLatest(OWNER, NAME).Subscribe(_ => { });
                    observableClient.Repository.
                    client.Repository.Release.GetLatest(OWNER, NAME);
                }
            );
    }
}