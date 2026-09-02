using System;
using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class InspectUserSaveDetails
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        Console.WriteLine($"OT: {sav.OT}");
        Console.WriteLine($"TID: {sav.TID16}, SID: {sav.SID16}");
        Console.WriteLine($"Language: {sav.Language} ({(LanguageID)sav.Language})");
        Console.WriteLine($"ConsoleRegion: {sav.ConsoleRegion}");
        Console.WriteLine($"Country: {sav.Country}");
        Console.WriteLine($"Region: {sav.Region}");
        Console.WriteLine($"Game: {sav.Version}");

        // Now test TryAPIConvert for Pikachu with this exact save!
        TrainerSettings.Register(sav);
        var template = EntityBlank.GetBlank(sav);
        var sset = new ShowdownSet("Pikachu\nVersion: Crystal");
        var set = new RegenTemplate(sset);
        var res = sav.TryAPIConvert(set, template);
        Console.WriteLine($"TryAPIConvert for Pikachu: Status = {res.Status}, Created = {(res.Created != null ? "Yes" : "No")}");

        var sset2 = new ShowdownSet("Pikachu");
        var set2 = new RegenTemplate(sset2);
        var res2 = sav.TryAPIConvert(set2, template);
        Console.WriteLine($"TryAPIConvert for Native Pikachu: Status = {res2.Status}, Created = {(res2.Created != null ? "Yes" : "No")}");

        // Also test tr.GetRandomEncounter
        var trVC = new SimpleTrainerInfo(GameVersion.C) { OT = "Oliverd", TID16 = sav.TID16, SID16 = 0, Language = sav.Language };
        bool enc = trVC.GetRandomEncounter(25, 0, false, false, out var pk);
        Console.WriteLine($"trVC.GetRandomEncounter for Pikachu: {enc}, pk = {(pk != null ? pk.Species.ToString() : "null")}");
        if (pk != null)
        {
            var conv = EntityConverter.ConvertToType(pk, typeof(PK7), out _);
            Console.WriteLine($"Converted to PK7: {(conv != null ? "Yes" : "No")}");
        }
    }
}
