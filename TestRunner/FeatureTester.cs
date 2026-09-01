using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class FeatureTester
{
    public static void Run()
    {
        Console.WriteLine("=== Testing New Living Dex Enhancements ===");

        string savePath = @"c:\Users\Eduardo\Documents\projetos\pkhexoliverduds\savedata.bin";
        byte[] saveBytes = File.ReadAllBytes(savePath);
        var sav = SaveUtil.GetSaveFile(saveBytes)!;

        TrainerSettings.Register(sav);
        APILegality.UseTrainerData = true;
        APILegality.GameVersionPriority = GameVersionPriorityType.NativeOnly;

        var normalCfg = new LivingDexConfig
        {
            IncludeForms = true,
            SetShiny = false,
            SetAlpha = false,
            TransferVersion = sav.Version,
        };

        var normalGenerated = sav.GenerateLivingDex(sav.Personal, normalCfg).ToList();
        var normal = normalGenerated.Where(z => !(z is PB7 pb7 && pb7.IsStarter)).ToList();

        Console.WriteLine($"Testing Thematic Pokéballs and Smart IVs across {normal.Count} Pokémon...\n");

        int ballsThemed = 0;
        int ivsOptimized = 0;

        foreach (var pk in normal)
        {
            var la = new LegalityAnalysis(pk, sav.Personal);
            
            // 1. Level normalization
            int targetLevel = Program.GetEvolutionMinimumLevel(pk, la, sav.Personal);
            pk.CurrentLevel = (byte)targetLevel;
            if (pk is PB7 pb7) pb7.ResetCP();

            var levelLA = new LegalityAnalysis(pk, sav.Personal);

            // 2. Thematic Ball
            byte originalBall = pk.Ball;
            if (TryApplyThematicBall(pk, levelLA, out var chosenBall))
            {
                ballsThemed++;
            }

            // 3. Smart IVs
            if (TryOptimizeIVs(pk, sav.Personal))
            {
                ivsOptimized++;
            }

            var finalLA = new LegalityAnalysis(pk, sav.Personal);
            string ballChange = originalBall != pk.Ball ? $" [Ball: {(Ball)originalBall} -> {(Ball)pk.Ball}]" : $" [Ball: {(Ball)pk.Ball}]";
            string ivStr = $"IVs: {pk.IV_HP}/{pk.IV_ATK}/{pk.IV_DEF}/{pk.IV_SPA}/{pk.IV_SPD}/{pk.IV_SPE}";
            Console.WriteLine($"{pk.Species,3} {GameInfo.Strings.Species[pk.Species],-16} Lv.{pk.CurrentLevel,3} (Met {pk.MetLevel,2}){ballChange,-30} | {ivStr,-25} | Valid: {finalLA.Valid}");
            
            if (!finalLA.Valid)
            {
                Console.WriteLine($"  -> ERROR: {finalLA.Report()}");
            }
        }

        Console.WriteLine($"\nTotal Balls Themed: {ballsThemed}");
        Console.WriteLine($"Total IVs Optimized: {ivsOptimized}");
    }

    public static bool TryApplyThematicBall(PKM pk, LegalityAnalysis la, out Ball chosenBall)
    {
        chosenBall = (Ball)pk.Ball;
        if (!la.Valid) return false;

        Span<Ball> legal = stackalloc Ball[BallApplicator.MaxBallSpanAlloc];
        int count = BallApplicator.GetLegalBalls(legal, pk, la);
        if (count <= 1) return false; // Fixed/only ball or none
        legal = legal[..count];

        var preferences = GetThematicBallList(pk);
        foreach (var candidate in preferences)
        {
            if (Contains(legal, candidate) && candidate != (Ball)pk.Ball)
            {
                byte old = pk.Ball;
                pk.Ball = (byte)candidate;
                var testLA = new LegalityAnalysis(pk, pk.PersonalInfo);
                if (testLA.Valid)
                {
                    chosenBall = candidate;
                    return true;
                }
                pk.Ball = old;
            }
        }

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

        // Shinies love Premier and Luxury Balls
        if (pk.IsShiny)
        {
            list.Add(Ball.Premier);
            list.Add(Ball.Luxury);
        }

        // Water / Ice
        if (types.Contains(MoveType.Water) || types.Contains(MoveType.Ice))
        {
            list.AddRange([Ball.Dive, Ball.Lure, Ball.Net, Ball.Great]);
        }

        // Bug
        if (types.Contains(MoveType.Bug))
        {
            list.AddRange([Ball.Net, Ball.Nest, Ball.Sport, Ball.Safari]);
        }

        // Ghost / Dark / Psychic / Fairy
        if (types.Contains(MoveType.Ghost) || types.Contains(MoveType.Dark) || types.Contains(MoveType.Psychic) || types.Contains(MoveType.Fairy))
        {
            list.AddRange([Ball.Moon, Ball.Dusk, Ball.Dream, Ball.Love, Ball.Heal, Ball.Premier]);
        }

        // Grass / Poison
        if (types.Contains(MoveType.Grass) || types.Contains(MoveType.Poison))
        {
            list.AddRange([Ball.Nest, Ball.Friend, Ball.Safari]);
        }

        // Fire / Electric / Dragon
        if (types.Contains(MoveType.Fire) || types.Contains(MoveType.Electric) || types.Contains(MoveType.Dragon))
        {
            list.AddRange([Ball.Fast, Ball.Level, Ball.Repeat, Ball.Ultra]);
        }

        // Heavy / Rock / Steel / Ground
        if (pk.PersonalInfo.Weight >= 100 || types.Contains(MoveType.Steel) || types.Contains(MoveType.Rock) || types.Contains(MoveType.Ground))
        {
            list.AddRange([Ball.Heavy, Ball.Level, Ball.Ultra]);
        }

        // Level-based defaults
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
        // Check if species is Special Attacker
        bool isSpecialAttacker = pk.PersonalInfo.SPA > pk.PersonalInfo.ATK + 25;

        Span<int> oldIVs = stackalloc int[6];
        pk.GetIVs(oldIVs);

        Span<int> targetIVs = stackalloc int[6] { 31, isSpecialAttacker ? 0 : 31, 31, 31, 31, 31 };

        if (oldIVs.SequenceEqual(targetIVs))
            return false;

        var candidate = pk.Clone();
        candidate.SetIVs(targetIVs);
        var la = new LegalityAnalysis(candidate, personal);
        if (la.Valid)
        {
            pk.SetIVs(targetIVs);
            return true;
        }

        // If 0 Atk wasn't legal or 6x31 wasn't legal, try standard 6x31
        if (isSpecialAttacker)
        {
            Span<int> standard31 = stackalloc int[6] { 31, 31, 31, 31, 31, 31 };
            candidate = pk.Clone();
            candidate.SetIVs(standard31);
            la = new LegalityAnalysis(candidate, personal);
            if (la.Valid)
            {
                pk.SetIVs(standard31);
                return true;
            }
        }

        return false;
    }

    private static bool Contains(ReadOnlySpan<Ball> balls, Ball value)
    {
        foreach (var ball in balls)
        {
            if (ball == value) return true;
        }
        return false;
    }
}
