using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoModPlugins;

public sealed class RareEventPickerWizardForm : Form
{
    private static readonly HttpClient _httpClient = new();
    private readonly SaveFile _sav;
    private readonly Action _onSaveModified;

    // UI Controls
    private TextBox _txtSearch = null!;
    private ComboBox _cbCategory = null!;
    private ComboBox _cbGameOrigin = null!;
    private ComboBox _cbSort = null!;
    private DataGridView _grid = null!;
    private Label _lblCount = null!;
    private NumericUpDown _nudStartBox = null!;
    private ComboBox _cbMode = null!;
    private Button _btnInject = null!;

    // Preview Panel Controls
    private PictureBox _pbSprite = null!;
    private Label _lblPreviewTitle = null!;
    private Label _lblPreviewBadge = null!;
    private Label _lblPreviewGame = null!;
    private Label _lblPreviewFeatures = null!;
    private TextBox _txtPreviewDesc = null!;
    private Label _lblPreviewCompat = null!;

    private readonly HashSet<string> _selectedIds = new(StringComparer.OrdinalIgnoreCase);
    private List<RareEventItem> _filteredItems = [];
    private string _currentSpriteLoadedKey = string.Empty;

    public RareEventPickerWizardForm(SaveFile sav, Action onSaveModified)
    {
        _sav = sav;
        _onSaveModified = onSaveModified;

        InitializeComponents();
        ApplyFilterAndSort();
        UpdatePreview(RareEventCatalog.Items.FirstOrDefault());
    }

    private void InitializeComponents()
    {
        Text = "🎁 Galeria & Assistente de Pokémon de Evento Raros (MGDB)";
        Size = new Size(1100, 750);
        MinimumSize = new Size(980, 660);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        BackColor = Color.FromArgb(241, 245, 249);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(16, 10, 16, 10)
        };

        var lblHeaderTitle = new Label
        {
            Text = "🎁 Galeria de Eventos Raros & Míticos Históricos",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(16, 10)
        };

        var lblHeaderSub = new Label
        {
            Text = $"Save: {_sav.Version} ({_sav.OT}) • Acervo: Project Pokémon MGDB • Selecione os eventos e adicione direto nas suas caixas com 1 clique.",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(18, 38)
        };

        pnlHeader.Controls.Add(lblHeaderTitle);
        pnlHeader.Controls.Add(lblHeaderSub);

