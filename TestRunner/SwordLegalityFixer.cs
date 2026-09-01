using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class SwordLegalityFixer
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Sword Living Dex Generation with Full Fixes ===");

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
        var filtered = FilterValidBoxPokemon(generated, sav).ToList();

        int invalidCount = 0;
        int legalCount = 0;

        for (int i = 0; i < filtered.Count; i++)
        {
            var pk = filtered[i];

            var la = new LegalityAnalysis(pk, sav.Personal);

            // 1. Level Normalization to Canonical Floor with upward safety scan
            int targetLevel = Program.GetEvolutionMinimumLevel(pk, la, sav.Personal);
            pk.CurrentLevel = (byte)targetLevel;

            if (pk.CurrentLevel < 100 && pk is IHyperTrain ht)
                ht.HyperTrainFlags = 0;

            TryApplyNaturalMoves(pk, sav.Personal);

            var testLA = new LegalityAnalysis(pk, sav.Personal);
            if (!testLA.Valid)
            {
                for (int lvl = targetLevel + 1; lvl <= 100; lvl++)
                {
                    pk.CurrentLevel = (byte)lvl;
                    TryApplyNaturalMoves(pk, sav.Personal);
                    if (new LegalityAnalysis(pk, sav.Personal).Valid)
                        break;
                }
            }

            var levelLA = new LegalityAnalysis(pk, sav.Personal);
            Program.TryApplyThematicBall(pk, levelLA, out _);
            Program.TryOptimizeIVs(pk, sav.Personal);
            Program.TryApplyGigantamax(pk, sav);

            var finalLA = new LegalityAnalysis(pk, sav.Personal);
            if (finalLA.Valid)
            {
                legalCount++;
            }
            else
            {
                invalidCount++;
                Console.WriteLine($"[INVALID #{invalidCount}] {pk.Species,3} {GameInfo.Strings.Species[pk.Species],-16} Form {pk.Form} Lv.{pk.CurrentLevel} - {finalLA.Report()}");
            }
        }

        Console.WriteLine($"\n--- Sword Normal Dex Result ---");
        Console.WriteLine($"Legal: {legalCount} / {filtered.Count}");
        Console.WriteLine($"Invalid: {invalidCount}");

        // Now test Shiny Dex
        var shinyCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = true,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        var shinyGenerated = sav.GenerateLivingDex(sav.Personal, shinyCfg).Where(z => z.IsShiny).ToList();
        var shinyFiltered = FilterValidBoxPokemon(shinyGenerated, sav).ToList();

        int shinyLegal = 0;
        int shinyInvalid = 0;

        for (int i = 0; i < shinyFiltered.Count; i++)
        {
            var pk = shinyFiltered[i];

            var la = new LegalityAnalysis(pk, sav.Personal);
            int targetLevel = Program.GetEvolutionMinimumLevel(pk, la, sav.Personal);
            pk.CurrentLevel = (byte)targetLevel;

            if (pk.CurrentLevel < 100 && pk is IHyperTrain ht)
                ht.HyperTrainFlags = 0;

            TryApplyNaturalMoves(pk, sav.Personal);

            var testLA = new LegalityAnalysis(pk, sav.Personal);
            if (!testLA.Valid)
            {
                for (int lvl = targetLevel + 1; lvl <= 100; lvl++)
                {
                    pk.CurrentLevel = (byte)lvl;
                    TryApplyNaturalMoves(pk, sav.Personal);
                    if (new LegalityAnalysis(pk, sav.Personal).Valid)
                        break;
                }
            }

            var levelLA = new LegalityAnalysis(pk, sav.Personal);
            Program.TryApplyThematicBall(pk, levelLA, out _);
            Program.TryOptimizeIVs(pk, sav.Personal);
            Program.TryApplyGigantamax(pk, sav);

            var finalLA = new LegalityAnalysis(pk, sav.Personal);
            if (finalLA.Valid)
            {
                shinyLegal++;
            }
            else
            {
                shinyInvalid++;
                Console.WriteLine($"[SHINY INVALID #{shinyInvalid}] {pk.Species,3} {GameInfo.Strings.Species[pk.Species],-16} Form {pk.Form} Lv.{pk.CurrentLevel} - {finalLA.Report()}");
            }
        }

        Console.WriteLine($"\n--- Sword Shiny Dex Result ---");
        Console.WriteLine($"Legal: {shinyLegal} / {shinyFiltered.Count}");
        Console.WriteLine($"Invalid: {shinyInvalid}");
    }

    public static IEnumerable<PKM> FilterValidBoxPokemon(IEnumerable<PKM> list, SaveFile sav)
    {
        foreach (var pk in list)
        {
            // Partner Pikachu / Eevee in LGPE
            if (pk is PB7 { IsStarter: true })
                continue;

            // In Gen 8/9, Silvally forms 1-17 are item-dependent in-battle memories, not valid standalone box forms
            if (pk.Species == (ushort)Species.Silvally && pk.Form > 0)
                continue;

            // Battle-only / fused forms
            if (FormInfo.IsBattleOnlyForm(pk.Species, pk.Form, sav.Generation) ||
                FormInfo.IsFusedForm(pk.Species, pk.Form, sav.Generation))
                continue;

            yield return pk;
        }
    }

    public static bool TryApplyNaturalMoves(PKM pk, IPersonalTable personal)
    {
        Span<ushort> oldMoves = stackalloc ushort[4];
        Span<ushort> oldRelearn = stackalloc ushort[4];
        pk.GetMoves(oldMoves);
        pk.GetRelearnMoves(oldRelearn);

        var la = new LegalityAnalysis(pk, personal);
        if (!la.Parsed)
            return false;

        const MoveSourceType natural = MoveSourceType.LevelUp | MoveSourceType.RelearnMoves | MoveSourceType.Evolve;
        Span<ushort> moves = stackalloc ushort[4];
        la.GetSuggestedCurrentMoves(moves, natural);

        if (moves[0] == 0)
        {
            moves.Clear();
            la.GetSuggestedCurrentMoves(moves, MoveSourceType.Encounter);
        }

        if (moves[0] != 0)
        {
            pk.SetMoves(moves);
            pk.SetMaximumPPCurrent(moves);
        }

        var afterMovesLA = new LegalityAnalysis(pk, personal);
        Span<ushort> relearn = stackalloc ushort[4];
        afterMovesLA.GetSuggestedRelearnMovesFromEncounter(relearn);
        pk.SetRelearnMoves(relearn);

        // Keldeo Form 0 cannot know Secret Sword; Form 1 MUST know Secret Sword
        if (pk.Species == (ushort)Species.Keldeo)
        {
            ushort secretSword = (ushort)Move.SecretSword;
            Span<ushort> curMoves = stackalloc ushort[4];
            pk.GetMoves(curMoves);
            if (pk.Form == 0 && curMoves.Contains(secretSword))
            {
                for (int m = 0; m < 4; m++)
                {
                    if (curMoves[m] == secretSword)
                        curMoves[m] = (ushort)Move.SacredSword;
                }
                pk.SetMoves(curMoves);
                pk.SetMaximumPPCurrent(curMoves);
            }
            else if (pk.Form == 1 && !curMoves.Contains(secretSword))
            {
                curMoves[0] = secretSword;
                pk.SetMoves(curMoves);
                pk.SetMaximumPPCurrent(curMoves);
            }
        }

        Span<ushort> newMoves = stackalloc ushort[4];
        Span<ushort> newRelearn = stackalloc ushort[4];
        pk.GetMoves(newMoves);
        pk.GetRelearnMoves(newRelearn);

        return !oldMoves.SequenceEqual(newMoves) || !oldRelearn.SequenceEqual(newRelearn);
    }
}
