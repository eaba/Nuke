using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Buildalyzer;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Nuke.Common;
using static Nuke.Common.IO.PathConstruction;

namespace Rocket.Surgery.Nuke.SyncPackages
{
    internal static class PackageSync
    {
        public static async Task AddMissingPackages(
            AbsolutePath solutionPath,
            AbsolutePath packagesProps,
            CancellationToken cancellationToken
        )
        {
            XDocument document;
            {
                using var packagesFile = File.OpenRead(packagesProps);
                document = XDocument.Load(packagesFile, LoadOptions.PreserveWhitespace);
            }

            var packageReferences = document.Descendants("PackageReference")
               .Concat(document.Descendants("GlobalPackageReference"))
               .Select(x => x.Attributes().First(x => x.Name == "Include" || x.Name == "Update").Value)
               .ToArray();

            Logger.Trace("Found {0} existing package references", packageReferences.Length);

            var missingPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var project in GetProjects(solutionPath).SelectMany(project => project.Build()))
            {
                if (project.Items.TryGetValue("PackageReference", out var projectPackageReferences))
                {
                    foreach (var item in projectPackageReferences
                       .Where(x => !x.Metadata.ContainsKey("IsImplicitlyDefined") && !x.Metadata.ContainsKey("Version"))
                    )
                    {
                        if (packageReferences.Any(z => z.Equals(item.ItemSpec, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        Logger.Info("Package {0} is missing and will be added to {1}", item.ItemSpec, packagesProps);
                        missingPackages.Add(item.ItemSpec);
                    }
                }
            }

            var itemGroups = document.Descendants("ItemGroup");
            var itemGroupToInsertInto = itemGroups.Count() > 2 ? itemGroups.Skip(1).Take(1).First() : itemGroups.Last();

            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3()); // Add v3 API support
            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            var sourceRepository = new SourceRepository(packageSource, providers);
            using var sourceCacheContext = new SourceCacheContext();

            foreach (var item in missingPackages.OrderBy(x => x))
            {
                var element = new XElement("PackageReference");
                element.SetAttributeValue("Update", item);
                {
                    var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>()
                       .ConfigureAwait(false);
                    var resolvedPackages = await dependencyInfoResource.ResolvePackages(
                        item,
                        sourceCacheContext,
                        NullLogger.Instance,
                        cancellationToken
                    ).ConfigureAwait(false);
                    var packageInfo = resolvedPackages.OrderByDescending(x => x.Identity.Version)
                       .First(x => !x.Identity.Version.IsPrerelease);
                    element.SetAttributeValue("Version", packageInfo.Identity.Version.ToString());
                    Logger.Trace(
                        "Found Version {0} for {1}",
                        packageInfo.Identity.Version.ToString(),
                        packageInfo.Identity.Id
                    );
                }
                itemGroupToInsertInto.Add(element);
            }

            OrderPackageReferences(itemGroups.ToArray());
            RemoveDuplicatePackageReferences(document);

            UpdateXDocument(packagesProps, document);
        }

        public static Task RemoveExtraPackages(
            AbsolutePath solutionPath,
            AbsolutePath packagesProps
        )
        {
            XDocument document;
            {
                using var packagesFile = File.OpenRead(packagesProps);
                document = XDocument.Load(packagesFile, LoadOptions.PreserveWhitespace);
            }
            var packageReferences = document.Descendants("PackageReference")
               .Concat(document.Descendants("GlobalPackageReference"))
               .Select(x => x.Attributes().First(x => x.Name == "Include" || x.Name == "Update").Value)
               .ToList();

            foreach (var project in GetProjects(solutionPath).SelectMany(project => project.Build()))
            {
                if (project.Items.TryGetValue("PackageReference", out var projectPackageReferences))
                {
                    foreach (var item in projectPackageReferences)
                    {
                        packageReferences.Remove(item.ItemSpec);
                    }
                }
            }

            if (packageReferences.Count > 0)
            {
                foreach (var package in packageReferences)
                {
                    foreach (var item in document.Descendants("PackageReference")
                       .Concat(
                            document.Descendants("GlobalPackageReference")
                               .Where(
                                    x => x.Attributes().First(x => x.Name == "Include" || x.Name == "Update").Value
                                       .Equals(package, StringComparison.OrdinalIgnoreCase)
                                )
                        )
                       .ToArray())
                    {
                        Logger.Info(
                            "Removing extra PackageReference for {0}",
                            item.Attribute("Include")?.Value ?? item.Attribute("Update")?.Value
                        );
                        item.Remove();
                    }
                }
            }

            UpdateXDocument(packagesProps, document);
            return Task.CompletedTask;
        }

        public static Task MoveVersions(
            AbsolutePath solutionPath,
            AbsolutePath packagesProps
        )
        {
            XDocument packagesDocument;
            {
                using var packagesFile = File.OpenRead(packagesProps);
                packagesDocument = XDocument.Load(packagesFile, LoadOptions.None);
            }

            var itemGroups = packagesDocument.Descendants("ItemGroup");
            var itemGroupToInsertInto = itemGroups.Count() > 2 ? itemGroups.Skip(1).Take(1).First() : itemGroups.Last();

            var projects = GetProjects(solutionPath).Select(x => x.ProjectFile.Path)
               .Select(
                    path =>
                    {
                        using var file = File.OpenRead(path);
                        return ( path, document: XDocument.Load(file, LoadOptions.PreserveWhitespace) );
                    }
                );
            foreach (var (path, document) in projects)
            {
                foreach (var item in document.Descendants("PackageReference")
                   .Where(x => !string.IsNullOrEmpty(x.Attribute("Version")?.Value))
                   .ToArray()
                )
                {
                    Logger.Info(
                        "Found Version {0} on {1} in {2} and moving it to {3}",
                        item.Attribute("Version").Value,
                        item.Attribute("Include").Value,
                        path,
                        packagesProps
                    );
                    var @new = new XElement(item);
                    @new.SetAttributeValue("Update", @new.Attribute("Include").Value);
                    @new.SetAttributeValue("Include", null);
                    @new.SetAttributeValue("Version", null);
                    @new.SetAttributeValue("Version", item.Attribute("Version").Value);
                    foreach (var an in itemGroupToInsertInto.Descendants("PackageReference").Last()
                       .Annotations<XmlSignificantWhitespace>())
                    {
                        @new.AddAnnotation(an);
                    }

                    itemGroupToInsertInto.Add(@new);
                    item.SetAttributeValue("Version", null);
                }

                UpdateXDocument(path, document);
            }

            OrderPackageReferences(itemGroups.ToArray());
            RemoveDuplicatePackageReferences(packagesDocument);

            UpdateXDocument(packagesProps, packagesDocument);
            return Task.CompletedTask;
        }

        private static IEnumerable<ProjectAnalyzer> GetProjects(AbsolutePath solutionPath)
        {
            var am = new AnalyzerManager(solutionPath, new AnalyzerManagerOptions());
            foreach (var project in am.Projects.Values.Where(
                x => !x.ProjectFile.Path.EndsWith(".build.csproj", StringComparison.OrdinalIgnoreCase)
            ))
            {
                yield return project;
            }
        }

        private static void UpdateXDocument(string path, XDocument document)
        {
            using var fileWrite = File.Open(path, FileMode.Truncate);
            using var writer = XmlWriter.Create(
                fileWrite,
                new XmlWriterSettings { OmitXmlDeclaration = true, Async = true, Indent = true }
            );
            document.Save(writer);
        }

        private static void OrderPackageReferences(params XElement[] itemGroups)
        {
            foreach (var itemGroup in itemGroups)
            {
                var toSort = itemGroup.Descendants("PackageReference").ToArray();
                var sorted = itemGroup.Descendants("PackageReference").Select(x => new XElement(x))
                   .OrderBy(x => x.Attribute("Include")?.Value ?? x.Attribute("Update")?.Value).ToArray();
                for (var i = 0; i < sorted.Length; i++)
                {
                    toSort[i].ReplaceAttributes(sorted[i].Attributes());
                }
            }
        }

        private static void RemoveDuplicatePackageReferences(XDocument document)
        {
            var packageReferences = document.Descendants("PackageReference")
               .ToLookup(x => x.Attribute("Include")?.Value ?? x.Attribute("Update")?.Value);
            foreach (var item in packageReferences.Where(item => item.Count() > 1))
            {
                item.Last().Remove();
            }
        }
    }
}