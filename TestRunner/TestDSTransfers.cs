using System;
using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestDSTransfers
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        Console.WriteLine($"Save: {sav.Version}, Trainer: {sav.OT}");

        // In Gen 4/5 DS games, English OT character limit is 7:
        string dsOT = sav.OT.Length > 7 ? sav.OT[..7] : sav.OT;

        // Trainers for DS Games
        var trPlatinum = new SimpleTrainerInfo(GameVersion.Pt) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trHeartGold = new SimpleTrainerInfo(GameVersion.HG) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trSoulSilver = new SimpleTrainerInfo(GameVersion.SS) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trDiamond = new SimpleTrainerInfo(GameVersion.D) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trPearl = new SimpleTrainerInfo(GameVersion.P) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trBlack = new SimpleTrainerInfo(GameVersion.B) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trWhite = new SimpleTrainerInfo(GameVersion.W) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trBlack2 = new SimpleTrainerInfo(GameVersion.B2) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };
        var trWhite2 = new SimpleTrainerInfo(GameVersion.W2) { OT = dsOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = 2 };

        // Test Palkia from Pearl (DS)
        TestSpecies(trPearl, Species.Palkia, sav);

        // Test Dialga from Diamond (DS)
        TestSpecies(trDiamond, Species.Dialga, sav);

        // Test Giratina from Platinum (DS)
        TestSpecies(trPlatinum, Species.Giratina, sav);

        // Test Kyogre from HeartGold (DS)
        TestSpecies(trHeartGold, Species.Kyogre, sav);

        // Test Groudon from SoulSilver (DS)
        TestSpecies(trSoulSilver, Species.Groudon, sav);

        // Test Rayquaza from HeartGold / SoulSilver (DS)
        TestSpecies(trHeartGold, Species.Rayquaza, sav);

        // Test Lugia from SoulSilver (DS)
        TestSpecies(trSoulSilver, Species.Lugia, sav);

        // Test Ho-Oh from HeartGold (DS)
        TestSpecies(trHeartGold, Species.HoOh, sav);

        // Test Entei from HeartGold (DS)
        TestSpecies(trHeartGold, Species.Entei, sav);

        // Test Raikou from SoulSilver (DS)
        TestSpecies(trSoulSilver, Species.Raikou, sav);

        // Test Suicune from HeartGold (DS)
        TestSpecies(trHeartGold, Species.Suicune, sav);

        // Test Reshiram from Black (DS)
        TestSpecies(trBlack, Species.Reshiram, sav);

        // Test Zekrom from White (DS)
        TestSpecies(trWhite, Species.Zekrom, sav);

        // Test Kyurem from Black 2 (DS)
        TestSpecies(trBlack2, Species.Kyurem, sav);
    }

    private static void TestSpecies(ITrainerInfo trDS, Species sp, SaveFile targetSav)
    {
        ushort s = (ushort)sp;
        if (trDS.GetRandomEncounter(s, 0, false, false, out var pk) && pk is not null)
        {
            var pk7 = EntityConverter.ConvertToType(pk, typeof(PK7), out _)!;
            pk7.OriginalTrainerName = trDS.OT;
            pk7.TID16 = targetSav.TID16;
            pk7.SID16 = targetSav.SID16;
            var la = new LegalityAnalysis(pk7, targetSav.Personal);
            string status = la.Valid ? "✅ LEGAL" : $"❌ INVALID: {la.Report()}";
            Console.WriteLine($"{sp,-12} from DS ({trDS.Version,-2}) -> Converted to PK7: {status} | Met: Location={pk7.MetLocation}, Level={pk7.MetLevel}");
        }
        else
        {
            Console.WriteLine($"{sp,-12} from DS ({trDS.Version,-2}) -> GetRandomEncounter FAILED");
        }
    }
}
