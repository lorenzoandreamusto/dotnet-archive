using System.CommandLine;
using DotNetArchive.Cli.Models;
using DotNetArchive.Cli.Services;

namespace DotNetArchive.Cli.Commands;

public static class CacheCommand
{
    public static Command Create(ICacheManager cacheManager)
    {
        ArgumentNullException.ThrowIfNull(cacheManager);
        var root = new Command("cache", "Manage the on-disk cache of extracted .dna archives.");

        // list
        var listCmd = new Command("list", "List all entries currently in the cache.");
        listCmd.SetAction(_ =>
        {
            var entries = cacheManager.List().OrderBy(e => e.LastAccess).ToList();
            if (entries.Count == 0) { Console.WriteLine("Cache is empty."); return 0; }
            Console.WriteLine($"{"Hash",-16}  {"Last access (UTC)",-25}  {"Size",12}  Source");
            foreach (var e in entries)
            {
                Console.WriteLine($"{e.Hash,-16}  {e.LastAccess.UtcDateTime:yyyy-MM-dd HH:mm:ss}    {FormatBytes(e.SizeBytes),12}  {e.SourcePath}");
            }
            Console.WriteLine($"\nTotal: {entries.Count} entries, {FormatBytes(entries.Sum(e => e.SizeBytes))}");
            return 0;
        });
        root.Subcommands.Add(listCmd);

        // clean
        var olderThanOpt = new Option<int?>("--older-than") { Description = "Remove entries older than N days." };
        var keepOpt = new Option<int?>("--keep") { Description = "Keep at most N most-recently-accessed entries." };
        var cleanCmd = new Command("clean", "Remove old or excess cache entries.");
        cleanCmd.Options.Add(olderThanOpt);
        cleanCmd.Options.Add(keepOpt);
        cleanCmd.SetAction(parseResult =>
        {
            int? older = parseResult.GetValue(olderThanOpt);
            int? keep = parseResult.GetValue(keepOpt);
            var all = cacheManager.List().ToList();

            int removed = 0;
            if (older.HasValue)
            {
                var cutoff = DateTimeOffset.UtcNow.AddDays(-older.Value);
                foreach (var e in all.Where(e => e.LastAccess < cutoff).ToList())
                {
                    cacheManager.Invalidate(e.Hash); removed++;
                }
            }
            if (keep.HasValue)
            {
                var survivors = cacheManager.List().OrderByDescending(e => e.LastAccess).Take(keep.Value).Select(e => e.Hash).ToHashSet();
                foreach (var e in cacheManager.List().Where(e => !survivors.Contains(e.Hash)).ToList())
                {
                    cacheManager.Invalidate(e.Hash); removed++;
                }
            }
            if (!older.HasValue && !keep.HasValue)
            {
                cacheManager.Clean();
                Console.WriteLine("Cache cleaned (default policy).");
            }
            else
            {
                Console.WriteLine($"Cache cleaned: removed {removed} entries.");
            }
            return 0;
        });
        root.Subcommands.Add(cleanCmd);

        // set-dir
        var pathArg = new Argument<string>("path") { Description = "New cache directory path." };
        var setDirCmd = new Command("set-dir", "Change the cache directory used by the tool.");
        setDirCmd.Arguments.Add(pathArg);
        setDirCmd.SetAction(parseResult =>
        {
            string path = parseResult.GetValue(pathArg)!;
            var newOpts = new CacheOptions
            {
                CacheDir = Path.GetFullPath(path),
                MaxAgeDays = cacheManager.Options.MaxAgeDays,
                MaxSizeBytes = cacheManager.Options.MaxSizeBytes,
            };
            cacheManager.SetOptions(newOpts);
            Console.WriteLine($"Cache directory set to: {newOpts.CacheDir}");
            return 0;
        });
        root.Subcommands.Add(setDirCmd);

        return root;
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return $"{v:0.##} {units[u]}";
    }
}
