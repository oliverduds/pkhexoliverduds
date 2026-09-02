using System;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace TestRunner;

public static class TestSpriteReflection
{
    public static Image? GetPokemonSprite(ushort species, byte form, bool isShiny)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType("PKHeX.Drawing.PokeSprite") ?? asm.GetTypes().FirstOrDefault(x => x.Name == "PokeSprite");
            if (t != null)
            {
                var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                Console.WriteLine($"Found PokeSprite in {asm.GetName().Name}! Methods: {methods.Length}");
                foreach (var m in methods)
                {
                    Console.WriteLine($"  {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))}) -> {m.ReturnType.Name}");
                }
            }
        }
        return null;
    }
}
