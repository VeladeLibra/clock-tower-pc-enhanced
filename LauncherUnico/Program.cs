// Clock Tower PC Enhanced - launcher independente para Windows.
// Projeto da adaptacao: VeladeLibra. O jogo original pertence aos respectivos titulares.
// Nao inclui ROM, BIOS, patch da traducao ou assets protegidos do Clock Tower.
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ClockTowerVeladeLibra;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new LauncherForm());
    }
}

internal sealed class LauncherPrefs
{
    public string ExePath { get; set; } = FindLocalGameExe();
    // Encontra o jogo na pasta do launcher ou em game/, sem caminhos do computador do autor.
    internal static string FindLocalGameExe()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] places = { "", "game", "build-release", "..", Path.Combine("..", "game"),
            Path.Combine("..", "build-release"), Path.Combine("..", "..", "game") };
        foreach (string place in places)
        {
            string path = Path.GetFullPath(Path.Combine(baseDir, place, "ClockTowerOriginal_Recompiled.exe"));
            if (File.Exists(path)) return path;
        }
        return ""; // Usa o botao Procurar em instalacoes personalizadas.
    }
    public string Graphics { get; set; } = "hd";
    public string Language { get; set; } = "en";
}

internal sealed class LauncherForm : Form
{
    private static readonly Color Back = Color.FromArgb(15, 17, 24);
    private static readonly Color PanelColor = Color.FromArgb(25, 28, 38);
    private static readonly Color FieldColor = Color.FromArgb(33, 37, 49);
    private static readonly Color Muted = Color.FromArgb(176, 181, 194);
    private static readonly Color Accent = Color.FromArgb(207, 67, 76);
    private static readonly string PrefsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClockTowerVeladeLibra", "launcher.json");

    private LauncherPrefs prefs = new();
    private readonly TextBox exeBox = new();
    private readonly RadioButton classicRadio = new();
    private readonly RadioButton hdRadio = new();
    private readonly RadioButton englishRadio = new();
    private readonly RadioButton portugueseRadio = new();
    private readonly Label languageNote = new();
    private readonly Label exeNote = new();
    private readonly Button playButton = new();

