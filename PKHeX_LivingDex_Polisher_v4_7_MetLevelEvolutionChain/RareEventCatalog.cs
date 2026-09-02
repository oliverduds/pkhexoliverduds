using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins;

public enum EventTier
{
    TierSPlus, // ⭐⭐⭐⭐⭐ S+ (Míticos de Cinema & Distribuições Históricas)
    TierS,     // 💎 S (Shinies Exclusivos de Evento / Shiny Locks)
    TierA,     // ⚔️ A (Formas Especiais & Golpes Exclusivos)
    TierB,     // 🏆 B (VGC & Campeonatos Mundiais)
}

public enum EventCategory
{
    All,
    Mythical,
    ShinyEvent,
    SpecialFormOrMove,
    WorldChampionshipsVGC,
}

public sealed class RareEventItem
{
    public string Id { get; init; } = string.Empty;
    public Species Species { get; init; }
    public byte Form { get; init; }
    public bool IsShiny { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string EventName { get; init; } = string.Empty;
    public int Year { get; init; }
    public string Region { get; init; } = "Global";
    public EventTier Tier { get; init; }
    public EventCategory Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public string KeyFeatures { get; init; } = string.Empty;
    public string ShowdownText { get; init; } = string.Empty;
    public string? WondercardFilePattern { get; init; }

    public string TierStars => Tier switch
    {
        EventTier.TierSPlus => "⭐⭐⭐⭐⭐ S+",
        EventTier.TierS => "⭐⭐⭐⭐ S",
        EventTier.TierA => "⭐⭐⭐ A",
        EventTier.TierB => "⭐⭐ B",
        _ => "⭐",
    };

    public string TierBadge => Tier switch
    {
        EventTier.TierSPlus => "👑 TIER S+ • MÍTICO CINEMA",
        EventTier.TierS => "💎 TIER S • SHINY EXCLUSIVO",
        EventTier.TierA => "⚔️ TIER A • FORMA / GOLPE ESPECIAL",
        EventTier.TierB => "🏆 TIER B • CAMPEONATO VGC",
        _ => "EVENTO RARO",
    };

    public bool IsCompatibleWith(SaveFile sav)
    {
        ushort s = (ushort)Species;
        return sav.Personal.IsSpeciesInGame(s);
    }

    public PKM? GeneratePKM(SaveFile sav)
    {
        // 1. If wondercard pattern is provided, search mgdb
        if (!string.IsNullOrEmpty(WondercardFilePattern))
        {
            var pkm = TryGenerateFromMGDB(sav, WondercardFilePattern);
            if (pkm != null) return pkm;
        }

        // 2. Otherwise generate via Showdown / ALM
        if (!string.IsNullOrWhiteSpace(ShowdownText))
        {
            var sset = new ShowdownSet(ShowdownText);
            var set = new RegenTemplate(sset) { Nickname = string.Empty };
            var template = EntityBlank.GetBlank(sav);
            var res = sav.TryAPIConvert(set, template);
            if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
                return res.Created;
        }

        return null;
    }

    private static PKM? TryGenerateFromMGDB(SaveFile sav, string pattern)
    {
        string[] searchDirs = [
            Path.Combine(Application.StartupPath, "mgdb", "Released"),
            Path.Combine(Application.StartupPath, "mgdb"),
            @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex\mgdb\Released",
            @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex\mgdb"
        ];

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;
            var file = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories).FirstOrDefault();
            if (file != null)
            {
                try
                {
                    var bytes = File.ReadAllBytes(file);
                    var mg = MysteryGift.GetMysteryGift(bytes);
                    if (mg != null)
                    {
                        var pkm = mg.ConvertToPKM(sav);
                        if (pkm != null) return pkm;
                    }
                }
                catch { }
            }
        }
        return null;
    }
}

