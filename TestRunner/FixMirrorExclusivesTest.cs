using System;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class FixMirrorExclusivesTest
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        Console.WriteLine($"Save: {sav.Version}, Trainer: {sav.OT} ({sav.TID16}/{sav.SID16})");

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NativeOnly;
        APILegality.SetMatchingBalls = false;
        APILegality.SetAllLegalRibbons = false;
        APILegality.ForceLevel100for50 = false;
        APILegality.SetBattleVersion = false;

        var testProblemSpecies = new Species[] {
            Species.Lugia, Species.HoOh, Species.Entei, Species.Raikou, Species.Suicune,
            Species.Latias, Species.Latios, Species.Kyogre, Species.Groudon, Species.Rayquaza,
            Species.Dialga, Species.Palkia, Species.Giratina, Species.Heatran, Species.Regigigas,
            Species.Reshiram, Species.Zekrom, Species.Tornadus, Species.Thundurus,
            Species.Xerneas, Species.Yveltal,
            Species.Lunala, Species.Solgaleo, Species.Pheromosa, Species.Buzzwole,
            Species.Celesteela, Species.Kartana, Species.Stakataka, Species.Blacephalon
        };

        int legalCount = 0;
        foreach (var sp in testProblemSpecies)
        {
            var pk = GenerateExclusivesSafe(sav, sp, isShiny: false);
            if (pk is null)
            {
                Console.WriteLine($"FAILED to generate: {sp}");
                continue;
            }

            var la = new LegalityAnalysis(pk, sav.Personal);
            string mark = (pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C) ? " [GB Mark]" : (pk.Version is GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS) ? " [Pentagon]" : (pk.Version is GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM) ? " [Alola Clover]" : "";
            string status = la.Valid ? "✅ LEGAL" : $"❌ INVALID: {la.Report()}";
            if (la.Valid) legalCount++;

            Console.WriteLine($"{(ushort)sp,3} {sp,-14} -> Origin: {pk.Version,-6}{mark,-14} | OT: {pk.OriginalTrainerName,-10} (TID {pk.TID16,5}) | {status}");
        }

        Console.WriteLine($"\nTotal tested: {testProblemSpecies.Length} | Legal: {legalCount}/{testProblemSpecies.Length}");
    }

    public static PKM? GenerateExclusivesSafe(SaveFile targetSav, Species species, bool isShiny)
    {
        ushort s = (ushort)species;
        int consoleRegion = (targetSav as SAV7)?.ConsoleRegion ?? 1;
        int country = (targetSav as SAV7)?.Country ?? 49;
        int region = (targetSav as SAV7)?.Region ?? 1;

        GameVersion originVersion = GetBestOriginForSpecies(species, targetSav.Version);

        bool isVC = originVersion is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
        string otName = isVC && targetSav.OT.Length > 7 ? targetSav.OT[..7] : targetSav.OT;
        ushort sid = isVC ? (ushort)0 : targetSav.SID16;

        var tr = new SimpleTrainerInfo(originVersion)
        {
            OT = otName,
            TID16 = targetSav.TID16,
            SID16 = sid,
            Language = targetSav.Language,
            ConsoleRegion = (byte)consoleRegion,
            Country = (byte)country,
            Region = (byte)region,
        };

        bool allowShiny = isShiny && !SimpleEdits.IsShinyLockedSpeciesForm(s, 0);

        // 1. Direct encounter
        if (tr.GetRandomEncounter(s, 0, allowShiny, false, out var pk) && pk is not null)
        {
            var converted = EntityConverter.ConvertToType(pk, typeof(PK7), out _);
            if (converted is not null)
            {
                converted.OriginalTrainerName = otName;
                converted.TID16 = targetSav.TID16;
                converted.SID16 = sid;
                converted.Language = targetSav.Language;
                return converted;
            }
        }

        // 2. Fallback via Showdown
        string showdownText = GameInfo.Strings.Species[s] + $"\nVersion: {GetVersionShowdownName(originVersion)}";
        if (allowShiny) showdownText += "\nShiny: Yes";

        var sset = new ShowdownSet(showdownText);
        var set = new RegenTemplate(sset) { Nickname = string.Empty };
        var template = EntityBlank.GetBlank(targetSav);

        var res = tr.TryAPIConvert(set, template);
        if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
        {
            var resPk = res.Created;
            resPk.OriginalTrainerName = otName;
            resPk.TID16 = targetSav.TID16;
            resPk.SID16 = sid;
            resPk.Language = targetSav.Language;
            return resPk;
        }

        return null;
    }

    public static GameVersion GetBestOriginForSpecies(Species species, GameVersion currentSave)
    {
        ushort id = (ushort)species;

        // Gen 1 & 2: VC Crystal
        if (id <= 251)
            return GameVersion.C;

        // Gen 3: Hoenn Remakes (OR / AS)
        if (species is Species.Kyogre or Species.Latias) return GameVersion.AS;
        if (species is Species.Groudon or Species.Latios) return GameVersion.OR;
        if (id <= 386) return GameVersion.OR;

        // Gen 4: Sinnoh / USUM
        if (species is Species.Palkia) return GameVersion.UM;
        if (species is Species.Dialga) return GameVersion.D;
        if (species is Species.Regigigas) return GameVersion.UM;
        if (species is Species.Heatran) return GameVersion.Pt;
        if (id <= 493) return GameVersion.Pt;

        // Gen 5: Unova / USUM
        if (species is Species.Zekrom or Species.Thundurus) return GameVersion.UM;
        if (species is Species.Reshiram or Species.Tornadus) return GameVersion.B;
        if (id <= 649) return GameVersion.B;

        // Gen 6: Kalos
        if (species is Species.Yveltal) return GameVersion.Y;
        if (species is Species.Xerneas) return GameVersion.X;
        if (id <= 721) return GameVersion.X;

        // Gen 7: Alola Mirror Exclusives
        if (species is Species.Lunala or Species.Pheromosa or Species.Celesteela or Species.Stakataka)
            return GameVersion.UM;

        if (species is Species.Solgaleo or Species.Buzzwole or Species.Kartana or Species.Blacephalon)
            return GameVersion.US;

        return currentSave;
    }

    private static string GetVersionShowdownName(GameVersion v) => v switch
    {
        GameVersion.C => "Crystal",
        GameVersion.D => "Diamond",
        GameVersion.P => "Pearl",
        GameVersion.Pt => "Platinum",
        GameVersion.B => "Black",
        GameVersion.W => "White",
        GameVersion.X => "X",
        GameVersion.Y => "Y",
        GameVersion.OR => "Omega Ruby",
        GameVersion.AS => "Alpha Sapphire",
        GameVersion.US => "Ultra Sun",
        GameVersion.UM => "Ultra Moon",
        _ => "Ultra Sun"
    };
}
