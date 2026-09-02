using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class Gen7OriginTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing Gen 7 Origin-Specific Living Dex Generation ===");

        // Create an Ultra Sun SaveFile (SAV7USUM) with realistic 3DS Geolocation
        var sav = new SAV7USUM();
        sav.Version = GameVersion.US;
        sav.OT = "Oliverduds";
        sav.TID16 = 3192;
        sav.SID16 = 30909;
        sav.Language = (int)LanguageID.English;
        sav.ConsoleRegion = 1; // Americas
        sav.Country = 49;       // USA
        sav.Region = 1;

        Console.WriteLine($"Loaded Save: {sav.Version} (Generation {sav.Generation}, Context: {sav.Context})");
        Console.WriteLine($"Trainer: OT = {sav.OT}, TID = {sav.TID16}, SID = {sav.SID16}");

        TrainerSettings.Register(sav);

        // Test generating with Showdown format specifying version or encounter
        TestRegen(sav, "Pikachu\nVersion: Yellow", "Pikachu VC Yellow");
        TestRegen(sav, "Charizard\nVersion: Red", "Charizard VC Red");
        TestRegen(sav, "Mew\nVersion: Crystal", "Mew VC Crystal");
        TestRegen(sav, "Typhlosion\nVersion: Gold", "Typhlosion VC Gold");
        TestRegen(sav, "Celebi\nVersion: Crystal", "Celebi VC Crystal");
        TestRegen(sav, "Blaziken\nVersion: Omega Ruby", "Blaziken ORAS");
        TestRegen(sav, "Deoxys\nVersion: Omega Ruby", "Deoxys ORAS");
        TestRegen(sav, "Jirachi", "Jirachi Event");
        TestRegen(sav, "Infernape\nVersion: Platinum", "Infernape Platinum");
        TestRegen(sav, "Darkrai", "Darkrai Event");
        TestRegen(sav, "Serperior\nVersion: Black", "Serperior Black");
        TestRegen(sav, "Victini", "Victini Event");
        TestRegen(sav, "Greninja\nVersion: X", "Greninja X");
        TestRegen(sav, "Diancie", "Diancie Event");
        TestRegen(sav, "Decidueye\nVersion: Ultra Sun", "Decidueye Ultra Sun");
        TestRegen(sav, "Magearna", "Magearna QR Event");
        TestRegen(sav, "Marshadow", "Marshadow Event");
    }

    private static void TestRegen(SaveFile sav, string showdownText, string label)
    {
        var sset = new ShowdownSet(showdownText);
        var template = EntityBlank.GetBlank(sav);
        var set = new RegenTemplate(sset) { Nickname = string.Empty };

        var res = sav.TryAPIConvert(set, template);
        if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
        {
            var pk = res.Created;
            var la = new LegalityAnalysis(pk, sav.Personal);
            string gbMark = (pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C) ? " [GB Mark]" : "";
            string pentagon = (pk.Version is GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS) ? " [Pentagon]" : "";
            string clover = (pk.Version is GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM) ? " [Alola Clover]" : "";
            string legality = la.Valid ? "✅ LEGAL" : $"❌ INVALID: {la.Report()}";

            Console.WriteLine($"[{label,-24}] {pk.Species,3} {GameInfo.Strings.Species[pk.Species],-12} -> Origin: {pk.Version,-10}{gbMark}{pentagon}{clover,-15} | Ball: {(Ball)pk.Ball,-8} | Lv.{pk.CurrentLevel,3} | Status: {legality}");
        }
        else
        {
            Console.WriteLine($"[{label,-24}] Failed to generate (Status: {res.Status})");
        }
    }
}