public static class RareEventCatalog
{
    public static IReadOnlyList<RareEventItem> Items { get; } =
    [
        // ==========================================
        // 👑 TIER S+: MÍTICOS DE CINEMA & HISTÓRICOS
        // ==========================================
        new()
        {
            Id = "mew_gf20",
            Species = Species.Mew,
            DisplayName = "Mew (20th Anniversary)",
            EventName = "Pokémon 20th Anniversary Distribution",
            Year = 2016,
            Region = "América / Europa / Japão",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "Distribuído em comemoração aos 20 anos da franquia Pokémon pela GameStop e Nintendo Network.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: GF",
            ShowdownText = "Mew\nAbility: Synchronize\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Psychic\n- Aura Sphere\n- Nasty Plot\n- Roost",
        },
        new()
        {
            Id = "celebi_gf20",
            Species = Species.Celebi,
            DisplayName = "Celebi (20th Anniversary)",
            EventName = "Pokémon 20th Anniversary Distribution",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Guardião da Floresta distribuído via Nintendo Network com a clássica fita de evento.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: GF",
            ShowdownText = "Celebi\nAbility: Natural Cure\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Leaf Storm\n- Psychic\n- Recover\n- Dazzling Gleam",
        },
        new()
        {
            Id = "jirachi_gf20",
            Species = Species.Jirachi,
            DisplayName = "Jirachi (20th Anniversary)",
            EventName = "Pokémon 20th Anniversary Distribution",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Realizador de Desejos concedido em celebração aos 20 anos com o lendário golpe Doom Desire.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: GF",
            ShowdownText = "Jirachi\nAbility: Serene Grace\nEVs: 252 Atk / 4 SpD / 252 Spe\nJolly Nature\n- Iron Head\n- Play Rough\n- U-turn\n- Fire Punch",
        },
        new()
        {
            Id = "deoxys_plasma",
            Species = Species.Deoxys,
            DisplayName = "Deoxys (Plasma Distribution)",
            EventName = "Team Plasma Tie-in Event",
            Year = 2013,
            Region = "América / Europa",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Pokémon DNA distribuído com Nasty Plot e Dark Pulse com a insígnia da Team Plasma.",
            KeyFeatures = "Dusk Ball / Cherish Ball • Classic Ribbon • OT: Plasma",
            ShowdownText = "Deoxys\nAbility: Pressure\nEVs: 252 SpA / 4 SpD / 252 Spe\nNaive Nature\n- Psycho Boost\n- Superpower\n- Extreme Speed\n- Shadow Ball",
        },
        new()
        {
            Id = "darkrai_alamos",
            Species = Species.Darkrai,
            DisplayName = "Darkrai (Movie 10 - Alamos)",
            EventName = "The Rise of Darkrai Theatrical Distribution",
            Year = 2008,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O famoso Darkrai do filme com os golpes exclusivos dos deuses do tempo e espaço: Roar of Time e Spacial Rend!",
            KeyFeatures = "Golpes Exclusivos: Roar of Time & Spacial Rend • Classic Ribbon",
            ShowdownText = "Darkrai\nAbility: Bad Dreams\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Dark Void\n- Dark Pulse\n- Sludge Bomb\n- Nasty Plot",
        },
        new()
        {
            Id = "shaymin_gf20",
            Species = Species.Shaymin,
            DisplayName = "Shaymin (20th Anniversary)",
            EventName = "Pokémon 20th Anniversary Distribution",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Mítico da Gratidão em sua forma terrestial capaz de se transformar em Sky Forme com a Gracidea Flower.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: GF",
            ShowdownText = "Shaymin\nAbility: Natural Cure\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Seed Flare\n- Earth Power\n- Air Slash\n- Synthesis",
        },
        new()
        {
            Id = "arceus_michina",
            Species = Species.Arceus,
            DisplayName = "Arceus (Movie 12 - Michina)",
            EventName = "Arceus and the Jewel of Life Distribution",
            Year = 2009,
            Region = "Global / América / Japão",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Criador do Universo com Judgment, Roar of Time, Spacial Rend e Shadow Force em um único conjunto celestial.",
            KeyFeatures = "Golpes dos Três Deuses de Sinnoh • Cherish Ball • Classic Ribbon",
            ShowdownText = "Arceus\nAbility: Multitype\nEVs: 252 Atk / 4 Def / 252 Spe\nJolly Nature\n- Judgment\n- Extreme Speed\n- Earthquake\n- Swords Dance",
        },
        new()
        {
            Id = "victini_vcreate",
            Species = Species.Victini,
            DisplayName = "Victini (Movie 14 - V-Create)",
            EventName = "Black: Victini and Reshiram Theatrical Event",
            Year = 2011,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Mítico da Vitória com o golpe lendário V-Create (180 de poder) mais Fusion Flare e Fusion Bolt!",
            KeyFeatures = "Golpe Lendário: V-Create • Fusion Flare • Fusion Bolt • Cherish Ball",
            ShowdownText = "Victini\nAbility: Victory Star\nEVs: 252 Atk / 4 SpD / 252 Spe\nJolly Nature\n- V-create\n- Bolt Strike\n- U-turn\n- Zen Headbutt",
        },
        new()
        {
            Id = "keldeo_gf20",
            Species = Species.Keldeo,
            DisplayName = "Keldeo (20th Anniversary)",
            EventName = "Pokémon 20th Anniversary Distribution",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O quarto Espadachim da Justiça em sua forma canônica com Secret Sword e Hydro Pump.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: GF",
            ShowdownText = "Keldeo-Resolute\nAbility: Justified\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Secret Sword\n- Hydro Pump\n- Scald\n- Icy Wind",
        },
        new()
        {
            Id = "meloetta_gf20",
            Species = Species.Meloetta,
            DisplayName = "Meloetta (20th Anniversary)",
            EventName = "Pokémon 20th Anniversary Distribution",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "A cantora mítica que altera sua forma entre Aria e Pirouette através da canção Relic Song.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: GF",
            ShowdownText = "Meloetta\nAbility: Serene Grace\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Relic Song\n- Psychic\n- Hyper Voice\n- Focus Blast",
        },
        new()
        {
            Id = "diancie_hope",
            Species = Species.Diancie,
            DisplayName = "Diancie (Hope Event)",
            EventName = "Hope Diancie Distribution",
            Year = 2015,
            Region = "América / Europa",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "A Princesa dos Diamantes de Kalos com o golpe Diamond Storm e Moonblast.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: Hope",
            ShowdownText = "Diancie\nAbility: Clear Body\nEVs: 252 Atk / 4 SpA / 252 Spe\nNaive Nature\n- Diamond Storm\n- Moonblast\n- Earth Power\n- Stealth Rock",
        },
        new()
        {
            Id = "hoopa_mac",
            Species = Species.Hoopa,
            DisplayName = "Hoopa (McDonald's / Harry)",
            EventName = "Hoopa Theatrical & Restaurant Distribution",
            Year = 2015,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Mítico dos Anéis Dimensionais capaz de assumir a forma Hoopa Unbound com a Prison Bottle.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • Hyperspace Hole",
            ShowdownText = "Hoopa\nAbility: Magician\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Hyperspace Hole\n- Shadow Ball\n- Focus Blast\n- Nasty Plot",
        },
        new()
        {
            Id = "volcanion_helen",
            Species = Species.Volcanion,
            DisplayName = "Volcanion (Helen Event)",
            EventName = "Volcanion and the Mechanical Marvel",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "A única combinação de tipos Fogo e Água em toda a franquia com Steam Eruption.",
            KeyFeatures = "Golpe Assinatura: Steam Eruption • Cherish Ball • Classic Ribbon",
            ShowdownText = "Volcanion\nAbility: Water Absorb\nEVs: 252 SpA / 4 SpD / 252 Spe\nModest Nature\n- Steam Eruption\n- Flamethrower\n- Earth Power\n- Sludge Bomb",
        },
        new()
        {
            Id = "marshadow_mttensei",
            Species = Species.Marshadow,
            DisplayName = "Marshadow (Mt. Tensei)",
            EventName = "Pokémon I Choose You! Movie Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O Mítico das Sombras de Alola com o golpe Spectral Thief que rouba os aumentos de atributos do oponente!",
            KeyFeatures = "Golpe Exclusivo: Spectral Thief • Cherish Ball • Classic Ribbon",
            ShowdownText = "Marshadow\nAbility: Technician\nEVs: 252 Atk / 4 Def / 252 Spe\nJolly Nature\n- Spectral Thief\n- Close Combat\n- Shadow Sneak\n- Rock Tomb",
        },
        new()
        {
            Id = "zeraora_fulacity",
            Species = Species.Zeraora,
            DisplayName = "Zeraora (Fula City)",
            EventName = "The Power of Us Movie Distribution",
            Year = 2018,
            Region = "Global",
            Tier = EventTier.TierSPlus,
            Category = EventCategory.Mythical,
            Description = "O velocista elétrico de Alola com o golpe assinatura Plasma Fists que transforma ataques normais em elétricos.",
            KeyFeatures = "Golpe Assinatura: Plasma Fists • Cherish Ball • Classic Ribbon",
            ShowdownText = "Zeraora\nAbility: Volt Absorb\nEVs: 252 Atk / 4 SpA / 252 Spe\nNaive Nature\n- Plasma Fists\n- Close Combat\n- Grass Knot\n- Knock Off",
        },

        // ==========================================
        // 💎 TIER S: SHINIES EXCLUSIVOS DE EVENTO
        // ==========================================
        new()
        {
            Id = "tapu_koko_melemele",
            Species = Species.TapuKoko,
            IsShiny = true,
            DisplayName = "Shiny Tapu Koko (Melemele)",
            EventName = "Melemele Island Guardian Shiny Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "O lendário guardião elétrico de Melemele em sua forma Shiny preta e dourada exclusiva.",
            KeyFeatures = "Shiny Bloqueado no Jogo • Cherish Ball • Classic Ribbon • OT: Melemele",
            ShowdownText = "Tapu Koko\nShiny: Yes\nAbility: Electric Surge\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Nature's Madness\n- Discharge\n- Agility\n- Electro Ball",
        },
        new()
        {
            Id = "tapu_lele_akala",
            Species = Species.TapuLele,
            IsShiny = true,
            DisplayName = "Shiny Tapu Lele (Akala)",
            EventName = "2018 International Challenge Distribution",
            Year = 2018,
            Region = "Global",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "A guardiã de Akala em sua belíssima forma Shiny preta exclusiva distribuída aos competidores do torneio.",
            KeyFeatures = "Shiny Bloqueado no Jogo • Cherish Ball • Classic Ribbon • OT: Akala",
            ShowdownText = "Tapu Lele\nShiny: Yes\nAbility: Psychic Surge\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Nature's Madness\n- Extrasensory\n- Flatter\n- Moonblast",
        },
        new()
        {
            Id = "tapu_bulu_ulaula",
            Species = Species.TapuBulu,
            IsShiny = true,
            DisplayName = "Shiny Tapu Bulu (Ula'ula)",
            EventName = "2019 International Challenge Distribution",
            Year = 2019,
            Region = "Global",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "O robusto guardião de Ula'ula com casco preto e chifres dourados exclusivos de evento.",
            KeyFeatures = "Shiny Bloqueado no Jogo • Cherish Ball • Classic Ribbon • OT: Ula'ula",
            ShowdownText = "Tapu Bulu\nShiny: Yes\nAbility: Grassy Surge\nEVs: 252 Atk / 4 Def / 252 Spe\nAdamant Nature\n- Nature's Madness\n- Zen Headbutt\n- Megahorn\n- Wood Hammer",
        },
        new()
        {
            Id = "tapu_fini_poni",
            Species = Species.TapuFini,
            IsShiny = true,
            DisplayName = "Shiny Tapu Fini (Poni)",
            EventName = "2019 International Challenge Distribution",
            Year = 2019,
            Region = "Global",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "A elegante guardiã das águas de Poni em sua forma Shiny preta completando os 4 Guardiões de Alola.",
            KeyFeatures = "Shiny Bloqueado no Jogo • Cherish Ball • Classic Ribbon • OT: Poni",
            ShowdownText = "Tapu Fini\nShiny: Yes\nAbility: Misty Surge\nEVs: 248 HP / 252 Def / 8 Spe\nBold Nature\n- Nature's Madness\n- Muddy Water\n- Moonblast\n- Aqua Ring",
        },
        new()
        {
            Id = "poipole_ultra",
            Species = Species.Poipole,
            IsShiny = true,
            DisplayName = "Shiny Poipole (Ultra)",
            EventName = "Ultra Shiny Poipole Distribution",
            Year = 2018,
            Region = "Global",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "A simpática Ultra Beast em sua forma Shiny branca e dourada distribuída para Ultra Sun e Ultra Moon.",
            KeyFeatures = "Ultra Beast Shiny • Cherish Ball • Classic Ribbon • OT: Ultra",
            ShowdownText = "Poipole\nShiny: Yes\nAbility: Beast Boost\nEVs: 252 SpA / 4 SpD / 252 Spe\nModest Nature\n- Venom Drench\n- Nasty Plot\n- Poison Jab\n- Dragon Pulse",
        },
        new()
        {
            Id = "silvally_aether",
            Species = Species.Silvally,
            IsShiny = true,
            DisplayName = "Shiny Silvally (Aether)",
            EventName = "Aether Shiny Silvally Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "O Pokémon sintético com RKS System em sua forma Shiny com crista prateada distribuído pela fundação Aether.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: Aether",
            ShowdownText = "Silvally\nShiny: Yes\nAbility: RKS System\nEVs: 252 Atk / 4 SpA / 252 Spe\nJolly Nature\n- Multi-Attack\n- Parting Shot\n- Crunch\n- Iron Head",
        },
        new()
        {
            Id = "genesect_movie13",
            Species = Species.Genesect,
            IsShiny = true,
            DisplayName = "Shiny Genesect (Movie 13 - Red Genesect)",
            EventName = "Genesect and the Legend Awakened",
            Year = 2013,
            Region = "Japão",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "O lendário Genesect Vermelho com Shift Gear, Blaze Kick e Extreme Speed (golpes impossíveis no jogo comum)!",
            KeyFeatures = "Golpes Exclusivos: Shift Gear & Blaze Kick • Cherish Ball • Wishing Ribbon",
            ShowdownText = "Genesect\nShiny: Yes\nAbility: Download\nEVs: 252 Atk / 4 SpA / 252 Spe\nHasty Nature\n- Extreme Speed\n- Iron Head\n- Shift Gear\n- Blaze Kick",
        },
        new()
        {
            Id = "diancie_allstar",
            Species = Species.Diancie,
            IsShiny = true,
            DisplayName = "Shiny Diancie (Pokémon Center)",
            EventName = "Pokémon Center Mega Evolution Distribution",
            Year = 2015,
            Region = "Japão / Coréia",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "A deslumbrante princesa dos diamantes em sua forma brilhante ultra rara rosa e dourada.",
            KeyFeatures = "Mítico Shiny Exclusivo • Cherish Ball • Classic Ribbon",
            ShowdownText = "Diancie\nShiny: Yes\nAbility: Clear Body\nEVs: 252 Atk / 4 SpA / 252 Spe\nNaive Nature\n- Diamond Storm\n- Moonblast\n- Reflect\n- Return",
        },
        new()
        {
            Id = "jirachi_tanabata",
            Species = Species.Jirachi,
            IsShiny = true,
            DisplayName = "Shiny Jirachi (Tanabata Festival)",
            EventName = "Tohoku Pokémon Center Tanabata Festival",
            Year = 2014,
            Region = "Japão",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "A lendária estrela dos desejos em sua forma Shiny com Moonblast e Healing Wish distribuída no festival Tanabata.",
            KeyFeatures = "Mítico Shiny de Festival • Cherish Ball • Classic Ribbon • Golpe Moonblast",
            ShowdownText = "Jirachi\nShiny: Yes\nAbility: Serene Grace\nEVs: 252 Atk / 4 SpD / 252 Spe\nTimid Nature\n- Wish\n- Swift\n- Healing Wish\n- Moonblast",
        },
        new()
        {
            Id = "rayquaza_galileo",
            Species = Species.Rayquaza,
            IsShiny = true,
            DisplayName = "Shiny Rayquaza (Galileo)",
            EventName = "Galileo Shiny Rayquaza Distribution",
            Year = 2015,
            Region = "América / Europa",
            Tier = EventTier.TierS,
            Category = EventCategory.ShinyEvent,
            Description = "O Dragão dos Céus em sua majestosa forma Shiny negra com Dragon Ascent e Dragon Dance.",
            KeyFeatures = "Cherish Ball • Classic Ribbon • OT: Galileo",
            ShowdownText = "Rayquaza\nShiny: Yes\nAbility: Air Lock\nEVs: 252 Atk / 4 SpD / 252 Spe\nJolly Nature\n- Dragon Ascent\n- Dragon Claw\n- Extreme Speed\n- Dragon Dance",
        },

        // ==========================================
        // ⚔️ TIER A: FORMAS ESPECIAIS & GOLPES ÚNICOS
        // ==========================================
        new()
        {
            Id = "greninja_ash",
            Species = Species.Greninja,
            Form = 1,
            DisplayName = "Ash-Greninja (Battle Bond)",
            EventName = "Pokémon Sun & Moon Special Demo Version",
            Year = 2016,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "O lendário Greninja do Ash com a habilidade exclusiva Battle Bond que se transforma após nocautear um oponente!",
            KeyFeatures = "Forma Exclusiva Battle Bond • OT: Ash / Satoshi • Poké Ball",
            WondercardFilePattern = "*Ash*Greninja*.wc7*",
            ShowdownText = "Greninja\nAbility: Battle Bond\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Water Shuriken\n- Aerial Ace\n- Double Team\n- Night Slash",
        },
        new()
        {
            Id = "pikachu_original_cap",
            Species = Species.Pikachu,
            Form = 1,
            DisplayName = "Pikachu (Original Cap)",
            EventName = "20th Movie Ash Cap Pikachu Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "Pikachu usando o boné original do Ash em Kanto e Johto, capaz de usar o Z-Move exclusivo 10,000,000 Volt Thunderbolt!",
            KeyFeatures = "Boné Clássico de Kanto • Z-Move Exclusivo: 10,000,000 Volt Thunderbolt • OT: Ash",
            WondercardFilePattern = "*Original*Cap*Pikachu*ENG*.wc7*",
        },
        new()
        {
            Id = "pikachu_hoenn_cap",
            Species = Species.Pikachu,
            Form = 2,
            DisplayName = "Pikachu (Hoenn Cap)",
            EventName = "20th Movie Ash Cap Pikachu Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "Pikachu usando o boné da saga Advance Generation em Hoenn.",
            KeyFeatures = "Boné de Hoenn • Z-Move Exclusivo • OT: Ash",
            WondercardFilePattern = "*Hoenn*Cap*Pikachu*ENG*.wc7*",
        },
        new()
        {
            Id = "pikachu_sinnoh_cap",
            Species = Species.Pikachu,
            Form = 3,
            DisplayName = "Pikachu (Sinnoh Cap)",
            EventName = "20th Movie Ash Cap Pikachu Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "Pikachu usando o boné da saga Diamond & Pearl em Sinnoh.",
            KeyFeatures = "Boné de Sinnoh • Z-Move Exclusivo • OT: Ash",
            WondercardFilePattern = "*Sinnoh*Cap*Pikachu*ENG*.wc7*",
        },
        new()
        {
            Id = "pikachu_unova_cap",
            Species = Species.Pikachu,
            Form = 4,
            DisplayName = "Pikachu (Unova Cap)",
            EventName = "20th Movie Ash Cap Pikachu Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "Pikachu usando o boné da saga Best Wishes em Unova.",
            KeyFeatures = "Boné de Unova • Z-Move Exclusivo • OT: Ash",
            WondercardFilePattern = "*Unova*Cap*Pikachu*ENG*.wc7*",
        },
        new()
        {
            Id = "pikachu_kalos_cap",
            Species = Species.Pikachu,
            Form = 5,
            DisplayName = "Pikachu (Kalos Cap)",
            EventName = "20th Movie Ash Cap Pikachu Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "Pikachu usando o boné vermelho e branco da saga XY em Kalos.",
            KeyFeatures = "Boné de Kalos • Z-Move Exclusivo • OT: Ash",
            WondercardFilePattern = "*Kalos*Cap*Pikachu*ENG*.wc7*",
        },
        new()
        {
            Id = "pikachu_alola_cap",
            Species = Species.Pikachu,
            Form = 6,
            DisplayName = "Pikachu (Alola Cap)",
            EventName = "20th Movie Ash Cap Pikachu Distribution",
            Year = 2017,
            Region = "Global",
            Tier = EventTier.TierA,
            Category = EventCategory.SpecialFormOrMove,
            Description = "Pikachu usando o boné listrado da saga Sun & Moon em Alola.",
            KeyFeatures = "Boné de Alola • Z-Move Exclusivo • OT: Ash",
            WondercardFilePattern = "*Alola*Cap*Pikachu*ENG*.wc7*",
        },

        // ==========================================
        // 🏆 TIER B: CAMPEÕES MUNDIAIS & VGC
        // ==========================================
        new()
        {
            Id = "meloetta_worlds18",
            Species = Species.Meloetta,
            DisplayName = "Meloetta (Worlds 2018)",
            EventName = "2018 World Championships Distribution",
            Year = 2018,
            Region = "Nashville, Estados Unidos",
            Tier = EventTier.TierB,
            Category = EventCategory.WorldChampionshipsVGC,
            Description = "Distribuída exclusivamente no Campeonato Mundial em Nashville com Celebrate e Relic Song.",
            KeyFeatures = "Golpe Exclusivo: Celebrate • Cherish Ball • Event Ribbon • OT: Worlds18",
            ShowdownText = "Meloetta\nAbility: Serene Grace\nEVs: 252 SpA / 4 SpD / 252 Spe\nTimid Nature\n- Celebrate\n- Relic Song\n- Psychic\n- Energy Ball",
        },
        new()
        {
            Id = "arcanine_worlds17",
            Species = Species.Arcanine,
            DisplayName = "Arcanine (Worlds 2017)",
            EventName = "2017 World Championships Distribution",
            Year = 2017,
            Region = "Anaheim, Estados Unidos",
            Tier = EventTier.TierB,
            Category = EventCategory.WorldChampionshipsVGC,
            Description = "Distribuído aos participantes do Mundial em Anaheim baseado na estratégia dominante de VGC 2017.",
            KeyFeatures = "Cherish Ball • Battle Champion Ribbon • Intimidate",
            ShowdownText = "Arcanine (M)\nAbility: Intimidate\nEVs: 252 HP / 4 SpA / 252 SpD\nCalm Nature\n- Flare Blitz\n- Burn Up\n- Will-O-Wisp\n- Extreme Speed",
        },
    ];
}
