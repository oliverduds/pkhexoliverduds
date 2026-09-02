using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins;

public enum LivingDexMode
{
    Normal,
    Shiny,
    Combined,
    BaseSpeciesOnly,
    Gen7CanonicalNormal,
    Gen7CanonicalShiny
}

public enum BallSelectionPreference
{
    ThematicAuto,
    StandardPokeBall,
    PremierBall,
    LuxuryBall,
    UltraBall,
    RandomApriball,
    KeepOriginal
}

public enum IVOptimizationPreference
{
    SmartIVs,
    All31,
    KeepEncounter
}

public enum LevelPreference
{
    CanonicalFloor,
    EncounterNative,
    Level100
}

public enum BoxPlacementPreference
{
    Overwrite,
    EmptySlotsOnly,
    ClearBoxesFirst
}

public sealed class LivingDexCustomOptions
{
    public LivingDexMode Mode { get; set; } = LivingDexMode.Normal;
    public bool IncludeForms { get; set; } = true;
    public bool RespectShinyLocks { get; set; } = true;
    public BallSelectionPreference BallPreference { get; set; } = BallSelectionPreference.ThematicAuto;
    public IVOptimizationPreference IVPreference { get; set; } = IVOptimizationPreference.SmartIVs;
    public LevelPreference LevelPref { get; set; } = LevelPreference.CanonicalFloor;
    public bool EnableGigantamax { get; set; } = true;
    public int StartBox { get; set; } = 1;
    public BoxPlacementPreference BoxPreference { get; set; } = BoxPlacementPreference.Overwrite;
    public bool ExportReport { get; set; } = true;
}

/// <summary>
/// Universal Living Dex Generator & Customizer for Nintendo Switch & 3DS games (Gen 7 USUM/SM, LGPE, SWSH, BDSP, PLA, SV, ZA).
/// Features:
/// 1) Dedicated Gen 7 Canonical Multi-Origin Living Dex (VC Gens 1/2 Game Boy mark, ORAS Gen 3 Pentagon mark, Gen 4/5/6/7, and Official Events).
/// 2) Universal Switch generation (Normal, Shiny, Combined, Base Species).
/// 3) Real-time visual progress bar with detailed Pokémon card, stats, and safe cancellation.
/// 4) Full customization: Pokéballs (Thematic auto, standard, premier, luxury, apriballs), Smart IVs, Canonical evolution levels, G-Max.
/// 5) Capacity preview & overflow handler.
/// </summary>
public sealed class CombinedLivingDex : AutoModPlugin
{
    public override string Name => "Living Dex Generator & Customizer";
    public override int Priority => 1;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var root = new ToolStripMenuItem("Living Dex Generator")
        {
            Name = "Menu_LivingDexGenerator"
        };

        var customItem = new ToolStripMenuItem("⭐ Configure & Generate Living Dex (Custom Wizard)...");
        customItem.Font = new Font(customItem.Font, FontStyle.Bold);
        customItem.Click += async (_, _) => await OpenCustomWizard();

        var gen7Normal = new ToolStripMenuItem("🎮 Gen 7: Generate Normal Canonical Origin Living Dex (VC + ORAS + Events)");
        gen7Normal.Click += async (_, _) => await GenerateQuick(LivingDexMode.Gen7CanonicalNormal);

        var gen7Shiny = new ToolStripMenuItem("✨ Gen 7: Generate Shiny Canonical Origin Living Dex (VC Shiny Celebi + Legal Shinies)");
        gen7Shiny.Click += async (_, _) => await GenerateQuick(LivingDexMode.Gen7CanonicalShiny);

        var quickNormal = new ToolStripMenuItem("📦 Quick: Generate Normal Living Dex");
        quickNormal.Click += async (_, _) => await GenerateQuick(LivingDexMode.Normal);

        var quickShiny = new ToolStripMenuItem("✨ Quick: Generate Shiny Living Dex");
        quickShiny.Click += async (_, _) => await GenerateQuick(LivingDexMode.Shiny);

        var quickCombined = new ToolStripMenuItem("🌟 Quick: Generate Normal + Shiny Living Dex (Combined)");
        quickCombined.Click += async (_, _) => await GenerateQuick(LivingDexMode.Combined);

        root.DropDownItems.Add(customItem);
        root.DropDownItems.Add(new ToolStripSeparator());
        root.DropDownItems.Add(gen7Normal);
        root.DropDownItems.Add(gen7Shiny);
        root.DropDownItems.Add(new ToolStripSeparator());
        root.DropDownItems.Add(quickNormal);
        root.DropDownItems.Add(quickShiny);
        root.DropDownItems.Add(quickCombined);

