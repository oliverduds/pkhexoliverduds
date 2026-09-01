using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins;

/// <summary>
/// Living Dex post-processor for Nintendo Switch mainline saves.
/// Goals:
/// 1) keep every already-legal Pokémon as intact as possible;
/// 2) normalize Current Level to PKHeX's minimum evolution-chain level for that exact Pokémon;
/// 3) repair level-dependent moves/training only when needed to make that target level legal;
/// 4) assign natural, thematic and aesthetic Pokéballs legally and automatically;
/// 5) optimize IVs naturally (6x31 for physical/mixed; 0 Atk for special attackers) when legal;
/// 6) apply Gigantamax factors in SWSH and natural Tera Types in SV;
/// 7) repair invalid entries conservatively, using ALM regeneration only as a last resort;
/// 8) perform a final full legality audit and export structured reports (.txt and .json).
/// </summary>
public sealed class LivingDexPolisher : AutoModPlugin
{
    public override string Name => "Living Dex Polish, Legalize & Audit";
    public override int Priority => 1;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var root = new ToolStripMenuItem(Name)
        {
            Name = "Menu_LivingDexPolisher"
        };

        var current = new ToolStripMenuItem("Polish Current Box");
        current.Click += (_, _) => ProcessRange(SaveFileEditor.CurrentBox, SaveFileEditor.CurrentBox, auditOnly: false);

        var all = new ToolStripMenuItem("Polish ALL Boxes (Recommended)");
        all.Click += (_, _) => ProcessRange(0, SaveFileEditor.SAV.BoxCount - 1, auditOnly: false);

        var range = new ToolStripMenuItem("Polish Box Range...");
        range.Click += (_, _) => PromptRange(auditOnly: false);

        var audit = new ToolStripMenuItem("Audit ALL Boxes Only");
        audit.Click += (_, _) => ProcessRange(0, SaveFileEditor.SAV.BoxCount - 1, auditOnly: true);

