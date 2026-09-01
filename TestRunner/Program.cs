using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class Program
{
    public static void Main(string[] args)
    {
        CustomizerTest.Run();
        return;
        Console.WriteLine("=== Testing Full Polisher Suite with Thematic Balls, Smart IVs & Reports ===");

        string savePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\savedata.bin";
        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found at {savePath}");
            return;
        }

        byte[] saveBytes = File.ReadAllBytes(savePath);
        var sav = SaveUtil.GetSaveFile(saveBytes);
        if (sav == null)
        {
            Console.WriteLine("Failed to load save file.");
            return;
        }

        Console.WriteLine($"Loaded Save: Version = {sav.Version}, Context = {sav.Context}");
        Console.WriteLine($"Trainer: OT = {sav.OT}, TID = {sav.TID16}, SID = {sav.SID16}");

        SimulateLivingDexAndPolish(sav);
    }

    private static void SimulateLivingDexAndPolish(SaveFile sav)
    {
        var testSav = sav.Clone();

        TrainerSettings.Register(testSav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NativeOnly;
        APILegality.SetMatchingBalls = false;
        APILegality.SetAllLegalRibbons = false;
        APILegality.ForceLevel100for50 = false;
        APILegality.SetBattleVersion = false;

        var normalCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = testSav.Version,
        };

        var shinyCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = true,
            SetAlpha = false,
            TransferVersion = testSav.Version,
        };

        Console.WriteLine("Generating Normal Living Dex...");
        var normalGenerated = testSav.GenerateLivingDex(testSav.Personal, normalCfg).ToList();

        Console.WriteLine("Generating Shiny Living Dex...");
        var shinyGenerated = testSav.GenerateLivingDex(testSav.Personal, shinyCfg).ToList();

        var normal = normalGenerated.Where(z => !IsSaveBoundPartner(z)).ToList();
        var shiny = shinyGenerated.Where(z => z.IsShiny && !IsSaveBoundPartner(z)).ToList();

        Console.WriteLine($"Normal (excluding partner): {normal.Count}");
        Console.WriteLine($"Shiny (shiny-only, excluding partner): {shiny.Count}");

        int normalStart = GetLivingDexStartSlot(testSav);
        if (!TryPlanWritableSlots(testSav, normalStart, normal.Count, out var normalSlots))
        {
            Console.WriteLine("ERROR: Normal Living Dex does not fit.");
            return;
        }

        int afterNormal = normalSlots.Count == 0 ? normalStart : normalSlots[^1] + 1;
        int shinyStart = AlignToNextBox(afterNormal, testSav.BoxSlotCount);

        if (!TryPlanWritableSlots(testSav, shinyStart, shiny.Count, out var shinySlots))
        {
            Console.WriteLine("ERROR: Shiny Living Dex does not fit.");
            return;
        }

        for (int i = 0; i < normal.Count; i++)
            testSav.SetBoxSlotAtIndex(normal[i], normalSlots[i]);

        for (int i = 0; i < shiny.Count; i++)
            testSav.SetBoxSlotAtIndex(shiny[i], shinySlots[i]);

        Console.WriteLine($"Placed {normal.Count} Normal and {shiny.Count} Shiny Pokémon in boxes.");

        Console.WriteLine("\n--- Polishing ALL boxes with Enhanced Suite ---");
        PolishSave(testSav);
    }

    private static void PolishSave(SaveFile sav)
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

        for (int box = 0; box < sav.BoxCount; box++)
        {
            for (int slot = 0; slot < sav.BoxSlotCount; slot++)
            {
                var original = sav.GetBoxSlotAtIndex(box, slot);
                if (original.Species == 0)
                    continue;

                found++;

                if (IsProtectedGameplaySlot(sav, (box * sav.BoxSlotCount) + slot))
                {
                    skippedGameplayProtected++;
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

                    if (TryConservativeRepair(original, sav.Personal, out var repaired))
                    {
                        working = repaired;
                        safeRepaired++;
                    }
                    else
                    {
                        var regenerated = sav.Legalize(original, sourceLA);
                        var regenLA = new LegalityAnalysis(regenerated, sav.Personal);

                        if (!regenLA.Valid ||
                            regenerated is IHomeTrack { HasTracker: true } ||
                            !IsNativeOrPaired(regenerated.Version, sav.Version))
                        {
                            regenerationRejected++;
                            Console.WriteLine($"[UNRESOLVED] Box {box + 1}, Slot {slot + 1}: {GetName(original)} Lv.{original.CurrentLevel} - {sourceLA.Report()}");
                            continue;
                        }

                        working = regenerated;
                        almRegenerated++;
                    }
                }

                var beforeLA = new LegalityAnalysis(working, sav.Personal);
                if (!beforeLA.Valid)
                {
                    Console.WriteLine($"[UNRESOLVED AFTER REPAIR] Box {box + 1}, Slot {slot + 1}: {GetName(original)}");
                    continue;
                }

                // 1. Level Normalization
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
                    }
                }
                else
                {
                    evolutionTargetBlocked++;
                    Console.WriteLine($"[LEVEL BLOCKED] Box {box + 1}, Slot {slot + 1}: {GetName(working)} Lv.{oldLevel}; Target Lv.{targetLevel}");
                }

                // 2. Thematic Pokéball Selection (Automatic)
                var levelLA = new LegalityAnalysis(working, sav.Personal);
                if (levelLA.Valid)
                {
                    var ballCandidate = working.Clone();
                    if (TryApplyThematicBall(ballCandidate, levelLA, out var replacementBall))
                    {
                        working = ballCandidate;
                        ballsThemed++;
                        details.Add($"BALL — Box {box + 1}, Slot {slot + 1}: {GetName(working)} -> {replacementBall}");
                    }
                }

                // 3. Smart IVs Optimization
                if (TryOptimizeIVs(working, sav.Personal))
                {
                    ivsOptimized++;
                }

                // 4. G-Max Factor (SWSH)
                if (TryApplyGigantamax(working, sav))
                {
                    gmaxApplied++;
                }

                var finalLA = new LegalityAnalysis(working, sav.Personal);
                if (!finalLA.Valid)
                {
                    Console.WriteLine($"[GUARD BLOCKED] Box {box + 1}, Slot {slot + 1}: {GetName(original)} - {finalLA.Report()}");
                    continue;
                }

                sav.SetBoxSlotAtIndex(working, box, slot);
            }
        }

        Console.WriteLine($"Found: {found}");
        Console.WriteLine($"Initially legal: {initialLegal}, Initially invalid: {initialInvalid}");
        Console.WriteLine($"Safe repaired: {safeRepaired}, ALM regenerated: {almRegenerated}, Rejected: {regenerationRejected}");
        Console.WriteLine($"Levels changed: {changedLevel}, Moves adjusted: {movesAdjusted}, Hyper Training cleared: {hyperTrainingCleared}");
        Console.WriteLine($"Thematic Balls applied: {ballsThemed}");
        Console.WriteLine($"IVs optimized: {ivsOptimized}");
        Console.WriteLine($"G-Max applied: {gmaxApplied}");
        Console.WriteLine($"Level targets blocked: {evolutionTargetBlocked}");
        Console.WriteLine($"Skipped gameplay protected: {skippedGameplayProtected}");

        // Final audit
        int finalLegal = 0;
        int finalInvalid = 0;
        int canonicalLevelViolations = 0;

        Console.WriteLine("\n--- FINAL AUDIT & DETAILED POKÉMON LIST ---");
        for (int box = 0; box < sav.BoxCount; box++)
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
                    if (!pk.IsEgg && !IsProtectedGameplaySlot(sav, (box * sav.BoxSlotCount) + slot))
                    {
                        int canonicalTarget = GetEvolutionMinimumLevel(pk, la, sav.Personal);
                        if (pk.CurrentLevel < canonicalTarget)
                        {
                            canonicalLevelViolations++;
                            Console.WriteLine($"[CANONICAL VIOLATION] Box {box + 1}, Slot {slot + 1}: {GetName(pk)} Lv.{pk.CurrentLevel} < canonical Lv.{canonicalTarget} (Met {pk.MetLevel})");
                        }
                    }
                }
                else
                {
                    finalInvalid++;
                    Console.WriteLine($"[INVALID] Box {box + 1}, Slot {slot + 1}: {GetName(pk)} Lv.{pk.CurrentLevel} (Met {pk.MetLevel}) - {la.Report()}");
                }
            }
        }

        Console.WriteLine($"\nFinal Legal: {finalLegal}");
        Console.WriteLine($"Final Invalid: {finalInvalid}");
        Console.WriteLine($"Canonical Evolution-Floor Violations: {canonicalLevelViolations}");

        // Print all boxes summary
        Console.WriteLine("\n--- Box by Box Overview (Sample) ---");
        for (int box = 0; box < sav.BoxCount; box++)
        {
            var pks = sav.GetBoxData(box).Where(p => p.Species != 0).ToList();
            if (pks.Count == 0) continue;
            Console.WriteLine($"\n=== Box {box + 1} ({pks.Count} Pokémon) ===");
            for (int slot = 0; slot < Math.Min(10, pks.Count); slot++)
            {
                var pk = pks[slot];
                string shinyTag = pk.IsShiny ? " ★" : "  ";
                string ivs = $"{pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
                Console.WriteLine($"Slot {slot + 1,2}: {GetName(pk),-20}{shinyTag} | Lv.{pk.CurrentLevel,3} | Met Lv.{pk.MetLevel,3} | Ball: {(Ball)pk.Ball,-10} | IVs: {ivs,-17} | OT: {pk.OriginalTrainerName,-10}");
            }
            if (pks.Count > 10)
                Console.WriteLine($"  ... plus {pks.Count - 10} more in this box.");
        }
    }

    private static int AlignToNextBox(int slot, int boxSize)
    {
        if (slot <= 0) return 0;
        return ((slot + boxSize - 1) / boxSize) * boxSize;
    }

    private static bool IsSaveBoundPartner(PKM pk) => pk is PB7 { IsStarter: true };

    private static bool IsProtectedGameplaySlot(SaveFile sav, int index)
    {
        var flags = sav.GetBoxSlotFlags(index);
        if (flags.IsOverwriteProtected()) return true;
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

        return highestProtected < 0 ? 0 : AlignToNextBox(highestProtected + 1, sav.BoxSlotCount);
    }

    private static bool TryPlanWritableSlots(SaveFile sav, int start, int count, out List<int> slots)
    {
        slots = new List<int>(count);
        int capacity = sav.BoxCount * sav.BoxSlotCount;

        for (int index = Math.Max(0, start); index < capacity && slots.Count < count; index++)
        {
            if (IsProtectedGameplaySlot(sav, index))
                continue;
            slots.Add(index);
        }

        return slots.Count == count;
    }

    private static bool TryConservativeRepair(PKM source, IPersonalTable personal, out PKM repaired)
    {
        repaired = source.Clone();
        int start = Math.Max(1, (int)source.MetLevel);
        for (int level = start; level <= 100; level++)
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
                if (!Contains(legal, ball)) continue;
                for (int level = start; level <= 100; level++)
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

        var candidate = source.Clone();
        candidate.CurrentLevel = (byte)targetLevel;
        RefreshLevelDependentData(candidate);
        if (new LegalityAnalysis(candidate, personal).Valid)
        {
            result = candidate;
            return true;
        }

        candidate = source.Clone();
        candidate.CurrentLevel = (byte)targetLevel;
        RefreshLevelDependentData(candidate);
        if (TryApplyNaturalMoves(candidate, personal) && new LegalityAnalysis(candidate, personal).Valid)
        {
            result = candidate;
            movesAdjusted = true;
            return true;
        }

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

        return false;
    }

    public static int GetEvolutionMinimumLevel(PKM pk, LegalityAnalysis la, IPersonalTable personal)
    {
        var tree = EvolutionTree.GetEvolutionTree(pk.Context);
        var allStages = tree.Reverse.GetPreEvolutions(pk.Species, pk.Form).ToList();
        allStages.Add((pk.Species, pk.Form));

        var stages = allStages.Where(s => personal.IsSpeciesInGame(s.Species)).ToList();

        if (stages.Count <= 1)
            return Math.Max(1, (int)pk.MetLevel);

        if (TryPropagateEvolutionMinimum(
                tree,
                stages,
                0,
                Math.Max(1, (int)pk.MetLevel),
                out int canonical))
        {
            return Math.Max((int)pk.MetLevel, canonical);
        }

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

    private static bool TryApplyNaturalMoves(PKM pk, IPersonalTable personal)
    {
        Span<ushort> oldMoves = stackalloc ushort[4];
        Span<ushort> oldRelearn = stackalloc ushort[4];
        pk.GetMoves(oldMoves);
        pk.GetRelearnMoves(oldRelearn);

        var la = new LegalityAnalysis(pk, personal);
        if (!la.Parsed) return false;

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

        Span<ushort> newMoves = stackalloc ushort[4];
        Span<ushort> newRelearn = stackalloc ushort[4];
        pk.GetMoves(newMoves);
        pk.GetRelearnMoves(newRelearn);
        return !oldMoves.SequenceEqual(newMoves) || !oldRelearn.SequenceEqual(newRelearn);
    }

    private static bool ClearHyperTraining(PKM pk)
    {
        if (pk is not IHyperTrain ht || ht.HyperTrainFlags == 0) return false;
        ht.HyperTrainFlags = 0;
        return true;
    }

    private static void RefreshLevelDependentData(PKM pk)
    {
        if (pk is PB7 pb7) pb7.ResetCP();
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

    private static bool SameEncounterIdentity(IEncounterTemplate a, IEncounterTemplate b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is EncounterInvalid || b is EncounterInvalid) return false;

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
            if (ball == value) return true;
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
        if (pk.Form == 0) return species;

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
