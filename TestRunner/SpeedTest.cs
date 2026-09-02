using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class SpeedTest
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Fast Direct Encounter Generation for Gen 7 ===");

        var sav = new SAV7USUM();
        sav.Version = GameVersion.US;
        sav.OT = "Oliverduds";
        sav.TID16 = 3192;
        sav.SID16 = 30909;
        sav.Language = (int)LanguageID.English;
        sav.ConsoleRegion = 1;
        sav.Country = 49;
        sav.Region = 1;

        TrainerSettings.Register(sav);

        var sw = Stopwatch.StartNew();

        var bag = new ConcurrentBag<PKM>();
        var personal = sav.Personal;

        // Trainers for each era
        var trVC = new SimpleTrainerInfo(GameVersion.C) { OT = "Oliverd", TID16 = 3192, SID16 = 0, Language = sav.Language };
        var trORAS = new SimpleTrainerInfo(GameVersion.OR) { OT = "Oliverduds", TID16 = 3192, SID16 = 30909, Language = sav.Language };
        var trGen7 = sav;

        Parallel.For(1, 808, id =>
        {
            ushort s = (ushort)id;
            if (!personal.IsSpeciesInGame(s))
                return;

            ITrainerInfo tr = s <= 251 ? trVC : s <= 386 ? trORAS : trGen7;
            if (tr.GetRandomEncounter(s, 0, false, false, out var pk) && pk is not null)
            {
                var converted = EntityConverter.ConvertToType(pk, typeof(PK7), out _);
                if (converted is not null)
                    bag.Add(converted);
            }
            else
            {
                // Fallback to sav
                if (sav.GetRandomEncounter(s, 0, false, false, out var fallback) && fallback is not null)
                    bag.Add(fallback);
            }
        });

        sw.Stop();
        Console.WriteLine($"Generated {bag.Count}/807 Pokemon in {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2} seconds)!");

        int legal = 0;
        int gb = 0;
        int pentagon = 0;
        int clover = 0;

        foreach (var pk in bag)
        {
            var la = new LegalityAnalysis(pk, sav.Personal);
            if (la.Valid) legal++;
            if (pk.Version is GameVersion.C or GameVersion.GD or GameVersion.SI or GameVersion.RD or GameVersion.BU or GameVersion.YW) gb++;
            if (pk.Version is GameVersion.OR or GameVersion.AS or GameVersion.X or GameVersion.Y) pentagon++;
            if (pk.Version is GameVersion.US or GameVersion.UM or GameVersion.SN or GameVersion.MN) clover++;
        }

        Console.WriteLine($"Results: Legal = {legal}/{bag.Count}");
        Console.WriteLine($"GB Marks = {gb}, Pentagons = {pentagon}, Alola Clovers = {clover}");
    }
}
