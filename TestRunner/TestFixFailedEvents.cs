using System;
using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestFixFailedEvents
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(File.ReadAllBytes(path))!;
        TrainerSettings.Register(sav);

        // Test Tanabata Shiny Jirachi (2014 / 2015)
        // Moves upon distribution: Wish, Swift, Healing Wish, Moonblast (2014) or Wish, Cosmic Power, Play Rough, Heart Stamp (2015)
        string[] testSets = [
            "Jirachi\nShiny: Yes",
            "Jirachi\nShiny: Yes\n- Wish\n- Swift\n- Healing Wish\n- Moonblast",
            "Jirachi\nShiny: Yes\n- Wish\n- Cosmic Power\n- Play Rough\n- Heart Stamp",
            "Jirachi\nShiny: Yes\n- Iron Head\n- Fire Punch\n- Ice Punch\n- Thunder Punch"
        ];

        foreach (var s in testSets)
        {
            var sset = new ShowdownSet(s);
            var set = new RegenTemplate(sset) { Nickname = string.Empty };
            var template = EntityBlank.GetBlank(sav);
            var res = sav.TryAPIConvert(set, template);
            Console.WriteLine($"Set '{s.Replace("\n", " ")}': {res.Status}");
            if (res.Created != null)
            {
                var la = new LegalityAnalysis(res.Created, sav.Personal);
                Console.WriteLine($"  Valid={la.Valid} | OT={res.Created.OriginalTrainerName} | TID={res.Created.TID16}");
            }
        }
    }
}
