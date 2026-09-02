using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public enum RegionalOriginKantoJohto
{
    VirtualConsole,
    NintendoDS_HGSS,
}

public enum RegionalOriginHoenn
{
    OmegaRubyAlphaSapphire,
    NintendoDS_HGSS,
}

public enum RegionalOriginSinnoh
{
    NintendoDS_PlatinumDP,
    Nintendo3DS_Alola,
}

public enum RegionalOriginUnova
{
    NintendoDS_BlackWhite,
    Nintendo3DS_Alola,
}

public enum RegionalOriginMirrorLegendary
{
    NintendoDS_Transfer,
    Nintendo3DS_MirrorVersion,
}

public class OriginSettings
{
    public RegionalOriginKantoJohto KantoJohto { get; set; } = RegionalOriginKantoJohto.VirtualConsole;
    public RegionalOriginHoenn Hoenn { get; set; } = RegionalOriginHoenn.OmegaRubyAlphaSapphire;
    public RegionalOriginSinnoh Sinnoh { get; set; } = RegionalOriginSinnoh.NintendoDS_PlatinumDP;
    public RegionalOriginUnova Unova { get; set; } = RegionalOriginUnova.NintendoDS_BlackWhite;
    public RegionalOriginMirrorLegendary Mirror { get; set; } = RegionalOriginMirrorLegendary.NintendoDS_Transfer;
}

public static class TestAllOriginOptions
{
    public static void Run()
    {
        string path = @"C:\Users\Eduardo\Desktop\saves\ultramon\original\main";
        var data = File.ReadAllBytes(path);
        var sav = (SAV7USUM)SaveUtil.GetSaveFile(data)!;

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NewestFirst;
        APILegality.SetMatchingBalls = false;
        APILegality.SetAllLegalRibbons = false;
        APILegality.ForceLevel100for50 = false;
        APILegality.SetBattleVersion = false;

        Console.WriteLine($"Testing 807 Legality with Custom Regional Origins Matrix on {sav.Version}:");

        // Preset 1: Canonical Perfect (VC Gen 1/2, ORAS Gen 3, DS Gen 4 & 5, 3DS Gen 6 & 7)
        var settings1 = new OriginSettings
        {
            KantoJohto = RegionalOriginKantoJohto.VirtualConsole,
            Hoenn = RegionalOriginHoenn.OmegaRubyAlphaSapphire,
            Sinnoh = RegionalOriginSinnoh.NintendoDS_PlatinumDP,
            Unova = RegionalOriginUnova.NintendoDS_BlackWhite,
            Mirror = RegionalOriginMirrorLegendary.NintendoDS_Transfer
        };
        TestPreset("Preset 1: Canônica Perfeita (VC 1/2 + ORAS 3 + DS 4/5 + 3DS 6/7)", sav, settings1);

        // Preset 2: Full Nintendo DS Nostalgia (HGSS 1/2, HGSS 3, Pt 4, BW 5)
        var settings2 = new OriginSettings
        {
            KantoJohto = RegionalOriginKantoJohto.NintendoDS_HGSS,
            Hoenn = RegionalOriginHoenn.NintendoDS_HGSS,
            Sinnoh = RegionalOriginSinnoh.NintendoDS_PlatinumDP,
            Unova = RegionalOriginUnova.NintendoDS_BlackWhite,
            Mirror = RegionalOriginMirrorLegendary.NintendoDS_Transfer
        };
        TestPreset("Preset 2: Nostalgia Nintendo DS (Gens 1 a 5 no DS via Poké Transfer)", sav, settings2);

        // Preset 3: 3DS Wormhole / Alola Native
        var settings3 = new OriginSettings
        {
            KantoJohto = RegionalOriginKantoJohto.VirtualConsole,
            Hoenn = RegionalOriginHoenn.OmegaRubyAlphaSapphire,
            Sinnoh = RegionalOriginSinnoh.Nintendo3DS_Alola,
            Unova = RegionalOriginUnova.Nintendo3DS_Alola,
            Mirror = RegionalOriginMirrorLegendary.Nintendo3DS_MirrorVersion
        };
        TestPreset("Preset 3: Coleção 3DS Nativa (Ultra Wormholes Alola)", sav, settings3);
    }

