using System;
using System.IO;
using System.Linq;
using AutoModPlugins;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestCatalogLegality
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(File.ReadAllBytes(path))!;

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = false;
        APILegality.GameVersionPriority = GameVersionPriorityType.NewestFirst;
        APILegality.SetMatchingBalls = false;
        APILegality.SetAllLegalRibbons = false;
        APILegality.ForceLevel100for50 = false;
        APILegality.SetBattleVersion = false;

        Console.WriteLine($"Testing all {RareEventCatalog.Items.Count} Curated Events against {sav.Version}:");

        int legal = 0;
        int failed = 0;

        foreach (var item in RareEventCatalog.Items)
        {
            if (!item.IsCompatibleWith(sav))
            {
                Console.WriteLine($"  [INCOMPATIBLE] {item.DisplayName} is not in this game.");
                continue;
            }

            var pkm = item.GeneratePKM(sav);
            if (pkm != null)
            {
                var la = new LegalityAnalysis(pkm, sav.Personal);
                if (la.Valid)
                {
                    legal++;
                    Console.WriteLine($"  ✅ [LEGAL] {item.TierStars} | {item.DisplayName,-32} | OT: {pkm.OriginalTrainerName,-10} (TID: {pkm.TID16,5}) | Ball: {(Ball)pkm.Ball}");
                }
                else
                {
                    failed++;
                    Console.WriteLine($"  ❌ [INVALID] {item.DisplayName}: {la.Report()}");
                }
            }
            else
            {
                failed++;
                Console.WriteLine($"  ❌ [FAILED TO GENERATE] {item.DisplayName}");
            }
        }

        Console.WriteLine($"\nCatalog Results: Legal = {legal}/{RareEventCatalog.Items.Count} | Failed = {failed}");
    }
}
