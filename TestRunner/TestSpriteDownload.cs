using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestRunner;

public static class TestSpriteDownload
{
    private static readonly HttpClient _http = new();

    public static async Task Run()
    {
        // Test downloading Arceus sprite
        string url = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/493.png";
        Console.WriteLine($"Downloading sprite from {url}...");
        var bytes = await _http.GetByteArrayAsync(url);
        Console.WriteLine($"Downloaded {bytes.Length} bytes!");

        // Test Shiny Tapu Koko (785)
        string shinyUrl = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/shiny/785.png";
        var shinyBytes = await _http.GetByteArrayAsync(shinyUrl);
        Console.WriteLine($"Downloaded Shiny Tapu Koko: {shinyBytes.Length} bytes!");
    }
}
