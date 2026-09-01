using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class RegionalDexTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Galar Living Dex (Base Species vs With Forms) ===");

        string swordSavePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\sword\main";
        byte[] saveBytes = File.ReadAllBytes(swordSavePath);
        var sav = SaveUtil.GetSaveFile(saveBytes)!;

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NativeOnly;

        // 1. Base Species Only (IncludeForms = false)
        var normalBaseCfg = new LivingDexConfig
        {
            IncludeForms = false,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };
        var normalBase = sav.GenerateLivingDex(sav.Personal, normalBaseCfg).ToList();

        var shinyBaseCfg = new LivingDexConfig
        {
            IncludeForms = false,
            SetShiny = true,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };
        var shinyBase = sav.GenerateLivingDex(sav.Personal, shinyBaseCfg).Where(z => z.IsShiny).ToList();

        Console.WriteLine($"\n[1. Base Species Only (Sem Formas Cosméticas/DLC extras duplicados)]:");
        Console.WriteLine($"Normal Base Count = {normalBase.Count} (ocupa {(normalBase.Count + 29) / 30} caixas)");
        Console.WriteLine($"Shiny Base Count  = {shinyBase.Count} (ocupa {(shinyBase.Count + 29) / 30} caixas)");
        Console.WriteLine($"Total Base Combined = {normalBase.Count + shinyBase.Count} slots");
        Console.WriteLine($"Cabe nas 32 caixas (960 slots)? {(normalBase.Count + shinyBase.Count <= 960 ? "SIM! (Sobram " + (960 - (normalBase.Count + shinyBase.Count)) + " slots)" : "NÃO")}");

        // 2. Full With Forms (IncludeForms = true)
        var normalFormsCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };
        var normalForms = SwordLegalityFixer.FilterValidBoxPokemon(sav.GenerateLivingDex(sav.Personal, normalFormsCfg), sav).ToList();

        var shinyFormsCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = true,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };
        var shinyForms = SwordLegalityFixer.FilterValidBoxPokemon(sav.GenerateLivingDex(sav.Personal, shinyFormsCfg).Where(z => z.IsShiny), sav).ToList();

        Console.WriteLine($"\n[2. Full Expanded Living Dex (Com Todas as Formas Regionais, Alcremie, etc.)]:");
        Console.WriteLine($"Normal Full Count = {normalForms.Count} (ocupa {(normalForms.Count + 29) / 30} caixas)");
        Console.WriteLine($"Shiny Full Count  = {shinyForms.Count} (ocupa {(shinyForms.Count + 29) / 30} caixas)");
        Console.WriteLine($"Total Full Combined = {normalForms.Count + shinyForms.Count} slots (precisaria de {(normalForms.Count + shinyForms.Count + 29) / 30} caixas)");
    }
}
