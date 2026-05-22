using System.CommandLine;

var rootCommand = new RootCommand("DotNet Archive Utility - Gestisci ed esegui applicazioni .NET come singoli archivi.");

var rootProjectArgument = new Argument<string>("project")
{
    Description = "Il percorso del progetto .NET o del file .csproj (default: cartella corrente)",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory()
};

var rootOutputOption = new Option<string>("--output", "-o")
{
    Description = "Il percorso del file .dna di output"
};

var registerCommand = new Command("register", "Associa automaticamente l'estensione .dna a questo tool sul sistema operativo corrente.");

registerCommand.SetAction(parseResult =>
{
    int result = AssociationManager.Register();
    return result;
});

rootCommand.Subcommands.Add(registerCommand);

rootCommand.Arguments.Add(rootProjectArgument);
rootCommand.Options.Add(rootOutputOption);

rootCommand.SetAction(parseResult =>
{
    string project = parseResult.GetValue(rootProjectArgument) ?? ".";
    string output = parseResult.GetValue(rootOutputOption) ?? string.Empty;
    
    int result = Exporter.ExportProject(project, output);
    return result;
});


var exportProjectArgument = new Argument<string>("project")
{
    Description = "Il percorso del progetto .NET o del file .csproj (default: cartella corrente)",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory()
};

var exportOutputOption = new Option<string>("--output", "-o")
{
    Description = "Il percorso del file .dna di output"
};

var exportCommand = new Command("export", "Compila un progetto .NET e lo esporta in un archivio .dna");
exportCommand.Arguments.Add(exportProjectArgument);
exportCommand.Options.Add(exportOutputOption);

exportCommand.SetAction(parseResult =>
{
    string project = parseResult.GetValue(exportProjectArgument) ?? ".";
    string output = parseResult.GetValue(exportOutputOption) ?? string.Empty;
    
    int result = Exporter.ExportProject(project, output);
    return result;
});


var archiveArgument = new Argument<string>("archive")
{
    Description = "Il percorso del file .dna o del progetto .csproj da eseguire (default: cartella corrente)",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory()
};

var appArgsArgument = new Argument<string[]>("args")
{
    Description = "Argomenti da passare all'applicazione",
    DefaultValueFactory = _ => Array.Empty<string>()
};

var runCommand = new Command("run", "Esegue un'applicazione contenuta in un file .dna o esegue un export-and-run");
runCommand.Arguments.Add(archiveArgument);
runCommand.Arguments.Add(appArgsArgument);

runCommand.SetAction(parseResult =>
{
    string archive = parseResult.GetValue(archiveArgument)!;
    string[] args = parseResult.GetValue(appArgsArgument) ?? Array.Empty<string>();
    
    int result = Runner.RunArchive(archive, args);
    return result;
});


rootCommand.Subcommands.Add(exportCommand);
rootCommand.Subcommands.Add(runCommand);

return rootCommand.Parse(args).Invoke();