        modmenu.DropDownItems.Add(root);
    }

    private async Task OpenCustomWizard()
    {
        var sav = SaveFileEditor.SAV;
        using var form = new LivingDexOptionsForm(sav);
        if (form.ShowDialog() == DialogResult.OK)
        {
            await ExecuteGeneration(form.Options);
        }
    }

    private async Task GenerateQuick(LivingDexMode mode)
    {
        var sav = SaveFileEditor.SAV;
        var options = new LivingDexCustomOptions
        {
            Mode = mode,
            IncludeForms = true,
            RespectShinyLocks = true,
            BallPreference = BallSelectionPreference.ThematicAuto,
            IVPreference = IVOptimizationPreference.SmartIVs,
            LevelPref = LevelPreference.CanonicalFloor,
            EnableGigantamax = true,
            StartBox = 1,
            BoxPreference = BoxPlacementPreference.Overwrite,
            ExportReport = true
        };

        int totalCapacity = sav.BoxCount * sav.BoxSlotCount;
        if (mode == LivingDexMode.Combined)
        {
            int estimatedCount = (sav.Version is GameVersion.SW or GameVersion.SH) ? 1520 : (sav.Version is GameVersion.SL or GameVersion.VL) ? 1450 : sav.Generation == 7 ? 1700 : 0;
            if (estimatedCount > totalCapacity)
            {
                var prompt = MessageBox.Show(
                    $"Atenção: A coleção Normal + Shiny completa possui ~{estimatedCount} Pokémon, mas o jogo suporta {totalCapacity} slots ({sav.BoxCount} caixas).\n\n" +
                    "Deseja abrir o Assistente de Configuração para personalizar caixas e opções?",
                    "Capacidade de Caixas",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (prompt == DialogResult.Yes)
                {
                    await OpenCustomWizard();
                    return;
                }
            }
        }

        await ExecuteGeneration(options);
    }

    private async Task ExecuteGeneration(LivingDexCustomOptions options)
    {
        var sav = SaveFileEditor.SAV;
        int totalCapacity = sav.BoxCount * sav.BoxSlotCount;

        bool oldUseTrainerData = APILegality.UseTrainerData;
        bool oldMatchingBalls = APILegality.SetMatchingBalls;
        bool oldRibbons = APILegality.SetAllLegalRibbons;
        bool oldForce100 = APILegality.ForceLevel100for50;
        bool oldBattleVersion = APILegality.SetBattleVersion;
        var oldPriority = APILegality.GameVersionPriority;

        try
        {
            TrainerSettings.Register(sav);
            APILegality.UseTrainerData = true;
            APILegality.GameVersionPriority = GameVersionPriorityType.NativeOnly;
            APILegality.SetMatchingBalls = false;
            APILegality.SetAllLegalRibbons = false;
            APILegality.ForceLevel100for50 = false;
            APILegality.SetBattleVersion = false;

            Cursor.Current = Cursors.WaitCursor;

            List<PKM> targetList = [];

            if (options.Mode is LivingDexMode.Gen7CanonicalNormal or LivingDexMode.Gen7CanonicalShiny)
            {
                bool isShiny = options.Mode == LivingDexMode.Gen7CanonicalShiny;
                targetList = await Task.Run(() => GenerateGen7CanonicalDexList(sav, isShiny, options));
            }
            else
            {
                List<PKM> normalList = [];
                List<PKM> shinyList = [];

                if (options.Mode is LivingDexMode.Normal or LivingDexMode.Combined or LivingDexMode.BaseSpeciesOnly)
                {
                    var normalCfg = new LivingDexConfig
                    {
                        IncludeForms = options.Mode != LivingDexMode.BaseSpeciesOnly && options.IncludeForms,
                        SetShiny = false,
                        SetAlpha = false,
                        TransferVersion = sav.Version,
                    };
                    var gen = await Task.Run(() => sav.GenerateLivingDex(sav.Personal, normalCfg));
                    normalList = FilterValidBoxPokemon(gen, sav).ToList();
                }

                if (options.Mode is LivingDexMode.Shiny or LivingDexMode.Combined or LivingDexMode.BaseSpeciesOnly)
                {
                    var shinyCfg = new LivingDexConfig
                    {
                        IncludeForms = options.Mode != LivingDexMode.BaseSpeciesOnly && options.IncludeForms,
                        SetShiny = true,
                        SetAlpha = false,
                        TransferVersion = sav.Version,
                    };
                    var gen = await Task.Run(() => sav.GenerateLivingDex(sav.Personal, shinyCfg).Where(z => z.IsShiny));
                    shinyList = FilterValidBoxPokemon(gen, sav).ToList();
                }

                targetList = options.Mode switch
                {
                    LivingDexMode.Normal => normalList,
                    LivingDexMode.Shiny => shinyList,
                    LivingDexMode.Combined => [.. normalList, .. shinyList],
                    LivingDexMode.BaseSpeciesOnly => normalList,
                    _ => normalList,
                };
            }

            if (targetList.Count == 0)
            {
                WinFormsUtil.Alert("Aviso", "Nenhum Pokémon pôde ser gerado para a configuração selecionada.");
                return;
            }

            int startSlot = Math.Max(0, (options.StartBox - 1) * sav.BoxSlotCount);
            if (sav.Version is GameVersion.GP or GameVersion.GE && startSlot == 0)
            {
                startSlot = GetLivingDexStartSlot(sav);
            }

            var plannedSlots = new List<int>();
            int maxSlot = sav.BoxCount * sav.BoxSlotCount;

            for (int idx = startSlot; idx < maxSlot && plannedSlots.Count < targetList.Count; idx++)
            {
                if (IsProtectedGameplaySlot(sav, idx))
                    continue;

                if (options.BoxPreference == BoxPlacementPreference.EmptySlotsOnly)
                {
                    if (sav.GetBoxSlotAtIndex(idx).Species != 0)
                        continue;
                }

                plannedSlots.Add(idx);
            }

            if (plannedSlots.Count == 0)
            {
                WinFormsUtil.Alert("Nenhum slot gravável disponível no intervalo selecionado.");
                return;
            }

            int toPlaceCount = Math.Min(targetList.Count, plannedSlots.Count);
            var overflowList = targetList.Skip(toPlaceCount).ToList();

            if (options.BoxPreference == BoxPlacementPreference.ClearBoxesFirst)
            {
                int endBox = (plannedSlots[^1] / sav.BoxSlotCount) + 1;
                for (int b = options.StartBox - 1; b < endBox && b < sav.BoxCount; b++)
                {
                    for (int s = 0; s < sav.BoxSlotCount; s++)
                    {
                        if (!IsProtectedGameplaySlot(sav, (b * sav.BoxSlotCount) + s))
                            sav.SetBoxSlotAtIndex(sav.BlankPKM, b, s);
                    }
                }
            }

            using var cts = new CancellationTokenSource();
            string modeName = options.Mode switch
            {
                LivingDexMode.Gen7CanonicalNormal => "Gen 7 Normal Canonical Origin Living Dex",
                LivingDexMode.Gen7CanonicalShiny => "Gen 7 Shiny Canonical Origin Living Dex",
                LivingDexMode.Normal => "Normal Living Dex",
                LivingDexMode.Shiny => "Shiny Living Dex",
                LivingDexMode.Combined => "Normal + Shiny Living Dex",
                LivingDexMode.BaseSpeciesOnly => "Base Species Living Dex",
                _ => "Living Dex",
            };

            var progressForm = new LivingDexProgressForm($"Gerando {modeName} — {sav.Version}", toPlaceCount, cts);
            progressForm.Show();

            int placed = 0;
            int themedBallsCount = 0;
            int gmaxCount = 0;
            var logLines = new List<string>();

            await Task.Run(() =>
            {
                var random = new Random();
                for (int i = 0; i < toPlaceCount; i++)
                {
                    if (cts.IsCancellationRequested)
                        break;

                    var pk = targetList[i];
                    int targetIndex = plannedSlots[i];
                    int box = targetIndex / sav.BoxSlotCount;
                    int slot = targetIndex % sav.BoxSlotCount;

                    // 1. Level adjustment
                    if (options.LevelPref == LevelPreference.Level100)
                    {
                        pk.CurrentLevel = 100;
                        LivingDexPolisher.RefreshLevelDependentData(pk);
                    }
                    else if (options.LevelPref == LevelPreference.CanonicalFloor)
                    {
                        var la = new LegalityAnalysis(pk, sav.Personal);
                        int targetLevel = LivingDexPolisher.GetEvolutionMinimumLevel(pk, la, sav.Personal);
                        pk.CurrentLevel = (byte)targetLevel;

                        if (pk.CurrentLevel < 100 && pk is IHyperTrain ht)
                            ht.HyperTrainFlags = 0;

                        LivingDexPolisher.TryApplyNaturalMoves(pk, sav.Personal);

                        var testLA = new LegalityAnalysis(pk, sav.Personal);
                        if (!testLA.Valid)
                        {
                            for (int lvl = targetLevel + 1; lvl <= 100; lvl++)
                            {
                                pk.CurrentLevel = (byte)lvl;
                                LivingDexPolisher.TryApplyNaturalMoves(pk, sav.Personal);
                                if (new LegalityAnalysis(pk, sav.Personal).Valid)
                                    break;
                            }
                        }
                    }

                    if (pk.CurrentLevel < 100 && pk is IHyperTrain ht2)
                        ht2.HyperTrainFlags = 0;

                    // 2. Pokéball selection (Preserve Cherish ball and VC Poke ball)
                    bool isVC = pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
                    if (pk.Ball != (byte)Ball.Cherish && !isVC)
                    {
                        ApplyCustomBall(pk, options.BallPreference, random, sav.Personal);
                        if (options.BallPreference == BallSelectionPreference.ThematicAuto)
                            themedBallsCount++;
                    }

                    // 3. IVs
                    ApplyCustomIVs(pk, options.IVPreference, sav.Personal);

                    // 4. Gigantamax (SWSH)
                    if (options.EnableGigantamax && LivingDexPolisher.TryApplyGigantamax(pk, sav))
                        gmaxCount++;

                    // 5. Reset CP (LGPE)
                    LivingDexPolisher.RefreshLevelDependentData(pk);

                    sav.SetBoxSlotAtIndex(pk, box, slot);
                    placed++;

                    string logEntry = $"Box {box + 1,2}, Slot {slot + 1,2}: {pk.Species,3} {GameInfo.Strings.Species[pk.Species],-16} {(pk.IsShiny ? "★" : " ")} Lv.{pk.CurrentLevel,3} | Origin: {pk.Version,-6} | Ball: {(Ball)pk.Ball,-10} | IVs: {pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
                    logLines.Add(logEntry);

                    progressForm.UpdateProgress(placed, toPlaceCount, pk, box, slot);
                }
            });

            progressForm.IsCompleted = true;
            progressForm.Close();
            SaveFileEditor.ReloadSlots();

            if (cts.IsCancellationRequested)
            {
                WinFormsUtil.Alert("Geração cancelada pelo usuário.", $"Foram inseridos {placed} Pokémon antes do cancelamento.");
                return;
            }

            int firstBoxNum = (plannedSlots[0] / sav.BoxSlotCount) + 1;
            int lastBoxNum = (plannedSlots[placed - 1] / sav.BoxSlotCount) + 1;

            if (options.ExportReport)
            {
                ExportGenerationReport(sav, logLines, firstBoxNum, lastBoxNum, placed, themedBallsCount, gmaxCount);
            }

            if (overflowList.Count > 0)
            {
                var saveExtra = MessageBox.Show(
                    $"As caixas do save foram preenchidas com {placed} Pokémon (Caixas {firstBoxNum} a {lastBoxNum}).\n" +
                    $"Restaram {overflowList.Count} Pokémon que não couberam nos slots disponíveis.\n\n" +
                    "Deseja salvar os Pokémon excedentes em uma pasta de arquivos?",
                    "Salvar Pokémon Excedentes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (saveExtra == DialogResult.Yes)
                {
                    using var fbd = new FolderBrowserDialog();
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        Span<byte> pokeData = stackalloc byte[sav.SIZE_PARTY];
                        foreach (var pk in overflowList)
                        {
                            pk.WriteDecryptedDataParty(pokeData);
                            string shinyStr = pk.IsShiny ? " ★" : "";
                            string fileName = $"{pk.Species:D3} - {GameInfo.Strings.Species[pk.Species]}{shinyStr}.{sav.Extension}";
                            File.WriteAllBytes(Path.Combine(fbd.SelectedPath, fileName), pokeData.ToArray());
                        }
                        WinFormsUtil.Alert("Exportação Concluída", $"{overflowList.Count} Pokémon salvos em:\n{fbd.SelectedPath}");
                    }
                }
            }
            else
            {
                WinFormsUtil.Alert(
                    $"{modeName} gerada com sucesso!",
                    $"Total de Pokémon inseridos: {placed} (Caixas {firstBoxNum} a {lastBoxNum})",
                    $"Treinador Original: {sav.OT} (TID: {sav.TID16})",
                    "Origens Canônicas aplicadas: VC (Game Boy Mark), ORAS (Pentágono), Alola (Trevo) e Eventos Oficiais.",
                    "Slots de Gameplay e Party preservados intactos.");
            }
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error("Falha na geração da Living Dex.", ex.Message);
        }
        finally
        {
            APILegality.UseTrainerData = oldUseTrainerData;
            APILegality.SetMatchingBalls = oldMatchingBalls;
            APILegality.SetAllLegalRibbons = oldRibbons;
            APILegality.ForceLevel100for50 = oldForce100;
            APILegality.SetBattleVersion = oldBattleVersion;
            APILegality.GameVersionPriority = oldPriority;
        }
    }

    public static List<PKM> GenerateGen7CanonicalDexList(SaveFile sav, bool isShiny, LivingDexCustomOptions options)
    {
        var bag = new ConcurrentBag<PKM>();
        var personal = sav.Personal;
        int maxSpecies = Math.Min(807, (int)personal.MaxSpeciesID);

        int consoleRegion = (sav as SAV7)?.ConsoleRegion ?? 1;
        int country = (sav as SAV7)?.Country ?? 49;
        int region = (sav as SAV7)?.Region ?? 1;

        string vcOT = sav.OT.Length > 7 ? sav.OT[..7] : sav.OT;
        var trVC = new SimpleTrainerInfo(GameVersion.C)
        {
            OT = vcOT,
            TID16 = sav.TID16,
            SID16 = 0,
            Language = sav.Language,
            ConsoleRegion = (byte)consoleRegion,
            Country = (byte)country,
            Region = (byte)region,
        };

        var trORAS = new SimpleTrainerInfo(GameVersion.OR)
        {
            OT = sav.OT,
            TID16 = sav.TID16,
            SID16 = sav.SID16,
            Language = sav.Language,
            ConsoleRegion = (byte)consoleRegion,
            Country = (byte)country,
            Region = (byte)region,
        };

        Parallel.For(1, maxSpecies + 1, id =>
        {
            var s = (ushort)id;
            if (!personal.IsSpeciesInGame(s))
                return;

            var species = (Species)s;
            bool isMythical = IsEventOnlyMythical(species);

            PKM? resultPKM = null;

            // Direct encounter generation
            if (!isMythical)
            {
                bool isBoxLegendary = IsUltraWormholeLegendary(species);
                ITrainerInfo tr = (s <= 251 && !isBoxLegendary) ? trVC : (s <= 386 && !isBoxLegendary) ? trORAS : sav;
                bool allowShiny = isShiny && !SimpleEdits.IsShinyLockedSpeciesForm(s, 0) && !IsShinyLockedMythicalGen7(species);

                if (tr.GetRandomEncounter(s, 0, allowShiny, false, out var pk) && pk is not null)
                {
                    resultPKM = EntityConverter.ConvertToType(pk, typeof(PK7), out _);
                    if (resultPKM is not null)
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

            // Fallback / Event Mythical generation
            if (resultPKM is null)
            {
                var origin = GetCanonicalOriginVersion(species, sav.Version);
                resultPKM = GenerateOriginPKM(sav, s, 0, isShiny, origin);
            }

            if (resultPKM is not null)
                bag.Add(resultPKM);
        });

        return bag.OrderBy(z => z.Species).ToList();
    }

    private static bool IsUltraWormholeLegendary(Species s) => s is
        Species.Articuno or Species.Zapdos or Species.Moltres or Species.Mewtwo or
        Species.Raikou or Species.Entei or Species.Suicune or Species.Lugia or Species.HoOh or
        Species.Regirock or Species.Regice or Species.Registeel or Species.Latias or Species.Latios or
        Species.Kyogre or Species.Groudon or Species.Rayquaza or
        Species.Uxie or Species.Mesprit or Species.Azelf or Species.Dialga or Species.Palkia or
        Species.Heatran or Species.Regigigas or Species.Giratina or Species.Cresselia or
        Species.Cobalion or Species.Terrakion or Species.Virizion or Species.Tornadus or Species.Thundurus or
        Species.Reshiram or Species.Zekrom or Species.Landorus or Species.Kyurem or
        Species.Xerneas or Species.Yveltal;

    private static GameVersion GetCanonicalOriginVersion(Species species, GameVersion defaultVersion)
    {
        ushort id = (ushort)species;

        if (id <= 251) return GameVersion.C;  // VC Gen 1/2 (Crystal/Yellow)
        if (id <= 386) return GameVersion.OR; // ORAS Gen 3
        if (id <= 493) return GameVersion.Pt; // Gen 4
        if (id <= 649) return GameVersion.B;  // Gen 5
        if (id <= 721) return GameVersion.X;  // Gen 6
        return defaultVersion;                // Gen 7 (USUM/SM)
    }

    private static PKM? GenerateOriginPKM(SaveFile targetSav, ushort species, byte form, bool isShiny, GameVersion originVersion)
    {
        int consoleRegion = (targetSav as SAV7)?.ConsoleRegion ?? 1;
        int country = (targetSav as SAV7)?.Country ?? 49;
        int region = (targetSav as SAV7)?.Region ?? 1;

        bool isVC = originVersion is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C;
        string otName = isVC && targetSav.OT.Length > 7 ? targetSav.OT[..7] : targetSav.OT;
        ushort sid = isVC ? (ushort)0 : targetSav.SID16;

        var tr = TrainerSettings.GetSavedTrainerData(originVersion, lang: (LanguageID)targetSav.Language)
                 ?? new SimpleTrainerInfo(originVersion)
                 {
                     OT = otName,
                     TID16 = targetSav.TID16,
                     SID16 = sid,
                     Language = targetSav.Language,
                     ConsoleRegion = (byte)consoleRegion,
                     Country = (byte)country,
                     Region = (byte)region,
                 };

        string speciesName = GameInfo.Strings.Species[species];
        string setStr = speciesName;
        if (form > 0)
            setStr += $"-{form}";

        bool isEventOnlyMythical = IsEventOnlyMythical((Species)species);

        if (isShiny && !SimpleEdits.IsShinyLockedSpeciesForm(species, form) && !IsShinyLockedMythicalGen7((Species)species))
        {
            setStr += "\nShiny: Yes";
        }

        if (!isEventOnlyMythical)
        {
            setStr += $"\nVersion: {GetVersionShowdownName(originVersion)}";
        }

        var sset = new ShowdownSet(setStr);
        var template = EntityBlank.GetBlank(targetSav);
        var set = new RegenTemplate(sset) { Nickname = string.Empty };

        var res = tr.TryAPIConvert(set, template);
        if (res.Status == LegalizationResult.Regenerated && res.Created is not null)
        {
            var pk = res.Created;

            if (!isEventOnlyMythical || IsInGameCatchableMythical((Species)species, pk.Version))
            {
                pk.OriginalTrainerName = otName;
                pk.TID16 = targetSav.TID16;
                pk.SID16 = sid;
                pk.Language = targetSav.Language;
            }

            return pk;
        }

        var fallbackSet = new ShowdownSet(isShiny && !SimpleEdits.IsShinyLockedSpeciesForm(species, form) ? $"{speciesName}\nShiny: Yes" : speciesName);
        var fallbackTemplate = EntityBlank.GetBlank(targetSav);
        var fallbackRegen = new RegenTemplate(fallbackSet) { Nickname = string.Empty };
        var fallbackRes = targetSav.TryAPIConvert(fallbackRegen, fallbackTemplate);
        if (fallbackRes.Status == LegalizationResult.Regenerated && fallbackRes.Created is not null)
        {
            return fallbackRes.Created;
        }

        return null;
    }

    private static string GetVersionShowdownName(GameVersion v) => v switch
    {
        GameVersion.RD => "Red",
        GameVersion.BU => "Blue",
        GameVersion.YW => "Yellow",
        GameVersion.GD => "Gold",
        GameVersion.SI => "Silver",
        GameVersion.C => "Crystal",
        GameVersion.R => "Ruby",
        GameVersion.S => "Sapphire",
        GameVersion.E => "Emerald",
        GameVersion.FR => "FireRed",
        GameVersion.LG => "LeafGreen",
        GameVersion.D => "Diamond",
        GameVersion.P => "Pearl",
        GameVersion.Pt => "Platinum",
        GameVersion.HG => "HeartGold",
        GameVersion.SS => "SoulSilver",
        GameVersion.B => "Black",
        GameVersion.W => "White",
        GameVersion.B2 => "Black 2",
        GameVersion.W2 => "White 2",
        GameVersion.X => "X",
        GameVersion.Y => "Y",
        GameVersion.OR => "Omega Ruby",
        GameVersion.AS => "Alpha Sapphire",
        GameVersion.SN => "Sun",
        GameVersion.MN => "Moon",
        GameVersion.US => "Ultra Sun",
        GameVersion.UM => "Ultra Moon",
        _ => "Ultra Sun"
    };

    private static bool IsEventOnlyMythical(Species s) => s is
        Species.Mew or Species.Jirachi or
        Species.Phione or Species.Manaphy or
        Species.Victini or Species.Keldeo or Species.Meloetta or Species.Genesect or
        Species.Diancie or Species.Hoopa or Species.Volcanion or
        Species.Marshadow or Species.Zeraora;

    private static bool IsInGameCatchableMythical(Species s, GameVersion v)
    {
        if (s == Species.Celebi && v == GameVersion.C) return true;
        if (s == Species.Deoxys && (v == GameVersion.OR || v == GameVersion.AS)) return true;
        if (s == Species.Darkrai && v == GameVersion.Pt) return true;
        if (s == Species.Shaymin && v == GameVersion.Pt) return true;
        if (s == Species.Arceus && v == GameVersion.Pt) return true;
        if (s == Species.Magearna && (v == GameVersion.US || v == GameVersion.UM || v == GameVersion.SN || v == GameVersion.MN)) return true;
        return false;
    }

    private static bool IsShinyLockedMythicalGen7(Species s) => s is
        Species.Victini or Species.Keldeo or Species.Meloetta or
        Species.Hoopa or Species.Volcanion or Species.Cosmog or Species.Cosmoem or
        Species.Magearna or Species.Marshadow or Species.Zeraora;

    private static void ApplyCustomBall(PKM pk, BallSelectionPreference pref, Random random, IPersonalTable personal)
    {
        var la = new LegalityAnalysis(pk, personal);
        if (!la.Valid) return;

        Span<Ball> legal = stackalloc Ball[BallApplicator.MaxBallSpanAlloc];
        int count = BallApplicator.GetLegalBalls(legal, pk, la);
        if (count <= 0) return;
        legal = legal[..count];

        if (pref == BallSelectionPreference.ThematicAuto)
        {
            LivingDexPolisher.TryApplyThematicBall(pk, la, out _);
            return;
        }

        if (pref == BallSelectionPreference.KeepOriginal)
            return;

        Ball targetBall = pref switch
        {
            BallSelectionPreference.StandardPokeBall => Ball.Poke,
            BallSelectionPreference.PremierBall => Ball.Premier,
            BallSelectionPreference.LuxuryBall => Ball.Luxury,
            BallSelectionPreference.UltraBall => Ball.Ultra,
            BallSelectionPreference.RandomApriball => GetRandomApriball(legal, random),
            _ => Ball.Poke,
        };

        if (legal.Contains(targetBall))
        {
            byte old = pk.Ball;
            pk.Ball = (byte)targetBall;
            var testLA = new LegalityAnalysis(pk, personal);
            if (!testLA.Valid)
                pk.Ball = old;
        }
    }

    private static Ball GetRandomApriball(ReadOnlySpan<Ball> legal, Random random)
    {
        var apriballs = new[] { Ball.Fast, Ball.Level, Ball.Lure, Ball.Heavy, Ball.Love, Ball.Friend, Ball.Moon, Ball.Sport, Ball.Safari, Ball.Dream, Ball.Beast };
        var available = new List<Ball>();
        foreach (var b in apriballs)
        {
            if (legal.Contains(b))
                available.Add(b);
        }
        return available.Count > 0 ? available[random.Next(available.Count)] : (legal.Contains(Ball.Premier) ? Ball.Premier : legal[0]);
    }

    private static void ApplyCustomIVs(PKM pk, IVOptimizationPreference pref, IPersonalTable personal)
    {
        if (pref == IVOptimizationPreference.KeepEncounter)
            return;

        if (pref == IVOptimizationPreference.SmartIVs)
        {
            LivingDexPolisher.TryOptimizeIVs(pk, personal);
            return;
        }

        if (pref == IVOptimizationPreference.All31)
        {
            Span<int> target = stackalloc int[6] { 31, 31, 31, 31, 31, 31 };
            var candidate = pk.Clone();
            candidate.SetIVs(target);
            LivingDexPolisher.RefreshLevelDependentData(candidate);
            if (new LegalityAnalysis(candidate, personal).Valid)
            {
                pk.SetIVs(target);
                LivingDexPolisher.RefreshLevelDependentData(pk);
            }
        }
    }

    private static void ExportGenerationReport(SaveFile sav, List<string> logLines, int firstBox, int lastBox, int placed, int balls, int gmax)
    {
        try
        {
            string dir = Path.GetDirectoryName(Application.ExecutablePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string txtPath = Path.Combine(dir, $"LivingDex_Report_{sav.Version}_{timestamp}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("===============================================================================");
            sb.AppendLine("                 LIVING DEX GENERATION & AUDIT REPORT");
            sb.AppendLine("===============================================================================");
            sb.AppendLine($"Date & Time : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Game Version: {sav.Version} (Context: {sav.Context})");
            sb.AppendLine($"Trainer Info: OT = {sav.OT}, TID = {sav.TID16}, SID = {sav.SID16}");
            sb.AppendLine($"Boxes Used  : Caixa {firstBox} a Caixa {lastBox} ({placed} Pokémon colocados)");
            sb.AppendLine($"Themed Balls: {balls} | Gigantamax Factor: {gmax}");
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("DETALHES DOS SLOTS GRAVADOS:");
            foreach (var line in logLines)
                sb.AppendLine(line);
            sb.AppendLine("===============================================================================");

            File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Report export failure is non-blocking
        }
    }

    public static IEnumerable<PKM> FilterValidBoxPokemon(IEnumerable<PKM> list, SaveFile sav)
    {
        foreach (var pk in list)
        {
            if (pk is PB7 { IsStarter: true })
                continue;

            if (pk.Species == (ushort)Species.Silvally && pk.Form > 0)
                continue;

            if (FormInfo.IsBattleOnlyForm(pk.Species, pk.Form, sav.Generation) ||
                FormInfo.IsFusedForm(pk.Species, pk.Form, sav.Generation))
                continue;

            yield return pk;
        }
    }

    private static int AlignToNextBox(int slot, int boxSize)
    {
        if (slot <= 0) return 0;
        return ((slot + boxSize - 1) / boxSize) * boxSize;
    }

    private static bool IsProtectedGameplaySlot(SaveFile sav, int index)
    {
        if (sav.Generation <= 7)
            return false;

        var flags = sav.GetBoxSlotFlags(index);
        if (flags.IsOverwriteProtected())
            return true;

        if (sav.Version is GameVersion.GP or GameVersion.GE)
            return flags.IsParty() >= 0;

        return false;
    }

    private static int GetLivingDexStartSlot(SaveFile sav)
    {
        if (sav.Version is not (GameVersion.GP or GameVersion.GE))
            return 0;

        int capacity = sav.BoxCount * sav.BoxSlotCount;
        int highestProtected = -1;
        for (int i = 0; i < capacity; i++)
        {
            if (IsProtectedGameplaySlot(sav, i))
                highestProtected = i;
        }

        return highestProtected < 0
            ? 0
            : AlignToNextBox(highestProtected + 1, sav.BoxSlotCount);
    }
}

/// <summary>
/// Customization Dialog Form with live capacity, preset modes, and box preview calculation.
/// </summary>
public sealed class LivingDexOptionsForm : Form
{
    private readonly SaveFile _sav;
    public LivingDexCustomOptions Options { get; } = new();

    private readonly ComboBox _cbMode;
    private readonly CheckBox _chkIncludeForms;
    private readonly CheckBox _chkRespectShinyLocks;
    private readonly ComboBox _cbBallPref;
    private readonly ComboBox _cbIVPref;
    private readonly ComboBox _cbLevelPref;
    private readonly CheckBox _chkGMax;
    private readonly NumericUpDown _nudStartBox;
    private readonly ComboBox _cbBoxPref;
    private readonly CheckBox _chkExportReport;
    private readonly Label _lblEstimation;
    private readonly Label _lblCapacityStatus;

    public LivingDexOptionsForm(SaveFile sav)
    {
        _sav = sav;

        Text = "Configurador Avançado de Living Dex";
        ClientSize = new Size(620, 610);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Color.FromArgb(24, 40, 72),
        };

        var lblHeaderTitle = new Label
        {
            Text = "Living Dex Generator & Customizer",
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
            Location = new Point(15, 10),
            AutoSize = true,
        };

        var lblHeaderSub = new Label
        {
            Text = $"Jogo: {sav.Version} (Gen {sav.Generation}) | Treinador: {sav.OT} (TID: {sav.TID16}) | Armazenamento: {sav.BoxCount} Caixas ({sav.BoxCount * sav.BoxSlotCount} slots)",
            ForeColor = Color.FromArgb(200, 220, 255),
            Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
            Location = new Point(15, 36),
            AutoSize = true,
        };

        pnlHeader.Controls.Add(lblHeaderTitle);
        pnlHeader.Controls.Add(lblHeaderSub);

        // Group 1: Conteúdo e Modo
        var grpMode = new GroupBox
        {
            Text = " 📦 1. Modo & Conteúdo da Living Dex ",
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            Location = new Point(15, 75),
            Size = new Size(590, 115),
        };

        var lblMode = new Label { Text = "Modo de Coleção:", Location = new Point(15, 25), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular) };
        _cbMode = new ComboBox
        {
            Location = new Point(140, 22),
            Size = new Size(430, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };

        if (sav.Generation == 7)
        {
            _cbMode.Items.AddRange([
                "🎮 Gen 7: Normal Canonical Origins (VC Gens 1/2 + ORAS Gen 3 + Alola + Eventos)",
                "✨ Gen 7: Shiny Canonical Origins (VC Shiny Celebi + USUM Shinies + Locks)",
                "📦 Normal Living Dex Padrão (Espécies normais nativas)",
                "✨ Shiny Living Dex Padrão (Espécies brilhantes nativas)",
                "🌟 Normal + Shiny Combinado",
                "🏆 Base Species Only (Apenas espécies base)"
            ]);
            _cbMode.SelectedIndex = 0;
        }
        else
        {
            _cbMode.Items.AddRange([
                "Normal Living Dex (Todas as espécies normais + expansões)",
                "Shiny Living Dex (Todas as espécies brilhantes legais)",
                "Normal + Shiny Living Dex (Coleção Combinada)",
                "Base Species Only (Apenas espécies base sem variantes cosméticas)"
            ]);
            _cbMode.SelectedIndex = 0;
        }
        _cbMode.SelectedIndexChanged += (_, _) => UpdateEstimations();

        _chkIncludeForms = new CheckBox
        {
            Text = "Incluir Formas Regionais & Alternativas (Alola, Galar, Hisui, Paldea, Vivillon, etc.)",
            Location = new Point(15, 55),
            Size = new Size(560, 22),
            Checked = true,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };
        _chkIncludeForms.CheckedChanged += (_, _) => UpdateEstimations();

        _chkRespectShinyLocks = new CheckBox
        {
            Text = "Respeitar Shiny Locks Oficiais (Garante 100% de conformidade com Pokémon HOME)",
            Location = new Point(15, 82),
            Size = new Size(560, 22),
            Checked = true,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };

        grpMode.Controls.Add(lblMode);
        grpMode.Controls.Add(_cbMode);
        grpMode.Controls.Add(_chkIncludeForms);
        grpMode.Controls.Add(_chkRespectShinyLocks);

        // Group 2: Personalização & Polimento
        var grpPolish = new GroupBox
        {
            Text = " 🪄 2. Personalização & Polimento dos Pokémon ",
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            Location = new Point(15, 200),
            Size = new Size(590, 150),
        };

        var lblBall = new Label { Text = "Pokébolas:", Location = new Point(15, 25), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular) };
        _cbBallPref = new ComboBox
        {
            Location = new Point(140, 22),
            Size = new Size(430, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };
        _cbBallPref.Items.AddRange([
            "🎨 Automático Temático / Smart Match (Tipo, Cor, Peso e Raridade)",
            "🔴 Standard Poké Ball",
            "⚪ Premier Ball",
            "⚫ Luxury Ball",
            "🟡 Ultra Ball",
            "🦕 Apriballs Aleatórias (Fast, Level, Lure, Heavy, Love, Friend, Moon)",
            "🔒 Manter Bola Original do Encontro"
        ]);
        _cbBallPref.SelectedIndex = 0;

        var lblIV = new Label { Text = "Otimização de IVs:", Location = new Point(15, 55), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular) };
        _cbIVPref = new ComboBox
        {
            Location = new Point(140, 52),
            Size = new Size(430, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };
        _cbIVPref.Items.AddRange([
            "⚡ Smart IVs (6x31 atacantes físicos / 0 Atk atacantes especiais) [Recomendado]",
            "🌟 Todos 31 (6x31 Perfeitos)",
            "🎲 Manter IVs Originais do Encontro"
        ]);
        _cbIVPref.SelectedIndex = 0;

        var lblLevel = new Label { Text = "Nível de Evolução:", Location = new Point(15, 85), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular) };
        _cbLevelPref = new ComboBox
        {
            Location = new Point(140, 82),
            Size = new Size(430, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };
        _cbLevelPref.Items.AddRange([
            "📈 Nível Canônico de Evolução (Progresso por MetLevel) [Recomendado]",
            "🎯 Nível Original do Encontro",
            "💯 Nível 100 Máximo"
        ]);
        _cbLevelPref.SelectedIndex = 0;

        _chkGMax = new CheckBox
        {
            Text = "Habilitar Gigantamax Factor no SWSH para espécies elegíveis",
            Location = new Point(15, 118),
            Size = new Size(560, 22),
            Checked = true,
            Enabled = sav.Version is GameVersion.SW or GameVersion.SH,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };

        grpPolish.Controls.Add(lblBall);
        grpPolish.Controls.Add(_cbBallPref);
        grpPolish.Controls.Add(lblIV);
        grpPolish.Controls.Add(_cbIVPref);
        grpPolish.Controls.Add(lblLevel);
        grpPolish.Controls.Add(_cbLevelPref);
        grpPolish.Controls.Add(_chkGMax);

        // Group 3: Caixas e Destino
        var grpBox = new GroupBox
        {
            Text = " 🗄️ 3. Localização das Caixas & Gravação ",
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            Location = new Point(15, 360),
            Size = new Size(590, 110),
        };

        var lblStartBox = new Label { Text = "Caixa Inicial:", Location = new Point(15, 25), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular) };
        _nudStartBox = new NumericUpDown
        {
            Location = new Point(140, 22),
            Size = new Size(80, 24),
            Minimum = 1,
            Maximum = sav.BoxCount,
            Value = 1,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };
        _nudStartBox.ValueChanged += (_, _) => UpdateEstimations();

        var lblBoxPref = new Label { Text = "Preenchimento:", Location = new Point(240, 25), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular) };
        _cbBoxPref = new ComboBox
        {
            Location = new Point(340, 22),
            Size = new Size(230, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };
        _cbBoxPref.Items.AddRange([
            "🔄 Sobrescrever slots necessários",
            "📭 Apenas preencher slots vazios",
            "🧹 Limpar caixas selecionadas primeiro"
        ]);
        _cbBoxPref.SelectedIndex = 0;

        _chkExportReport = new CheckBox
        {
            Text = "Gerar relatório completo de auditoria (.txt) na pasta do PKHeX",
            Location = new Point(15, 55),
            Size = new Size(560, 22),
            Checked = true,
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
        };

        grpBox.Controls.Add(lblStartBox);
        grpBox.Controls.Add(_nudStartBox);
        grpBox.Controls.Add(lblBoxPref);
        grpBox.Controls.Add(_cbBoxPref);
        grpBox.Controls.Add(_chkExportReport);

        // Previsão de Capacidade
        var pnlPreview = new Panel
        {
            Location = new Point(15, 480),
            Size = new Size(590, 60),
            BackColor = Color.FromArgb(240, 244, 250),
            BorderStyle = BorderStyle.FixedSingle,
        };

        _lblEstimation = new Label
        {
            Location = new Point(10, 8),
            Size = new Size(570, 20),
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 50, 90),
            Text = "Calculando estimativa...",
        };

        _lblCapacityStatus = new Label
        {
            Location = new Point(10, 30),
            Size = new Size(570, 22),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
            ForeColor = Color.DarkGreen,
            Text = "",
        };

        pnlPreview.Controls.Add(_lblEstimation);
        pnlPreview.Controls.Add(_lblCapacityStatus);

        // Bottom buttons
        var btnGenerate = new Button
        {
            Text = "🚀 Gerar Living Dex Agora",
            Location = new Point(370, 555),
            Size = new Size(235, 38),
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            UseVisualStyleBackColor = false,
        };
        btnGenerate.Click += (_, _) => OnGenerateClicked();

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Location = new Point(255, 555),
            Size = new Size(105, 38),
            Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
            UseVisualStyleBackColor = true,
        };
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.Add(pnlHeader);
        Controls.Add(grpMode);
        Controls.Add(grpPolish);
        Controls.Add(grpBox);
        Controls.Add(pnlPreview);
        Controls.Add(btnGenerate);
        Controls.Add(btnCancel);

        UpdateEstimations();
    }

    private void UpdateEstimations()
    {
        int modeIdx = _cbMode.SelectedIndex;
        bool includeForms = _chkIncludeForms.Checked;
        int startBox = (int)_nudStartBox.Value;

        int count = GetEstimatedCount(modeIdx, includeForms);
        int neededBoxes = (count + _sav.BoxSlotCount - 1) / _sav.BoxSlotCount;
        int endBox = startBox + neededBoxes - 1;
        int totalCapacity = _sav.BoxCount * _sav.BoxSlotCount;
        int availableSlotsFromStart = (_sav.BoxCount - startBox + 1) * _sav.BoxSlotCount;

        _lblEstimation.Text = $"Estimativa: {count} Pokémon | Ocupará {neededBoxes} Caixas (Caixa {startBox} até Caixa {Math.Min(_sav.BoxCount, endBox)})";

        if (count <= availableSlotsFromStart)
        {
            _lblCapacityStatus.ForeColor = Color.FromArgb(0, 130, 50);
            int leftover = (_sav.BoxCount - endBox);
            _lblCapacityStatus.Text = $"✅ Cabe perfeitamente no save! (Sobram {Math.Max(0, leftover)} caixas livres após a coleção)";
        }
        else
        {
            _lblCapacityStatus.ForeColor = Color.FromArgb(180, 80, 0);
            int overflow = count - availableSlotsFromStart;
            _lblCapacityStatus.Text = $"⚠️ Excede a capacidade a partir da Caixa {startBox} em {overflow} slots. (O excedente poderá ser salvo em pasta)";
        }
    }

    private int GetEstimatedCount(int modeIdx, bool includeForms)
    {
        if (_sav.Generation == 7)
        {
            return modeIdx switch
            {
                0 => 807, // Gen 7 Canonical Normal (Base 807)
                1 => 807, // Gen 7 Canonical Shiny (Base 807)
                2 => includeForms ? 982 : 807, // Normal Padrão
                3 => includeForms ? 953 : 807, // Shiny Padrão
                4 => (includeForms ? 982 : 807) * 2, // Combinado
                5 => 807, // Base
                _ => 807,
            };
        }

        int baseCount = _sav.Version switch
        {
            GameVersion.GP or GameVersion.GE => 153,
            GameVersion.SW or GameVersion.SH => 658,
            GameVersion.BD or GameVersion.SP => 493,
            GameVersion.PLA => 242,
            GameVersion.SL or GameVersion.VL => 680,
            _ => 400,
        };

        int formsCount = _sav.Version switch
        {
            GameVersion.GP or GameVersion.GE => 171,
            GameVersion.SW or GameVersion.SH => 777,
            GameVersion.BD or GameVersion.SP => 520,
            GameVersion.PLA => 250,
            GameVersion.SL or GameVersion.VL => 750,
            _ => 450,
        };

        int perSection = includeForms ? formsCount : baseCount;

        return modeIdx switch
        {
            0 => perSection,
            1 => (int)(perSection * 0.95),
            2 => perSection + (int)(perSection * 0.95),
            3 => baseCount,
            _ => perSection,
        };
    }

    private void OnGenerateClicked()
    {
        if (_sav.Generation == 7)
        {
            Options.Mode = _cbMode.SelectedIndex switch
            {
                0 => LivingDexMode.Gen7CanonicalNormal,
                1 => LivingDexMode.Gen7CanonicalShiny,
                2 => LivingDexMode.Normal,
                3 => LivingDexMode.Shiny,
                4 => LivingDexMode.Combined,
                5 => LivingDexMode.BaseSpeciesOnly,
                _ => LivingDexMode.Gen7CanonicalNormal,
            };
        }
        else
        {
            Options.Mode = _cbMode.SelectedIndex switch
            {
                0 => LivingDexMode.Normal,
                1 => LivingDexMode.Shiny,
                2 => LivingDexMode.Combined,
                3 => LivingDexMode.BaseSpeciesOnly,
                _ => LivingDexMode.Normal,
            };
        }

        Options.IncludeForms = _chkIncludeForms.Checked;
        Options.RespectShinyLocks = _chkRespectShinyLocks.Checked;

        Options.BallPreference = _cbBallPref.SelectedIndex switch
        {
            0 => BallSelectionPreference.ThematicAuto,
            1 => BallSelectionPreference.StandardPokeBall,
            2 => BallSelectionPreference.PremierBall,
            3 => BallSelectionPreference.LuxuryBall,
            4 => BallSelectionPreference.UltraBall,
            5 => BallSelectionPreference.RandomApriball,
            6 => BallSelectionPreference.KeepOriginal,
            _ => BallSelectionPreference.ThematicAuto,
        };

        Options.IVPreference = _cbIVPref.SelectedIndex switch
        {
            0 => IVOptimizationPreference.SmartIVs,
            1 => IVOptimizationPreference.All31,
            2 => IVOptimizationPreference.KeepEncounter,
            _ => IVOptimizationPreference.SmartIVs,
        };

        Options.LevelPref = _cbLevelPref.SelectedIndex switch
        {
            0 => LevelPreference.CanonicalFloor,
            1 => LevelPreference.EncounterNative,
            2 => LevelPreference.Level100,
            _ => LevelPreference.CanonicalFloor,
        };

        Options.EnableGigantamax = _chkGMax.Checked;
        Options.StartBox = (int)_nudStartBox.Value;
        Options.BoxPreference = _cbBoxPref.SelectedIndex switch
        {
            0 => BoxPlacementPreference.Overwrite,
            1 => BoxPlacementPreference.EmptySlotsOnly,
            2 => BoxPlacementPreference.ClearBoxesFirst,
            _ => BoxPlacementPreference.Overwrite,
        };

        Options.ExportReport = _chkExportReport.Checked;

        DialogResult = DialogResult.OK;
    }
}

