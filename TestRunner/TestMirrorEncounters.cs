using System;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestMirrorEncounters
{
    public static void Run()
    {
        var trUM = new SimpleTrainerInfo(GameVersion.UM) { OT = "Oliverduds", TID16 = 63517, SID16 = 11102, Language = 2, ConsoleRegion = 1, Country = 49, Region = 2 };
        var trAS = new SimpleTrainerInfo(GameVersion.AS) { OT = "Oliverduds", TID16 = 63517, SID16 = 11102, Language = 2, ConsoleRegion = 1, Country = 49, Region = 2 };
        var trOR = new SimpleTrainerInfo(GameVersion.OR) { OT = "Oliverduds", TID16 = 63517, SID16 = 11102, Language = 2, ConsoleRegion = 1, Country = 49, Region = 2 };
        var trVC = new SimpleTrainerInfo(GameVersion.C) { OT = "Oliverd", TID16 = 63517, SID16 = 0, Language = 2 };

        // Test Palkia in UM
        bool okP = trUM.GetRandomEncounter((ushort)Species.Palkia, 0, false, false, out var pkP);
        Console.WriteLine($"Palkia in UM: {okP}");
        if (okP && pkP != null)
        {
            var pk7 = EntityConverter.ConvertToType(pkP, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = "Oliverduds";
            pk7.TID16 = 63517;
            pk7.SID16 = 11102;
            var la = new LegalityAnalysis(pk7);
            Console.WriteLine($"  Palkia PK7: Valid={la.Valid}, Report={la.Report()}");
        }

        // Test Kyogre in UM
        bool okK = trUM.GetRandomEncounter((ushort)Species.Kyogre, 0, false, false, out var pkK);
        Console.WriteLine($"Kyogre in UM: {okK}");
        if (okK && pkK != null)
        {
            var pk7 = EntityConverter.ConvertToType(pkK, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = "Oliverduds";
            pk7.TID16 = 63517;
            pk7.SID16 = 11102;
            var la = new LegalityAnalysis(pk7);
            Console.WriteLine($"  Kyogre in UM PK7: Valid={la.Valid}, Report={la.Report()}");
        }

        // Test Entei in UM
        bool okE = trUM.GetRandomEncounter((ushort)Species.Entei, 0, false, false, out var pkE);
        Console.WriteLine($"Entei in UM: {okE}");
        if (okE && pkE != null)
        {
            var pk7 = EntityConverter.ConvertToType(pkE, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = "Oliverduds";
            pk7.TID16 = 63517;
            pk7.SID16 = 11102;
            var la = new LegalityAnalysis(pk7);
            Console.WriteLine($"  Entei in UM PK7: Valid={la.Valid}, Report={la.Report()}");
        }

        // Test Lugia in UM
        bool okLu = trUM.GetRandomEncounter((ushort)Species.Lugia, 0, false, false, out var pkLu);
        Console.WriteLine($"Lugia in UM: {okLu}");
        if (okLu && pkLu != null)
        {
            var pk7 = EntityConverter.ConvertToType(pkLu, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = "Oliverduds";
            pk7.TID16 = 63517;
            pk7.SID16 = 11102;
            var la = new LegalityAnalysis(pk7);
            Console.WriteLine($"  Lugia in UM PK7: Valid={la.Valid}, Report={la.Report()}");
        }

        // Test Entei in VC Crystal
        bool okEvc = trVC.GetRandomEncounter((ushort)Species.Entei, 0, false, false, out var pkEvc);
        Console.WriteLine($"Entei in VC Crystal: {okEvc}");
        if (okEvc && pkEvc != null)
        {
            var pk7 = EntityConverter.ConvertToType(pkEvc, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = "Oliverd";
            pk7.TID16 = 63517;
            pk7.SID16 = 0;
            var la = new LegalityAnalysis(pk7);
            Console.WriteLine($"  Entei in VC PK7: Valid={la.Valid}, Report={la.Report()}");
        }

        // Test Lugia in VC Crystal
        bool okLuvc = trVC.GetRandomEncounter((ushort)Species.Lugia, 0, false, false, out var pkLuvc);
        Console.WriteLine($"Lugia in VC Crystal: {okLuvc}");
        if (okLuvc && pkLuvc != null)
        {
            var pk7 = EntityConverter.ConvertToType(pkLuvc, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = "Oliverd";
            pk7.TID16 = 63517;
            pk7.SID16 = 0;
            var la = new LegalityAnalysis(pk7);
            Console.WriteLine($"  Lugia in VC PK7: Valid={la.Valid}, Report={la.Report()}");
        }
    }
}