    private static void TestPreset(string title, SAV7USUM sav, OriginSettings opt)
    {
        Console.WriteLine($"\n--- {title} ---");
        int consoleRegion = sav.ConsoleRegion;
        int country = sav.Country;
        int region = sav.Region;

        string shortOT = sav.OT.Length > 7 ? sav.OT[..7] : sav.OT;
        var trVC = new SimpleTrainerInfo(GameVersion.C) { OT = shortOT, TID16 = sav.TID16, SID16 = 0, Language = sav.Language };

        // DS Trainers (OT max 7 chars)
        var trPt = new SimpleTrainerInfo(GameVersion.Pt) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trD = new SimpleTrainerInfo(GameVersion.D) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trP = new SimpleTrainerInfo(GameVersion.P) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trHG = new SimpleTrainerInfo(GameVersion.HG) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trSS = new SimpleTrainerInfo(GameVersion.SS) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trB = new SimpleTrainerInfo(GameVersion.B) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trW = new SimpleTrainerInfo(GameVersion.W) { OT = shortOT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };

        // 3DS Trainers (OT up to 12 chars)
        var trOR = new SimpleTrainerInfo(GameVersion.OR) { OT = sav.OT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trAS = new SimpleTrainerInfo(GameVersion.AS) { OT = sav.OT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        GameVersion mirrorVersion = sav.Version == GameVersion.US ? GameVersion.UM : GameVersion.US;
        var trMirror = new SimpleTrainerInfo(mirrorVersion) { OT = sav.OT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };
        var trX = new SimpleTrainerInfo(GameVersion.X) { OT = sav.OT, TID16 = sav.TID16, SID16 = sav.SID16, Language = sav.Language, ConsoleRegion = (byte)consoleRegion, Country = (byte)country, Region = (byte)region };

        int valid = 0;
        int invalid = 0;
        var invalidList = new List<string>();

        for (int id = 1; id <= 807; id++)
        {
            var species = (Species)id;
            ushort s = (ushort)id;
            bool isEventMythical = IsEventOnlyMythical(species);

            PKM? resultPKM = null;

            if (!isEventMythical)
            {
                ITrainerInfo tr;

                // 1. Kanto & Johto (#001-#251)
                if (s <= 251)
                {
                    tr = opt.KantoJohto == RegionalOriginKantoJohto.VirtualConsole ? trVC : trHG;
                }
                // 2. Hoenn (#252-#386)
                else if (s <= 386)
                {
                    if (opt.Hoenn == RegionalOriginHoenn.OmegaRubyAlphaSapphire)
                    {
                        if (species is Species.Kyogre or Species.Latias) tr = trMirror; // Ultra Moon / Alpha Sapphire
                        else if (species is Species.Groudon or Species.Latios) tr = sav; // Ultra Sun / Omega Ruby
                        else tr = trOR;
                    }
                    else
                    {
                        if (species is Species.Kyogre) tr = trHG;
                        else if (species is Species.Groudon) tr = trSS;
                        else tr = trHG;
                    }
                }
                // 3. Sinnoh (#387-#493)
                else if (s <= 493)
                {
                    if (opt.Sinnoh == RegionalOriginSinnoh.NintendoDS_PlatinumDP)
                    {
                        if (species is Species.Palkia) tr = trP;
                        else if (species is Species.Dialga) tr = trD;
                        else tr = trPt;
                    }
                    else
                    {
                        if (species is Species.Palkia or Species.Regigigas) tr = trMirror;
                        else if (species is Species.Dialga or Species.Heatran) tr = sav;
                        else tr = trPt;
                    }
                }
                // 4. Unova (#494-#649)
                else if (s <= 649)
                {
                    if (opt.Unova == RegionalOriginUnova.NintendoDS_BlackWhite)
                    {
                        if (species is Species.Reshiram or Species.Tornadus) tr = trB;
                        else if (species is Species.Zekrom or Species.Thundurus) tr = trW;
                        else if (species is Species.Cobalion or Species.Terrakion or Species.Virizion or Species.Landorus or Species.Kyurem) tr = sav;
                        else tr = trB;
                    }
                    else
                    {
                        if (species is Species.Zekrom or Species.Thundurus) tr = trMirror;
                        else if (species is Species.Reshiram or Species.Tornadus) tr = sav;
                        else tr = trB;
                    }
                }
                // 5. Kalos (#650-#721)
                else if (s <= 721)
                {
                    if (species is Species.Scatterbug or Species.Spewpa or Species.Vivillon)
                        tr = sav;
                    else
                        tr = trX;
                }
                // 6. Alola (#722-#807)
                else
                {
                    if (IsUltraMoonExclusive(species))
                        tr = sav.Version == GameVersion.US ? trMirror : sav;
                    else
                        tr = sav;
                }

                if (tr.GetRandomEncounter(s, 0, false, false, out var pk) && pk is not null)
                {
                    resultPKM = pk is PK7 ? pk : EntityConverter.ConvertToType(pk, typeof(PK7), out _);
                    if (resultPKM is not null && !ReferenceEquals(tr, sav))
                    {
                        bool isVC = resultPKM.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
                        bool isDS = resultPKM.Version is GameVersion.D or GameVersion.P or GameVersion.Pt or GameVersion.HG or GameVersion.SS or GameVersion.B or GameVersion.W or GameVersion.B2 or GameVersion.W2;

                        if (isVC)
                        {
                            resultPKM.OriginalTrainerName = shortOT;
                            resultPKM.TID16 = sav.TID16;
                            resultPKM.SID16 = 0;
                        }
                        else if (isDS)
                        {
                            resultPKM.OriginalTrainerName = shortOT;
                            resultPKM.TID16 = sav.TID16;
                            resultPKM.SID16 = sav.SID16;
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
                invalid++;
                invalidList.Add($"{id} {species}: FAILED");
                continue;
            }

            var la = new LegalityAnalysis(resultPKM, sav.Personal);
            if (la.Valid) valid++;
            else
            {
                invalid++;
                invalidList.Add($"{id} {species} ({resultPKM.Version}): {la.Report()}");
            }
        }

        Console.WriteLine($"Result: Valid = {valid}/807 ({(valid * 100.0 / 807):F1}%) | Invalid = {invalid}");
        if (invalidList.Count > 0)
        {
            Console.WriteLine($"  First {Math.Min(5, invalidList.Count)} Invalids:");
            foreach (var inv in invalidList.Take(5))
                Console.WriteLine($"    ❌ {inv}");
        }
    }

    private static bool IsUltraMoonExclusive(Species s) => s is
        Species.Lugia or Species.Entei or Species.Kyogre or Species.Latias or
        Species.Palkia or Species.Regigigas or Species.Zekrom or Species.Thundurus or
        Species.Yveltal or Species.Lunala or Species.Pheromosa or Species.Celesteela or Species.Stakataka;

    private static bool IsEventOnlyMythical(Species s) => s is
        Species.Mew or Species.Jirachi or Species.Manaphy or Species.Phione or
        Species.Darkrai or Species.Shaymin or Species.Arceus or Species.Victini or
        Species.Keldeo or Species.Meloetta or Species.Genesect or Species.Diancie or
        Species.Hoopa or Species.Volcanion or Species.Marshadow or Species.Zeraora;
}
