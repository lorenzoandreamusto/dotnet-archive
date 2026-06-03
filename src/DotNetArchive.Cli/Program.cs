using System.CommandLine;
using DotNetArchive.Cli.Commands;
using DotNetArchive.Cli.Services;

ManifestSerializer serializer = new();
ToolPathResolver toolPathResolver = new();
CacheManager cacheManager = new();

IArchiveExporter exporter = new ArchiveExporter(serializer);
IArchiveRunner runner = new ArchiveRunner(exporter, serializer, cacheManager);
IFileAssociationManager associations = new FileAssociationManager(toolPathResolver);

RootCommand root = CommandFactory.BuildRoot(exporter, runner, associations, serializer, cacheManager);

return await root.Parse(args).InvokeAsync();
