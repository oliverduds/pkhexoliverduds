using System;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using AutoModPlugins;

namespace TestRunner;

public static class FullSaveTest
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        Console.WriteLine($"Loading user save: {path}");
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        Console.WriteLine($"Save: Version={sav.Version}, Gen={sav.Generation}, BoxCount={sav.BoxCount}, Slots={sav.BoxCount * sav.BoxSlotCount}");
        TrainerSettings.Register(sav);

        var options = new LivingDexCustomOptions
        {
            Mode = LivingDexMode.Gen7CanonicalNormal,
            IncludeForms = true,
            RespectShinyLocks = true,
            BallPreference = BallSelectionPreference.ThematicAuto,
            IVPreference = IVOptimizationPreference.SmartIVs,
            LevelPref = LevelPreference.CanonicalFloor,
            EnableGigantamax = false,
            StartBox = 1,
            BoxPreference = BoxPlacementPreference.Overwrite,
            ExportReport = false
        };

        Console.WriteLine("\n--- Generating Gen 7 Canonical Living Dex ---");
        var targetList = CombinedLivingDex.GenerateGen7CanonicalDexList(sav, false, options);
        Console.WriteLine($"targetList.Count = {targetList.Count}");

        int startSlot = Math.Max(0, (options.StartBox - 1) * sav.BoxSlotCount);
        var plannedSlots = new System.Collections.Generic.List<int>();
        int maxSlot = sav.BoxCount * sav.BoxSlotCount;

        for (int idx = startSlot; idx < maxSlot && plannedSlots.Count < targetList.Count; idx++)
        {
            plannedSlots.Add(idx);
        }

        Console.WriteLine($"plannedSlots.Count = {plannedSlots.Count}");
        if (plannedSlots.Count == 0)
        {
            Console.WriteLine("ERROR: plannedSlots is 0!");
            return;
        }

        int placed = 0;
        for (int i = 0; i < plannedSlots.Count; i++)
        {
            var pk = targetList[i];
            int targetIndex = plannedSlots[i];
            int box = targetIndex / sav.BoxSlotCount;
            int slot = targetIndex % sav.BoxSlotCount;

            sav.SetBoxSlotAtIndex(pk, box, slot);
            placed++;
        }

        Console.WriteLine($"Successfully placed {placed} Pokemon into save!");
        Console.WriteLine($"Boxes used: Box 1 to Box {(placed + 29) / 30}");

        // Now verify legality of first 10 and last 10
        int valid = 0;
        int invalid = 0;
        for (int i = 0; i < placed; i++)
        {
            var pk = sav.GetBoxSlotAtIndex(plannedSlots[i]);
            var la = new LegalityAnalysis(pk, sav.Personal);
            if (la.Valid) valid++;
            else
            {
                invalid++;
                if (invalid <= 5)
                    Console.WriteLine($"Invalid #{invalid}: {pk.Species} {GameInfo.Strings.Species[pk.Species]} - {la.Report()}");
            }
        }

        Console.WriteLine($"\nLegality in Save: Valid = {valid}/{placed} ({(valid * 100.0 / placed):F1}%), Invalid = {invalid}");
    }
}
