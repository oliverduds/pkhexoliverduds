using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class Gen7CanonicalDexTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Gen 7 Canonical Origin Living Dex (Normal & Shiny) ===");

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

        // 1. Normal Dex Count & Legality
        var normalCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        var normalPkms = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        Console.WriteLine($"\n[Normal Living Dex Gen 7]:");
        Console.WriteLine($"Total Pokemon generated = {normalPkms.Count}");
        Console.WriteLine($"Boxes required = {(normalPkms.Count + 29) / 30} caixas (de {sav.BoxCount} caixas disponíveis)");
        Console.WriteLine($"Cabe no save de Ultra Sun? {(normalPkms.Count <= sav.BoxCount * sav.BoxSlotCount ? "SIM! (Sobram " + ((sav.BoxCount * sav.BoxSlotCount) - normalPkms.Count) + " slots)" : "NÃO")}");

        // 2. Shiny Dex Count & Legality
        var shinyCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = true,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        var shinyPkms = sav.GenerateLivingDex(sav.Personal, shinyCfg).Where(z => z.IsShiny).ToList();
        Console.WriteLine($"\n[Shiny Living Dex Gen 7]:");
        Console.WriteLine($"Total Legal Shiny Pokemon = {shinyPkms.Count}");
        Console.WriteLine($"Boxes required = {(shinyPkms.Count + 29) / 30} caixas (de {sav.BoxCount} caixas disponíveis)");
        Console.WriteLine($"Cabe no save de Ultra Sun? {(shinyPkms.Count <= sav.BoxCount * sav.BoxSlotCount ? "SIM! (Sobram " + ((sav.BoxCount * sav.BoxSlotCount) - shinyPkms.Count) + " slots)" : "NÃO")}");

        // 3. Combined Count Check
        int combined = normalPkms.Count + shinyPkms.Count;
        Console.WriteLine($"\n[Normal + Shiny Juntas]:");
        Console.WriteLine($"Total somado = {combined} slots (precisaria de {(combined + 29) / 30} caixas)");
        Console.WriteLine($"Cabe junto no mesmo save? {(combined <= sav.BoxCount * sav.BoxSlotCount ? "SIM" : "NÃO (O save só tem 32 caixas / 960 slots)")}");
    }
}
