using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using AutoModPlugins;

namespace TestRunner;

public static class SimulateExecuteGeneration
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = SaveUtil.GetSaveFile(data)!;

        Console.WriteLine($"Testing on User Save: {sav.Version}, BoxCount={sav.BoxCount}, Slots={sav.BoxCount * sav.BoxSlotCount}");

        foreach (LivingDexMode mode in Enum.GetValues<LivingDexMode>())
        {
            var opt = new LivingDexCustomOptions
            {
                Mode = mode,
                IncludeForms = true,
                RespectShinyLocks = true,
                StartBox = 1,
                BoxPreference = BoxPlacementPreference.Overwrite
            };

            List<PKM> targetList = [];
            if (opt.Mode is LivingDexMode.Gen7CanonicalNormal or LivingDexMode.Gen7CanonicalShiny)
            {
                bool isShiny = opt.Mode == LivingDexMode.Gen7CanonicalShiny;
                targetList = CombinedLivingDex.GenerateGen7CanonicalDexList(sav, isShiny, opt);
            }
            else
            {
                List<PKM> normalList = [];
                List<PKM> shinyList = [];

                if (opt.Mode is LivingDexMode.Normal or LivingDexMode.Combined or LivingDexMode.BaseSpeciesOnly)
                {
                    var normalCfg = new LivingDexConfig
                    {
                        IncludeForms = opt.Mode != LivingDexMode.BaseSpeciesOnly && opt.IncludeForms,
                        SetShiny = false,
                        SetAlpha = false,
                        TransferVersion = sav.Version,
                    };
                    var gen = sav.GenerateLivingDex(sav.Personal, normalCfg);
                    normalList = CombinedLivingDex.FilterValidBoxPokemon(gen, sav).ToList();
                }

                if (opt.Mode is LivingDexMode.Shiny or LivingDexMode.Combined or LivingDexMode.BaseSpeciesOnly)
                {
                    var shinyCfg = new LivingDexConfig
                    {
                        IncludeForms = opt.Mode != LivingDexMode.BaseSpeciesOnly && opt.IncludeForms,
                        SetShiny = true,
                        SetAlpha = false,
                        TransferVersion = sav.Version,
                    };
                    var gen = sav.GenerateLivingDex(sav.Personal, shinyCfg).Where(z => z.IsShiny);
                    shinyList = CombinedLivingDex.FilterValidBoxPokemon(gen, sav).ToList();
                }

                targetList = opt.Mode switch
                {
                    LivingDexMode.Normal => normalList,
                    LivingDexMode.Shiny => shinyList,
                    LivingDexMode.Combined => [.. normalList, .. shinyList],
                    LivingDexMode.BaseSpeciesOnly => normalList,
                    _ => normalList,
                };
            }

            int startSlot = Math.Max(0, (opt.StartBox - 1) * sav.BoxSlotCount);
            var plannedSlots = new List<int>();
            int maxSlot = sav.BoxCount * sav.BoxSlotCount;

            for (int idx = startSlot; idx < maxSlot && plannedSlots.Count < targetList.Count; idx++)
            {
                plannedSlots.Add(idx);
            }

            Console.WriteLine($"Mode {mode,-22}: targetList.Count = {targetList.Count,4} | plannedSlots = {plannedSlots.Count,4}");
        }
    }
}
