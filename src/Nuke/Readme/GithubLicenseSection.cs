using System.Collections.Generic;
using System.Dynamic;

namespace Rocket.Surgery.Nuke.Readme
{
    class GithubLicenseSection : IBadgeSection
    {
        public string Name => "Github Release";

        public string ConfigKey => "github";

        public string Process(IDictionary<object, object> config, IMarkdownReferences references, RocketBoosterBuild build)
        {
            var url = references.AddReference("github-license", $"https://github.com/{config["owner"]}/{config["repository"]}/blob/master/LICENSE");
            var badge = references.AddReference("github-license-badge", $"https://img.shields.io/github/license/{config["owner"]}/{config["repository"]}.svg?style=flat", "License");
            return $"[!{badge}]{url}";
        }
    }
}