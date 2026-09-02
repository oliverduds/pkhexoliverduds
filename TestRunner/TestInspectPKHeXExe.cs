using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace TestRunner;

public static class TestInspectPKHeXExe
{
    public static void Run()
    {
        string exe = @"C:\Users\Eduardo\Documents\projetos\pkhexoliverduds\pkhex\PKHeX.exe";
        using var fs = File.OpenRead(exe);
        using var pe = new PEReader(fs);
        if (pe.HasMetadata)
        {
            var reader = pe.GetMetadataReader();
            Console.WriteLine($"Found metadata in PKHeX.exe!");
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                string name = reader.GetString(type.Name);
                if (name.Contains("Sprite") || name.Contains("PokeSprite") || name.Contains("Drawing") || name.Contains("Image"))
                {
                    string ns = reader.GetString(type.Namespace);
                    Console.WriteLine($"  {ns}.{name}");
                }
            }
        }
        else
        {
            Console.WriteLine("PKHeX.exe has no direct PE metadata (likely bundled single-file).");
        }
    }
}