        // Bottom Bar
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 85,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12)
        };

        _lblCount = new Label
        {
            Text = "0 eventos selecionados",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoSize = true,
            Location = new Point(16, 14)
        };

        var lblBox = new Label
        {
            Text = "Caixa Inicial:",
            AutoSize = true,
            Location = new Point(18, 45),
            Font = new Font("Segoe UI", 9F)
        };

        _nudStartBox = new NumericUpDown
        {
            Location = new Point(100, 43),
            Width = 60,
            Minimum = 1,
            Maximum = Math.Max(1, _sav.BoxCount),
            Value = Math.Min(Math.Max(1, _sav.BoxCount - 2), Math.Max(1, _sav.BoxCount))
        };

        var lblMode = new Label
        {
            Text = "Destino:",
            AutoSize = true,
            Location = new Point(175, 45),
            Font = new Font("Segoe UI", 9F)
        };

        _cbMode = new ComboBox
        {
            Location = new Point(230, 42),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbMode.Items.AddRange([
            "📭 Apenas preencher slots vazios (Seguro)",
            "🔄 Preencher caixas a partir da inicial"
        ]);
        _cbMode.SelectedIndex = 0;

        _btnInject = new Button
        {
            Text = "📥 Injetar Eventos Selecionados no Save",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            BackColor = Color.FromArgb(13, 110, 253),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(330, 58),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlBottom.Width - 350, 14),
            Cursor = Cursors.Hand
        };
        _btnInject.FlatAppearance.BorderSize = 0;
        _btnInject.Click += async (_, _) => await OnInjectClicked();

        pnlBottom.Controls.Add(_lblCount);
        pnlBottom.Controls.Add(lblBox);
        pnlBottom.Controls.Add(_nudStartBox);
        pnlBottom.Controls.Add(lblMode);
        pnlBottom.Controls.Add(_cbMode);
        pnlBottom.Controls.Add(_btnInject);

        // Split Container
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 680,
            SplitterWidth = 6,
            BackColor = Color.FromArgb(203, 213, 225)
        };

        // Left Panel (Grid + Filters)
        var pnlLeft = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12, 10, 8, 10)
        };

        // Filters bar (2 rows)
        var pnlFilters = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = Color.Transparent
        };

        _txtSearch = new TextBox
        {
            Location = new Point(0, 4),
            Width = 200,
            PlaceholderText = "🔍 Buscar espécie, evento, golpe..."
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilterAndSort();

        _cbCategory = new ComboBox
        {
            Location = new Point(208, 4),
            Width = 175,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbCategory.Items.AddRange([
            "Todas as Categorias",
            "👑 Míticos de Cinema (Tier S+)",
            "💎 Shinies de Evento (Tier S)",
            "⚔️ Formas & Golpes Especiais",
            "🏆 Campeonatos VGC (Mundiais)"
        ]);
        _cbCategory.SelectedIndex = 0;
        _cbCategory.SelectedIndexChanged += (_, _) => ApplyFilterAndSort();

        _cbGameOrigin = new ComboBox
        {
            Location = new Point(390, 4),
            Width = 145,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbGameOrigin.Items.AddRange([
            "🎮 Todos os Jogos",
            "🎮 Gen 4 (DP / Pt / HGSS)",
            "🎮 Gen 5 (BW / B2W2)",
            "🎮 Gen 6 (XY / ORAS)",
            "🎮 Gen 7 (SM / USUM)"
        ]);
        _cbGameOrigin.SelectedIndex = 0;
        _cbGameOrigin.SelectedIndexChanged += (_, _) => ApplyFilterAndSort();

        _cbSort = new ComboBox
        {
            Location = new Point(542, 4),
            Width = 125,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cbSort.Items.AddRange([
            "Maior Raridade",
            "🎮 Jogo (Mais Antigo)",
            "🎮 Jogo (Mais Novo)",
            "Nome da Espécie",
            "Ano do Evento"
        ]);
        _cbSort.SelectedIndex = 0;
        _cbSort.SelectedIndexChanged += (_, _) => ApplyFilterAndSort();

        // Quick Select Buttons (Row 2)
        var btnQuickMyth = CreateQuickButton("👑 Míticos", 0, 44, () => QuickSelect(EventTier.TierSPlus));
        var btnQuickShiny = CreateQuickButton("💎 Shinies", 95, 44, () => QuickSelect(EventTier.TierS));
        var btnQuickAsh = CreateQuickButton("🧢 Ash & Bonés", 190, 44, () => QuickSelectCategory(EventCategory.SpecialFormOrMove));
        var btnSelectAll = CreateQuickButton("✔️ Todos", 305, 44, SelectAll);
        var btnClear = CreateQuickButton("❌ Limpar", 385, 44, ClearSelection);

        pnlFilters.Controls.AddRange([_txtSearch, _cbCategory, _cbGameOrigin, _cbSort, btnQuickMyth, btnQuickShiny, btnQuickAsh, btnSelectAll, btnClear]);

        // Grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            RowTemplate = { Height = 28 }
        };

        var colCheck = new DataGridViewCheckBoxColumn
        {
            HeaderText = "Injetar",
            Width = 50,
            Name = "colCheck"
        };
        var colSpecies = new DataGridViewTextBoxColumn
        {
            HeaderText = "Pokémon",
            Width = 150,
            ReadOnly = true,
            Name = "colSpecies"
        };
        var colGame = new DataGridViewTextBoxColumn
        {
            HeaderText = "Jogo de Origem",
            Width = 135,
            ReadOnly = true,
            Name = "colGame"
        };
        var colEvent = new DataGridViewTextBoxColumn
        {
            HeaderText = "Evento Oficial",
            Width = 175,
            ReadOnly = true,
            Name = "colEvent"
        };
        var colTier = new DataGridViewTextBoxColumn
        {
            HeaderText = "Raridade",
            Width = 100,
            ReadOnly = true,
            Name = "colTier"
        };
        var colYear = new DataGridViewTextBoxColumn
        {
            HeaderText = "Ano",
            Width = 55,
            ReadOnly = true,
            Name = "colYear"
        };

        _grid.Columns.AddRange([colCheck, colSpecies, colGame, colEvent, colTier, colYear]);
        _grid.CellContentClick += OnGridCellContentClick;
        _grid.SelectionChanged += OnGridSelectionChanged;

        pnlLeft.Controls.Add(_grid);
        pnlLeft.Controls.Add(pnlFilters);

        // Right Panel (Detail Card Preview)
        var pnlRight = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(16)
        };

        var grpCard = new GroupBox
        {
            Text = "Destaque do Evento Selecionado",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ForeColor = Color.FromArgb(15, 23, 42)
        };

        // Real Pokémon Sprite PictureBox
        _pbSprite = new PictureBox
        {
            Size = new Size(100, 100),
            Location = new Point(16, 28),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(248, 250, 252),
            BorderStyle = BorderStyle.FixedSingle
        };

        _lblPreviewTitle = new Label
        {
            Text = "Nome do Evento",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(125, 28),
            Size = new Size(245, 42),
            AutoEllipsis = true
        };

        _lblPreviewBadge = new Label
        {
            Text = "TIER S+",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(220, 38, 38),
            Location = new Point(125, 74),
            Size = new Size(240, 22),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblPreviewGame = new Label
        {
            Text = "🎮 Jogo: Diamond / Pearl (Gen 4)",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(37, 99, 235),
            Location = new Point(16, 134),
            Size = new Size(350, 22),
            AutoEllipsis = true
        };

        _lblPreviewFeatures = new Label
        {
            Text = "Cherish Ball • Classic Ribbon • OT: GF",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location = new Point(16, 158),
            Size = new Size(350, 32),
            AutoEllipsis = true
        };

        var lblHistHeader = new Label
        {
            Text = "Histórico & Detalhes da Distribuição:",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(16, 194),
            AutoSize = true
        };

        // Text box with high contrast styling so it is NEVER white-on-white
        _txtPreviewDesc = new TextBox
        {
            Location = new Point(16, 218),
            Size = new Size(350, 205),
            Multiline = true,
            ReadOnly = true,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(15, 23, 42), // Dark Slate / Black for 100% legibility
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9F)
        };

        _lblPreviewCompat = new Label
        {
            Text = "✅ 100% Compatível com seu jogo",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 163, 74),
            Location = new Point(16, 430),
            Size = new Size(350, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        grpCard.Controls.AddRange([
            _pbSprite, _lblPreviewTitle, _lblPreviewBadge,
            _lblPreviewGame, _lblPreviewFeatures, lblHistHeader, _txtPreviewDesc, _lblPreviewCompat
        ]);

        pnlRight.Controls.Add(grpCard);

        split.Panel1.Controls.Add(pnlLeft);
        split.Panel2.Controls.Add(pnlRight);

        Controls.Add(split);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);
    }

    private Button CreateQuickButton(string text, int x, int y, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(x == 305 ? 75 : 85, 26),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 8F),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private void ApplyFilterAndSort()
    {
        string query = _txtSearch.Text.Trim().ToLowerInvariant();
        int catIndex = _cbCategory.SelectedIndex;
        int gameIndex = _cbGameOrigin.SelectedIndex;
        int sortIndex = _cbSort.SelectedIndex;

        var items = RareEventCatalog.Items.AsEnumerable();

        // 1. Search Query
        if (!string.IsNullOrWhiteSpace(query))
        {
            items = items.Where(i =>
                i.DisplayName.ToLowerInvariant().Contains(query) ||
                i.EventName.ToLowerInvariant().Contains(query) ||
                i.Species.ToString().ToLowerInvariant().Contains(query) ||
                i.OriginGame.ToLowerInvariant().Contains(query) ||
                i.Year.ToString().Contains(query) ||
                i.KeyFeatures.ToLowerInvariant().Contains(query) ||
                i.Description.ToLowerInvariant().Contains(query));
        }

        // 2. Category Filter
        items = catIndex switch
        {
            1 => items.Where(i => i.Tier == EventTier.TierSPlus),
            2 => items.Where(i => i.Tier == EventTier.TierS),
            3 => items.Where(i => i.Category == EventCategory.SpecialFormOrMove),
            4 => items.Where(i => i.Category == EventCategory.WorldChampionshipsVGC),
            _ => items
        };

        // 3. Game Origin Filter
        items = gameIndex switch
        {
            1 => items.Where(i => i.Generation == 4),
            2 => items.Where(i => i.Generation == 5),
            3 => items.Where(i => i.Generation == 6),
            4 => items.Where(i => i.Generation == 7),
            _ => items
        };

        // 4. Sort
        items = sortIndex switch
        {
            1 => items.OrderBy(i => i.Generation).ThenBy(i => i.Year).ThenBy(i => i.Species.ToString()), // Oldest game first
            2 => items.OrderByDescending(i => i.Generation).ThenByDescending(i => i.Year).ThenBy(i => i.Species.ToString()), // Newest game first
            3 => items.OrderBy(i => i.Species.ToString()).ThenBy(i => i.DisplayName),
            4 => items.OrderByDescending(i => i.Year).ThenBy(i => i.DisplayName),
            _ => items.OrderBy(i => i.Tier).ThenBy(i => i.Generation).ThenBy(i => i.Species.ToString()) // Highest rarity first
        };

        _filteredItems = items.ToList();

        // Populate Grid
        _grid.Rows.Clear();
        foreach (var item in _filteredItems)
        {
            bool isChecked = _selectedIds.Contains(item.Id);
            string speciesText = item.IsShiny ? $"★ {item.DisplayName}" : item.DisplayName;
            _grid.Rows.Add(isChecked, speciesText, item.OriginGame, item.EventName, item.TierStars, item.Year);
            _grid.Rows[^1].Tag = item;
        }

        UpdateCountLabel();
    }

    private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == 0)
        {
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            bool isChecked = (bool)(_grid.Rows[e.RowIndex].Cells[0].Value ?? false);
            if (_grid.Rows[e.RowIndex].Tag is RareEventItem item)
            {
                if (isChecked) _selectedIds.Add(item.Id);
                else _selectedIds.Remove(item.Id);
            }
            UpdateCountLabel();
        }
    }

    private void OnGridSelectionChanged(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is RareEventItem item)
        {
            UpdatePreview(item);
        }
    }

    private void UpdatePreview(RareEventItem? item)
    {
        if (item == null) return;

        _lblPreviewTitle.Text = item.DisplayName;
        _lblPreviewBadge.Text = item.TierBadge;

        // Badge color based on tier
        _lblPreviewBadge.BackColor = item.Tier switch
        {
            EventTier.TierSPlus => Color.FromArgb(220, 38, 38),  // Red / Mythical
            EventTier.TierS => Color.FromArgb(147, 51, 234),      // Purple / Shiny
            EventTier.TierA => Color.FromArgb(14, 165, 233),      // Cyan / Special
            _ => Color.FromArgb(234, 88, 12)                      // Amber / VGC
        };

        _lblPreviewGame.Text = $"🎮 Jogo de Origem: {item.OriginGame}";
        _lblPreviewFeatures.Text = $"{item.KeyFeatures}\r\n🌍 {item.Region} ({item.Year})";

        // Text color is strictly set to dark slate for absolute readability
        _txtPreviewDesc.ForeColor = Color.FromArgb(15, 23, 42);
        _txtPreviewDesc.BackColor = Color.FromArgb(248, 250, 252);
        _txtPreviewDesc.Text =
            $"{item.Description}\r\n\r\n" +
            $"🎮 Lançamento: {item.OriginGame} ({item.Year})\r\n" +
            $"🌍 Região de Origem: {item.Region}\r\n" +
            $"⭐ Classificação: {item.TierStars} ({item.TierBadge})\r\n" +
            $"⚔️ Características: {item.KeyFeatures}";

        bool compat = item.IsCompatibleWith(_sav);
        if (compat)
        {
            _lblPreviewCompat.Text = $"✅ 100% Compatível com seu jogo ({_sav.Version})";
            _lblPreviewCompat.ForeColor = Color.FromArgb(22, 163, 74);
        }
        else
        {
            _lblPreviewCompat.Text = $"❌ Incompatível com o jogo atual ({_sav.Version})";
            _lblPreviewCompat.ForeColor = Color.FromArgb(220, 38, 38);
        }

        // Load Pokemon Sprite asynchronously
        _ = LoadPokemonSpriteAsync(item);
    }

    private async Task LoadPokemonSpriteAsync(RareEventItem item)
    {
        string spriteKey = $"{item.Species}_{(item.IsShiny ? "s" : "n")}_{item.Form}";
        if (_currentSpriteLoadedKey == spriteKey) return;
        _currentSpriteLoadedKey = spriteKey;

        // 1. Try PKHeX's internal PokeSprite via reflection
        var sprite = TryGetPKHeXInternalSprite(item.Species, item.Form, item.IsShiny);
        if (sprite != null)
        {
            _pbSprite.Image = sprite;
            return;
        }

        // 2. Check local cached file in sprites/
        string spritesDir = Path.Combine(Application.StartupPath, "sprites");
        try { Directory.CreateDirectory(spritesDir); } catch { }
        string cacheFile = Path.Combine(spritesDir, $"{spriteKey}.png");

        if (File.Exists(cacheFile))
        {
            try
            {
                using var stream = new MemoryStream(File.ReadAllBytes(cacheFile));
                _pbSprite.Image = Image.FromStream(stream);
                return;
            }
            catch { }
        }

        // 3. Fallback: Draw clean placeholder while loading
        _pbSprite.Image = DrawPlaceholderBadge(item);

        // 4. Download sprite from PokeAPI GitHub CDN asynchronously
        try
        {
            int spId = (int)item.Species;
            string url = item.IsShiny
                ? $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/shiny/{spId}.png"
                : $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{spId}.png";

            var bytes = await _httpClient.GetByteArrayAsync(url);
            if (bytes != null && bytes.Length > 0)
            {
                try { File.WriteAllBytes(cacheFile, bytes); } catch { }

                if (_currentSpriteLoadedKey == spriteKey)
                {
                    using var stream = new MemoryStream(bytes);
                    _pbSprite.Image = Image.FromStream(stream);
                }
            }
        }
        catch { }
    }

    private static Image? TryGetPKHeXInternalSprite(Species species, byte form, bool isShiny)
    {
        try
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("PKHeX.Drawing.PokeSprite") ?? a.GetType("PKHeX.Drawing.Misc.SpriteBuilder"))
                .FirstOrDefault(t => t != null);

            if (type != null)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "GetSprite");
                foreach (var m in methods)
                {
                    var p = m.GetParameters();
                    if (p.Length >= 5 && (p[0].ParameterType == typeof(int) || p[0].ParameterType == typeof(ushort)))
                    {
                        object[] args = new object[p.Length];
                        args[0] = Convert.ChangeType((ushort)species, p[0].ParameterType);
                        args[1] = Convert.ChangeType(form, p[1].ParameterType);
                        args[2] = Convert.ChangeType(0, p[2].ParameterType);
                        args[3] = Convert.ChangeType(0, p[3].ParameterType);
                        args[4] = isShiny;
                        for (int i = 5; i < p.Length; i++)
                            args[i] = p[i].HasDefaultValue ? p[i].DefaultValue! : (p[i].ParameterType.IsValueType ? Activator.CreateInstance(p[i].ParameterType)! : null!);

                        if (m.Invoke(null, args) is Image img) return img;
                    }
                }
            }
        }
        catch { }
        return null;
    }

    private static Bitmap DrawPlaceholderBadge(RareEventItem item)
    {
        var bmp = new Bitmap(100, 100);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(241, 245, 249));

        using var brush = new SolidBrush(Color.FromArgb(15, 23, 42));
        using var fontNum = new Font("Segoe UI", 9F, FontStyle.Bold);
        using var fontName = new Font("Segoe UI", 10F, FontStyle.Bold);

        string num = $"#{(int)item.Species:000}";
        string star = item.IsShiny ? " ★" : "";
        g.DrawString(num, fontNum, brush, new PointF(8, 8));
        g.DrawString(item.Species.ToString() + star, fontName, brush, new PointF(8, 30));

        return bmp;
    }

    private void QuickSelect(EventTier tier)
    {
        foreach (var item in RareEventCatalog.Items.Where(i => i.Tier == tier && i.IsCompatibleWith(_sav)))
        {
            _selectedIds.Add(item.Id);
        }
        RefreshCheckboxes();
    }

    private void QuickSelectCategory(EventCategory cat)
    {
        foreach (var item in RareEventCatalog.Items.Where(i => i.Category == cat && i.IsCompatibleWith(_sav)))
        {
            _selectedIds.Add(item.Id);
        }
        RefreshCheckboxes();
    }

    private void SelectAll()
    {
        foreach (var item in _filteredItems.Where(i => i.IsCompatibleWith(_sav)))
        {
            _selectedIds.Add(item.Id);
        }
        RefreshCheckboxes();
    }

    private void ClearSelection()
    {
        _selectedIds.Clear();
        RefreshCheckboxes();
    }

    private void RefreshCheckboxes()
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is RareEventItem item)
            {
                row.Cells[0].Value = _selectedIds.Contains(item.Id);
            }
        }
        UpdateCountLabel();
    }

    private void UpdateCountLabel()
    {
        _lblCount.Text = $"{_selectedIds.Count} eventos selecionados";
        _btnInject.Enabled = _selectedIds.Count > 0;
    }

    private async Task OnInjectClicked()
    {
        var itemsToInject = RareEventCatalog.Items
            .Where(i => _selectedIds.Contains(i.Id) && i.IsCompatibleWith(_sav))
            .ToList();

        if (itemsToInject.Count == 0)
        {
            MessageBox.Show("Selecione pelo menos um Pokémon de evento compatível para injetar!", "Nenhum Selecionado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int startBox = (int)_nudStartBox.Value - 1; // 0-indexed
        bool emptySlotsOnly = _cbMode.SelectedIndex == 0;

        var confirm = MessageBox.Show(
            $"Deseja injetar {itemsToInject.Count} Pokémon de evento a partir da Caixa {_nudStartBox.Value}?\n\n" +
            $"Modo: {(emptySlotsOnly ? "Preencher apenas slots vazios" : "Preencher caixas a partir da inicial")}\n" +
            "Todos os Pokémon serão gerados com legalidade 100% autêntica.",
            "Confirmar Injeção de Eventos",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        _btnInject.Enabled = false;
        Cursor = Cursors.WaitCursor;

        int injectedCount = 0;
        int currentBox = startBox;
        int currentSlot = 0;

        var failures = new List<string>();

        await Task.Run(() =>
        {
            foreach (var item in itemsToInject)
            {
                var pkm = item.GeneratePKM(_sav);
                if (pkm == null)
                {
                    failures.Add($"{item.DisplayName} (Falha ao converter)");
                    continue;
                }

                // Find next slot
                bool slotFound = false;
                while (currentBox < _sav.BoxCount)
                {
                    while (currentSlot < _sav.BoxSlotCount)
                    {
                        var existing = _sav.GetBoxSlotAtIndex(currentBox, currentSlot);
                        if (!emptySlotsOnly || existing.Species == 0)
                        {
                            _sav.SetBoxSlotAtIndex(pkm, currentBox, currentSlot);
                            injectedCount++;
                            slotFound = true;
                            currentSlot++;
                            break;
                        }
                        currentSlot++;
                    }

                    if (slotFound) break;
                    currentBox++;
                    currentSlot = 0;
                }

                if (!slotFound)
                {
                    failures.Add($"{item.DisplayName} (Sem espaço nas caixas restantes)");
                    break;
                }
            }
        });

        Cursor = Cursors.Default;
        _btnInject.Enabled = true;

        if (injectedCount > 0)
        {
            _onSaveModified();
            string msg = $"🎉 {injectedCount} Pokémon de Evento Raros foram adicionados com sucesso ao seu save!\n\n";
            if (failures.Count > 0)
            {
                msg += $"Avisos ({failures.Count}):\n" + string.Join("\n", failures.Take(5));
            }

            MessageBox.Show(msg, "Injeção Concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else
        {
            MessageBox.Show("Nenhum Pokémon pôde ser adicionado. Verifique o espaço disponível nas suas caixas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
