using System;
using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestGen5Transfer
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        string shortOT = sav.OT.Length > 7 ? sav.OT[..7] : sav.OT;
        var trB = new SimpleTrainerInfo(GameVersion.B) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language };

        // Test Snivy (495)
        if (trB.GetRandomEncounter(495, 0, false, false, out var pk) && pk is not null)
        {
            Console.WriteLine($"Original PK5: Version={pk.Version}, MetLoc={pk.MetLocation}, EggLoc={pk.EggLocation}, WasEgg={pk.WasEgg}");
            
            // Convert to PK7 natively
            var pk7 = EntityConverter.ConvertToType(pk, typeof(PK7), out _)!;
            Console.WriteLine($"Native converted PK7: Version={pk7.Version}, MetLoc={pk7.MetLocation}, EggLoc={pk7.EggLocation}, WasEgg={pk7.WasEgg}");
            var la1 = new LegalityAnalysis(pk7, sav.Personal);
            Console.WriteLine($"Native PK7 Legality: {(la1.Valid ? "LEGAL" : la1.Report())}");

            // Now see what happened when we manually set MetLocation = 30001
            pk7.MetLocation = 30001;
            var la2 = new LegalityAnalysis(pk7, sav.Personal);
            Console.WriteLine($"After MetLocation=30001: {(la2.Valid ? "LEGAL" : la2.Report())}");
        }

        // Test Arceus (493)
        Console.WriteLine("\nTesting Arceus (493):");
        if (trB.GetRandomEncounter(493, 0, false, false, out var arc) && arc is not null)
        {
            var arc7 = EntityConverter.ConvertToType(arc, typeof(PK7), out _)!;
            var laArc = new LegalityAnalysis(arc7, sav.Personal);
            Console.WriteLine($"Arceus converted: Version={arc7.Version}, Valid={laArc.Valid}, Report={laArc.Report()}");
        }
        else
        {
            Console.WriteLine("Arceus: GetRandomEncounter returned false/null");
        }
    }
}
