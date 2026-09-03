using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins;

/// <summary>
/// Assistente dedicado com abas para geração de Living Dex em Pokémon LeafGreen e FireRed
/// no Nintendo Switch Edition (eShop 2026).
/// Garante que apenas espécies legítimas e acessíveis no Switch sejam geradas.
/// </summary>
public sealed class SwitchFRLGWizardForm : Form
{
    private readonly SaveFile _sav;
    private readonly Action _reloadCallback;

    private readonly TabControl _tabs;
    private readonly RadioButton _rbKantoNormal;
    private readonly RadioButton _rbKantoShiny;
    private readonly RadioButton _rbKantoCombined;

    private readonly RadioButton _rbSeviiNormal;
    private readonly RadioButton _rbSeviiShiny;
    private readonly RadioButton _rbSeviiCombined;

    private readonly RadioButton _rbNativeNormal;
    private readonly RadioButton _rbNativeShiny;
    private readonly RadioButton _rbNativeCombined;

    private readonly ComboBox _cbBallPref;
    private readonly ComboBox _cbIVPref;
    private readonly ComboBox _cbLevelPref;
    private readonly NumericUpDown _nudStartBox;
    private readonly ComboBox _cbBoxPref;
    private readonly CheckBox _chkExportReport;

    private readonly Label _lblEstimation;
    private readonly Label _lblCapacityStatus;
    private readonly Button _btnGenerate;

    public SwitchFRLGWizardForm(SaveFile sav, Action reloadCallback)
    {
        _sav = sav;
        _reloadCallback = reloadCallback;

        Text = "🍃 Pokémon LeafGreen & FireRed — Nintendo Switch Edition";
        ClientSize = new Size(680, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;

        // Top Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(18, 52, 38),
        };

        var lblTitle = new Label
        {
            Text = "🍃 Pokémon LeafGreen & FireRed — Nintendo Switch Edition",
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 11.5f, FontStyle.Bold),
            Location = new Point(15, 12),
            AutoSize = true,
        };

        var lblSub = new Label
        {
            Text = $"Jogo: {sav.Version} (Switch eShop) | Treinador: {sav.OT} (TID: {sav.TID16}) | Caixas: {sav.BoxCount} ({sav.BoxCount * sav.BoxSlotCount} slots)",
            ForeColor = Color.FromArgb(190, 240, 215),
            Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
            Location = new Point(15, 38),
            AutoSize = true,
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSub);

        // TabControl
        _tabs = new TabControl
        {
            Location = new Point(15, 80),
            Size = new Size(650, 380),
            Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
        };

        // Tab 1: Kanto 151
        var tabKanto = new TabPage("🏆 Kanto Dex (151)");
        tabKanto.BackColor = Color.White;
        BuildTabBanner(tabKanto,
            "Pokédex Regional de Kanto Oficial (#001 Bulbasaur a #151 Mew)",
            "• Coleção pura com os 151 Pokémon clássicos de Kanto.\n" +
            "• Fiel ao objetivo principal de LeafGreen no Nintendo Switch.\n" +
            "• Ocupa exatamente 5 caixas e 1 slot (sobrando 9 caixas inteiras livres para o jogo!).");

        _rbKantoNormal = new RadioButton { Text = "🌟 Coleção Normal (151 Pokémon)", Location = new Point(30, 150), AutoSize = true, Checked = true };
        _rbKantoShiny = new RadioButton { Text = "✨ Coleção Shiny (151 Pokémon - 100% Legais)", Location = new Point(30, 185), AutoSize = true };
        _rbKantoCombined = new RadioButton { Text = "💫 Normal + Shiny Combinado (302 Pokémon - Ocupa 11 caixas)", Location = new Point(30, 220), AutoSize = true };

        _rbKantoNormal.CheckedChanged += (_, _) => UpdateEstimations();
        _rbKantoShiny.CheckedChanged += (_, _) => UpdateEstimations();
        _rbKantoCombined.CheckedChanged += (_, _) => UpdateEstimations();

        tabKanto.Controls.AddRange([_rbKantoNormal, _rbKantoShiny, _rbKantoCombined]);

