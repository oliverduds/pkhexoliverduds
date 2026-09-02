using System;
using PKHeX.Core.AutoMod;

namespace TestRunner;

public static class CheckEnum
{
    public static void Run()
    {
        var type = typeof(APILegality).GetProperty("GameVersionPriority")?.PropertyType;
        Console.WriteLine($"Type: {type?.FullName}");
        if (type != null && type.IsEnum)
        {
            foreach (var val in Enum.GetNames(type))
            {
                Console.WriteLine($"  {val}");
            }
        }
    }
}
