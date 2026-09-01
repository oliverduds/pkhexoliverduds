using System;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class SwordTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Sword Save File ===");

        string swordSavePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\sword\main";
        if (!File.Exists(swordSavePath))
        {
            Console.WriteLine($"Sword save file not found at {swordSavePath}");
            return;
        }

        byte[] saveBytes = File.ReadAllBytes(swordSavePath);
        var sav = SaveUtil.GetSaveFile(saveBytes);
        if (sav == null)
        {
            Console.WriteLine("Failed to load Sword save file.");
            return;
        }

        Console.WriteLine($"Loaded Save: Version = {sav.Version}, Context = {sav.Context}, Type = {sav.GetType().Name}");
        Console.WriteLine($"Trainer: OT = {sav.OT}, TID16 = {sav.TID16}, SID16 = {sav.SID16}");
        Console.WriteLine($"BoxCount = {sav.BoxCount}, BoxSlotCount = {sav.BoxSlotCount}, Total Slots = {sav.SlotCount}");

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

        var normalNoFormsCfg = new LivingDexConfig
        {
            IncludeForms = false,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        Console.WriteLine("\nTesting GenerateLivingDex for Normal (with forms)...");
        var normalPkms = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        Console.WriteLine($"Normal (with forms) count: {normalPkms.Count}");

        Console.WriteLine("\nTesting GenerateLivingDex for Normal (without forms)...");
        var normalNoFormsPkms = sav.GenerateLivingDex(sav.Personal, normalNoFormsCfg).ToList();
        Console.WriteLine($"Normal (no forms) count: {normalNoFormsPkms.Count}");

        var shinyCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = true,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        Console.WriteLine("\nTesting GenerateLivingDex for Shiny (with forms)...");
        var shinyPkms = sav.GenerateLivingDex(sav.Personal, shinyCfg).Where(z => z.IsShiny).ToList();
        Console.WriteLine($"Shiny (with forms) count: {shinyPkms.Count}");

        Console.WriteLine($"\nTotal Box Capacity in Sword: {sav.BoxCount * sav.BoxSlotCount} slots.");
        Console.WriteLine($"Normal ({normalPkms.Count}) + Shiny ({shinyPkms.Count}) = {normalPkms.Count + shinyPkms.Count} slots needed.");
    }
}
