using System;
using System.IO;
using System.Linq;
using PKHeX.Core;

namespace TestRunner;

public static class BabyInspector
{
    public static void Run()
    {
        string savePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\savedata.bin";
        byte[] saveBytes = File.ReadAllBytes(savePath);
        var sav = SaveUtil.GetSaveFile(saveBytes)!;
        var tree = EvolutionTree.GetEvolutionTree(sav.Context);

        Console.WriteLine($"Save Context: {sav.Context}");
        Console.WriteLine($"Is Pichu (172) in sav.Personal? {sav.Personal.IsSpeciesInGame((ushort)Species.Pichu)}");
        Console.WriteLine($"Is Cleffa (173) in sav.Personal? {sav.Personal.IsSpeciesInGame((ushort)Species.Cleffa)}");
        Console.WriteLine($"Is Magby (240) in sav.Personal? {sav.Personal.IsSpeciesInGame((ushort)Species.Magby)}");

        ushort[] babySpecies = { 
            (ushort)Species.Pikachu, (ushort)Species.Raichu,
            (ushort)Species.Clefairy, (ushort)Species.Clefable,
            (ushort)Species.Jigglypuff, (ushort)Species.Wigglytuff,
            (ushort)Species.Electabuzz, (ushort)Species.Magmar, (ushort)Species.Jynx, (ushort)Species.Snorlax
        };

        foreach (var sp in babySpecies)
        {
            var preEvos = tree.Reverse.GetPreEvolutions(sp, 0).ToList();
            Console.WriteLine($"Species: {GameInfo.Strings.Species[sp]} preEvos: {string.Join(", ", preEvos.Select(p => $"{GameInfo.Strings.Species[p.Species]} (InGame: {sav.Personal.IsSpeciesInGame(p.Species)})"))}");
        }
    }
}