        root.DropDownItems.Add(current);
        root.DropDownItems.Add(all);
        root.DropDownItems.Add(range);
        root.DropDownItems.Add(new ToolStripSeparator());
        root.DropDownItems.Add(audit);
        modmenu.DropDownItems.Add(root);
    }

    private void PromptRange(bool auditOnly)
    {
        var sav = SaveFileEditor.SAV;
        var current = SaveFileEditor.CurrentBox + 1;
        var input = Interaction.InputBox(
            $"Enter the box range (1-{sav.BoxCount}).\nExamples: 1-6 or 5",
            auditOnly ? "Audit Living Dex" : "Polish Living Dex",
            $"{current}-{sav.BoxCount}");

        if (string.IsNullOrWhiteSpace(input))
            return;

        if (!TryParseRange(input, sav.BoxCount, out var first, out var last))
        {
            WinFormsUtil.Alert(
                "Invalid box range.",
                $"Use a value from 1 to {sav.BoxCount}, for example 1-6 or 5.");
            return;
        }

        ProcessRange(first - 1, last - 1, auditOnly);
    }

    private void ProcessRange(int firstBox, int lastBox, bool auditOnly)
    {
        var sav = SaveFileEditor.SAV;

        if (firstBox < 0 || lastBox >= sav.BoxCount || firstBox > lastBox)
        {
            WinFormsUtil.Alert("Invalid box range.");
            return;
        }

        if (!IsSupportedSwitchGame(sav.Version))
        {
            WinFormsUtil.Alert(
                "This tool is restricted to the Nintendo Switch games used by this Living Dex workflow.",
                $"Loaded save version: {sav.Version}");
            return;
        }

        if (!auditOnly && ParseSettings.Settings.HOMETransfer.HOMETransferTrackerNotPresent != Severity.Invalid)
        {
            WinFormsUtil.Alert(
                "PKHeX HOME transfer checks are currently relaxed.",
                "For native-by-game normalization, set HOMETransferTrackerNotPresent back to Invalid and run again.",
                "No Pokémon were changed.");
            return;
        }

        if (!auditOnly)
        {
            var confirm = MessageBox.Show(
                $"Boxes {firstBox + 1}-{lastBox + 1} will be processed.\n\n" +
                "The tool will:\n" +
                "• preserve encounter/origin data;\n" +
                "• set Current Level to the game's canonical evolution-chain floor;\n" +
                "• adjust moves/relearn moves only if needed for that target level;\n" +
                "• clear Hyper Training only if it blocks the natural minimum level;\n" +
                "• assign thematic and legal Pokéballs automatically;\n" +
                "• optimize IVs naturally (6x31 / 0 Atk Special Attackers) when encounter-legal;\n" +
                "• enable Gigantamax factors (SWSH) and natural Tera Types (SV);\n" +
                "• try conservative repair for invalid Pokémon;\n" +
                "• use ALM regeneration only if conservative repair fails;\n" +
                "• skip Pokémon that already have a HOME Tracker;\n" +
                "• export detailed reports (.txt and .json) and run a final legality audit.\n\n" +
                "Use a backup save. Continue?",
                "Living Dex Polish, Legalize & Audit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;
        }

        Cursor.Current = Cursors.WaitCursor;
        try
        {
            int found = 0;
            int initialLegal = 0;
            int initialInvalid = 0;
            int changedLevel = 0;
            int movesAdjusted = 0;
            int hyperTrainingCleared = 0;
            int evolutionTargetBlocked = 0;
            int ballsThemed = 0;
            int ivsOptimized = 0;
            int gmaxApplied = 0;
            int safeRepaired = 0;
            int almRegenerated = 0;
            int regenerationRejected = 0;
            int skippedTracked = 0;
            int skippedEgg = 0;
            int skippedGameplayProtected = 0;

            var details = new List<string>();
            var regeneratedDetails = new List<string>();
            var slotLogLines = new List<string>();

            if (!auditOnly)
            {
                for (int box = firstBox; box <= lastBox; box++)
                {
                    for (int slot = 0; slot < sav.BoxSlotCount; slot++)
                    {
                        var original = sav.GetBoxSlotAtIndex(box, slot);
                        if (original.Species == 0)
                            continue;

                        found++;

                        if (IsProtectedGameplaySlot(sav, box, slot))
                        {
                            skippedGameplayProtected++;
                            details.Add(
                                $"SKIP GAMEPLAY SLOT — Box {box + 1}, Slot {slot + 1}: " +
                                $"{GetName(original)} Lv.{original.CurrentLevel}");
                            continue;
                        }

                        if (original.IsEgg)
                        {
                            skippedEgg++;
                            continue;
                        }

                        if (original is IHomeTrack { HasTracker: true })
                        {
                            skippedTracked++;
                            details.Add($"SKIP HOME — Box {box + 1}, Slot {slot + 1}: {GetName(original)} Lv.{original.CurrentLevel}");
                            continue;
                        }

                        var sourceLA = new LegalityAnalysis(original, sav.Personal);
                        PKM working;

                        if (sourceLA.Valid)
                        {
                            initialLegal++;
                            working = original.Clone();
                        }
                        else
                        {
                            initialInvalid++;

                            // First try a repair that changes only Current Level and/or a conspicuous Master Ball.
                            if (TryConservativeRepair(original, sav.Personal, out var repaired))
                            {
                                working = repaired;
                                safeRepaired++;
                                details.Add($"SAFE REPAIR — Box {box + 1}, Slot {slot + 1}: {GetName(original)}");
                            }
                            else
                            {
                                // Last resort: use the same ALM legalization path used by Legalize Active Pokémon.
                                var regenerated = sav.Legalize(original, sourceLA);
                                var regenLA = new LegalityAnalysis(regenerated, sav.Personal);

                                if (!regenLA.Valid ||
                                    regenerated is IHomeTrack { HasTracker: true } ||
                                    !IsNativeOrPaired(regenerated.Version, sav.Version))
                                {
                                    regenerationRejected++;
                                    details.Add($"UNRESOLVED — Box {box + 1}, Slot {slot + 1}: {GetName(original)} Lv.{original.CurrentLevel}");
                                    continue;
                                }

                                working = regenerated;
                                almRegenerated++;
                                regeneratedDetails.Add(
                                    $"Box {box + 1}, Slot {slot + 1}: {GetName(original)} — ALM regenerated; " +
                                    $"Version {original.Version}->{working.Version}, Lv.{original.CurrentLevel}->{working.CurrentLevel}");
                            }
                        }

                        // 1. Normalize to the minimum level at which this exact species/form can naturally exist.
                        var beforeLA = new LegalityAnalysis(working, sav.Personal);
                        if (!beforeLA.Valid)
                        {
                            details.Add($"UNRESOLVED AFTER REPAIR — Box {box + 1}, Slot {slot + 1}: {GetName(original)}");
                            continue;
                        }

                        int oldLevel = working.CurrentLevel;
                        if (TryNormalizeToEvolutionMinimum(
                                working, sav.Personal, beforeLA,
                                out var levelCandidate, out var targetLevel,
                                out var didAdjustMoves, out var didClearHyperTraining))
                        {
                            if (targetLevel != oldLevel)
                            {
                                working = levelCandidate;
                                changedLevel++;
                                if (didAdjustMoves)
                                    movesAdjusted++;
                                if (didClearHyperTraining)
                                    hyperTrainingCleared++;

                                var extras = new List<string>(2);
                                if (didAdjustMoves)
                                    extras.Add("moves refreshed");
                                if (didClearHyperTraining)
                                    extras.Add("Hyper Training cleared");
                                var suffix = extras.Count == 0 ? string.Empty : $"; {string.Join(", ", extras)}";

                                details.Add(
                                    $"LEVEL — Box {box + 1}, Slot {slot + 1}: {GetName(working)} " +
                                    $"{oldLevel}->{targetLevel} (Met {working.MetLevel}; evolution minimum {targetLevel}{suffix})");
                            }
                        }
                        else
                        {
                            evolutionTargetBlocked++;
                            details.Add(
                                $"LEVEL TARGET BLOCKED — Box {box + 1}, Slot {slot + 1}: {GetName(working)} " +
                                $"Lv.{oldLevel}; target Lv.{targetLevel} could not be made legal conservatively.");
                        }

                        // 2. Thematic Pokéball matching (Automatic)
                        var levelLA = new LegalityAnalysis(working, sav.Personal);
                        if (levelLA.Valid)
                        {
                            var ballCandidate = working.Clone();
                            if (TryApplyThematicBall(ballCandidate, levelLA, out var chosenBall))
                            {
                                var ballLA = new LegalityAnalysis(ballCandidate, sav.Personal);
                                if (ballLA.Valid && SameEncounterIdentity(levelLA.EncounterOriginal, ballLA.EncounterOriginal))
                                {
                                    working = ballCandidate;
                                    ballsThemed++;
                                    details.Add($"BALL — Box {box + 1}, Slot {slot + 1}: {GetName(working)} -> {chosenBall}");
                                }
                            }
                        }

                        // 3. Smart IVs Optimization
                        if (TryOptimizeIVs(working, sav.Personal))
                        {
                            ivsOptimized++;
                        }

                        // 4. Gigantamax Factor (SWSH)
                        if (TryApplyGigantamax(working, sav))
                        {
                            gmaxApplied++;
                        }

                        // Final guard before writing.
                        var finalLA = new LegalityAnalysis(working, sav.Personal);
                        if (!finalLA.Valid)
                        {
                            details.Add($"GUARD BLOCKED — Box {box + 1}, Slot {slot + 1}: {GetName(original)}");
                            continue;
                        }

                        sav.SetBoxSlotAtIndex(working, box, slot);
                    }
                }

                SaveFileEditor.ReloadSlots();
            }
            else
            {
                // Count for audit-only mode.
                for (int box = firstBox; box <= lastBox; box++)
                {
                    for (int slot = 0; slot < sav.BoxSlotCount; slot++)
                    {
                        var pk = sav.GetBoxSlotAtIndex(box, slot);
                        if (pk.Species != 0)
                            found++;
                    }
                }
            }

            // Final audit.
            int finalLegal = 0;
            int finalInvalid = 0;
            int canonicalLevelViolations = 0;
            var invalidDetails = new List<string>();
            var canonicalLevelDetails = new List<string>();

            for (int box = firstBox; box <= lastBox; box++)
            {
                for (int slot = 0; slot < sav.BoxSlotCount; slot++)
                {
                    var pk = sav.GetBoxSlotAtIndex(box, slot);
                    if (pk.Species == 0)
                        continue;

                    var la = new LegalityAnalysis(pk, sav.Personal);
                    if (la.Valid)
                    {
                        finalLegal++;

                        if (!pk.IsEgg && !IsProtectedGameplaySlot(sav, box, slot))
                        {
                            int canonicalTarget = GetEvolutionMinimumLevel(pk, la, sav.Personal);
                            if (pk.CurrentLevel < canonicalTarget)
                            {
                                canonicalLevelViolations++;
                                canonicalLevelDetails.Add(
                                    $"Box {box + 1}, Slot {slot + 1}: {GetName(pk)} " +
                                    $"Lv.{pk.CurrentLevel} < canonical Lv.{canonicalTarget} (Met {pk.MetLevel})");
                            }
                        }
                    }
                    else
                    {
                        finalInvalid++;
                        invalidDetails.Add(
                            $"Box {box + 1}, Slot {slot + 1}: {GetName(pk)} Lv.{pk.CurrentLevel} " +
                            $"(Met Lv.{pk.MetLevel}, {pk.Version}, {(Ball)pk.Ball})");
                    }

                    string shinyTag = pk.IsShiny ? "★" : " ";
                    string ivStr = $"{pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
                    string gmaxTag = (pk is IGigantamax g && g.CanGigantamax) ? " [G-Max]" : "";
                    string statusTag = la.Valid ? "LEGAL" : $"INVALID ({la.Report()})";
                    slotLogLines.Add(
                        $"Box {box + 1,2}, Slot {slot + 1,2} | Dex #{pk.Species,3} {GetName(pk),-18} {shinyTag} | " +
                        $"Lv.{pk.CurrentLevel,3} (Met {pk.MetLevel,2}) | Ball: {(Ball)pk.Ball,-10} | IVs: {ivStr,-17} | " +
                        $"OT: {pk.OriginalTrainerName,-10}{gmaxTag} | Status: {statusTag}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Game: {sav.Version}");
            sb.AppendLine($"Boxes: {firstBox + 1}-{lastBox + 1}");
            sb.AppendLine($"Pokémon found: {found}");

            if (!auditOnly)
            {
                sb.AppendLine();
                sb.AppendLine($"Initially legal: {initialLegal}");
                sb.AppendLine($"Initially invalid: {initialInvalid}");
                sb.AppendLine($"Conservative repairs: {safeRepaired}");
                sb.AppendLine($"ALM regenerations: {almRegenerated}");
                sb.AppendLine($"ALM regeneration rejected/failed: {regenerationRejected}");
                sb.AppendLine($"Evolution-minimum levels applied: {changedLevel}");
                sb.AppendLine($"Moves/relearn adjusted for target level: {movesAdjusted}");
                sb.AppendLine($"Hyper Training cleared for target level: {hyperTrainingCleared}");
                sb.AppendLine($"Thematic Pokéballs applied: {ballsThemed}");
                sb.AppendLine($"Smart IVs optimized: {ivsOptimized}");
                if (gmaxApplied > 0)
                    sb.AppendLine($"Gigantamax factor enabled: {gmaxApplied}");
                sb.AppendLine($"Evolution-level targets blocked: {evolutionTargetBlocked}");
                sb.AppendLine("Level rule: EvolutionTree chain starting from actual MetLevel");
                if (sav.Version is GameVersion.GP or GameVersion.GE)
                    sb.AppendLine("LGPE: Combat Power recalculated after level changes");
                sb.AppendLine($"Skipped HOME-tracked: {skippedTracked}");
                sb.AppendLine($"Skipped eggs: {skippedEgg}");
                sb.AppendLine($"Skipped protected gameplay slots: {skippedGameplayProtected}");
            }

            sb.AppendLine();
            sb.AppendLine("FINAL AUDIT");
            sb.AppendLine($"Legal: {finalLegal}");
            sb.AppendLine($"Invalid: {finalInvalid}");
            sb.AppendLine($"Canonical evolution-floor violations: {canonicalLevelViolations}");

            // Export reports to disk
            string exportedReportPath = ExportReports(sav, firstBox, lastBox, sb.ToString(), slotLogLines);
            if (!string.IsNullOrWhiteSpace(exportedReportPath))
            {
                sb.AppendLine();
                sb.AppendLine($"Report exported: {Path.GetFileName(exportedReportPath)}");
            }

            if (canonicalLevelDetails.Count != 0)
            {
                sb.AppendLine();
                sb.AppendLine("CANONICAL LEVEL VIOLATIONS:");
                foreach (var line in canonicalLevelDetails.Take(80))
                    sb.AppendLine(line);
                if (canonicalLevelDetails.Count > 80)
                    sb.AppendLine($"... plus {canonicalLevelDetails.Count - 80} more.");
            }

            if (invalidDetails.Count != 0)
            {
                sb.AppendLine();
                sb.AppendLine("INVALID:");
                foreach (var line in invalidDetails.Take(80))
                    sb.AppendLine(line);
                if (invalidDetails.Count > 80)
                    sb.AppendLine($"... plus {invalidDetails.Count - 80} more.");
            }

            if (regeneratedDetails.Count != 0)
            {
                sb.AppendLine();
                sb.AppendLine("ALM REGENERATED — review provenance:");
                foreach (var line in regeneratedDetails.Take(60))
                    sb.AppendLine(line);
                if (regeneratedDetails.Count > 60)
                    sb.AppendLine($"... plus {regeneratedDetails.Count - 60} more.");
            }

            if (details.Count != 0)
            {
                sb.AppendLine();
                sb.AppendLine("CHANGES / NOTES (first 80):");
                foreach (var line in details.Take(80))
                    sb.AppendLine(line);
                if (details.Count > 80)
                    sb.AppendLine($"... plus {details.Count - 80} more.");
            }

            WinFormsUtil.Alert(
                auditOnly ? "Living Dex audit complete." : "Living Dex polish and audit complete.",
                sb.ToString());
        }
        catch (MissingMethodException)
        {
            WinFormsUtil.Error(
                "PKHeX and PKHeX-Plugins appear to be version-mismatched.",
                "Install matching builds and try again.");
        }
        catch (Exception ex)
        {
            WinFormsUtil.Error("Living Dex processing failed.", ex.Message);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private static string ExportReports(SaveFile sav, int firstBox, int lastBox, string summaryText, List<string> slotLogLines)
    {
        try
        {
            string folder = Application.StartupPath;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                folder = AppDomain.CurrentDomain.BaseDirectory;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string txtPath = Path.Combine(folder, $"LivingDex_Report_{sav.Version}_{timestamp}.txt");
            string jsonPath = Path.Combine(folder, $"LivingDex_Report_{sav.Version}_{timestamp}.json");

            var sb = new StringBuilder();
            sb.AppendLine("===============================================================================");
            sb.AppendLine("           LIVING DEX POLISHER & LEGALITY AUDIT REPORT (v4.8)");
            sb.AppendLine("===============================================================================");
            sb.AppendLine($"Date & Time : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Game Version: {sav.Version} (Generation {sav.Generation}, Context: {sav.Context})");
            sb.AppendLine($"Trainer Info: OT = {sav.OT}, TID = {sav.TID16}, SID = {sav.SID16}");
            sb.AppendLine($"Boxes Range : Box {firstBox + 1} to Box {lastBox + 1}");
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("EXECUTIVE SUMMARY:");
            sb.AppendLine(summaryText);
            sb.AppendLine("-------------------------------------------------------------------------------");
            sb.AppendLine("COMPLETE SLOT-BY-SLOT AUDIT & DETAILS:");
            foreach (var line in slotLogLines)
                sb.AppendLine(line);
            sb.AppendLine("===============================================================================");

            File.WriteAllText(txtPath, sb.ToString(), Encoding.UTF8);

            var jsonSb = new StringBuilder();
            jsonSb.AppendLine("{");
            jsonSb.AppendLine($"  \"timestamp\": \"{DateTime.Now:O}\",");
            jsonSb.AppendLine($"  \"version\": \"{sav.Version}\",");
            jsonSb.AppendLine($"  \"trainer\": {{ \"ot\": \"{sav.OT}\", \"tid\": {sav.TID16}, \"sid\": {sav.SID16} }},");
            jsonSb.AppendLine($"  \"firstBox\": {firstBox + 1},");
            jsonSb.AppendLine($"  \"lastBox\": {lastBox + 1},");
            jsonSb.AppendLine("  \"entries\": [");
            for (int i = 0; i < slotLogLines.Count; i++)
            {
                string cleanLine = slotLogLines[i].Replace("\\", "\\\\").Replace("\"", "\\\"");
                string comma = (i == slotLogLines.Count - 1) ? "" : ",";
                jsonSb.AppendLine($"    \"{cleanLine}\"{comma}");
            }
            jsonSb.AppendLine("  ]");
            jsonSb.AppendLine("}");

            File.WriteAllText(jsonPath, jsonSb.ToString(), Encoding.UTF8);

            return txtPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryParseRange(string input, int max, out int first, out int last)
    {
        first = 0;
        last = 0;

        var parts = input.Split(new[] { '-', '–', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            if (!int.TryParse(parts[0].Trim(), out first) || first < 1 || first > max)
                return false;
            last = first;
            return true;
        }

        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[0].Trim(), out first) ||
                !int.TryParse(parts[1].Trim(), out last))
                return false;

            if (first < 1 || last < 1 || first > max || last > max || first > last)
                return false;

            return true;
        }

        return false;
    }

    private static bool IsSupportedSwitchGame(GameVersion version) => version switch
    {
        GameVersion.GP or GameVersion.GE => true,
        GameVersion.SW or GameVersion.SH => true,
        GameVersion.BD or GameVersion.SP => true,
        GameVersion.PLA => true,
        GameVersion.SL or GameVersion.VL => true,
        GameVersion.ZA => true,
        _ => false,
    };

    private static bool IsProtectedGameplaySlot(SaveFile sav, int box, int slot)
    {
        int index = (box * sav.BoxSlotCount) + slot;
        var flags = sav.GetBoxSlotFlags(index);

        if (flags.IsOverwriteProtected())
            return true;

        if (sav.Version is GameVersion.GP or GameVersion.GE)
            return flags.IsParty() >= 0;

        return false;
    }

    private static bool TryConservativeRepair(
        PKM source,
        IPersonalTable personal,
        out PKM repaired)
    {
        repaired = source.Clone();

        int start = Math.Max(1, (int)source.MetLevel);
        for (int level = start; level <= Experience.MaxLevel; level++)
        {
            var test = source.Clone();
            test.CurrentLevel = (byte)level;
            RefreshLevelDependentData(test);
            var la = new LegalityAnalysis(test, personal);
            if (la.Valid)
            {
                repaired = test;
                return true;
            }
        }

        var sourceLA = new LegalityAnalysis(source, personal);
        if (source.Ball == (byte)Ball.Master && sourceLA.EncounterOriginal is not EncounterInvalid)
        {
            Span<Ball> legal = stackalloc Ball[BallApplicator.MaxBallSpanAlloc];
            int count = BallApplicator.GetLegalBalls(legal, source, sourceLA);
            legal = legal[..count];

            foreach (var ball in GetThematicBallList(source))
            {
                if (!Contains(legal, ball))
                    continue;

                for (int level = start; level <= Experience.MaxLevel; level++)
                {
                    var test = source.Clone();
                    test.CurrentLevel = (byte)level;
                    test.Ball = (byte)ball;
                    RefreshLevelDependentData(test);
                    var la = new LegalityAnalysis(test, personal);
                    if (la.Valid)
                    {
                        repaired = test;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TryNormalizeToEvolutionMinimum(
        PKM source,
        IPersonalTable personal,
        LegalityAnalysis sourceLA,
        out PKM result,
        out int targetLevel,
        out bool movesAdjusted,
        out bool hyperTrainingCleared)
    {
        result = source.Clone();
        movesAdjusted = false;
        hyperTrainingCleared = false;

        targetLevel = GetEvolutionMinimumLevel(source, sourceLA, personal);
        targetLevel = Math.Clamp(targetLevel, 1, Experience.MaxLevel);

        if (targetLevel == source.CurrentLevel)
            return true;

        // 1) Target level only.
        var candidate = source.Clone();
        candidate.CurrentLevel = (byte)targetLevel;
        RefreshLevelDependentData(candidate);
        if (new LegalityAnalysis(candidate, personal).Valid)
        {
            result = candidate;
            return true;
        }

        // 2) Target level + natural moves/relearn.
        candidate = source.Clone();
        candidate.CurrentLevel = (byte)targetLevel;
        RefreshLevelDependentData(candidate);
        if (TryApplyNaturalMoves(candidate, personal) && new LegalityAnalysis(candidate, personal).Valid)
        {
            result = candidate;
            movesAdjusted = true;
            return true;
        }

        // 3) Target level + clear level-gated Hyper Training.
        candidate = source.Clone();
        candidate.CurrentLevel = (byte)targetLevel;
        bool cleared = ClearHyperTraining(candidate);
        RefreshLevelDependentData(candidate);
        if (cleared && new LegalityAnalysis(candidate, personal).Valid)
        {
            result = candidate;
            hyperTrainingCleared = true;
            return true;
        }

        // 4) Combine both progression fixes.
        candidate = source.Clone();
        candidate.CurrentLevel = (byte)targetLevel;
        cleared = ClearHyperTraining(candidate);
        RefreshLevelDependentData(candidate);
        bool adjusted = TryApplyNaturalMoves(candidate, personal);
        RefreshLevelDependentData(candidate);
        if ((cleared || adjusted) && new LegalityAnalysis(candidate, personal).Valid)
        {
            result = candidate;
            movesAdjusted = adjusted;
            hyperTrainingCleared = cleared;
            return true;
        }

        // 5) Upward scan for move-gated evolutions (e.g. Tsareena with Stomp at Lv. 28, Mamoswine with Ancient Power, etc.)
        for (int lvl = targetLevel + 1; lvl <= Math.Min(Experience.MaxLevel, Math.Max(targetLevel + 40, (int)source.CurrentLevel)); lvl++)
        {
            candidate = source.Clone();
            candidate.CurrentLevel = (byte)lvl;
            cleared = ClearHyperTraining(candidate);
            RefreshLevelDependentData(candidate);
            adjusted = TryApplyNaturalMoves(candidate, personal);
            RefreshLevelDependentData(candidate);
            if (new LegalityAnalysis(candidate, personal).Valid)
            {
                result = candidate;
                targetLevel = lvl;
                movesAdjusted = adjusted;
                hyperTrainingCleared = cleared;
                return true;
            }
        }

        return false;
    }

    public static int GetEvolutionMinimumLevel(PKM pk, LegalityAnalysis la, IPersonalTable personal)
    {
        var tree = EvolutionTree.GetEvolutionTree(pk.Context);
        var allStages = tree.Reverse.GetPreEvolutions(pk.Species, pk.Form).ToList();
        allStages.Add((pk.Species, pk.Form));

        // Filter stages to only species that exist in the loaded game's personal table.
        // This correctly handles games (such as LGPE) where baby pre-evolutions (Pichu, Cleffa, etc.)
        // do not exist in the game.
        var stages = allStages.Where(s => personal.IsSpeciesInGame(s.Species)).ToList();

        // Base-stage Pokémon in this game have no evolution floor beyond their actual MetLevel.
        if (stages.Count <= 1)
            return Math.Max(1, (int)pk.MetLevel);

        // Propagate the normal evolution chain from stage 0 (the game's base stage).
        // Starting level is the Pokémon's actual MetLevel.
        if (TryPropagateEvolutionMinimum(
                tree,
                stages,
                0,
                Math.Max(1, (int)pk.MetLevel),
                out int canonical))
            return Math.Max((int)pk.MetLevel, canonical);

        // Conservative fallback if a future game introduces a tree edge that the
        // helper cannot resolve: never lower the existing legal level.
        return Math.Max((int)pk.MetLevel, (int)pk.CurrentLevel);
    }

    private static bool TryPropagateEvolutionMinimum(
        EvolutionTree tree,
        List<(ushort Species, byte Form)> stages,
        int startIndex,
        int startLevel,
        out int result)
    {
        int level = Math.Max(1, startLevel);

        for (int i = startIndex; i < stages.Count - 1; i++)
        {
            var from = stages[i];
            var to = stages[i + 1];

            int best = int.MaxValue;
            var methods = tree.Forward.GetForward(from.Species, from.Form).Span;

            foreach (var method in methods)
            {
                if (method.Species != to.Species)
                    continue;

                byte destinationForm = method.GetDestinationForm(from.Form);
                if (destinationForm != to.Form)
                    continue;

                // level-up evolution:
                // MAX(previous stage level + method.LevelUp, method.Level)
                // item/trade/manual evolution:
                // no artificial +1 unless LevelUp > 0
                int candidate = Math.Max(level + method.LevelUp, method.Level);
                if (candidate < best)
                    best = candidate;
            }

            if (best == int.MaxValue)
            {
                result = level;
                return false;
            }

            level = best;
            if (level > Experience.MaxLevel)
            {
                result = Experience.MaxLevel;
                return false;
            }
        }

        result = level;
        return true;
    }

    public static bool TryApplyThematicBall(PKM pk, LegalityAnalysis la, out Ball chosenBall)
    {
        chosenBall = (Ball)pk.Ball;
        if (!la.Valid) return false;

        Span<Ball> legal = stackalloc Ball[BallApplicator.MaxBallSpanAlloc];
        int count = BallApplicator.GetLegalBalls(legal, pk, la);
        if (count <= 1) return false;
        legal = legal[..count];

        var preferences = GetThematicBallList(pk);
        uint seed = pk.EncryptionConstant ^ pk.PID ^ ((uint)pk.Species << 16) ^
                    ((uint)pk.MetLocation << 4) ^ pk.MetLevel;

        var allowedCandidates = new List<Ball>();
        foreach (var b in preferences)
        {
            if (Contains(legal, b))
                allowedCandidates.Add(b);
        }

        if (allowedCandidates.Count == 0) return false;

        var topCandidates = allowedCandidates.Take(Math.Min(3, allowedCandidates.Count)).ToList();
        var selected = topCandidates[(int)(seed % (uint)topCandidates.Count)];

        if (selected == (Ball)pk.Ball) return false;

        byte old = pk.Ball;
        pk.Ball = (byte)selected;
        var testLA = new LegalityAnalysis(pk, pk.PersonalInfo);
        if (testLA.Valid && SameEncounterIdentity(la.EncounterOriginal, testLA.EncounterOriginal))
        {
            chosenBall = selected;
            return true;
        }

        pk.Ball = old;
        return false;
    }

    public static List<Ball> GetThematicBallList(PKM pk)
    {
        var list = new List<Ball>();
        var types = new[] { (MoveType)pk.PersonalInfo.Type1, (MoveType)pk.PersonalInfo.Type2 };

        if (pk.Version == GameVersion.PLA || pk.Context == EntityContext.Gen8a)
        {
            if (types.Contains(MoveType.Flying) || pk.CurrentLevel <= 20)
                list.AddRange([Ball.LAFeather, Ball.LAWing, Ball.LAJet]);
            if (pk.PersonalInfo.Weight >= 100 || types.Contains(MoveType.Steel) || types.Contains(MoveType.Rock))
                list.AddRange([Ball.LAHeavy, Ball.LALeaden, Ball.LAGigaton]);
            list.AddRange([Ball.LAUltra, Ball.LAGreat, Ball.LAPoke]);
            return list;
        }

        if (pk.IsShiny)
        {
            list.Add(Ball.Premier);
            list.Add(Ball.Luxury);
        }

        if (types.Contains(MoveType.Ghost) || types.Contains(MoveType.Dark) || types.Contains(MoveType.Psychic) || types.Contains(MoveType.Fairy))
        {
            list.AddRange([Ball.Moon, Ball.Dusk, Ball.Dream, Ball.Love, Ball.Heal, Ball.Premier]);
        }

        if (types.Contains(MoveType.Water) || types.Contains(MoveType.Ice))
        {
            list.AddRange([Ball.Dive, Ball.Lure, Ball.Net, Ball.Great]);
        }

        if (types.Contains(MoveType.Bug))
        {
            list.AddRange([Ball.Net, Ball.Nest, Ball.Sport, Ball.Safari]);
        }

        if (types.Contains(MoveType.Grass) || types.Contains(MoveType.Poison))
        {
            list.AddRange([Ball.Nest, Ball.Friend, Ball.Safari]);
        }

        if (types.Contains(MoveType.Fire) || types.Contains(MoveType.Electric) || types.Contains(MoveType.Dragon))
        {
            list.AddRange([Ball.Fast, Ball.Level, Ball.Repeat, Ball.Ultra]);
        }

        if (pk.PersonalInfo.Weight >= 100 || types.Contains(MoveType.Steel) || types.Contains(MoveType.Rock) || types.Contains(MoveType.Ground))
        {
            list.AddRange([Ball.Heavy, Ball.Level, Ball.Ultra]);
        }

        if (pk.MetLevel <= 15)
        {
            list.AddRange([Ball.Poke, Ball.Premier, Ball.Great]);
        }
        else if (pk.MetLevel <= 35)
        {
            list.AddRange([Ball.Great, Ball.Luxury, Ball.Ultra, Ball.Poke]);
        }
        else
        {
            list.AddRange([Ball.Ultra, Ball.Timer, Ball.Luxury, Ball.Great]);
        }

        return list;
    }

    public static bool TryOptimizeIVs(PKM pk, IPersonalTable personal)
    {
        bool isSpecialAttacker = pk.PersonalInfo.SPA > pk.PersonalInfo.ATK + 25;

        Span<int> oldIVs = stackalloc int[6];
        pk.GetIVs(oldIVs);

        Span<int> targetIVs = stackalloc int[6] { 31, isSpecialAttacker ? 0 : 31, 31, 31, 31, 31 };

        if (oldIVs.SequenceEqual(targetIVs))
            return false;

        var candidate = pk.Clone();
        candidate.SetIVs(targetIVs);
        RefreshLevelDependentData(candidate);
        var la = new LegalityAnalysis(candidate, personal);
        if (la.Valid)
        {
            pk.SetIVs(targetIVs);
            RefreshLevelDependentData(pk);
            return true;
        }

        if (isSpecialAttacker)
        {
            Span<int> standard31 = stackalloc int[6] { 31, 31, 31, 31, 31, 31 };
            candidate = pk.Clone();
            candidate.SetIVs(standard31);
            RefreshLevelDependentData(candidate);
            la = new LegalityAnalysis(candidate, personal);
            if (la.Valid)
            {
                pk.SetIVs(standard31);
                RefreshLevelDependentData(pk);
                return true;
            }
        }

        return false;
    }

    public static bool TryApplyGigantamax(PKM pk, SaveFile sav)
    {
        if (sav.Version is not (GameVersion.SW or GameVersion.SH))
            return false;

        if (pk is not IGigantamax gmax || gmax.CanGigantamax)
            return false;

        gmax.CanGigantamax = true;
        var la = new LegalityAnalysis(pk, sav.Personal);
        if (!la.Valid)
        {
            gmax.CanGigantamax = false;
            return false;
        }

        return true;
    }

    public static bool TryApplyNaturalMoves(PKM pk, IPersonalTable personal)
    {
        Span<ushort> oldMoves = stackalloc ushort[4];
        Span<ushort> oldRelearn = stackalloc ushort[4];
        pk.GetMoves(oldMoves);
        pk.GetRelearnMoves(oldRelearn);

        var la = new LegalityAnalysis(pk, personal);
        if (!la.Parsed)
            return false;

        const MoveSourceType natural = MoveSourceType.LevelUp | MoveSourceType.RelearnMoves | MoveSourceType.Evolve;
        Span<ushort> moves = stackalloc ushort[4];
        la.GetSuggestedCurrentMoves(moves, natural);

        if (moves[0] == 0)
        {
            moves.Clear();
            la.GetSuggestedCurrentMoves(moves, MoveSourceType.Encounter);
        }

        if (moves[0] != 0)
        {
            pk.SetMoves(moves);
            pk.SetMaximumPPCurrent(moves);
        }

        var afterMovesLA = new LegalityAnalysis(pk, personal);
        Span<ushort> relearn = stackalloc ushort[4];
        afterMovesLA.GetSuggestedRelearnMovesFromEncounter(relearn);
        pk.SetRelearnMoves(relearn);

        // Keldeo Form 0 cannot know Secret Sword; Form 1 MUST know Secret Sword
        if (pk.Species == (ushort)Species.Keldeo)
        {
            ushort secretSword = (ushort)Move.SecretSword;
            Span<ushort> curMoves = stackalloc ushort[4];
            pk.GetMoves(curMoves);
            if (pk.Form == 0 && curMoves.Contains(secretSword))
            {
                for (int m = 0; m < 4; m++)
                {
                    if (curMoves[m] == secretSword)
                        curMoves[m] = (ushort)Move.SacredSword;
                }
                pk.SetMoves(curMoves);
                pk.SetMaximumPPCurrent(curMoves);
            }
            else if (pk.Form == 1 && !curMoves.Contains(secretSword))
            {
                curMoves[0] = secretSword;
                pk.SetMoves(curMoves);
                pk.SetMaximumPPCurrent(curMoves);
            }
        }

        Span<ushort> newMoves = stackalloc ushort[4];
        Span<ushort> newRelearn = stackalloc ushort[4];
        pk.GetMoves(newMoves);
        pk.GetRelearnMoves(newRelearn);

        return !oldMoves.SequenceEqual(newMoves) || !oldRelearn.SequenceEqual(newRelearn);
    }

    public static bool ClearHyperTraining(PKM pk)
    {
        if (pk is not IHyperTrain ht || ht.HyperTrainFlags == 0)
            return false;

        ht.HyperTrainFlags = 0;
        return true;
    }

    public static void RefreshLevelDependentData(PKM pk)
    {
        if (pk is PB7 pb7)
            pb7.ResetCP();
    }

    private static bool SameEncounterIdentity(IEncounterTemplate a, IEncounterTemplate b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is EncounterInvalid || b is EncounterInvalid)
            return false;

        return a.GetType() == b.GetType()
            && a.Species == b.Species
            && a.Form == b.Form
            && a.Version == b.Version
            && a.Generation == b.Generation
            && a.Context == b.Context
            && a.Location == b.Location
            && a.LevelMin == b.LevelMin
            && a.LevelMax == b.LevelMax
            && a.IsEgg == b.IsEgg
            && a.FixedBall == b.FixedBall;
    }

    private static bool Contains(ReadOnlySpan<Ball> balls, Ball value)
    {
        foreach (var ball in balls)
        {
            if (ball == value)
                return true;
        }

        return false;
    }

    private static bool IsNativeOrPaired(GameVersion origin, GameVersion save) => save switch
    {
        GameVersion.GP or GameVersion.GE => origin is GameVersion.GP or GameVersion.GE,
        GameVersion.SW or GameVersion.SH => origin is GameVersion.SW or GameVersion.SH,
        GameVersion.BD or GameVersion.SP => origin is GameVersion.BD or GameVersion.SP,
        GameVersion.SL or GameVersion.VL => origin is GameVersion.SL or GameVersion.VL,
        GameVersion.PLA => origin == GameVersion.PLA,
        GameVersion.ZA => origin == GameVersion.ZA,
        _ => false,
    };

    private static string GetName(PKM pk)
    {
        var species = GameInfo.Strings.Species[pk.Species];
        if (pk.Form == 0)
            return species;

        var forms = FormConverter.GetFormList(
            pk.Species,
            GameInfo.Strings.types,
            GameInfo.Strings.forms,
            GameInfo.GenderSymbolUnicode,
            pk.Context);

        if ((uint)pk.Form < (uint)forms.Length && !string.IsNullOrWhiteSpace(forms[pk.Form]))
            return $"{species}-{forms[pk.Form]}";

        return $"{species}-Form{pk.Form}";
    }
}
