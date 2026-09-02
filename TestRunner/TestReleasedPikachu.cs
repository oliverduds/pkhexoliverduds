using System;
using System.IO;
using System.Linq;
using PKHeX.Core;

namespace TestRunner;

public static class TestReleasedPikachu
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(File.ReadAllBytes(path))!;
        string mgdb = @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex\mgdb\Released";

        string[] caps = [
            "*Original*Cap*Pikachu*ENG*.wc7*",
            "*Hoenn*Cap*Pikachu*ENG*.wc7*",
            "*Sinnoh*Cap*Pikachu*ENG*.wc7*",
            "*Unova*Cap*Pikachu*ENG*.wc7*",
            "*Kalos*Cap*Pikachu*ENG*.wc7*",
            "*Alola*Cap*Pikachu*ENG*.wc7*"
        ];

        foreach (var c in caps)
        {
            var file = Directory.GetFiles(mgdb, c, SearchOption.AllDirectories).FirstOrDefault();
            if (file != null)
            {
                var bytes = File.ReadAllBytes(file);
                var mg = MysteryGift.GetMysteryGift(bytes);
                var pkm = mg?.ConvertToPKM(sav);
                if (pkm != null)
                {
                    var la = new LegalityAnalysis(pkm, sav.Personal);
                    Console.WriteLine($"Cap {c,-30} -> Valid={la.Valid} | OT={pkm.OriginalTrainerName} | Form={pkm.Form}");
                }
            }
            else
            {
                Console.WriteLine($"[NOT FOUND] {c}");
            }
        }
    }
}
