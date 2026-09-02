using System;
using System.Linq;
using System.Reflection;

namespace TestRunner;

public static class TestSpriteGeneration
{
    public static void Run()
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var types = a.GetTypes().Where(t => t.Name.Contains("Sprite")).ToList();
            if (types.Count > 0)
            {
                Console.WriteLine($"Assembly: {a.GetName().Name} has {types.Count} Sprite types:");
                foreach (var t in types)
                {
                    Console.WriteLine($"  {t.FullName}");
                }
            }
        }
    }
}
