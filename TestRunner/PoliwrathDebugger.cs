using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class PoliwrathDebugger
{
    public static void Run()
    {
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
        
        // Let's inspect Poliwrath, Gengar, Raichu, Clefable, etc.
        ushort[] inspectSpecies = { 
            (ushort)Species.Poliwag, (ushort)Species.Poliwhirl, (ushort)Species.Poliwrath,
            (ushort)Species.Pikachu, (ushort)Species.Raichu,
            (ushort)Species.Clefairy, (ushort)Species.Clefable,
            (ushort)Species.Gastly, (ushort)Species.Haunter, (ushort)Species.Gengar
        };

        foreach (var sp in inspectSpecies)
        {
            var pk = normalGenerated.FirstOrDefault(p => p.Species == sp);
            if (pk == null)
            {
                Console.WriteLine($"Species {GameInfo.Strings.Species[sp]} not found in generated list!");
                continue;
            }

            var la = new LegalityAnalysis(pk, sav.Personal);
            var enc = la.EncounterOriginal;
            var tree = EvolutionTree.GetEvolutionTree(pk.Context);
            var stages = tree.Reverse.GetPreEvolutions(pk.Species, pk.Form).ToList();
            stages.Add((pk.Species, pk.Form));

            int encounterStage = -1;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].Species == enc.Species && stages[i].Form == enc.Form)
                    encounterStage = i;
            }
            if (encounterStage < 0)
            {
                for (int i = 0; i < stages.Count; i++)
                {
                    if (stages[i].Species == enc.Species)
                        encounterStage = i;
                }
            }

            Console.WriteLine($"\n--- Species: {GameInfo.Strings.Species[pk.Species]} ---");
            Console.WriteLine($"  CurrentLevel: {pk.CurrentLevel}, MetLevel: {pk.MetLevel}");
            Console.WriteLine($"  EncounterOriginal: Type={enc.GetType().Name}, Species={GameInfo.Strings.Species[enc.Species]}, LevelMin={enc.LevelMin}, LevelMax={enc.LevelMax}");
            Console.WriteLine($"  Stages: {string.Join(" -> ", stages.Select(s => GameInfo.Strings.Species[s.Species]))}");
            Console.WriteLine($"  encounterStage index: {encounterStage} (stages.Count = {stages.Count})");

            if (encounterStage >= 0 && encounterStage < stages.Count - 1)
            {
                int startLevel = Math.Max((int)pk.MetLevel, enc.LevelMin);
                bool propOk = TryPropagateEvolutionMinimum(tree, stages, encounterStage, startLevel, out int fromEncounter);
                Console.WriteLine($"  Branch A (from encounter stage {encounterStage}): startLevel={startLevel}, propOk={propOk}, result={fromEncounter}");
            }
            else
            {
                bool propOk = TryPropagateEvolutionMinimum(tree, stages, 0, Math.Max(1, (int)pk.MetLevel), out int canonical);
                Console.WriteLine($"  Branch B (direct/full chain from 0): startLevel={Math.Max(1, (int)pk.MetLevel)}, propOk={propOk}, result={canonical}");
            }
        }
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
}
