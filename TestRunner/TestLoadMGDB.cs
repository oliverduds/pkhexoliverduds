using System;
using System.IO;
using System.Linq;
using PKHeX.Core;

namespace TestRunner;

public static class TestLoadMGDB
{
    public static void Run()
    {
        string mgdbDir = @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex\mgdb";
        Console.WriteLine($"Checking MGDB directory: {mgdbDir}");

        var files = Directory.EnumerateFiles(mgdbDir, "*.*", SearchOption.AllDirectories)
            .Where(f =>
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".wc6" or ".wc7" or ".wc8" or ".wc9" or ".wc6full" or ".wc7full" or ".pgf" or ".pcd" or ".wb7" or ".wb8" or ".wa8" or ".wa9";
            })
            .ToArray();

        Console.WriteLine($"Found {files.Length} Wonder Card files in MGDB!");

        // Refresh PKHeX MGDB
        EncounterEvent.RefreshMGDB(files);

        Console.WriteLine($"After RefreshMGDB:");
        Console.WriteLine($"  G4 Wondercards: {EncounterEvent.EGDB_G4.Length}");
        Console.WriteLine($"  G5 Wondercards: {EncounterEvent.EGDB_G5.Length}");
        Console.WriteLine($"  G6 Wondercards: {EncounterEvent.EGDB_G6.Length}");
        Console.WriteLine($"  G7 Wondercards: {EncounterEvent.EGDB_G7.Length}");
        Console.WriteLine($"  G8 Wondercards: {EncounterEvent.EGDB_G8.Length}");
        Console.WriteLine($"  G9 Wondercards: {EncounterEvent.EGDB_G9.Length}");
    }
}