    internal LauncherForm()
    {
        prefs = ReadPrefs();
        // V1.7: sempre priorizar o jogo que acompanha esta instalacao.
        // Evita que preferencias antigas apontem para E:\Games no PC do autor.
        string bundledExe = LauncherPrefs.FindLocalGameExe();
        if (!string.IsNullOrEmpty(bundledExe))
            prefs.ExePath = bundledExe;
        else if (!File.Exists(prefs.ExePath))
            prefs.ExePath = "";
        Text = "Clock Tower PC Enhanced | VeladeLibra | Launcher Unico V1.8.1";
        ClientSize = new Size(940, 610);
        MinimumSize = MaximumSize = new Size(956, 649);
        BackColor = Back;
        ForeColor = Color.WhiteSmoke;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 10f);
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "tesoura_vilao.ico")); }
        catch { Icon = SystemIcons.Application; }

        // V1.8.1: desenhar a arte diretamente no fundo do header evita que
        // Labels com BackColor Transparent apaguem partes da tesoura.
        // O desenho preserva proporcao e fica totalmente dentro de um quadrado.
        string artPath = Path.Combine(AppContext.BaseDirectory, "tesoura_vilao.png");
        Image? scissorsArt = null;
        try { if (File.Exists(artPath)) scissorsArt = Image.FromFile(artPath); }
        catch (Exception) { /* arte opcional: o launcher continua funcionando */ }
        FormClosed += (_, _) => scissorsArt?.Dispose();

        var header = new Panel { Location = new Point(0, 0), Size = new Size(940, 162) };
        header.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(header.ClientRectangle,
                Color.FromArgb(54, 25, 34), Back, LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
            if (scissorsArt is not null)
            {
                // Caixa reservada de 146 x 146 no canto direito, sem cortar.
                var slot = new Rectangle(776, 5, 146, 146);
                float scale = Math.Min((float)slot.Width / scissorsArt.Width,
                    (float)slot.Height / scissorsArt.Height);
                int w = Math.Max(1, (int)Math.Round(scissorsArt.Width * scale));
                int h = Math.Max(1, (int)Math.Round(scissorsArt.Height * scale));
                var dest = new Rectangle(slot.X + (slot.Width - w) / 2,
                    slot.Y + (slot.Height - h) / 2, w, h);
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.DrawImage(scissorsArt, dest);
            }
            using var redPen = new Pen(Accent, 3);
            e.Graphics.DrawLine(redPen, 38, 154, 902, 154);
        };
        Controls.Add(header);
        // Os textos terminam ANTES da area da arte; evita sobreposicao em DPI alto.
        header.Controls.Add(Txt("CLOCK TOWER", 37, 22, 458, 59, 34, FontStyle.Bold, Color.White));
        header.Controls.Add(Txt("PC ENHANCED", 40, 85, 260, 27, 14, FontStyle.Bold, Accent));
        header.Controls.Add(Txt("Criado por VeladeLibra  •  Tradução PT-BR por VeladeLibra", 40, 118, 620, 24, 10, FontStyle.Regular, Muted));
        header.Controls.Add(Txt("CONFIGURAÇÃO E INICIALIZAÇÃO", 500, 88, 260, 28, 10, FontStyle.Bold, Color.WhiteSmoke));

        exeBox.Text = prefs.ExePath;
        exeBox.TextChanged += (s, e) => { RefreshExeNote(); UpdateLanguageNote(); SavePrefsSafe(); };
        exeNote.Location = new Point(42, 188);
        exeNote.Size = new Size(856, 44);
        exeNote.ForeColor = Color.FromArgb(142, 206, 161);
        exeNote.Font = new Font("Segoe UI", 10);
        Controls.Add(exeNote);

        // IMPORTANTE: cada painel e o contêiner REAL de seus componentes.
        // Antes os retangulos eram adicionados como controles irmaos e, na ordem
        // Z do WinForms, ocultavam os titulos e todos os RadioButtons.
        var graphicPanel = CreateSectionPanel(40, 248, 438, 226);
        var langPanel = CreateSectionPanel(493, 248, 407, 226);

        graphicPanel.Controls.Add(Txt("MODO GRÁFICO", 22, 14, 390, 30, 15, FontStyle.Bold, Color.White));
        classicRadio.Location = new Point(24, 58);
        classicRadio.Size = new Size(391, 43);
        classicRadio.Text = "Clássico PS1  •  resolução interna 1x";
        classicRadio.ForeColor = Color.WhiteSmoke;
        classicRadio.BackColor = PanelColor;
        classicRadio.Checked = prefs.Graphics != "hd";
        graphicPanel.Controls.Add(classicRadio);

        hdRadio.Location = new Point(24, 103);
        hdRadio.Size = new Size(391, 44);
        hdRadio.Text = "Alta resolução HD  •  2x + widescreen (opcional)";
        hdRadio.ForeColor = Color.WhiteSmoke;
        hdRadio.BackColor = PanelColor;
        hdRadio.Checked = prefs.Graphics == "hd";
        graphicPanel.Controls.Add(hdRadio);
        graphicPanel.Controls.Add(Txt("Widescreen disponível no modo HD quando o mod validado estiver instalado.", 24, 154, 390, 34, 9, FontStyle.Italic, Muted));
        graphicPanel.Controls.Add(Txt("Configuração otimizada para estabilidade e desempenho.", 24, 190, 396, 22, 9, FontStyle.Regular, Muted));

        langPanel.Controls.Add(Txt("IDIOMA DO JOGO", 22, 14, 365, 30, 15, FontStyle.Bold, Color.White));
        langPanel.Controls.Add(Txt("Escolha antes de clicar em JOGAR:", 24, 52, 359, 23, 10, FontStyle.Regular, Muted));
        // Os radios de idioma sao filhos de langPanel, independentes dos graficos.
        englishRadio.Location = new Point(24, 83);
        englishRadio.Size = new Size(358, 38);
        englishRadio.Text = "English — original";
        englishRadio.ForeColor = Color.WhiteSmoke;
        englishRadio.BackColor = PanelColor;
        englishRadio.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        englishRadio.Checked = prefs.Language != "pt-br";
        langPanel.Controls.Add(englishRadio);

        portugueseRadio.Location = new Point(24, 124);
        portugueseRadio.Size = new Size(358, 38);
        portugueseRadio.Text = "Português (Brasil) — PT-BR";
        portugueseRadio.ForeColor = Color.WhiteSmoke;
        portugueseRadio.BackColor = PanelColor;
        portugueseRadio.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        portugueseRadio.Checked = prefs.Language == "pt-br";
        langPanel.Controls.Add(portugueseRadio);
        englishRadio.CheckedChanged += (_, _) => { UpdateLanguageNote(); SavePrefsSafe(); };
        portugueseRadio.CheckedChanged += (_, _) => { UpdateLanguageNote(); SavePrefsSafe(); };
        classicRadio.CheckedChanged += (_, _) => SavePrefsSafe();
        hdRadio.CheckedChanged += (_, _) => SavePrefsSafe();

        languageNote.Location = new Point(24, 166);
        languageNote.Size = new Size(358, 40);
        languageNote.Font = new Font("Segoe UI", 9);
        languageNote.ForeColor = Muted;
        langPanel.Controls.Add(languageNote);
        langPanel.Controls.Add(Txt("Suporte a mouse no PC e menu moderno integrado.", 24, 190, 361, 22, 9, FontStyle.Regular, Muted));

        var advancedButton = MakeButton("CONFIG. AVANÇADAS", 42, 516, 222, 40, Color.FromArgb(57, 62, 75));
        advancedButton.Click += (_, _) => LaunchGame(true);
        Controls.Add(advancedButton);

        playButton.Text = "JOGAR";
        playButton.Location = new Point(754, 510);
        playButton.Size = new Size(144, 48);
        playButton.BackColor = Accent;
        playButton.ForeColor = Color.White;
        playButton.FlatStyle = FlatStyle.Flat;
        playButton.FlatAppearance.BorderSize = 0;
        playButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        playButton.Cursor = Cursors.Hand;
        playButton.Click += (_, _) => LaunchGame(false);
        Controls.Add(playButton);

        Controls.Add(Txt("Adaptação independente para PC. Clock Tower original: respectivos titulares.",
            42, 576, 520, 18, 8, FontStyle.Regular, Muted));
        RefreshExeNote();
        UpdateLanguageNote();
    }

    private static Label Txt(string value, int x, int y, int w, int h, float size, FontStyle style, Color color)
        => new() { Text = value, Location = new Point(x, y), Size = new Size(w, h),
                   Font = new Font("Segoe UI", size, style), ForeColor = color,
                   BackColor = Color.Transparent };

    private static Button MakeButton(string text, int x, int y, int w, int h, Color color)
        => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h),
                   BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                   Cursor = Cursors.Hand };

    private Panel CreateSectionPanel(int x, int y, int w, int h)
    {
        var panel = new Panel
        {
            Location = new Point(x, y), Size = new Size(w, h),
            BackColor = PanelColor
        };
        Controls.Add(panel);
        return panel;
    }

    private void RefreshExeNote()
    {
        bool valid = File.Exists(exeBox.Text) &&
            Path.GetFileName(exeBox.Text).Equals("ClockTowerOriginal_Recompiled.exe", StringComparison.OrdinalIgnoreCase);
        exeNote.Text = valid
            ? "Jogo pronto para iniciar com as configurações atuais."
            : "Instalação não localizada. Reinstale o pacote ou verifique os arquivos do jogo.";
        exeNote.ForeColor = valid ? Color.FromArgb(142, 206, 161) : Color.FromArgb(235, 191, 103);
    }

    // O idioma PT-BR e um recurso de mod; nao altera nem reempacota a imagem original.
    // V0.7 valida campos por secao e escolhe apenas um pacote local compativel.
    private const string PtBrPackageId = "veladelibra.clocktower-ptbr-dados";
    private const string PtBrFeatureId = "ptbr-dados";
    private const string PtBrVersion = "0.1.0";

    // O widescreen e um MOD ESPECIFICO do Clock Tower; o launcher nao inventa
    // geometria nem converte 4:3 em 16:9 esticando a imagem. Este controle
    // integra o mod quando houver uma implementacao validada instalada.
    private const string WidePackageId = "veladelibra.clocktower.widescreen";
    private const string WideFeatureId = "widescreen-16x9";
    private const string WideVersion = "0.1.0";

    private static string? FindInstalledWide(string gameDir)
    {
        foreach (string kind in new[] { "installed", "bundled", "preloaded" })
        {
            string manifest = Path.Combine(gameDir, "mods", kind,
                WidePackageId, WideVersion, "manifest.toml");
            if (!File.Exists(manifest) && kind == "preloaded")
                manifest = Path.Combine(gameDir, "mods", "preloaded", "packages",
                    WidePackageId, WideVersion, "manifest.toml");
            if (!File.Exists(manifest)) continue;
            string content = File.ReadAllText(manifest).TrimStart('\uFEFF');
            var firstTable = Regex.Match(content, @"(?m)^[ \t]*\[\[");
            string header = firstTable.Success ? content.Substring(0, firstTable.Index) : content;
            if (GetTomlLiteral(header, "id") != WidePackageId ||
                GetTomlLiteral(header, "version") != WideVersion ||
                !ReadTomlArraySections(content, "feature")
                    .Any(f => GetTomlLiteral(f, "id") == WideFeatureId))
                continue;
            // Um pacote valido deve identificar a versao USA do jogo; mesmo
            // assim, apenas testes in-game confirmam o aspecto correto.
            if (!ReadTomlArraySections(content, "target")
                    .Any(t => GetTomlLiteral(t, "game_id") == "SLUS-00539"))
                continue;
            return manifest;
        }
        return null;
    }

    private static string SetWideStateV2(string original, bool enabled)
    {
        string newline = original.Contains("\r\n") ? "\r\n" : "\n";
        string text = original.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
        if (string.IsNullOrWhiteSpace(text)) text = "format_version = 2\n";
        if (!Regex.IsMatch(text, @"(?m)^[ \t]*format_version[ \t]*=[ \t]*2[ \t]*(?:#.*)?$"))
            throw new InvalidOperationException("mods/state.toml nao esta no formato v2; abra a pagina Mods para converter.");
        var headers = Regex.Matches(text, @"(?m)^[ \t]*\[\[(?:package|feature)\]\][ \t]*(?:#.*)?$");
        int pkgCount = 0, featCount = 0;
        for (int i = headers.Count - 1; i >= 0; --i)
        {
            int start = headers[i].Index;
            int end = i + 1 < headers.Count ? headers[i+1].Index : text.Length;
            string part = text.Substring(start, end-start);
            bool package = Regex.IsMatch(part, @"^\s*\[\[package\]\]");
            if (package && GetTomlLiteral(part, "id") == WidePackageId)
            {
                pkgCount++;
                var re = new Regex(@"(?m)^([ \t]*version[ \t]*=[ \t]*)""[^""\r\n]*""");
                if (!re.IsMatch(part)) throw new InvalidDataException("Pacote widescreen sem version no state.toml.");
                part = re.Replace(part, m => m.Groups[1].Value + "\"" + WideVersion + "\"", 1);
                text = text.Remove(start,end-start).Insert(start,part);
            }
            if (!package && GetTomlLiteral(part, "package_id") == WidePackageId &&
                GetTomlLiteral(part, "id") == WideFeatureId)
            {
                featCount++;
                var re = new Regex(@"(?m)^([ \t]*enabled[ \t]*=[ \t]*)(true|false)\b");
                if (re.IsMatch(part))
                    part = re.Replace(part, m => m.Groups[1].Value + (enabled ? "true" : "false"), 1);
                else
                {
                    int eol = part.IndexOf('\n'); if(eol<0) eol=part.Length;
                    part = part.Insert(eol, "\nenabled = " + (enabled ? "true" : "false"));
                }
                text = text.Remove(start,end-start).Insert(start,part);
            }
        }
        if (pkgCount > 1 || featCount > 1)
            throw new InvalidDataException("Entradas widescreen duplicadas no state.toml.");
        if (pkgCount == 0)
            text = text.TrimEnd('\n') + "\n\n[[package]]\nid = \"" + WidePackageId +
                "\"\nversion = \"" + WideVersion + "\"\n";
        if (featCount == 0)
            text = text.TrimEnd('\n') + "\n\n[[feature]]\npackage_id = \"" + WidePackageId +
                "\"\nid = \"" + WideFeatureId + "\"\nenabled = " +
                (enabled ? "true" : "false") + "\n";
        return text.Replace("\n",newline);
    }

    private static bool ApplyWideChoice(string gameDir, bool hd)
    {
        // Sem mod instalado, HD continua em 2x 4:3; nunca usar stretch como fallback.
        if (FindInstalledWide(gameDir) is null) return false;
        string file = Path.Combine(gameDir, "mods", "state.toml");
        string oldText = File.Exists(file) ? File.ReadAllText(file) : "";
        string newText = SetWideStateV2(oldText, hd);
        if (newText != oldText)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (File.Exists(file) && !File.Exists(file + ".veladelibra-ws.bak"))
                File.Copy(file, file + ".veladelibra-ws.bak");
            string temporary = file + ".veladelibra-ws.tmp";
            try
            {
                File.WriteAllText(temporary, newText, new UTF8Encoding(false));
                File.Move(temporary,file,overwrite:true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        return hd;
    }

    private static void LogWide(string gameDir, bool hd, bool activated)
    {
        try
        {
            string file = Path.Combine(gameDir,"mods","VeladeLibra_widescreen.log");
            File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                " | modo=" + (hd ? "HD" : "CLASSICO") +
                " | 16:9 solicitado=" + activated +
                " | mod instalado=" + (FindInstalledWide(gameDir) is not null) +
                Environment.NewLine, new UTF8Encoding(false));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }


    private static bool HasTomlString(string text, string name, string value)
        => Regex.IsMatch(text, @"(?m)^[ \t]*" + Regex.Escape(name) + @"[ \t]*=[ \t]*""" +
            Regex.Escape(value) + @"""[ \t]*(?:#.*)?$");

    private static string? FindInstalledPtBr(string gameDir)
    {
        foreach (string kind in new[] { "installed", "bundled" })
        {
            string root = Path.Combine(gameDir, "mods", kind, PtBrPackageId, PtBrVersion);
            string manifest = Path.Combine(root, "manifest.toml");
            if (!File.Exists(manifest)) continue;
            string content = File.ReadAllText(manifest).TrimStart('\uFEFF');
            var firstTable = Regex.Match(content, @"(?m)^[ \t]*\[\[");
            string header = firstTable.Success ? content.Substring(0, firstTable.Index) : content;
            if (GetTomlLiteral(header, "id") != PtBrPackageId ||
                GetTomlLiteral(header, "version") != PtBrVersion ||
                !ReadTomlArraySections(content, "feature")
                    .Any(f => GetTomlLiteral(f, "id") == PtBrFeatureId))
                continue;
            return manifest;
        }
        return null;
    }

    private bool HasPtBrMod()
    {
        try
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(exeBox.Text)) ?? "";
            return FindInstalledPtBr(dir) is not null;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (ArgumentException) { return false; }
    }

    private static string? GetTomlLiteral(string text, string name)
    {
        var match = Regex.Match(text, @"(?m)^[ \t]*" + Regex.Escape(name) +
            @"[ \t]*=[ \t]*""([^""\r\n]*)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    // Altera somente a selecao do nosso recurso no formato publico v2 do PSXRecomp.
    // Preserva todos os demais pacotes, opcoes, atalhos e configuracoes do jogador.
    private static string SetModStateV2(string original, bool enable)
    {
        string newLine = original.Contains("\r\n") ? "\r\n" : "\n";
        string input = original.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
        if (string.IsNullOrWhiteSpace(input)) input = "format_version = 2\n";
        if (!Regex.IsMatch(input, @"(?m)^[ \t]*format_version[ \t]*=[ \t]*2[ \t]*(?:#.*)?$"))
            throw new InvalidOperationException(
                "O arquivo mods/state.toml nao esta no formato v2. Abra CONFIG. AVANCADAS > Mods uma vez " +
                "para migrar o estado. Nenhum arquivo foi alterado.");

        var headers = Regex.Matches(input, @"(?m)^[ \t]*\[\[(?:package|feature)\]\][ \t]*(?:#.*)?$");
        int foundPackage = 0, foundFeature = 0;
        for (int h = headers.Count - 1; h >= 0; h--)
        {
            int begin = headers[h].Index;
            int end = h + 1 < headers.Count ? headers[h + 1].Index : input.Length;
            string part = input.Substring(begin, end - begin);
            bool isPackage = part.StartsWith("[[package]]", StringComparison.Ordinal) ||
                Regex.IsMatch(part, @"^\s*\[\[package\]\]");
            if (isPackage && GetTomlLiteral(part, "id") == PtBrPackageId)
            {
                foundPackage++;
                var r = new Regex(@"(?m)^([ \t]*version[ \t]*=[ \t]*)""[^""\r\n]*""");
                if (!r.IsMatch(part))
                    throw new InvalidOperationException("Selecao do pacote PT-BR sem versao; abra Mods para corrigi-la.");
                part = r.Replace(part, m => m.Groups[1].Value + "\"" + PtBrVersion + "\"", 1);
                input = input.Remove(begin, end - begin).Insert(begin, part);
            }
            if (!isPackage && GetTomlLiteral(part, "package_id") == PtBrPackageId &&
                GetTomlLiteral(part, "id") == PtBrFeatureId)
            {
                foundFeature++;
                var r = new Regex(@"(?m)^([ \t]*enabled[ \t]*=[ \t]*)(true|false)\b");
                if (r.IsMatch(part))
                    part = r.Replace(part, m => m.Groups[1].Value + (enable ? "true" : "false"), 1);
                else
                {
                    int lineEnd = part.IndexOf('\n');
                    if (lineEnd < 0) lineEnd = part.Length;
                    part = part.Insert(lineEnd, "\n" + "enabled = " + (enable ? "true" : "false"));
                }
                input = input.Remove(begin, end - begin).Insert(begin, part);
            }
        }
        if (foundPackage > 1 || foundFeature > 1)
            throw new InvalidOperationException("Estado PT-BR duplicado em mods/state.toml; nenhuma alteracao foi salva.");
        if (foundPackage == 0)
            input = input.TrimEnd('\n') + "\n\n[[package]]\nid = \"" + PtBrPackageId +
                "\"\nversion = \"" + PtBrVersion + "\"\n";
        if (foundFeature == 0)
            input = input.TrimEnd('\n') + "\n\n[[feature]]\npackage_id = \"" + PtBrPackageId +
                "\"\nid = \"" + PtBrFeatureId + "\"\nenabled = " +
                (enable ? "true" : "false") + "\n";
        return input.Replace("\n", newLine);
    }

    private static void ApplyPtBrChoice(string gameDir, bool enable)
    {
        string? manifest = FindInstalledPtBr(gameDir);
        if (manifest is null)
        {
            if (enable)
                throw new InvalidOperationException(
                    "O mod PT-BR foi gerado, mas ainda nao esta instalado nesta pasta do jogo. " +
                    "Clique em CONFIG. AVANCADAS > Mods > Install Package, selecione o arquivo .psxmod " +
                    "VeladeLibra_ClockTower_PTBR_Dados_Experimental.psxmod e instale-o. " +
                    "Volte ao launcher e selecione Portugues (Brasil).");
            // Sem mod instalado nao ha nada para desativar. O disco segue original em ingles.
            return;
        }
        string path = Path.Combine(gameDir, "mods", "state.toml");
        string oldText = File.Exists(path) ? File.ReadAllText(path) : "";
        string newText = SetModStateV2(oldText, enable);
        if (newText == oldText) return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path) && !File.Exists(path + ".veladelibra.bak"))
            File.Copy(path, path + ".veladelibra.bak", overwrite: false);
        string temporary = path + ".veladelibra.tmp";
        try
        {
            File.WriteAllText(temporary, newText, new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    // Confere o estado EFETIVO salvo em disco. Nao pressupoe que a selecao
    // foi ativada somente porque o pacote existe na pasta installed/.
    private static bool? ReadPtBrEnabled(string gameDir)
    {
        string statePath = Path.Combine(gameDir, "mods", "state.toml");
        if (!File.Exists(statePath)) return null;
        string text = File.ReadAllText(statePath).TrimStart('\uFEFF');
        foreach (string section in ReadTomlArraySections(text, "feature"))
        {
            if (GetTomlLiteral(section, "package_id") != PtBrPackageId ||
                GetTomlLiteral(section, "id") != PtBrFeatureId) continue;
            Match match = Regex.Match(section,
                @"(?m)^[ \t]*enabled[ \t]*=[ \t]*(true|false)\b",
                RegexOptions.IgnoreCase);
            return match.Success
                ? string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase)
                : null;
        }
        return null;
    }

    // Diagnostico sem dados do jogo. Diferencia falha de gravacao da
    // preferencia e uma possivel alteracao do estado pelo runtime no boot.
    private static void LogPtBrState(string gameDir, string moment, bool? enabled)
    {
        try
        {
            string file = Path.Combine(gameDir, "mods", "VeladeLibra_ativacao.log");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + moment +
                " | ptbr-dados=" + (enabled.HasValue ? (enabled.Value ? "true" : "false") : "ausente") +
                Environment.NewLine;
            File.AppendAllText(file, line, new UTF8Encoding(false));
        }
        catch (IOException) { } // Erro de log nao impede iniciar o jogo.
        catch (UnauthorizedAccessException) { }
    }

    // Localiza a saida ja gerada pelo utilitario de traducao VeladeLibra.
    // Nao procura downloads na internet nem inclui dados protegidos no launcher.
    private static List<string> FindLocalPtBrArchives(string gameDir)
    {
        const string archiveName = "VeladeLibra_ClockTower_PTBR_Dados_Experimental.psxmod";
        string projectDir = Directory.GetParent(gameDir)?.FullName ?? "";
        string launcherDir = AppContext.BaseDirectory;
        var candidates = new List<string>();
        foreach (string path in new[] {
            Path.Combine(projectDir, "analise-ptbr"),
            Path.Combine(launcherDir, "mods"),
            Path.Combine(Directory.GetParent(launcherDir)?.FullName ?? launcherDir, "mods"),
            Path.Combine(gameDir, "mods", "pacotes-locais")
        })
        {
            try
            {
                if (!Directory.Exists(path)) continue;
                string direct = Path.Combine(path, archiveName);
                if (File.Exists(direct)) candidates.Add(direct);
                foreach (string sub in Directory.EnumerateDirectories(
                    path, "veladelibra_ptbr_dados_v01_*", SearchOption.TopDirectoryOnly))
                {
                    string p = Path.Combine(sub, archiveName);
                    if (File.Exists(p)) candidates.Add(p);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(File.GetLastWriteTimeUtc).ToList();
    }

    private static string? FindLocalPtBrArchive(string gameDir)
        => FindLocalPtBrArchives(gameDir).FirstOrDefault();

    // Le o manifesto TOML por secoes. Nao confunde o id do pacote com o id
    // de uma feature e nao depende da ordem de linhas do gerador.
    private static List<string> ReadTomlArraySections(string text, string section)
    {
        // V0.9: O arquivo state.toml/manifest.toml pode ser CRLF (Windows),
        // LF (Git/Python) ou CR. O '$' do Regex nao ignora o '\r' antes de '\n'.
        // Sem normalizar, as secoes [[package]] e [[feature]] nao eram achadas.
        text = text.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace('\r', '\n');
        var result = new List<string>();
        var headers = Regex.Matches(text, @"(?m)^[ \t]*\[\[([A-Za-z0-9_-]+)\]\][ \t]*(?:#.*)?$");
        for (int i = 0; i < headers.Count; i++)
        {
            if (headers[i].Groups[1].Value != section) continue;
            int start = headers[i].Index + headers[i].Length;
            int end = i + 1 < headers.Count ? headers[i + 1].Index : text.Length;
            result.Add(text.Substring(start, end - start));
        }
        return result;
    }

    private static void CheckManifest(bool okay, string problem)
    {
        if (!okay) throw new InvalidDataException("Manifesto PT-BR invalido: " + problem);
    }

    private static Dictionary<string, byte[]> ReadVerifiedPtBr(string archivePath)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        const int maxTotal = 2 * 1024 * 1024;
        long total = 0;
        using (var zip = ZipFile.OpenRead(archivePath))
        {
            foreach (var entry in zip.Entries)
            {
                string name = entry.FullName;
                if (name == "assets/" && entry.Length == 0) continue;
                bool allowed = name == "manifest.toml" || name == "README.txt" ||
                    Regex.IsMatch(name, @"^assets/[A-Za-z0-9._-]+[.]bin$");
                if (!allowed || !files.TryAdd(name, ReadArchiveEntry(entry, maxTotal)))
                    throw new InvalidDataException("Entrada nao permitida ou duplicada: " + name);
                total += files[name].Length;
                if (total > maxTotal)
                    throw new InvalidDataException("Pacote PT-BR maior que 2 MiB.");
            }
        }
        CheckManifest(files.TryGetValue("manifest.toml", out _), "manifest.toml ausente");
        string manifest = Encoding.UTF8.GetString(files["manifest.toml"]).TrimStart('\uFEFF');
        // Cabecalho termina na primeira secao de tabela.
        var firstTable = Regex.Match(manifest, @"(?m)^[ \t]*\[\[");
        string root = firstTable.Success ? manifest.Substring(0, firstTable.Index) : manifest;
        CheckManifest(Regex.IsMatch(root, @"(?m)^[ \t]*format_version[ \t]*=[ \t]*1[ \t]*(?:#.*)?$"),
            "format_version deve ser 1");
        CheckManifest(GetTomlLiteral(root, "id") == PtBrPackageId,
            "id do pacote diferente (encontrado: " + (GetTomlLiteral(root, "id") ?? "ausente") + ")");
        CheckManifest(GetTomlLiteral(root, "version") == PtBrVersion,
            "version diferente (encontrado: " + (GetTomlLiteral(root, "version") ?? "ausente") + ")");
        CheckManifest(GetTomlLiteral(root, "resolver") == "declarative", "resolver nao declarativo");

        var targets = ReadTomlArraySections(manifest, "target");
        CheckManifest(targets.Count == 1, "esperada uma secao [[target]]");
        CheckManifest(GetTomlLiteral(targets[0], "game_id") == "SLUS-00539",
            "[[target]]/game_id incorreto (encontrado: " + (GetTomlLiteral(targets[0], "game_id") ?? "ausente") + ")");
        const string expectedDiscHash = "e3d4392e0685864dd239eadd6b141f976556a040ff4dd867560eec7e7357a89f";
        CheckManifest(string.Equals(GetTomlLiteral(targets[0], "disc_sha256"), expectedDiscHash,
            StringComparison.OrdinalIgnoreCase), "[[target]]/disc_sha256 nao corresponde ao Clock Tower USA original");

        var features = ReadTomlArraySections(manifest, "feature");
        CheckManifest(features.Count == 1 && GetTomlLiteral(features[0], "id") == PtBrFeatureId,
            "[[feature]] ptbr-dados ausente ou com identificador diferente");

        var overlays = ReadTomlArraySections(manifest, "overlay");
        CheckManifest(overlays.Count == 20, "esperados 20 overlays, encontrados " + overlays.Count);
        CheckManifest(files.Keys.Count(x => x.StartsWith("assets/", StringComparison.Ordinal)) == 20,
            "esperados 20 arquivos de recursos no ZIP");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var spans = new List<(long start, long end)>();
        long payloadBytes = 0;
        foreach (var body in overlays)
        {
            string? name = GetTomlLiteral(body, "file");
            string? hash = GetTomlLiteral(body, "sha256");
            string? expected = GetTomlLiteral(body, "expected_sha256");
            CheckManifest(name is not null &&
                Regex.IsMatch(name, @"^assets/[A-Za-z0-9._-]+[.]bin$") &&
                files.ContainsKey(name) && names.Add(name), "asset ausente, duplicado ou invalido: " + name);
            CheckManifest(GetTomlLiteral(body, "target") == "disc_raw", "overlay nao e disc_raw");
            CheckManifest(GetTomlLiteral(body, "feature") == PtBrFeatureId,
                "overlay pertence a outra feature");
            CheckManifest(hash is not null && Regex.IsMatch(hash, @"\A[0-9a-fA-F]{64}\z"),
                "sha256 do overlay ausente/invalido: " + name);
            CheckManifest(expected is not null && Regex.IsMatch(expected, @"\A[0-9a-fA-F]{64}\z"),
                "expected_sha256 do overlay ausente/invalido: " + name);
            byte[] payload = files[name!];
            CheckManifest(Convert.ToHexString(SHA256.HashData(payload)).Equals(hash, StringComparison.OrdinalIgnoreCase),
                "SHA256 do arquivo nao confere: " + name);
            var offset = Regex.Match(body, @"(?m)^[ \t]*offset[ \t]*=[ \t]*([0-9]+)[ \t]*(?:#.*)?$");
            CheckManifest(offset.Success && long.TryParse(offset.Groups[1].Value, out _), "offset invalido: " + name);
            CheckManifest(long.TryParse(offset.Groups[1].Value, out long position) &&
                position >= 0 && position % 2352 == 0 && payload.Length > 0 &&
                payload.Length % 2352 == 0 && position <= 632629200L - payload.Length,
                "offset ou tamanho fora dos setores originais: " + name);
            spans.Add((position, position + payload.Length));
            payloadBytes += payload.Length;
        }
        spans.Sort((a, b) => a.start.CompareTo(b.start));
        for (int i = 1; i < spans.Count; i++)
            CheckManifest(spans[i].start >= spans[i - 1].end, "overlays com regioes sobrepostas");
        CheckManifest(payloadBytes == 555072L,
            "esperados 555072 bytes de dados, encontrados " + payloadBytes);
        return files;
    }

    private static byte[] ReadArchiveEntry(ZipArchiveEntry entry, int maxBytes)
    {
        if (entry.Length < 0 || entry.Length > maxBytes)
            throw new InvalidDataException("O pacote PT-BR tem um arquivo maior que o permitido.");
        using var stream = entry.Open();
        using var dest = new MemoryStream();
        byte[] buf = new byte[65536];
        int got;
        while ((got = stream.Read(buf, 0, buf.Length)) > 0)
        {
            if (dest.Length + got > maxBytes)
                throw new InvalidDataException("O pacote PT-BR excede o limite de tamanho.");
            dest.Write(buf, 0, got);
        }
        if (dest.Length != entry.Length)
            throw new InvalidDataException("Arquivo do mod incompleto: " + entry.FullName);
        return dest.ToArray();
    }

    // Instala apenas arquivos com hashes verificados e procedencia local.
    // Se houver varios gerados, tenta os mais recentes e informa por que recusou cada um.
    private static bool InstallLocalPtBr(string gameDir)
    {
        if (FindInstalledPtBr(gameDir) is not null) return false;
        var candidates = FindLocalPtBrArchives(gameDir);
        if (candidates.Count == 0)
            throw new FileNotFoundException(
                "Nao encontrei o pacote PT-BR. Execute gerar_mod_ptbr.ps1 ou " +
                "copie seu .psxmod para a pasta mods ao lado do launcher.");
        Dictionary<string, byte[]>? files = null;
        var failures = new List<string>();
        foreach (string candidate in candidates)
        {
            try
            {
                files = ReadVerifiedPtBr(candidate);
                break;
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or
                UnauthorizedAccessException)
            {
                failures.Add(Path.GetDirectoryName(candidate) + ": " + ex.Message);
            }
        }
        if (files is null)
            throw new InvalidDataException(
                "Nenhum pacote local corresponde a traducao PT-BR V0.1. Detalhes:\n" +
                string.Join("\n", failures.Take(5)) +
                "\nConfira se selecionou o .psxmod gerado pelo script VeladeLibra.");

        string installedRoot = Path.Combine(gameDir, "mods", "installed", PtBrPackageId);
        string versionRoot = Path.Combine(installedRoot, PtBrVersion);
        if (Directory.Exists(versionRoot))
            throw new IOException("Ja existe uma pasta da traducao incompleta: " + versionRoot +
                                  ". Faca backup antes de tentar remover uma instalacao incompleta.");
        Directory.CreateDirectory(installedRoot);
        string staging = Path.Combine(installedRoot, ".veladelibra-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(staging);
            foreach (var item in files)
            {
                string dest = Path.Combine(staging, item.Key.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.WriteAllBytes(dest, item.Value);
            }
            Directory.Move(staging, versionRoot);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
        return true;
    }

    private void UpdateLanguageNote()
    {
        if (!portugueseRadio.Checked)
        {
            languageNote.Text = "Inglês original. Ao jogar, a tradução PT-BR fica desativada.";
            languageNote.ForeColor = Muted;
        }
        else if (HasPtBrMod())
        {
            languageNote.Text = "PT-BR disponível. JOGAR ativa a tradução automaticamente.";
            languageNote.ForeColor = Color.FromArgb(142, 206, 161);
        }
        else
        {
            string dir = Path.GetDirectoryName(exeBox.Text.Trim()) ?? "";
            bool localMod = FindLocalPtBrArchive(dir) is not null;
            languageNote.Text = localMod
                ? "Pacote PT-BR encontrado. JOGAR instala e ativa automaticamente."
                : "Gere o pacote PT-BR uma vez; o launcher fará o resto.";
            languageNote.ForeColor = Color.FromArgb(235, 191, 103);
        }
    }

    private static LauncherPrefs ReadPrefs()
    {
        try
        {
            if (File.Exists(PrefsPath))
                return JsonSerializer.Deserialize<LauncherPrefs>(File.ReadAllText(PrefsPath)) ?? new LauncherPrefs();
        }
        catch (Exception) { /* prefer default if config is corrupt */ }
        return new LauncherPrefs();
    }

    private void SavePrefs()
    {
        prefs.ExePath = exeBox.Text.Trim();
        prefs.Graphics = hdRadio.Checked ? "hd" : "classic";
        prefs.Language = portugueseRadio.Checked ? "pt-br" : "en";
        Directory.CreateDirectory(Path.GetDirectoryName(PrefsPath)!);
        File.WriteAllText(PrefsPath, JsonSerializer.Serialize(prefs, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(false));
    }

    private void SavePrefsSafe()
    {
        try { SavePrefs(); }
        catch { }
    }

    // Atualiza apenas uma chave TOML, preservando as demais seções e criando backup.
    // Não edita game.toml, fontes, BIN/CUE, tradução nem arquivos gerados.
    private static void SetTomlKey(string path, string section, string key, string value)
    {
        string original = File.Exists(path) ? File.ReadAllText(path) : "";
        string newline = original.Contains("\r\n") ? "\r\n" : "\n";
        var lines = original.Replace("\r\n", "\n").Split('\n').ToList();
        int sectionIndex = -1;
        int end = lines.Count;
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i].Trim();
            if (t.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase))
            {
                sectionIndex = i;
                end = lines.FindIndex(i + 1, line => Regex.IsMatch(line, @"^\s*\["));
                if (end < 0) end = lines.Count;
                break;
            }
        }
        string assignment = key + " = " + value;
        if (sectionIndex < 0)
        {
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1])) lines.Add("");
            lines.Add($"[{section}]");
            lines.Add(assignment);
        }
        else
        {
            int match = -1;
            for (int i = sectionIndex + 1; i < end; i++)
                if (Regex.IsMatch(lines[i], @"^\s*" + Regex.Escape(key) + @"\s*=", RegexOptions.IgnoreCase))
                { match = i; break; }
            if (match >= 0) lines[match] = assignment;
            else lines.Insert(sectionIndex + 1, assignment);
        }
        string changed = string.Join(newline, lines);
        if (changed == original) return;
        if (File.Exists(path) && !File.Exists(path + ".veladelibra.bak"))
            File.Copy(path, path + ".veladelibra.bak");
        File.WriteAllText(path, changed, new UTF8Encoding(false));
    }

    private void LaunchGame(bool advanced)
    {
        string exe = exeBox.Text.Trim();
        if (!File.Exists(exe) || !Path.GetFileName(exe).Equals(
                "ClockTowerOriginal_Recompiled.exe", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "Instalação do jogo não localizada. Reinstale o pacote ou verifique os arquivos do jogo.",
                "Executável não encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        bool wantsPtBr = portugueseRadio.Checked;

        try
        {
            string dir = Path.GetDirectoryName(exe)!;
            // Se PT-BR foi selecionado, instala o .psxmod local na primeira vez,
            // depois alterna somente a feature de idioma. Nenhum clique em Mods.
            if (!advanced)
            {
                bool? oldValue = ReadPtBrEnabled(dir);
                LogPtBrState(dir, "antes de JOGAR, idioma=" + (wantsPtBr ? "pt" : "en"), oldValue);
                if (wantsPtBr) InstallLocalPtBr(dir);
                ApplyPtBrChoice(dir, wantsPtBr);
                bool? actual = ReadPtBrEnabled(dir);
                LogPtBrState(dir, "apos salvar selecao", actual);
                // Se a gravacao falhou ou foi revertida imediatamente,
                // avisar ao jogador em vez de iniciar no idioma errado.
                if (wantsPtBr && actual != true)
                    throw new InvalidOperationException(
                        "O mod foi instalado, mas o state.toml nao ficou enabled = true. " +
                        "Confira mods/VeladeLibra_ativacao.log e permissoes da pasta.");
                if (!wantsPtBr && actual == true)
                    throw new InvalidOperationException(
                        "O state.toml continua com PT-BR ativado apos selecionar English. " +
                        "Confira mods/VeladeLibra_ativacao.log.");
                // V1.1: o mod wide so e habilitado se usuario escolheu HD.
                // Sem o mod especifico, 2x permanece em 4:3 (sem stretch/crop).
                bool wideActivated = ApplyWideChoice(dir, hdRadio.Checked);
                LogWide(dir, hdRadio.Checked, wideActivated);
            }
            // V1.0: Nao forcar renderer. Preservar opengl, render_thread e vsync
            // escolhidos no settings.toml do usuario (perfil de desempenho aprovado).
            // Ignorar a tela inicial do PSXRecomp quando o jogador inicia o jogo.
            // A opção --launcher ainda permite entrar nela pelo botão avançado.
            // O mod especializado define 16:9 no boot; apenas o modo classico
            // e HD sem mod mantem o aspecto nativo. Nao ativamos 16:9 no TOML
            // porque o PSXRecomp atual rejeita widescreen generico.
            if (!advanced && (!hdRadio.Checked || FindInstalledWide(dir) is null))
                SetTomlKey(Path.Combine(dir,"settings.toml"), "video", "aspect_ratio", "\"4:3\"");
            SetTomlKey(Path.Combine(dir, "settings.toml"), "launcher", "skip_launcher", "true");
            // ESC abre o menu moderno V1.2 dentro do runtime recompilado.
            // Nao alterar config.ini: F1 nao deve ser novamente imposto pelo launcher.
            SavePrefs();

            var psi = new ProcessStartInfo(exe)
            {
                WorkingDirectory = dir,
                UseShellExecute = false
            };
            psi.ArgumentList.Add(advanced ? "--launcher" : "--no-launcher");
            // Nao passar --renderer: o executavel usa settings.toml (OpenGL otimizado).
            // Opcao aprovada nos testes: 2x. Pode mudar para 1x no launcher.
            psi.Environment["PSX_INTERNAL_RESOLUTION"] = hdRadio.Checked ? "2x" : "native";
            var gameProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            if (!advanced)
            {
                gameProcess.Exited += (_, _) =>
                {
                    try { LogPtBrState(dir, "apos fechar o jogo", ReadPtBrEnabled(dir)); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                };
            }
            if (!gameProcess.Start())
                throw new InvalidOperationException("Falha ao iniciar o executavel recompilado.");
            UpdateLanguageNote();
            RefreshExeNote();
        }
        catch (Exception ex)
        {
            // V0.9: Antes o log terminava em "antes de JOGAR" quando havia
            // excecao, sem explicar o motivo. Guardar a falha para diagnostico.
            try
            {
                string? dir = Path.GetDirectoryName(Path.GetFullPath(exe));
                if (!string.IsNullOrEmpty(dir))
                {
                    string reason = ex.GetType().Name + ": " + ex.Message.Replace('\r', ' ').Replace('\n', ' ');
                    LogPtBrState(dir, "FALHA na ativacao: " + reason, ReadPtBrEnabled(dir));
                }
            }
            catch (Exception) { /* diagnostico nao deve ocultar erro original */ }
            MessageBox.Show(this,
                "Não foi possível iniciar ou configurar o jogo:\n" + ex.Message,
                "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
