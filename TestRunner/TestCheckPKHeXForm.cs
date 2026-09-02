using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TestRunner;

public static class TestCheckPKHeXForm
{
    public static void Run()
    {
        string pkhexExe = @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex\PKHeX.exe";
        var asm = Assembly.LoadFrom(pkhexExe);
        Console.WriteLine($"Loaded PKHeX assembly: {asm.FullName}");

        var mgdbTypes = asm.GetTypes().Where(t => t.Name.Contains("MGDB") || t.Name.Contains("MysteryGift")).ToList();
        foreach (var t in mgdbTypes)
        {
            Console.WriteLine($"  {t.FullName}");
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (m.Name.Contains("Load") || m.Name.Contains("Refresh") || m.Name.Contains("Init"))
                    Console.WriteLine($"    Method: {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
            }
        }
    }
}
