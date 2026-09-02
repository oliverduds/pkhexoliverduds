using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class Gen7FullDexTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Gen 7 Canonical Multi-Origin Living Dex Generation (Parallel) ===");

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

        // Test sample from each era
        var testSpecies = new ushort[] {
            1, 6, 25, 151, // Gen 1 (Bulbasaur, Charizard, Pikachu, Mew)
            152, 157, 249, 251, // Gen 2 (Chikorita, Typhlosion, Lugia, Celebi)
            252, 257, 384, 386, // Gen 3 (Treecko, Blaziken, Rayquaza, Deoxys)
            387, 392, 483, 491, // Gen 4 (Turtwig, Infernape, Dialga, Darkrai)
            494, 497, 643, 649, // Gen 5 (Victini, Serperior, Reshiram, Genesect)
            650, 658, 716, 719, // Gen 6 (Chespin, Greninja, Xerneas, Diancie)
            722, 724, 791, 801, 802 // Gen 7 (Rowlet, Decidueye, Solgaleo, Magearna, Marshadow)
        };

        Console.WriteLine("\n--- Testing Sample Generation for Normal Mode ---");
        var normalPkms = GenerateSample(sav, testSpecies, isShiny: false);
        foreach (var pk in normalPkms)
        {
            var la = new LegalityAnalysis(pk, sav.Personal);
            string gbMark = (pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C) ? " [GB Mark]" : "";
            string pentagon = (pk.Version is GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS) ? " [Pentagon]" : "";
            string clover = (pk.Version is GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM) ? " [Alola Clover]" : "";
            string legality = la.Valid ? "✅ LEGAL" : $"❌ INVALID: {la.Report()}";

            Console.WriteLine($"{pk.Species,3} {GameInfo.Strings.Species[pk.Species],-12} -> Origin: {pk.Version,-6}{gbMark}{pentagon}{clover,-14} | OT: {pk.OriginalTrainerName,-10} | Ball: {(Ball)pk.Ball,-8} | {legality}");
        }

        Console.WriteLine("\n--- Testing Sample Generation for Shiny Mode ---");
        var shinyPkms = GenerateSample(sav, testSpecies, isShiny: true);
        foreach (var pk in shinyPkms)
        {
            var la = new LegalityAnalysis(pk, sav.Personal);
            string gbMark = (pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C) ? " [GB Mark]" : "";
            string pentagon = (pk.Version is GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS) ? " [Pentagon]" : "";
            string clover = (pk.Version is GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM) ? " [Alola Clover]" : "";
            string shinyStr = pk.IsShiny ? "★ Shiny" : "Normal(Lock)";
            string legality = la.Valid ? "✅ LEGAL" : $"❌ INVALID: {la.Report()}";

            Console.WriteLine($"{pk.Species,3} {GameInfo.Strings.Species[pk.Species],-12} -> {shinyStr,-12} | Origin: {pk.Version,-6}{gbMark}{pentagon}{clover,-14} | OT: {pk.OriginalTrainerName,-10} | {legality}");
        }
    }

    public static List<PKM> GenerateSample(SaveFile sav, ushort[] speciesList, bool isShiny)
    {
        var bag = new ConcurrentBag<PKM>();

        Parallel.ForEach(speciesList, s =>
        {
            var origin = GetCanonicalOriginVersion((Species)s, sav.Version);
            var pk = GenerateOriginPKM(sav, s, 0, isShiny, origin);
            if (pk is not null)
                bag.Add(pk);
        });

        return bag.OrderBy(z => z.Species).ToList();
    }

    public static GameVersion GetCanonicalOriginVersion(Species species, GameVersion defaultVersion)
    {
        ushort id = (ushort)species;

        if (id <= 251) return GameVersion.C;  // VC Gen 1/2
        if (id <= 386) return GameVersion.OR; // ORAS Gen 3
        if (id <= 493) return GameVersion.Pt; // Gen 4
        if (id <= 649) return GameVersion.B;  // Gen 5
        if (id <= 721) return GameVersion.X;  // Gen 6
        return defaultVersion;                // Gen 7 (USUM)
    }

    public static PKM? GenerateOriginPKM(SaveFile targetSav, ushort species, byte form, bool isShiny, GameVersion originVersion)
    {
        int consoleRegion = (targetSav as SAV7)?.ConsoleRegion ?? 1;
        int country = (targetSav as SAV7)?.Country ?? 49;
        int region = (targetSav as SAV7)?.Region ?? 1;

        bool isVC = originVersion is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
        string otName = isVC && targetSav.OT.Length > 7 ? targetSav.OT[..7] : targetSav.OT;
        ushort sid = isVC ? (ushort)0 : targetSav.SID16;

        var tr = TrainerSettings.GetSavedTrainerData(originVersion, lang: (LanguageID)targetSav.Language)
                 ?? new SimpleTrainerInfo(originVersion)
                 {
                     OT = otName,
                     TID16 = targetSav.TID16,
                     SID16 = sid,
                     Language = targetSav.Language,
                     ConsoleRegion = (byte)consoleRegion,
                     Country = (byte)country,
                     Region = (byte)region,
                 };

        string speciesName = GameInfo.Strings.Species[species];
        string setStr = speciesName;
        if (form > 0)
            setStr += $"-{form}";

        bool isEventOnlyMythical = IsEventOnlyMythical((Species)species);

        if (isShiny && !SimpleEdits.IsShinyLockedSpeciesForm(species, form) && !IsShinyLockedMythicalGen7((Species)species))
        {
            setStr += "\nShiny: Yes";
        }

        if (!isEventOnlyMythical)
        {
            setStr += $"\nVersion: {GetVersionShowdownName(originVersion)}";
        }

        var sset = new ShowdownSet(setStr);
        var template = EntityBlank.GetBlank(targetSav);
        var set = new RegenTemplate(sset) { Nickname = string.Empty };

        var res = tr.TryAPIConvert(set, template);
        if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
        {
            var pk = res.Created;

            if (!isEventOnlyMythical || IsInGameCatchableMythical((Species)species, pk.Version))
            {
                pk.OriginalTrainerName = otName;
                pk.TID16 = targetSav.TID16;
                pk.SID16 = sid;
                pk.Language = targetSav.Language;
            }

            return pk;
        }

        var fallbackSet = new ShowdownSet(isShiny && !SimpleEdits.IsShinyLockedSpeciesForm(species, form) ? $"{speciesName}\nShiny: Yes" : speciesName);
        var fallbackTemplate = EntityBlank.GetBlank(targetSav);
        var fallbackRegen = new RegenTemplate(fallbackSet) { Nickname = string.Empty };
        var fallbackRes = targetSav.TryAPIConvert(fallbackRegen, fallbackTemplate);
        if (fallbackRes.Status == LegalizationResult.Regenerated && fallbackRes.Created is not null)
        {
            return fallbackRes.Created;
        }

        return null;
    }

    private static string GetVersionShowdownName(GameVersion v) => v switch
    {
        GameVersion.RD => "Red",
        GameVersion.BU => "Blue",
        GameVersion.YW => "Yellow",
        GameVersion.GD => "Gold",
        GameVersion.SI => "Silver",
        GameVersion.C => "Crystal",
        GameVersion.R => "Ruby",
        GameVersion.S => "Sapphire",
        GameVersion.E => "Emerald",
        GameVersion.FR => "FireRed",
        GameVersion.LG => "LeafGreen",
        GameVersion.D => "Diamond",
        GameVersion.P => "Pearl",
        GameVersion.Pt => "Platinum",
        GameVersion.HG => "HeartGold",
        GameVersion.SS => "SoulSilver",
        GameVersion.B => "Black",
        GameVersion.W => "White",
        GameVersion.B2 => "Black 2",
        GameVersion.W2 => "White 2",
        GameVersion.X => "X",
        GameVersion.Y => "Y",
        GameVersion.OR => "Omega Ruby",
        GameVersion.AS => "Alpha Sapphire",
        GameVersion.SN => "Sun",
        GameVersion.MN => "Moon",
        GameVersion.US => "Ultra Sun",
        GameVersion.UM => "Ultra Moon",
        _ => "Ultra Sun"
    };

    private static bool IsEventOnlyMythical(Species s) => s is
        Species.Mew or Species.Jirachi or
        Species.Phione or Species.Manaphy or
        Species.Victini or Species.Keldeo or Species.Meloetta or Species.Genesect or
        Species.Diancie or Species.Hoopa or Species.Volcanion or
        Species.Marshadow or Species.Zeraora;

    private static bool IsInGameCatchableMythical(Species s, GameVersion v)
    {
        if (s == Species.Celebi && v == GameVersion.C) return true;
        if (s == Species.Deoxys && (v == GameVersion.OR || v == GameVersion.AS)) return true;
        if (s == Species.Darkrai && v == GameVersion.Pt) return true;
        if (s == Species.Shaymin && v == GameVersion.Pt) return true;
        if (s == Species.Arceus && v == GameVersion.Pt) return true;
        if (s == Species.Magearna && (v == GameVersion.US || v == GameVersion.UM || v == GameVersion.SN || v == GameVersion.MN)) return true;
        return false;
    }

    private static bool IsShinyLockedMythicalGen7(Species s) => s is
        Species.Victini or Species.Keldeo or Species.Meloetta or
        Species.Hoopa or Species.Volcanion or Species.Cosmog or Species.Cosmoem or
        Species.Magearna or Species.Marshadow or Species.Zeraora;
}
