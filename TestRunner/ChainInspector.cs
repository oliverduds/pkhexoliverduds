using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;

namespace TestRunner;

public static class ChainInspector
{
    public static void Run()
    {
        string savePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\savedata.bin";
        byte[] saveBytes = File.ReadAllBytes(savePath);
        var sav = SaveUtil.GetSaveFile(saveBytes)!;
        var tree = EvolutionTree.GetEvolutionTree(sav.Context);

        ushort[] checkSpecies = {
            (ushort)Species.Poliwag, (ushort)Species.Poliwhirl, (ushort)Species.Poliwrath
        };

        foreach (var sp in checkSpecies)
        {
            var preEvos = tree.Reverse.GetPreEvolutions(sp, 0).ToList();
            Console.WriteLine($"\nSpecies: {GameInfo.Strings.Species[sp]} (ID: {sp})");
            Console.WriteLine($"  PreEvolutions count: {preEvos.Count}");
            for (int i = 0; i < preEvos.Count; i++)
            {
                Console.WriteLine($"    preEvos[{i}]: {GameInfo.Strings.Species[preEvos[i].Species]} (ID: {preEvos[i].Species}, Form: {preEvos[i].Form})");
            }

            var stages = preEvos.ToList();
            stages.Add((sp, 0));
            Console.WriteLine($"  Assembled stages: {string.Join(" -> ", stages.Select(s => GameInfo.Strings.Species[s.Species]))}");

            // Now trace propagation step by step with startLevel = 3
            int level = 3;
            Console.WriteLine($"  Tracing propagation starting at Level {level}:");
            for (int i = 0; i < stages.Count - 1; i++)
            {
                var from = stages[i];
                var to = stages[i + 1];
                var methods = tree.Forward.GetForward(from.Species, from.Form).Span;
                Console.WriteLine($"    Stage {i}: from {GameInfo.Strings.Species[from.Species]} to {GameInfo.Strings.Species[to.Species]} (methods count: {methods.Length})");
                int best = int.MaxValue;
                foreach (var m in methods)
                {
                    Console.WriteLine($"      Method: Target={GameInfo.Strings.Species[m.Species]}, Form={m.Form}, MethodType={m.Method}, Level={m.Level}, LevelUp={m.LevelUp}");
                    if (m.Species != to.Species)
                    {
                        Console.WriteLine("        -> skipped (species mismatch)");
                        continue;
                    }
                    byte destForm = m.GetDestinationForm(from.Form);
                    if (destForm != to.Form)
                    {
                        Console.WriteLine($"        -> skipped (form mismatch: {destForm} != {to.Form})");
                        continue;
                    }
                    int candidate = Math.Max(level + m.LevelUp, m.Level);
                    Console.WriteLine($"        -> MATCH! candidate = Max({level} + {m.LevelUp}, {m.Level}) = {candidate}");
                    if (candidate < best)
                        best = candidate;
                }
                if (best == int.MaxValue)
                {
                    Console.WriteLine($"    FAILED to find forward method from {GameInfo.Strings.Species[from.Species]} to {GameInfo.Strings.Species[to.Species]}!");
                    break;
                }
                level = best;
                Console.WriteLine($"    Stage {i} result level: {level}");
            }
            Console.WriteLine($"  Final computed level: {level}");
        }
    }
}
