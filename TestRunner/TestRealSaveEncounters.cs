using System;
using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestRealSaveEncounters
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        var test = new Species[] { Species.Latios, Species.Groudon, Species.Dialga, Species.Heatran, Species.Tornadus, Species.Reshiram, Species.Xerneas };
        foreach (var sp in test)
        {
            bool ok = sav.GetRandomEncounter((ushort)sp, 0, false, false, out var pk);
            Console.WriteLine($"{sp} in real sav: {ok}");
            if (ok && pk != null)
            {
                var la = new LegalityAnalysis(pk);
                Console.WriteLine($"  Valid: {la.Valid}, Report: {la.Report()}");
            }
        }
    }
}
