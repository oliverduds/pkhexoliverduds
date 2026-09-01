using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class CustomizerTest
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Full Customizer & Polisher Suite on Sword Save ===");

        string swordSavePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\sword\main";
        byte[] saveBytes = File.ReadAllBytes(swordSavePath);
        var sav = SaveUtil.GetSaveFile(saveBytes)!;

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NativeOnly;

        var normalCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        var generated = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        var list = SwordLegalityFixer.FilterValidBoxPokemon(generated, sav).ToList();

        Console.WriteLine($"Total filtered Pokemon: {list.Count}");

        int themedBallApplied = 0;
        int smartIVApplied = 0;
        int gmaxApplied = 0;
        int legalCount = 0;
        int invalidCount = 0;

        for (int i = 0; i < list.Count; i++)
        {
            var pk = list[i];

            // 1. Level adjustment to canonical floor
            var la = new LegalityAnalysis(pk, sav.Personal);
            int targetLevel = Program.GetEvolutionMinimumLevel(pk, la, sav.Personal);
            pk.CurrentLevel = (byte)targetLevel;

            if (pk.CurrentLevel < 100 && pk is IHyperTrain ht)
                ht.HyperTrainFlags = 0;

            SwordLegalityFixer.TryApplyNaturalMoves(pk, sav.Personal);

            var testLA = new LegalityAnalysis(pk, sav.Personal);
            if (!testLA.Valid)
            {
                for (int lvl = targetLevel + 1; lvl <= 100; lvl++)
                {
                    pk.CurrentLevel = (byte)lvl;
                    SwordLegalityFixer.TryApplyNaturalMoves(pk, sav.Personal);
                    if (new LegalityAnalysis(pk, sav.Personal).Valid)
                        break;
                }
            }

            // 2. Ball
            var levelLA = new LegalityAnalysis(pk, sav.Personal);
            if (Program.TryApplyThematicBall(pk, levelLA, out _))
                themedBallApplied++;

            // 3. Smart IVs
            if (Program.TryOptimizeIVs(pk, sav.Personal))
                smartIVApplied++;

            // 4. G-Max
            if (Program.TryApplyGigantamax(pk, sav))
                gmaxApplied++;

            // 5. Final legality check
            var finalLA = new LegalityAnalysis(pk, sav.Personal);
            if (finalLA.Valid)
            {
                legalCount++;
            }
            else
            {
                invalidCount++;
                Console.WriteLine($"[INVALID] {pk.Species} {GameInfo.Strings.Species[pk.Species]} Lv.{pk.CurrentLevel} - {finalLA.Report()}");
            }
        }

        Console.WriteLine($"\n--- Customizer Integration Test Results ---");
        Console.WriteLine($"Total Tested: {list.Count}");
        Console.WriteLine($"100% Legal: {legalCount} / {list.Count}");
        Console.WriteLine($"Invalid: {invalidCount}");
        Console.WriteLine($"Thematic Balls: {themedBallApplied}");
        Console.WriteLine($"Smart IVs Optimized: {smartIVApplied}");
        Console.WriteLine($"Gigantamax Factor Enabled: {gmaxApplied}");
    }
}
