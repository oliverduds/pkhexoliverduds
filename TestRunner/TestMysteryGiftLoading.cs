using System;
using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestMysteryGiftLoading
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(File.ReadAllBytes(path))!;

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = false;
        APILegality.GameVersionPriority = GameVersionPriorityType.NewestFirst;

        // Test with Showdown sets
        TestShowdown("Tapu Koko\nShiny: Yes", sav);
        TestShowdown("Tapu Lele\nShiny: Yes", sav);
        TestShowdown("Tapu Bulu\nShiny: Yes", sav);
        TestShowdown("Tapu Fini\nShiny: Yes", sav);
        TestShowdown("Poipole\nShiny: Yes", sav);
        TestShowdown("Silvally\nShiny: Yes", sav);
        TestShowdown("Rayquaza\nShiny: Yes", sav);
        TestShowdown("Zeraora", sav);
        TestShowdown("Marshadow", sav);
        TestShowdown("Genesect\nShiny: Yes", sav);
        TestShowdown("Diancie\nShiny: Yes", sav);
        TestShowdown("Jirachi\nShiny: Yes", sav);
        TestShowdown("Mew", sav);
        TestShowdown("Celebi", sav);
        TestShowdown("Volcanion", sav);
        TestShowdown("Hoopa", sav);
        TestShowdown("Meloetta", sav);
        TestShowdown("Keldeo", sav);
        TestShowdown("Victini", sav);
    }

    private static void TestShowdown(string text, SaveFile sav)
    {
        var sset = new ShowdownSet(text);
        var set = new RegenTemplate(sset) { Nickname = string.Empty };
        var template = EntityBlank.GetBlank(sav);
        var res = sav.TryAPIConvert(set, template);
        if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
        {
            var pkm = res.Created;
            var la = new LegalityAnalysis(pkm, sav.Personal);
            string spName = GameInfo.Strings.Species[pkm.Species];
            Console.WriteLine($"[SUCCESS] {spName,-12} -> PK7: Lv.{pkm.CurrentLevel,3}, Shiny={pkm.IsShiny,-5}, Ball={(Ball)pkm.Ball,-8}, OT={pkm.OriginalTrainerName,-12} (TID={pkm.TID16}) | Valid={la.Valid}");
        }
        else
        {
            Console.WriteLine($"[FAILED] {text.Replace("\n", " ")}: {res.Status}");
        }
    }
}
