using System.Diagnostics;
using System.Text.Json;

namespace AizenX;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    readonly Color Bg = Color.FromArgb(10, 10, 12);
    readonly Color Panel = Color.FromArgb(20, 20, 24);
    readonly Color Panel2 = Color.FromArgb(27, 27, 32);
    readonly Color Red = Color.FromArgb(225, 16, 36);
    readonly Color TextMain = Color.FromArgb(238, 238, 240);
    readonly Color TextDim = Color.FromArgb(155, 155, 165);

    readonly TextBox engineBox = new();
    readonly TextBox gameBox = new();
    readonly TextBox outputBox = new();
    readonly TextBox archiveAssetsBox = new();
    readonly TextBox looseFilesBox = new();
    readonly TextBox logBox = new();
    readonly Label engineStatus = new();
    readonly RadioButton archiveRadio = new();
    readonly RadioButton looseRadio = new();
    readonly Button convertButton = new();
    readonly Panel archivePanel = new();
    readonly Panel loosePanel = new();

    readonly string settingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AizenX");
    string SettingsPath => Path.Combine(settingsDir, "settings.json");

    public MainForm()
    {
        Text = "AizenX RDR2 XML Forge";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 680);
        Size = new Size(1080, 790);
        BackColor = Bg;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 10f);
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;

        BuildUi();
        Load += async (_, _) => await InitializeAsync();
        FormClosing += (_, _) => SaveSettings();
    }

    void BuildUi()
    {
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(22), BackColor = Bg,
            ColumnCount = 1, RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 164));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
        var title = new Label {
            Text = "AIZENX", AutoSize = true, ForeColor = Red,
            Font = new Font("Segoe UI Black", 28f, FontStyle.Bold), Location = new Point(0, 0)
        };
        var sub = new Label {
            Text = "RDR2 XML FORGE  //  ARCHIVE + LOOSE ASSET CONVERTER",
            AutoSize = true, ForeColor = TextDim, Font = new Font("Consolas", 10f, FontStyle.Bold),
            Location = new Point(4, 53)
        };
        var line = new Panel { BackColor = Red, Height = 3, Width = 300, Location = new Point(0, 80) };

        engineStatus.AutoSize = false;
        engineStatus.Size = new Size(150, 24);
        engineStatus.Location = new Point(840, 8);
        engineStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        engineStatus.TextAlign = ContentAlignment.MiddleRight;
        engineStatus.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        engineStatus.ForeColor = TextDim;
        engineStatus.Text = "ENGINE CHECK...";
        engineStatus.BackColor = Bg;

        header.Controls.AddRange([title, sub, line, engineStatus]);
        root.Controls.Add(header, 0, 0);

        var config = MakeCard();
        root.Controls.Add(config, 0, 1);
        AddRow(config, 16, "ENGINE", engineBox, BrowseEngine, out _);
        AddRow(config, 64, "RDR2 FOLDER", gameBox, BrowseGame, out _);
        AddRow(config, 112, "OUTPUT", outputBox, BrowseOutput, out _);

        var modeCard = MakeCard();
        root.Controls.Add(modeCard, 0, 2);
        archiveRadio.Text = "ARCHIVE MODE";
        archiveRadio.Checked = true;
        archiveRadio.ForeColor = TextMain;
        archiveRadio.Location = new Point(18, 14);
        archiveRadio.AutoSize = true;
        archiveRadio.CheckedChanged += (_, _) => UpdateMode();
        looseRadio.Text = "LOOSE RDR2 FILE MODE";
        looseRadio.ForeColor = TextMain;
        looseRadio.Location = new Point(170, 14);
        looseRadio.AutoSize = true;
        looseRadio.CheckedChanged += (_, _) => UpdateMode();
        modeCard.Controls.AddRange([archiveRadio, looseRadio]);

        archivePanel.Location = new Point(14, 44);
        archivePanel.Size = new Size(980, 160);
        archivePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        archivePanel.BackColor = Panel;
        var aLabel = MakeLabel("ASSET NAMES — one per line (example: meta_base_player.yft)", 4, 0);
        archiveAssetsBox.Multiline = true;
        archiveAssetsBox.ScrollBars = ScrollBars.Vertical;
        StyleTextBox(archiveAssetsBox);
        archiveAssetsBox.Location = new Point(4, 28);
        archiveAssetsBox.Size = new Size(954, 110);
        archiveAssetsBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        archiveAssetsBox.Text = "meta_base_player.yft";
        archivePanel.Controls.AddRange([aLabel, archiveAssetsBox]);
        modeCard.Controls.Add(archivePanel);

        loosePanel.Location = archivePanel.Location;
        loosePanel.Size = archivePanel.Size;
        loosePanel.Anchor = archivePanel.Anchor;
        loosePanel.BackColor = Panel;
        var lLabel = MakeLabel("LOOSE RDR2 FILES — clean standalone resources; malformed/extracted copies may fail", 4, 0);
        looseFilesBox.Multiline = true;
        looseFilesBox.ScrollBars = ScrollBars.Vertical;
        StyleTextBox(looseFilesBox);
        looseFilesBox.Location = new Point(4, 28);
        looseFilesBox.Size = new Size(800, 110);
        looseFilesBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var chooseFiles = MakeButton("ADD FILES", 816, 28, 142, 40);
        chooseFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        chooseFiles.Click += (_, _) => BrowseLooseFiles();
        var clearFiles = MakeButton("CLEAR", 816, 78, 142, 40);
        clearFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        clearFiles.Click += (_, _) => looseFilesBox.Clear();
        loosePanel.Controls.AddRange([lLabel, looseFilesBox, chooseFiles, clearFiles]);
        modeCard.Controls.Add(loosePanel);

        var logCard = MakeCard();
        root.Controls.Add(logCard, 0, 3);
        var logLabel = MakeLabel("CONVERSION LOG", 14, 12);
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Both;
        logBox.WordWrap = false;
        logBox.BackColor = Color.FromArgb(7, 7, 9);
        logBox.ForeColor = Color.FromArgb(195, 195, 205);
        logBox.BorderStyle = BorderStyle.FixedSingle;
        logBox.Font = new Font("Consolas", 9f);
        logBox.Location = new Point(14, 40);
        logBox.Size = new Size(980, 190);
        logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        logCard.Controls.AddRange([logLabel, logBox]);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
        convertButton.Text = "FORGE XML";
        convertButton.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
        convertButton.BackColor = Red;
        convertButton.ForeColor = Color.White;
        convertButton.FlatStyle = FlatStyle.Flat;
        convertButton.FlatAppearance.BorderSize = 0;
        convertButton.Size = new Size(180, 42);
        convertButton.Location = new Point(0, 8);
        convertButton.Click += async (_, _) => await ConvertAsync();

        var open = MakeButton("OPEN OUTPUT", 194, 8, 150, 42);
        open.Click += (_, _) => OpenOutput();
        var clear = MakeButton("CLEAR LOG", 356, 8, 130, 42);
        clear.Click += (_, _) => logBox.Clear();
        var note = new Label {
            Text = "AizenX does not claim ownership of third-party conversion engines.",
            AutoSize = true, ForeColor = TextDim, Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(570, 21)
        };
        footer.Controls.AddRange([convertButton, open, clear, note]);
        footer.Resize += (_, _) => note.Left = Math.Max(520, footer.ClientSize.Width - note.Width);
        root.Controls.Add(footer, 0, 4);

        UpdateMode();
    }

    Panel MakeCard() => new() {
        Dock = DockStyle.Fill, BackColor = Panel, Margin = new Padding(0, 6, 0, 6),
        BorderStyle = BorderStyle.FixedSingle
    };

    Label MakeLabel(string text, int x, int y) => new() {
        Text = text, AutoSize = true, ForeColor = TextDim,
        Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold), Location = new Point(x, y)
    };

    Button MakeButton(string text, int x, int y, int w, int h)
    {
        var b = new Button {
            Text = text, Location = new Point(x, y), Size = new Size(w, h),
            FlatStyle = FlatStyle.Flat, BackColor = Panel2, ForeColor = TextMain,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 78);
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 20, 25);
        return b;
    }

    void StyleTextBox(TextBox box)
    {
        box.BackColor = Color.FromArgb(12, 12, 15);
        box.ForeColor = TextMain;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Consolas", 9.5f);
    }

    void AddRow(Panel card, int y, string label, TextBox box, EventHandler browse, out Button button)
    {
        var l = MakeLabel(label, 16, y + 8);
        l.Width = 120;
        StyleTextBox(box);
        box.Location = new Point(142, y);
        box.Size = new Size(700, 31);
        box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        button = MakeButton("BROWSE", 856, y, 120, 31);
        button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        button.Click += browse;
        card.Controls.AddRange([l, box, button]);
    }

    void UpdateMode()
    {
        archivePanel.Visible = archiveRadio.Checked;
        loosePanel.Visible = looseRadio.Checked;
    }

    async Task InitializeAsync()
    {
        LoadSettings();
        if (string.IsNullOrWhiteSpace(outputBox.Text))
            outputBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "AizenX_Output");

        if (string.IsNullOrWhiteSpace(engineBox.Text) || !File.Exists(engineBox.Text))
        {
            engineStatus.Text = "ENGINE SEARCH...";
            var found = await Task.Run(FindEngine);
            if (found is not null) engineBox.Text = found;
        }

        if (string.IsNullOrWhiteSpace(gameBox.Text) || !File.Exists(Path.Combine(gameBox.Text, "RDR2.exe")))
        {
            var foundGame = await Task.Run(FindGame);
            if (foundGame is not null) gameBox.Text = foundGame;
        }

        UpdateEngineStatus();
        Log("AizenX initialized.");
        if (File.Exists(engineBox.Text)) Log("Exporter engine detected.");
    }

    string? FindEngine()
    {
        var candidates = new List<string> {
            Path.Combine(AppContext.BaseDirectory, "engine", "RDR2YtdExporter.exe")
        };
        var env = Environment.GetEnvironmentVariable("AIZENX_ENGINE");
        if (!string.IsNullOrWhiteSpace(env)) candidates.Add(env);
        foreach (var c in candidates) if (File.Exists(c)) return c;

        try
        {
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            return Directory.EnumerateFiles(downloads, "RDR2YtdExporter.exe", SearchOption.AllDirectories)
                .FirstOrDefault(p => p.Contains("RDR2YtdExporterRuntime", StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    string? FindGame()
    {
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[] {
            Path.Combine(user, @"Downloads\ABDM\Compressed\Red-Dead-Redemption-2-AnkerGames\Red Dead Redemption 2"),
            @"C:\Program Files\Rockstar Games\Red Dead Redemption 2",
            @"C:\Program Files (x86)\Steam\steamapps\common\Red Dead Redemption 2",
            @"C:\Program Files\Steam\steamapps\common\Red Dead Redemption 2"
        };
        return candidates.FirstOrDefault(c => File.Exists(Path.Combine(c, "RDR2.exe")));
    }

    void UpdateEngineStatus()
    {
        if (File.Exists(engineBox.Text))
        {
            engineStatus.Text = "● ENGINE READY";
            engineStatus.ForeColor = Color.FromArgb(90, 210, 120);
        }
        else
        {
            engineStatus.Text = "● ENGINE MISSING";
            engineStatus.ForeColor = Red;
        }
    }

    void BrowseEngine(object? s, EventArgs e)
    {
        using var d = new OpenFileDialog { Filter = "RDR2 Exporter|RDR2YtdExporter.exe|Executable|*.exe" };
        if (d.ShowDialog() == DialogResult.OK) engineBox.Text = d.FileName;
        UpdateEngineStatus();
    }

    void BrowseGame(object? s, EventArgs e)
    {
        using var d = new FolderBrowserDialog { Description = "Select the folder containing RDR2.exe" };
        if (d.ShowDialog() == DialogResult.OK) gameBox.Text = d.SelectedPath;
    }

    void BrowseOutput(object? s, EventArgs e)
    {
        using var d = new FolderBrowserDialog { Description = "Select XML output folder" };
        if (d.ShowDialog() == DialogResult.OK) outputBox.Text = d.SelectedPath;
    }

    void BrowseLooseFiles()
    {
        using var d = new OpenFileDialog {
            Multiselect = true,
            Filter = "RDR2 resources|*.yft;*.ydd;*.ydr;*.ytd;*.ybn;*.ymt;*.ycd|All files|*.*"
        };
        if (d.ShowDialog() != DialogResult.OK) return;
        looseFilesBox.Text = string.Join(Environment.NewLine, d.FileNames);
    }

    async Task ConvertAsync()
    {
        if (!File.Exists(engineBox.Text)) { Fail("Exporter engine not found."); return; }
        if (!File.Exists(Path.Combine(gameBox.Text, "RDR2.exe"))) { Fail("RDR2 folder is invalid."); return; }

        var items = (archiveRadio.Checked ? archiveAssetsBox.Text : looseFilesBox.Text)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (items.Length == 0) { Fail("Add at least one asset/file."); return; }

        Directory.CreateDirectory(outputBox.Text);
        var tempList = Path.Combine(Path.GetTempPath(), $"aizenx_{Guid.NewGuid():N}.txt");
        await File.WriteAllLinesAsync(tempList, items);

        convertButton.Enabled = false;
        convertButton.Text = "FORGING...";
        Log($"Mode: {(archiveRadio.Checked ? "archive" : "local")}");
        Log($"Items: {items.Length}");
        Log($"Output: {outputBox.Text}");

        try
        {
            var psi = new ProcessStartInfo(engineBox.Text) {
                UseShellExecute = false, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(engineBox.Text)!
            };
            psi.ArgumentList.Add("--game"); psi.ArgumentList.Add(gameBox.Text);
            psi.ArgumentList.Add("--list"); psi.ArgumentList.Add(tempList);
            psi.ArgumentList.Add("--out"); psi.ArgumentList.Add(outputBox.Text);
            psi.ArgumentList.Add("--mode"); psi.ArgumentList.Add(archiveRadio.Checked ? "archive" : "local");
            psi.ArgumentList.Add("--format"); psi.ArgumentList.Add("xml");
            psi.ArgumentList.Add("--layout"); psi.ArgumentList.Add("flat");

            using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (_, e) => { if (e.Data is not null) BeginInvoke(() => Log(e.Data)); };
            p.ErrorDataReceived += (_, e) => { if (e.Data is not null) BeginInvoke(() => Log("ERR  " + e.Data)); };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            await p.WaitForExitAsync();

            if (p.ExitCode == 0)
            {
                Log("✓ Conversion completed.");
                MessageBox.Show("XML conversion completed.", "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Log($"✗ Exporter exited with code {p.ExitCode}.");
                MessageBox.Show("Conversion failed. Check the AizenX log for details.", "AizenX",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log("FATAL  " + ex.Message);
            MessageBox.Show(ex.Message, "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try { File.Delete(tempList); } catch { }
            convertButton.Enabled = true;
            convertButton.Text = "FORGE XML";
            SaveSettings();
        }
    }

    void Log(string message)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(message)); return; }
        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    void Fail(string message)
    {
        Log("✗ " + message);
        MessageBox.Show(message, "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    void OpenOutput()
    {
        try
        {
            Directory.CreateDirectory(outputBox.Text);
            Process.Start(new ProcessStartInfo("explorer.exe", outputBox.Text) { UseShellExecute = true });
        }
        catch (Exception ex) { Fail(ex.Message); }
    }

    void OnDragEnter(object? s, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
    }

    void OnDragDrop(object? s, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files) return;
        var resources = files.Where(File.Exists).ToArray();
        if (resources.Length == 0) return;
        looseRadio.Checked = true;
        looseFilesBox.Text = string.Join(Environment.NewLine, resources);
        Log($"Added {resources.Length} dropped file(s).");
    }

    void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            if (s is null) return;
            engineBox.Text = s.Engine ?? "";
            gameBox.Text = s.Game ?? "";
            outputBox.Text = s.Output ?? "";
        }
        catch { }
    }

    void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(settingsDir);
            var s = new AppSettings(engineBox.Text, gameBox.Text, outputBox.Text);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    record AppSettings(string? Engine, string? Game, string? Output);
}