namespace DotNetArchive.Cli.Services;

public interface IFileAssociationManager
{
    int Register();
    int Unregister();
}