        // Tab 2: Kanto + Sevii Islands
        var tabSevii = new TabPage("🏝️ Kanto + Sevii Islands (281)");
        tabSevii.BackColor = Color.White;
        BuildTabBanner(tabSevii,
            "Kanto + Sevii Islands + Tickets do Switch (Sem espécies de Hoenn)",
            "• Inclui os 151 de Kanto + Pokémon de Johto das Sevii Islands no pós-game.\n" +
            "• Inclui Lugia, Ho-Oh e Deoxys (Tickets nativos do Switch desbloqueados na Elite 4).\n" +
            "• ZERO Pokémon de Hoenn (sem Treecko, Torchic, Mudkip, Rayquaza, etc., já que Hoenn não existe no Switch!).\n" +
            "• Ocupa cerca de 10 caixas (sobrando 4 caixas livres no save).");

        _rbSeviiNormal = new RadioButton { Text = "🌟 Coleção Normal (281 Pokémon)", Location = new Point(30, 160), AutoSize = true, Checked = true };
        _rbSeviiShiny = new RadioButton { Text = "✨ Coleção Shiny (281 Pokémon - 100% Legais)", Location = new Point(30, 195), AutoSize = true };
        _rbSeviiCombined = new RadioButton { Text = "💫 Normal + Shiny Combinado (562 Pokémon - Excede 14 caixas)", Location = new Point(30, 230), AutoSize = true };

        _rbSeviiNormal.CheckedChanged += (_, _) => UpdateEstimations();
        _rbSeviiShiny.CheckedChanged += (_, _) => UpdateEstimations();
        _rbSeviiCombined.CheckedChanged += (_, _) => UpdateEstimations();

        tabSevii.Controls.AddRange([_rbSeviiNormal, _rbSeviiShiny, _rbSeviiCombined]);

        // Tab 3: Versão Nativa
        string verName = sav.Version == GameVersion.LG ? "LeafGreen" : "FireRed";
        string oppName = sav.Version == GameVersion.LG ? "FireRed" : "LeafGreen";
        var tabNative = new TabPage($"🌿 Nativos de {verName}");
        tabNative.BackColor = Color.White;
        BuildTabBanner(tabNative,
            $"Apenas Pokémon Estritamente Capturáveis no {verName}",
            $"• Filtra exclusivamente os Pokémon que você pode capturar no {verName} no Switch.\n" +
            $"• Exclui os Pokémon exclusivos de {oppName} que exigiriam trocas via cabo.\n" +
            "• Ocupa cerca de 9 caixas (sobrando 5 caixas livres no save).");

        _rbNativeNormal = new RadioButton { Text = $"🌟 Coleção Normal Nativa (~259 Pokémon)", Location = new Point(30, 150), AutoSize = true, Checked = true };
        _rbNativeShiny = new RadioButton { Text = $"✨ Coleção Shiny Nativa (~259 Pokémon)", Location = new Point(30, 185), AutoSize = true };
        _rbNativeCombined = new RadioButton { Text = $"💫 Normal + Shiny Combinado (~518 Pokémon)", Location = new Point(30, 220), AutoSize = true };

        _rbNativeNormal.CheckedChanged += (_, _) => UpdateEstimations();
        _rbNativeShiny.CheckedChanged += (_, _) => UpdateEstimations();
        _rbNativeCombined.CheckedChanged += (_, _) => UpdateEstimations();

        tabNative.Controls.AddRange([_rbNativeNormal, _rbNativeShiny, _rbNativeCombined]);

        // Tab 4: Personalização & Caixas
        var tabSettings = new TabPage("⚙️ Personalização & Caixas");
        tabSettings.BackColor = Color.White;

