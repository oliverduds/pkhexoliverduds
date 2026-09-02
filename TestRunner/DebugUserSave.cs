using System;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using AutoModPlugins;

namespace TestRunner;

public static class DebugUserSave
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        Console.WriteLine($"Loading save: {path}");
        var data = File.ReadAllBytes(path);
        var sav = SaveUtil.GetSaveFile(data);
        if (sav is null) return;

        Console.WriteLine($"Save: Version={sav.Version}, Gen={sav.Generation}, BoxCount={sav.BoxCount}");
        TrainerSettings.Register(sav);

        // 1. Test Mode.Normal
        var normalCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };
        var gen = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        var filteredNormal = CombinedLivingDex.FilterValidBoxPokemon(gen, sav).ToList();
        Console.WriteLine($"Mode.Normal: gen.Count = {gen.Count}, filtered.Count = {filteredNormal.Count}");

        // 2. Test Gen7CanonicalNormal
        var optionsNormal = new LivingDexCustomOptions { Mode = LivingDexMode.Gen7CanonicalNormal };
        var gen7Normal = CombinedLivingDex.GenerateGen7CanonicalDexList(sav, false, optionsNormal);
        Console.WriteLine($"Gen7CanonicalNormal: Count = {gen7Normal.Count}");

        // 3. Test Gen7CanonicalShiny
        var optionsShiny = new LivingDexCustomOptions { Mode = LivingDexMode.Gen7CanonicalShiny };
        var gen7Shiny = CombinedLivingDex.GenerateGen7CanonicalDexList(sav, true, optionsShiny);
        Console.WriteLine($"Gen7CanonicalShiny: Count = {gen7Shiny.Count}");
    }
}