/// <summary>
/// Real-time progress bar dialog showing live placement and Pokémon information card.
/// </summary>
public sealed class LivingDexProgressForm : Form
{
    private readonly ProgressBar _progressBar;
    private readonly Label _lblStage;
    private readonly Label _lblPokemon;
    private readonly Label _lblBox;
    private readonly Label _lblBall;
    private readonly Label _lblIVs;
    private readonly Label _lblStats;
    private readonly Button _btnCancel;
    private readonly CancellationTokenSource _cts;
    private readonly DateTime _startTime;
    private int _shinyCount = 0;
    private int _gmaxCount = 0;

    public bool WasCancelled => _cts.IsCancellationRequested;
    public CancellationToken Token => _cts.Token;
    public bool IsCompleted { get; set; }

    public LivingDexProgressForm(string title, int totalItems, CancellationTokenSource cts)
    {
        _cts = cts;
        _startTime = DateTime.Now;

        Text = title;
        ClientSize = new Size(540, 240);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;

        _lblStage = new Label
        {
            Location = new Point(20, 15),
            Size = new Size(500, 24),
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 40, 80),
            Text = $"Iniciando processo... (0/{totalItems})",
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(20, 42),
            Size = new Size(500, 26),
            Minimum = 0,
            Maximum = Math.Max(1, totalItems),
            Value = 0,
            Style = ProgressBarStyle.Continuous,
        };

