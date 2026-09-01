using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using Microsoft.VisualBasic;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins;

public class ExportBoxToShowdown : AutoModPlugin
{
    public override string Name => "Export Box to ALM Showdown Template";
    public static string Name2 => "Export Active to ALM Showdown Template";

    private const string ExportAllName = "Export ALL Boxes to ALM Showdown Template";
    private const string ExportRangeName = "Export Box Range to ALM Showdown Template";

    public override int Priority => 1;

    protected override void AddPluginControl(ToolStripDropDownItem modmenu)
    {
        var icon = WinFormsUtil.GetIconForTheme(Resources.exportboxtoshowdown, Application.IsDarkModeEnabled);

        var ctrl = new ToolStripMenuItem(Name) { Image = icon };
        ctrl.Click += (_, _) => ExportCurrentBox(SaveFileEditor);
        ctrl.Name = "Menu_ExportBoxtoShowdown";
        modmenu.DropDownItems.Add(ctrl);

        var all = new ToolStripMenuItem(ExportAllName) { Image = icon };
        all.Click += (_, _) => ExportAllBoxes();
        all.Name = "Menu_ExportAllBoxesToShowdown";
        modmenu.DropDownItems.Add(all);

        var range = new ToolStripMenuItem(ExportRangeName) { Image = icon };
        range.Click += (_, _) => ExportBoxRange();
        range.Name = "Menu_ExportBoxRangeToShowdown";
        modmenu.DropDownItems.Add(range);

        var ctrl2 = new ToolStripMenuItem(Name2) { Image = icon };
        ctrl2.Click += (_, _) => ExportActive();
        ctrl2.Name = "Menu_ExportActivetoShowdown";
        modmenu.DropDownItems.Add(ctrl2);
    }

    private static void ExportCurrentBox(ISaveFileProvider provider)
    {
        try
        {
            var str = provider.GetRegenSetsFromBoxCurrent();
            CopyToClipboard(str, "No Pokémon were found in the active box.",
                "Exported the active box in RegenTemplate format to clipboard.");
        }
        catch (Exception e)
        {
            WinFormsUtil.Error("Unable to export text to clipboard.", e.Message);
        }
    }

    private void ExportAllBoxes()
    {
        try
        {
            var sav = SaveFileEditor.SAV;
            var text = GetBoxesText(sav, 0, sav.BoxCount - 1, out var boxesWithPokemon, out var pokemonCount);

            if (string.IsNullOrWhiteSpace(text))
            {
                WinFormsUtil.Alert("No Pokémon were found in any box.");
                return;
            }

            Clipboard.SetText(text);
            WinFormsUtil.Alert(
                "Exported all populated boxes in RegenTemplate format to clipboard.",
                $"Boxes with Pokémon: {boxesWithPokemon} / {sav.BoxCount}",
                $"Pokémon exported: {pokemonCount}"
            );
        }
        catch (Exception e)
        {
            HandleClipboardFailure(e);
        }
    }

    private void ExportBoxRange()
    {
        try
        {
            var sav = SaveFileEditor.SAV;
            var current = SaveFileEditor.CurrentBox + 1;
            var defaultRange = $"{current}-{sav.BoxCount}";

            var input = Interaction.InputBox(
                $"Enter the box range to export (1-{sav.BoxCount}).\n\nExamples: 1-6   or   5",
                "Export Box Range to ALM Showdown Template",
                defaultRange);

            if (string.IsNullOrWhiteSpace(input))
                return;

            if (!TryParseRange(input, sav.BoxCount, out var first, out var last))
            {
                WinFormsUtil.Alert(
                    "Invalid box range.",
                    $"Use a value from 1 to {sav.BoxCount}, for example:",
                    "1-6",
                    "5"
                );
                return;
            }

            var text = GetBoxesText(sav, first - 1, last - 1, out var boxesWithPokemon, out var pokemonCount);
            if (string.IsNullOrWhiteSpace(text))
            {
                WinFormsUtil.Alert($"No Pokémon were found in boxes {first}-{last}.");
                return;
            }

            Clipboard.SetText(text);
            WinFormsUtil.Alert(
                $"Exported boxes {first}-{last} in RegenTemplate format to clipboard.",
                $"Boxes with Pokémon: {boxesWithPokemon}",
                $"Pokémon exported: {pokemonCount}"
            );
        }
        catch (Exception e)
        {
            HandleClipboardFailure(e);
        }
    }

    private static bool TryParseRange(string input, int boxCount, out int first, out int last)
    {
        first = 0;
        last = 0;

        var clean = input.Trim().Replace(" ", string.Empty);
        var split = clean.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (split.Length == 1)
        {
            if (!int.TryParse(split[0], out first))
                return false;
            last = first;
        }
        else if (split.Length == 2)
        {
            if (!int.TryParse(split[0], out first) || !int.TryParse(split[1], out last))
                return false;
        }
        else
        {
            return false;
        }

        if (first > last)
            (first, last) = (last, first);

        return first >= 1 && last <= boxCount;
    }

    private static string GetBoxesText(
        SaveFile sav,
        int firstBox,
        int lastBox,
        out int boxesWithPokemon,
        out int pokemonCount)
    {
        var sb = new StringBuilder();
        boxesWithPokemon = 0;
        pokemonCount = 0;

        for (int box = firstBox; box <= lastBox; box++)
        {
            var data = sav.GetBoxData(box);
            var count = 0;
            foreach (var pk in data)
            {
                if (pk.Species != 0)
                    count++;
            }

            if (count == 0)
                continue;

            var text = sav.GetRegenSetsFromBox(box);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (sb.Length != 0)
                sb.AppendLine().AppendLine();

            sb.Append(text.Trim());
            boxesWithPokemon++;
            pokemonCount += count;
        }

        return sb.ToString();
    }

    private static void CopyToClipboard(string str, string emptyMessage, string successMessage)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            WinFormsUtil.Alert(emptyMessage);
            return;
        }

        Clipboard.SetText(str);
        WinFormsUtil.Alert(successMessage);
    }

    private static void HandleClipboardFailure(Exception e)
    {
        WinFormsUtil.Error(
            "Unable to export boxes to the clipboard.",
            e.Message,
            "Try again after closing any application that may be locking the Windows clipboard."
        );
    }

    public void ExportActive()
    {
        try
        {
            var str = PKMEditor.PreparePKM().GetRegenText();
            CopyToClipboard(str, "No active Pokémon data was available.",
                "Exported the active Pokémon in RegenTemplate format to clipboard.");
        }
        catch (Exception e)
        {
            WinFormsUtil.Error("Unable to export text to clipboard.", e.Message);
        }
    }
}
