using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;

namespace AutoModPlugins;

/// <summary>
/// Regras, listas de encontros e filtros de Pokédex específicos para o relançamento de
/// Pokémon FireRed & LeafGreen no Nintendo Switch (eShop 2026).
/// Garante que espécies de Hoenn (que não existem no Switch) não sejam geradas nas caixas.
/// </summary>
public static class SwitchFRLGDex
{
    /// <summary>
    /// Pokémon exclusivos de FireRed (não capturáveis no LeafGreen sem trocas).
    /// </summary>
    public static readonly HashSet<ushort> FireRedExclusives =
    [
        (ushort)Species.Ekans,
        (ushort)Species.Arbok,
        (ushort)Species.Oddish,
        (ushort)Species.Gloom,
        (ushort)Species.Vileplume,
        (ushort)Species.Bellossom,
        (ushort)Species.Psyduck,
        (ushort)Species.Golduck,
        (ushort)Species.Growlithe,
        (ushort)Species.Arcanine,
        (ushort)Species.Shellder,
        (ushort)Species.Cloyster,
        (ushort)Species.Scyther,
        (ushort)Species.Scizor,
        (ushort)Species.Electabuzz,
        (ushort)Species.Elekid,
        (ushort)Species.Wooper,
        (ushort)Species.Quagsire,
        (ushort)Species.Murkrow,
        (ushort)Species.Qwilfish,
        (ushort)Species.Delibird,
        (ushort)Species.Skarmory,
    ];

    /// <summary>
    /// Pokémon exclusivos de LeafGreen (não capturáveis no FireRed sem trocas).
    /// </summary>
    public static readonly HashSet<ushort> LeafGreenExclusives =
    [
        (ushort)Species.Sandshrew,
        (ushort)Species.Sandslash,
        (ushort)Species.Vulpix,
        (ushort)Species.Ninetales,
        (ushort)Species.Bellsprout,
        (ushort)Species.Weepinbell,
        (ushort)Species.Victreebel,
        (ushort)Species.Slowpoke,
        (ushort)Species.Slowbro,
        (ushort)Species.Slowking,
        (ushort)Species.Staryu,
        (ushort)Species.Starmie,
        (ushort)Species.Magmar,
        (ushort)Species.Magby,
        (ushort)Species.Pinsir,
        (ushort)Species.Azurill,
        (ushort)Species.Marill,
        (ushort)Species.Azumarill,
        (ushort)Species.Misdreavus,
        (ushort)Species.Sneasel,
        (ushort)Species.Remoraid,
        (ushort)Species.Octillery,
        (ushort)Species.Mantine,
    ];

    /// <summary>
    /// Verifica se a espécie pertence à Pokédex Regional de Kanto (#001 Bulbasaur a #151 Mew).
    /// </summary>
    public static bool IsKanto(ushort species) => species is >= 1 and <= 151;

    /// <summary>
    /// Verifica se a espécie é nativa da versão de Switch de FRLG:
    /// - Kanto 151
    /// - Johto das Sevii Islands (152 a 251)
    /// - Deoxys (#386 - Birth Island / Aurora Ticket nativo no Switch)
    /// - Azurill (#298) e Wynaut (#360) gerados via Sea/Lax Incense em Sevii Islands
    /// Exclui todas as outras 133 espécies de Hoenn (Treecko, Mudkip, Rayquaza, etc.)
    /// </summary>
    public static bool IsFRLGNative(ushort species)
    {
        if (species is >= 1 and <= 251)
            return true;

        if (species is (ushort)Species.Deoxys or (ushort)Species.Azurill or (ushort)Species.Wynaut)
            return true;

        return false;
    }

    /// <summary>
    /// Verifica se a espécie é estritamente capturável na versão especificada (sem trocas).
    /// </summary>
    public static bool IsVersionNative(ushort species, GameVersion version)
    {
        if (!IsFRLGNative(species))
            return false;

        if (version == GameVersion.LG && FireRedExclusives.Contains(species))
            return false;

        if (version == GameVersion.FR && LeafGreenExclusives.Contains(species))
            return false;

        return true;
    }

    public static List<PKM> FilterKanto(IEnumerable<PKM> list)
    {
        return list.Where(p => IsKanto(p.Species)).ToList();
    }

    public static List<PKM> FilterFRLGNative(IEnumerable<PKM> list)
    {
        return list.Where(p => IsFRLGNative(p.Species)).ToList();
    }

    public static List<PKM> FilterVersionNative(IEnumerable<PKM> list, GameVersion version)
    {
        return list.Where(p => IsVersionNative(p.Species, version)).ToList();
    }
}
