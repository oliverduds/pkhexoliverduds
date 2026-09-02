using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using AutoModPlugins;

namespace TestRunner;

public static class EndToEndGen7Verification
{
    public static void Run()
    {
        Console.WriteLine("=== End-to-End Verification: Gen 7 Canonical Living Dex Generation & Polishing ===");

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

        // 1. Verify Normal Mode
        VerifyMode(sav, isShiny: false);

        // 2. Verify Shiny Mode
        VerifyMode(sav, isShiny: true);
    }

    private static void VerifyMode(SaveFile sav, bool isShiny)
    {
        string modeLabel = isShiny ? "✨ SHINY CANONICAL DEX" : "📦 NORMAL CANONICAL DEX";
        Console.WriteLine($"\n--- Testing {modeLabel} ---");

        var options = new LivingDexCustomOptions
        {
            Mode = isShiny ? LivingDexMode.Gen7CanonicalShiny : LivingDexMode.Gen7CanonicalNormal,
            IncludeForms = false,
            RespectShinyLocks = true,
            BallPreference = BallSelectionPreference.ThematicAuto,
            IVPreference = IVOptimizationPreference.SmartIVs,
            LevelPref = LevelPreference.CanonicalFloor,
            EnableGigantamax = false,
            StartBox = 1,
            BoxPreference = BoxPlacementPreference.Overwrite,
            ExportReport = false
        };

        var list = CombinedLivingDex.GenerateGen7CanonicalDexList(sav, isShiny, options);
        Console.WriteLine($"Generated {list.Count} species.");

        int totalChecked = 0;
        int validCount = 0;
        int invalidCount = 0;
        int gbCount = 0;
        int pentagonCount = 0;
        int cloverCount = 0;
        int otherCount = 0;
        int actualShinyCount = 0;
        int lockedNormalCount = 0;
        int cherishCount = 0;

        var invalidSamples = new List<string>();

        // Polish each Pokemon exactly as ExecuteGeneration does
        var random = new Random();
        for (int i = 0; i < list.Count; i++)
        {
            var pk = list[i];
            totalChecked++;

            // Polishing step
            if (options.LevelPref == LevelPreference.CanonicalFloor)
            {
                var la = new LegalityAnalysis(pk, sav.Personal);
                int targetLevel = LivingDexPolisher.GetEvolutionMinimumLevel(pk, la, sav.Personal);
                pk.CurrentLevel = (byte)targetLevel;

                if (pk.CurrentLevel < 100 && pk is IHyperTrain ht)
                    ht.HyperTrainFlags = 0;

                LivingDexPolisher.TryApplyNaturalMoves(pk, sav.Personal);

                var testLA = new LegalityAnalysis(pk, sav.Personal);
                if (!testLA.Valid)
                {
                    for (int lvl = targetLevel + 1; lvl <= 100; lvl++)
                    {
                        pk.CurrentLevel = (byte)lvl;
                        LivingDexPolisher.TryApplyNaturalMoves(pk, sav.Personal);
                        if (new LegalityAnalysis(pk, sav.Personal).Valid)
                            break;
                    }
                }
            }

            // Ball
            if (pk.Ball != (byte)Ball.Cherish)
            {
                // Only for non-VC and non-event
                bool isVC = pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
                if (!isVC)
                {
                    var la = new LegalityAnalysis(pk, sav.Personal);
                    if (la.Valid)
                        LivingDexPolisher.TryApplyThematicBall(pk, la, out _);
                }
            }

            // IVs
            LivingDexPolisher.TryOptimizeIVs(pk, sav.Personal);

            // Final legality check
            var finalLA = new LegalityAnalysis(pk, sav.Personal);
            if (finalLA.Valid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                if (invalidSamples.Count < 20)
                {
                    invalidSamples.Add($"#{pk.Species,3} {GameInfo.Strings.Species[pk.Species],-14} (Origin: {pk.Version}, Lv.{pk.CurrentLevel}): {finalLA.Report()}");
                }
            }

            // Stats
            if (pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C)
                gbCount++;
            else if (pk.Version is GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS)
                pentagonCount++;
            else if (pk.Version is GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM)
                cloverCount++;
            else
                otherCount++;

            if (pk.Ball == (byte)Ball.Cherish) cherishCount++;
            if (pk.IsShiny) actualShinyCount++;
            else lockedNormalCount++;
        }

        Console.WriteLine($"Result: Valid = {validCount}/{totalChecked} ({(validCount * 100.0 / totalChecked):F1}%), Invalid = {invalidCount}");
        Console.WriteLine($"Marks breakdown: GB Mark (VC 1/2) = {gbCount} | Pentagon (Gen 6/ORAS) = {pentagonCount} | Alola Clover = {cloverCount} | Other = {otherCount}");
        Console.WriteLine($"Balls: Cherish Ball Events = {cherishCount}");
        if (isShiny)
        {
            Console.WriteLine($"Shinies: {actualShinyCount} ★ Shinies | {lockedNormalCount} Legal Shiny-Locked Mythicals");
        }

        if (invalidSamples.Count > 0)
        {
            Console.WriteLine("\nFirst Invalid Samples:");
            foreach (var s in invalidSamples)
                Console.WriteLine("  " + s);
        }
    }
}