        var lblBall = new Label { Text = "Pokébola Preferida:", Location = new Point(25, 25), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold) };
        _cbBallPref = new ComboBox
        {
            Location = new Point(175, 22),
            Size = new Size(420, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cbBallPref.Items.AddRange([
            "🔴 Poké Ball Clássica (Vermelho & Branco original para todos)",
            "🎨 Automático Temático (Poké/Great/Ultra/Safari/Timer/Repeat/Dive/Net)",
            "⚪ Premier Ball (Branca minimalista clássica)",
            "🟡 Ultra Ball (Preta e amarela de alta captura)",
            "🧭 Safari Ball (Estilo clássico da Safari Zone de Kanto)",
            "🔒 Manter Pokébola Original do Encontro"
        ]);
        _cbBallPref.SelectedIndex = 0;

        var lblIV = new Label { Text = "Otimização de IVs:", Location = new Point(25, 65), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold) };
        _cbIVPref = new ComboBox
        {
            Location = new Point(175, 62),
            Size = new Size(420, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cbIVPref.Items.AddRange([
            "⚡ Smart IVs (6x31 ou 0 Atk para Special Attackers) [Recomendado]",
            "💎 6x31 Perfeitos (31 em todos os 6 atributos)",
            "🎯 Nativos de Captura (Manter IVs originais do encontro)"
        ]);
        _cbIVPref.SelectedIndex = 0;

        var lblLevel = new Label { Text = "Progressão de Nível:", Location = new Point(25, 105), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold) };
        _cbLevelPref = new ComboBox
        {
            Location = new Point(175, 102),
            Size = new Size(420, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cbLevelPref.Items.AddRange([
            "📈 Mínimo Canônico de Evolução (MetLevel progressivo legal)",
            "💯 Nível 100 Máximo Competitivo",
            "🎲 Nível Original de Encontro"
        ]);
        _cbLevelPref.SelectedIndex = 0;

        var lblStartBox = new Label { Text = "Caixa Inicial:", Location = new Point(25, 155), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold) };
        _nudStartBox = new NumericUpDown
        {
            Location = new Point(175, 152),
            Size = new Size(70, 24),
            Minimum = 1,
            Maximum = Math.Max(1, sav.BoxCount),
            Value = 1,
        };

        var lblBoxMode = new Label { Text = "Modo de Gravação:", Location = new Point(25, 195), AutoSize = true, Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold) };
        _cbBoxPref = new ComboBox
        {
            Location = new Point(175, 192),
            Size = new Size(420, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cbBoxPref.Items.AddRange([
            "Sobrescrever slots necessários (Preserva caixas além do alcance)",
            "Preencher apenas slots vazios (Não substitui nenhum Pokémon existente)",
            "Limpar caixas selecionadas antes de gerar"
        ]);
        _cbBoxPref.SelectedIndex = 0;

        _chkExportReport = new CheckBox
        {
            Text = "Exportar relatório detalhado da coleção em arquivo de texto (.txt)",
            Location = new Point(25, 240),
            AutoSize = true,
            Checked = true,
        };

        tabSettings.Controls.AddRange([
            lblBall, _cbBallPref,
            lblIV, _cbIVPref,
            lblLevel, _cbLevelPref,
            lblStartBox, _nudStartBox,
            lblBoxMode, _cbBoxPref,
            _chkExportReport
        ]);

        _tabs.TabPages.AddRange([tabKanto, tabSevii, tabNative, tabSettings]);
        _tabs.SelectedIndexChanged += (_, _) => UpdateEstimations();

        // Bottom Footer
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 90,
            BackColor = Color.FromArgb(245, 247, 250),
        };

        _lblEstimation = new Label
        {
            Location = new Point(15, 12),
            Size = new Size(420, 22),
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 30, 50),
        };

        _lblCapacityStatus = new Label
        {
            Location = new Point(15, 34),
            Size = new Size(420, 22),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(60, 130, 60),
        };

        _btnGenerate = new Button
        {
            Text = "🚀 Gerar no Save do Switch",
            Location = new Point(460, 15),
            Size = new Size(205, 42),
            BackColor = Color.FromArgb(24, 134, 75),
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
        };
        _btnGenerate.FlatAppearance.BorderSize = 0;
        _btnGenerate.Click += async (_, _) => await OnGenerateClicked();

        var btnCancel = new Button
        {
            Text = "Fechar",
            Location = new Point(460, 60),
            Size = new Size(205, 24),
            Font = new Font(Font.FontFamily, 8.5f, FontStyle.Regular),
            FlatStyle = FlatStyle.System,
            DialogResult = DialogResult.Cancel,
        };

        pnlBottom.Controls.AddRange([_lblEstimation, _lblCapacityStatus, _btnGenerate, btnCancel]);

        Controls.Add(_tabs);
        Controls.Add(pnlHeader);
        Controls.Add(pnlBottom);

        UpdateEstimations();
    }

    private static void BuildTabBanner(TabPage page, string header, string details)
    {
        var pnl = new Panel
        {
            Location = new Point(15, 15),
            Size = new Size(615, 110),
            BackColor = Color.FromArgb(235, 248, 240),
            BorderStyle = BorderStyle.FixedSingle,
        };

        var lblH = new Label
        {
            Text = header,
            Font = new Font(page.Font.FontFamily, 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 75, 42),
            Location = new Point(12, 10),
            AutoSize = true,
        };

        var lblD = new Label
        {
            Text = details,
            Font = new Font(page.Font.FontFamily, 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(40, 60, 50),
            Location = new Point(12, 34),
            Size = new Size(590, 68),
        };

        pnl.Controls.Add(lblH);
        pnl.Controls.Add(lblD);
        page.Controls.Add(pnl);
    }

    private (int count, string desc) GetCurrentSelectionCount()
    {
        int tabIdx = _tabs.SelectedIndex;
        if (tabIdx == 0) // Kanto
        {
            if (_rbKantoCombined.Checked) return (302, "Kanto Normal + Shiny");
            if (_rbKantoShiny.Checked) return (151, "Kanto Shiny");
            return (151, "Kanto Normal");
        }
        if (tabIdx == 1) // Sevii
        {
            if (_rbSeviiCombined.Checked) return (562, "Kanto+Sevii Normal + Shiny");
            if (_rbSeviiShiny.Checked) return (281, "Kanto+Sevii Shiny");
            return (281, "Kanto+Sevii Normal");
        }
        if (tabIdx == 2) // Native
        {
            if (_rbNativeCombined.Checked) return (518, "Nativos Normal + Shiny");
            if (_rbNativeShiny.Checked) return (259, "Nativos Shiny");
            return (259, "Nativos Normal");
        }

        return (151, "Kanto Normal");
    }

    private void UpdateEstimations()
    {
        var (count, desc) = GetCurrentSelectionCount();
        int totalCapacity = _sav.BoxCount * _sav.BoxSlotCount;
        int boxesNeeded = (count + _sav.BoxSlotCount - 1) / _sav.BoxSlotCount;
        int freeBoxes = _sav.BoxCount - boxesNeeded;

        _lblEstimation.Text = $"📦 {desc}: {count} Pokémon ({boxesNeeded} caixas necessárias)";

        if (count > totalCapacity)
        {
            _lblCapacityStatus.Text = $"⚠️ Excede a capacidade do save ({totalCapacity} slots / {_sav.BoxCount} caixas). Slots extras serão ignorados.";
            _lblCapacityStatus.ForeColor = Color.Firebrick;
            _btnGenerate.BackColor = Color.FromArgb(180, 50, 40);
        }
        else
        {
            _lblCapacityStatus.Text = $"✅ Cabe perfeitamente no save! Sobrarão {freeBoxes} caixas livres de armazenamento.";
            _lblCapacityStatus.ForeColor = Color.FromArgb(24, 134, 75);
            _btnGenerate.BackColor = Color.FromArgb(24, 134, 75);
        }
    }

    private async Task OnGenerateClicked()
    {
        int tabIdx = _tabs.SelectedIndex;
        bool isShiny = false;
        bool isCombined = false;
        int modeType = 0; // 0=Kanto, 1=Sevii, 2=Native

        if (tabIdx == 0)
        {
            modeType = 0;
            isShiny = _rbKantoShiny.Checked;
            isCombined = _rbKantoCombined.Checked;
        }
        else if (tabIdx == 1)
        {
            modeType = 1;
            isShiny = _rbSeviiShiny.Checked;
            isCombined = _rbSeviiCombined.Checked;
        }
        else if (tabIdx == 2)
        {
            modeType = 2;
            isShiny = _rbNativeShiny.Checked;
            isCombined = _rbNativeCombined.Checked;
        }
        else
        {
            modeType = 0;
            isShiny = _rbKantoShiny.Checked;
            isCombined = _rbKantoCombined.Checked;
        }

        var ballPref = _cbBallPref.SelectedIndex switch
        {
            0 => BallSelectionPreference.StandardPokeBall,
            1 => BallSelectionPreference.ThematicAuto,
            2 => BallSelectionPreference.PremierBall,
            3 => BallSelectionPreference.UltraBall,
            4 => BallSelectionPreference.ThematicAuto, // Handled as Safari where legal
            _ => BallSelectionPreference.StandardPokeBall,
        };

        var ivPref = _cbIVPref.SelectedIndex switch
        {
            0 => IVOptimizationPreference.SmartIVs,
            1 => IVOptimizationPreference.All31,
            _ => IVOptimizationPreference.KeepEncounter,
        };

        var lvlPref = _cbLevelPref.SelectedIndex switch
        {
            0 => LevelPreference.CanonicalFloor,
            1 => LevelPreference.Level100,
            _ => LevelPreference.EncounterNative,
        };

        var boxPref = _cbBoxPref.SelectedIndex switch
        {
            1 => BoxPlacementPreference.EmptySlotsOnly,
            2 => BoxPlacementPreference.ClearBoxesFirst,
            _ => BoxPlacementPreference.Overwrite,
        };

        int startBox = (int)_nudStartBox.Value;
        bool exportReport = _chkExportReport.Checked;

        _btnGenerate.Enabled = false;
        Cursor = Cursors.WaitCursor;

        try
        {
            List<PKM> targetList = await Task.Run(() =>
            {
                var normalCfg = new LivingDexConfig
                {
                    IncludeForms = true,
                    SetShiny = false,
                    SetAlpha = false,
                    TransferVersion = _sav.Version,
                };
                var shinyCfg = new LivingDexConfig
                {
                    IncludeForms = true,
                    SetShiny = true,
                    SetAlpha = false,
                    TransferVersion = _sav.Version,
                };

                List<PKM> normals = _sav.GenerateLivingDex(_sav.Personal, normalCfg).ToList();
                List<PKM> shinies = _sav.GenerateLivingDex(_sav.Personal, shinyCfg).Where(p => p.IsShiny).ToList();

                // Apply Switch Filter
                normals = FilterSwitchDex(normals, modeType, _sav.Version);
                shinies = FilterSwitchDex(shinies, modeType, _sav.Version);

                if (isCombined)
                {
                    var combined = new List<PKM>();
                    combined.AddRange(normals);
                    combined.AddRange(shinies);
                    return combined;
                }
                return isShiny ? shinies : normals;
            });

            if (targetList.Count == 0)
            {
                WinFormsUtil.Alert("Nenhum Pokémon pôde ser gerado para o filtro selecionado.");
                return;
            }

            // Plan placement slots
            int startSlot = Math.Max(0, (startBox - 1) * _sav.BoxSlotCount);
            int maxSlot = _sav.BoxCount * _sav.BoxSlotCount;
            var plannedSlots = new List<int>();

            for (int idx = startSlot; idx < maxSlot && plannedSlots.Count < targetList.Count; idx++)
            {
                if (boxPref == BoxPlacementPreference.EmptySlotsOnly)
                {
                    if (_sav.GetBoxSlotAtIndex(idx).Species != 0)
                        continue;
                }
                plannedSlots.Add(idx);
            }

            if (plannedSlots.Count == 0)
            {
                WinFormsUtil.Alert("Nenhum slot gravável disponível no intervalo de caixas selecionado.");
                return;
            }

            if (boxPref == BoxPlacementPreference.ClearBoxesFirst)
            {
                int endBox = (plannedSlots[^1] / _sav.BoxSlotCount) + 1;
                for (int b = startBox - 1; b < endBox && b < _sav.BoxCount; b++)
                {
                    for (int s = 0; s < _sav.BoxSlotCount; s++)
                    {
                        _sav.SetBoxSlotAtIndex(_sav.BlankPKM, b, s);
                    }
                }
            }

            int toPlace = Math.Min(targetList.Count, plannedSlots.Count);
            using var cts = new CancellationTokenSource();
            using var progressForm = new LivingDexProgressForm($"Gerando Living Dex — Switch Edition ({_sav.Version})", toPlace, cts);
            progressForm.Show();

            var reportLines = new List<string>
            {
                "================================================================================",
                $"RELATÓRIO DE LIVING DEX — NINTENDO SWITCH EDITION ({_sav.Version})",
                $"Treinador: {_sav.OT} (TID: {_sav.TID16}) | Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Modo: {(modeType == 0 ? "Kanto Dex (151)" : modeType == 1 ? "Kanto + Sevii (281)" : "Nativos de " + _sav.Version)} | Variação: {(isCombined ? "Normal + Shiny" : isShiny ? "Shiny" : "Normal")}",
                "================================================================================",
                string.Format("{0,-12} | {1,-18} | {2,-7} | {3,-6} | {4,-15} | {5,-20} | {6}", "Posição", "Pokémon", "Shiny", "Nível", "Pokébola", "IVs (HP/Atk/Def/SpA/SpD/Spe)", "Status"),
                new string('-', 95)
            };

            await Task.Run(() =>
            {
                for (int i = 0; i < toPlace; i++)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;

                    var pkm = targetList[i];
                    int slotIndex = plannedSlots[i];
                    int box = slotIndex / _sav.BoxSlotCount;
                    int slot = slotIndex % _sav.BoxSlotCount;

                    // Apply Ball
                    if (ballPref == BallSelectionPreference.StandardPokeBall)
                        pkm.Ball = (byte)Ball.Poke;
                    else if (ballPref == BallSelectionPreference.PremierBall)
                        pkm.Ball = (byte)Ball.Premier;
                    else if (ballPref == BallSelectionPreference.UltraBall)
                        pkm.Ball = (byte)Ball.Ultra;

                    // Apply IVs
                    if (ivPref == IVOptimizationPreference.SmartIVs)
                    {
                        bool isSpecialAttacker = pkm.PersonalInfo.SPA > pkm.PersonalInfo.ATK + 25;
                        pkm.IV_HP = 31;
                        pkm.IV_DEF = 31;
                        pkm.IV_SPA = 31;
                        pkm.IV_SPD = 31;
                        pkm.IV_SPE = 31;
                        pkm.IV_ATK = isSpecialAttacker ? 0 : 31;
                    }
                    else if (ivPref == IVOptimizationPreference.All31)
                    {
                        pkm.IV_HP = 31;
                        pkm.IV_ATK = 31;
                        pkm.IV_DEF = 31;
                        pkm.IV_SPA = 31;
                        pkm.IV_SPD = 31;
                        pkm.IV_SPE = 31;
                    }

                    // Apply Level
                    if (lvlPref == LevelPreference.Level100)
                    {
                        pkm.CurrentLevel = 100;
                    }

                    pkm.RefreshChecksum();
                    _sav.SetBoxSlotAtIndex(pkm, box, slot);

                    string ivStr = $"{pkm.IV_HP}/{pkm.IV_ATK}/{pkm.IV_DEF}/{pkm.IV_SPA}/{pkm.IV_SPD}/{pkm.IV_SPE}";
                    string shinyStr = pkm.IsShiny ? "✨ Sim" : "Não";
                    string ballStr = ((Ball)pkm.Ball).ToString();
                    string specName = GameInfo.Strings.Species[pkm.Species];
                    reportLines.Add(string.Format("Box {0,2} Slot {1,2} | {2,-18} | {3,-7} | Lv.{4,-3} | {5,-15} | {6,-27} | Inserido",
                        box + 1, slot + 1, $"#{pkm.Species} {specName}", shinyStr, pkm.CurrentLevel, ballStr, ivStr));

                    progressForm.UpdateProgress(i + 1, toPlace, pkm, box, slot);
                }
            });

            progressForm.IsCompleted = true;
            progressForm.Close();
            _reloadCallback();

            string reportSummary = "";
            if (exportReport)
            {
                string reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"LivingDex_Report_Switch_{_sav.Version}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllLines(reportPath, reportLines);
                reportSummary = $"\n\n📄 Relatório exportado com sucesso em:\n{reportPath}";
            }

            WinFormsUtil.Alert(
                "Geração Concluída com Sucesso!",
                $"A Living Dex do Nintendo Switch ({_sav.Version}) foi gerada com sucesso!\n" +
                $"Total gravado: {toPlace} Pokémon inseridos nas caixas a partir da Caixa {startBox}.{reportSummary}");

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            WinFormsUtil.Alert("Erro durante a geração da Living Dex:", ex.Message);
        }
        finally
        {
            _btnGenerate.Enabled = true;
            Cursor = Cursors.Default;
        }
    }

    private static List<PKM> FilterSwitchDex(IEnumerable<PKM> list, int modeType, GameVersion version)
    {
        return modeType switch
        {
            0 => SwitchFRLGDex.FilterKanto(list),
            1 => SwitchFRLGDex.FilterFRLGNative(list),
            2 => SwitchFRLGDex.FilterVersionNative(list, version),
            _ => SwitchFRLGDex.FilterKanto(list),
        };
    }
}
