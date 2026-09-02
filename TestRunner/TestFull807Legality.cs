using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class TestFull807Legality
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        Console.WriteLine($"Save: {sav.Version}, Trainer: {sav.OT} ({sav.TID16}/{sav.SID16})");

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NewestFirst;
        APILegality.SetMatchingBalls = false;
        APILegality.SetAllLegalRibbons = false;
        APILegality.ForceLevel100for50 = false;
        APILegality.SetBattleVersion = false;

        int consoleRegion = sav.ConsoleRegion;
        int country = sav.Country;
        int region = sav.Region;

        string vcOT = sav.OT.Length > 7 ? sav.OT[..7] : sav.OT;
        var trVC = new SimpleTrainerInfo(GameVersion.C) { OT = vcOT, TID16 = sav.TID16, SID16 = 0, Language = sav.Language };

        GameVersion mirrorVersion = sav.Version == GameVersion.US ? GameVersion.UM : GameVersion.US;
        var trMirror = new SimpleTrainerInfo(mirrorVersion)
        {
            OT = sav.OT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language,
            ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region
        };

        GameVersion orasVersion = sav.Version == GameVersion.US ? GameVersion.OR : GameVersion.AS;
        var trORAS = new SimpleTrainerInfo(orasVersion)
        {
            OT = sav.OT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language,
            ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region
        };

        int validCount = 0;
        int invalidCount = 0;
        var invalidList = new List<string>();

        for (int id = 1; id <= 807; id++)
        {
            var species = (Species)id;
            ushort s = (ushort)id;
            bool isEventMythical = IsEventOnlyMythical(species);

            PKM? resultPKM = null;

            if (!isEventMythical)
            {
                bool isMirror = sav.Version == GameVersion.US ? IsUltraMoonExclusive(species) : IsUltraSunExclusive(species);
                ITrainerInfo tr;

                if (isMirror)
                {
                    tr = trMirror; // Transferred from mirror counterpart version!
                }
                else if (s <= 251)
                {
                    tr = trVC; // VC Crystal
                }
                else if (s <= 386 && !IsUltraWormholeLegendary(species))
                {
                    tr = trORAS; // ORAS
                }
                else
                {
                    tr = sav; // Ultra Sun / Ultra Moon native!
                }

                if (species == Species.Latios)
                {
                    Console.WriteLine($"Latios: tr is sav? {ReferenceEquals(tr, sav)}, tr.Version={tr.Version}, getEnc={tr.GetRandomEncounter(s, 0, false, false, out var pkl)}, pkl is null? {pkl == null}");
                }

                if (tr.GetRandomEncounter(s, 0, false, false, out var pk) && pk is not null)
                {
                    resultPKM = pk is PK7 ? pk : EntityConverter.ConvertToType(pk, typeof(PK7), out _);
                    if (resultPKM is not null && !ReferenceEquals(tr, sav))
                    {
                        bool isVC = resultPKM.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
                        if (isVC)
                        {
                            resultPKM.OriginalTrainerName = vcOT;
                            resultPKM.TID16 = sav.TID16;
                            resultPKM.SID16 = 0;
                        }
                        else
                        {
                            resultPKM.OriginalTrainerName = sav.OT;
                            resultPKM.TID16 = sav.TID16;
                            resultPKM.SID16 = sav.SID16;
                        }
                    }
                }
            }

            if (resultPKM is null)
            {
                // Fallback / Event Mythicals
                string showdown = GameInfo.Strings.Species[s];
                var sset = new ShowdownSet(showdown);
                var set = new RegenTemplate(sset) { Nickname = string.Empty };
                var template = EntityBlank.GetBlank(sav);
                var res = sav.TryAPIConvert(set, template);
                if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
                {
                    resultPKM = res.Created;
                }
            }

            if (resultPKM is null)
            {
                invalidCount++;
                invalidList.Add($"{id} {species}: FAILED TO GENERATE");
                continue;
            }

            var la = new LegalityAnalysis(resultPKM, sav.Personal);
            if (la.Valid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                invalidList.Add($"{id} {species} (Orig: {resultPKM.Version}): {la.Report()}");
            }
        }

        Console.WriteLine($"\n==========================================");
        Console.WriteLine($"FULL 807 RESULTS: Valid = {validCount}/807 ({(validCount * 100.0 / 807):F1}%) | Invalid = {invalidCount}");
        Console.WriteLine($"==========================================");

        if (invalidList.Count > 0)
        {
            Console.WriteLine($"First {Math.Min(10, invalidList.Count)} Invalids:");
            foreach (var inv in invalidList.Take(10))
            {
                Console.WriteLine($"  ❌ {inv}");
            }
        }
    }

    private static bool IsUltraMoonExclusive(Species s) => s is
        Species.Lugia or Species.Entei or Species.Kyogre or Species.Latias or
        Species.Palkia or Species.Regigigas or Species.Zekrom or Species.Thundurus or
        Species.Yveltal or Species.Lunala or Species.Pheromosa or Species.Celesteela or Species.Stakataka;

    private static bool IsUltraSunExclusive(Species s) => s is
        Species.HoOh or Species.Raikou or Species.Groudon or Species.Latios or
        Species.Dialga or Species.Heatran or Species.Reshiram or Species.Tornadus or
        Species.Xerneas or Species.Solgaleo or Species.Buzzwole or Species.Kartana or Species.Blacephalon;

    private static bool IsUltraWormholeLegendary(Species s) =>
        IsUltraMoonExclusive(s) || IsUltraSunExclusive(s) || s is
        Species.Articuno or Species.Zapdos or Species.Moltres or Species.Mewtwo or
        Species.Suicune or Species.Regirock or Species.Regice or Species.Registeel or
        Species.Rayquaza or Species.Uxie or Species.Mesprit or Species.Azelf or
        Species.Giratina or Species.Cresselia or Species.Cobalion or Species.Terrakion or Species.Virizion or
        Species.Landorus or Species.Kyurem;

    private static bool IsEventOnlyMythical(Species s) => s is
        Species.Mew or Species.Jirachi or Species.Manaphy or Species.Phione or
        Species.Darkrai or Species.Shaymin or Species.Arceus or Species.Victini or
        Species.Keldeo or Species.Meloetta or Species.Genesect or Species.Diancie or
        Species.Hoopa or Species.Volcanion or Species.Marshadow or Species.Zeraora;
}
