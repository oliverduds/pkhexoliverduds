using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PKHeX.Core;

namespace TestRunner;

public static class TestEventDatabase
{
    public static void Run()
    {
        var asm = typeof(PKM).Assembly;
        var t = asm.GetType("PKHeX.Core.EncounterEvent");
        var m = t?.GetMethod("RefreshMGDB", BindingFlags.Public | BindingFlags.Static);
        Console.WriteLine($"RefreshMGDB method: {m}");

        // Check if there are default search directories or paths
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        Console.WriteLine($"BaseDirectory: {appDir}");

        // Check PKHeX executable dir
        string pkhexDir = @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex";
        string mgdbPath = Path.Combine(pkhexDir, "mgdb");
        Console.WriteLine($"mgdb target path: {mgdbPath}");
    }
}
