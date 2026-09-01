using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class SwordNormalTest
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Sword Normal Living Dex Placement & Polish ===");

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

        Console.WriteLine("Generating Normal Living Dex list in memory...");
        var generated = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        Console.WriteLine($"Total generated: {generated.Count}");

        int startSlot = 0;
        int placed = 0;
        int polished = 0;
        int gmaxCount = 0;
        int ballsThemed = 0;

        for (int i = 0; i < generated.Count && startSlot < sav.SlotCount; i++)
        {
            var pk = generated[i];
            
            // Polish the Pokémon
            var la = new LegalityAnalysis(pk, sav.Personal);
            if (la.Valid)
            {
                // Level normalization
                int targetLevel = Program.GetEvolutionMinimumLevel(pk, la, sav.Personal);
                pk.CurrentLevel = (byte)targetLevel;

                var levelLA = new LegalityAnalysis(pk, sav.Personal);

                // Thematic Ball
                if (Program.TryApplyThematicBall(pk, levelLA, out var chosenBall))
                    ballsThemed++;

                // Smart IVs
                Program.TryOptimizeIVs(pk, sav.Personal);

                // G-Max
                if (Program.TryApplyGigantamax(pk, sav))
                    gmaxCount++;

                polished++;
            }

            int box = startSlot / sav.BoxSlotCount;
            int slot = startSlot % sav.BoxSlotCount;
            sav.SetBoxSlotAtIndex(pk, box, slot);
            placed++;
            startSlot++;

            if (i < 15 || i % 100 == 0 || i == generated.Count - 1)
            {
                string gmaxStr = (pk is IGigantamax g && g.CanGigantamax) ? " [G-Max]" : "";
                Console.WriteLine($"[{i + 1,3}/{generated.Count}] Placed {pk.Species,3} {GameInfo.Strings.Species[pk.Species],-16} Lv.{pk.CurrentLevel,3} | Ball: {(Ball)pk.Ball,-10}{gmaxStr} -> Box {box + 1,2}, Slot {slot + 1,2}");
            }
        }

        Console.WriteLine($"\nSuccessfully placed {placed} Pokémon in Sword across {(placed + sav.BoxSlotCount - 1) / sav.BoxSlotCount} boxes!");
        Console.WriteLine($"Polished: {polished}, Thematic Balls: {ballsThemed}, G-Max enabled: {gmaxCount}");

        // Audit the boxes
        int legal = 0;
        int invalid = 0;
        for (int b = 0; b < sav.BoxCount; b++)
        {
            for (int s = 0; s < sav.BoxSlotCount; s++)
            {
                var pk = sav.GetBoxSlotAtIndex(b, s);
                if (pk.Species == 0) continue;
                var la = new LegalityAnalysis(pk, sav.Personal);
                if (la.Valid) legal++;
                else
                {
                    invalid++;
                    Console.WriteLine($"[INVALID] Box {b + 1}, Slot {s + 1}: {GameInfo.Strings.Species[pk.Species]} Lv.{pk.CurrentLevel} - {la.Report()}");
                }
            }
        }

        Console.WriteLine($"\nSword Living Dex Audit: Legal = {legal}, Invalid = {invalid}");
    }
}
