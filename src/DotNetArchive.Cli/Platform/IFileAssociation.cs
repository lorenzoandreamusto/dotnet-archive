namespace DotNetArchive.Cli.Platform;

public interface IFileAssociation
{
    /// <summary>Register the .dna extension on the current OS. Returns 0 on success, non-zero on failure.</summary>
    int Register();

    /// <summary>Unregister the .dna extension. Returns 0 on success, non-zero on failure.</summary>
    int Unregister();
}