        var pnlCard = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(500, 110),
            BackColor = Color.FromArgb(245, 248, 253),
            BorderStyle = BorderStyle.FixedSingle,
        };

        _lblPokemon = new Label
        {
            Location = new Point(12, 8),
            Size = new Size(475, 22),
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(10, 30, 70),
            Text = "Preparando coleção...",
        };

        _lblBox = new Label
        {
            Location = new Point(12, 32),
            Size = new Size(230, 20),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(40, 60, 90),
            Text = "📍 Localização: Aguardando...",
        };

        _lblBall = new Label
        {
            Location = new Point(250, 32),
            Size = new Size(235, 20),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(40, 60, 90),
            Text = "⚽ Bola: Aguardando...",
        };

        _lblIVs = new Label
        {
            Location = new Point(12, 55),
            Size = new Size(475, 20),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
            ForeColor = Color.DarkSlateGray,
            Text = "📊 IVs: Aguardando...",
        };

        _lblStats = new Label
        {
            Location = new Point(12, 78),
            Size = new Size(475, 20),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(70, 90, 120),
            Text = "⏱️ Tempo decorrido: 00:00",
        };

        pnlCard.Controls.Add(_lblPokemon);
        pnlCard.Controls.Add(_lblBox);
        pnlCard.Controls.Add(_lblBall);
        pnlCard.Controls.Add(_lblIVs);
        pnlCard.Controls.Add(_lblStats);

        _btnCancel = new Button
        {
            Location = new Point(410, 195),
            Size = new Size(110, 32),
            Text = "Cancelar",
            Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
            UseVisualStyleBackColor = true,
        };
        _btnCancel.Click += (_, _) => RequestCancel();

        Controls.Add(_lblStage);
        Controls.Add(_progressBar);
        Controls.Add(pnlCard);
        Controls.Add(_btnCancel);

        FormClosing += (_, e) =>
        {
            if (IsCompleted)
                return;

            if (e.CloseReason == CloseReason.UserClosing && !_cts.IsCancellationRequested)
            {
                var res = MessageBox.Show(
                    "Deseja cancelar a geração da Living Dex?",
                    "Cancelar Geração",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    _cts.Cancel();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        };
    }

    public void UpdateProgress(int current, int total, PKM pk, int box, int slot)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => UpdateProgress(current, total, pk, box, slot)));
            }
            catch
            {
                // Form disposed
            }
            return;
        }

        int percent = total > 0 ? (int)((current / (double)total) * 100) : 0;
        _progressBar.Value = Math.Min(current, _progressBar.Maximum);
        _lblStage.Text = $"Progresso: [{current} / {total}] ({percent}%)";

        if (pk.IsShiny) _shinyCount++;
        if (pk is IGigantamax g && g.CanGigantamax) _gmaxCount++;

        string shinyTag = pk.IsShiny ? " ★ (Shiny)" : "";
        string speciesName = GameInfo.Strings.Species[pk.Species];
        string gmaxTag = (pk is IGigantamax g2 && g2.CanGigantamax) ? " [G-Max]" : "";
        string mark = (pk.Version is GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GD or GameVersion.SI or GameVersion.C) ? " [GB Mark]" : (pk.Version is GameVersion.X or GameVersion.Y or GameVersion.OR or GameVersion.AS) ? " [Pentagon]" : (pk.Version is GameVersion.SN or GameVersion.MN or GameVersion.US or GameVersion.UM) ? " [Alola Clover]" : "";
        
        _lblPokemon.Text = $"{speciesName}{shinyTag}{gmaxTag} — Lv. {pk.CurrentLevel} ({pk.Version}{mark})";
        _lblBox.Text = $"📍 Caixa {box + 1}, Slot {slot + 1}";
        _lblBall.Text = $"⚽ Bola: {(Ball)pk.Ball}";
        _lblIVs.Text = $"📊 IVs: {pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE} | OT: {pk.OriginalTrainerName}";

        var elapsed = DateTime.Now - _startTime;
        _lblStats.Text = $"⏱️ Tempo: {elapsed:mm\\:ss} | ✨ Shinies: {_shinyCount} | 🦖 G-Max: {_gmaxCount}";
    }

    private void RequestCancel()
    {
        var res = MessageBox.Show(
            "Deseja cancelar a geração da Living Dex?",
            "Cancelar Geração",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (res == DialogResult.Yes)
        {
            _cts.Cancel();
            _btnCancel.Enabled = false;
            _btnCancel.Text = "Cancelando...";
        }
    }
}
