using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class LogicTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Fixed In-Game Evolution Logic ===");

        string savePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\savedata.bin";
        byte[] saveBytes = File.ReadAllBytes(savePath);
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

        var normalGenerated = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        var normal = normalGenerated.Where(z => !(z is PB7 pb7 && pb7.IsStarter)).ToList();

        Console.WriteLine($"Generated {normal.Count} Normal Pokémon.");

        var testSav = sav.Clone();
        for (int i = 0; i < normal.Count; i++)
        {
            var pk = normal[i];
            var la = new LegalityAnalysis(pk, testSav.Personal);
            int oldLevel = pk.CurrentLevel;
            int newLevel = GetEvolutionMinimumLevelFixed(pk, la, testSav.Personal);
            
            // Check if level needs change
            pk.CurrentLevel = (byte)newLevel;
            if (pk is PB7 pb7) pb7.ResetCP();
            
            var newLA = new LegalityAnalysis(pk, testSav.Personal);
            string status = newLA.Valid ? "VALID" : $"INVALID: {newLA.Report()}";
            
            Console.WriteLine($"{GetName(pk),-20} | Met: {pk.MetLevel,2} | Old: {oldLevel,3} -> New: {newLevel,3} | {status}");
        }
    }

    public static int GetEvolutionMinimumLevelFixed(PKM pk, LegalityAnalysis la, IPersonalTable personal)
    {
        var tree = EvolutionTree.GetEvolutionTree(pk.Context);
        var allStages = tree.Reverse.GetPreEvolutions(pk.Species, pk.Form).ToList();
        allStages.Add((pk.Species, pk.Form));

        // Filter stages to only species that exist in the loaded game's personal table.
        // This correctly handles Gen 1 games (like LGPE) where baby pre-evolutions (Pichu, Cleffa, etc.)
        // do not exist in the game.
        var stages = allStages.Where(s => personal.IsSpeciesInGame(s.Species)).ToList();

        // Base-stage Pokémon in this game have no evolution floor beyond their actual MetLevel.
        if (stages.Count <= 1)
            return Math.Max(1, (int)pk.MetLevel);

        var enc = la.EncounterOriginal;

        // Propagate the normal evolution chain from stage 0 (the game's base stage).
        // Starting level is the Pokémon's actual MetLevel.
        if (TryPropagateEvolutionMinimum(
                tree,
                stages,
                0,
                Math.Max(1, (int)pk.MetLevel),
                out int canonical))
        {
            return Math.Max((int)pk.MetLevel, canonical);
        }

        return Math.Max((int)pk.MetLevel, (int)pk.CurrentLevel);
    }

    private static bool TryPropagateEvolutionMinimum(
        EvolutionTree tree,
        List<(ushort Species, byte Form)> stages,
        int startIndex,
        int startLevel,
        out int result)
    {
        int level = Math.Max(1, startLevel);

        for (int i = startIndex; i < stages.Count - 1; i++)
        {
            var from = stages[i];
            var to = stages[i + 1];

            int best = int.MaxValue;
            var methods = tree.Forward.GetForward(from.Species, from.Form).Span;

            foreach (var method in methods)
            {
                if (method.Species != to.Species)
                    continue;

                byte destinationForm = method.GetDestinationForm(from.Form);
                if (destinationForm != to.Form)
                    continue;

                int candidate = Math.Max(level + method.LevelUp, method.Level);
                if (candidate < best)
                    best = candidate;
            }

            if (best == int.MaxValue)
            {
                result = level;
                return false;
            }

            level = best;
            if (level > Experience.MaxLevel)
            {
                result = Experience.MaxLevel;
                return false;
            }
        }

        result = level;
        return true;
    }

    private static string GetName(PKM pk)
    {
        var species = GameInfo.Strings.Species[pk.Species];
        if (pk.Form == 0) return species;

        var forms = FormConverter.GetFormList(
            pk.Species,
            GameInfo.Strings.types,
            GameInfo.Strings.forms,
            GameInfo.GenderSymbolUnicode,
            pk.Context);

        if ((uint)pk.Form < (uint)forms.Length && !string.IsNullOrWhiteSpace(forms[pk.Form]))
            return $"{species}-{forms[pk.Form]}";

        return $"{species}-Form{pk.Form}";
    }
}
