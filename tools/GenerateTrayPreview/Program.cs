// Generates docs/tray-preview.png — a composite strip showing all four tray icon
// states (no data, green, amber, red) at 64×64 on a taskbar-style background.
// Run from repo root: dotnet run --project tools/GenerateTrayPreview

using System.Drawing;
using System.Drawing.Text;

const int IconSize  = 64;
const int Padding   = 12;   // around the whole strip
const int Gap       = 8;    // between icons

// (percentage, label) pairs representing each state
(double Pct, string Label)[] states =
[
    (-1,  "No data"),
    (24,  "24%"),
    (67,  "67%"),
    (89,  "89%"),
];

const int LabelHeight = 18;
int totalW = Padding * 2 + IconSize * states.Length + Gap * (states.Length - 1);
int totalH = Padding * 2 + IconSize + Gap + LabelHeight;

// Windows 11 taskbar colour
var taskbarBg = Color.FromArgb(255, 32, 32, 32);

using var composite = new Bitmap(totalW, totalH);
using var g = Graphics.FromImage(composite);
g.Clear(taskbarBg);
g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

using var labelFont  = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel);
using var labelBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
using var sf = new StringFormat
{
    Alignment     = StringAlignment.Center,
    LineAlignment = StringAlignment.Center,
};

for (int i = 0; i < states.Length; i++)
{
    var (pct, label) = states[i];
    int x = Padding + i * (IconSize + Gap);
    int y = Padding;

    using var icon = RenderIcon(pct, IconSize);
    g.DrawImage(icon, x, y, IconSize, IconSize);

    var labelRect = new RectangleF(x, y + IconSize + Gap / 2f, IconSize, LabelHeight);
    g.DrawString(label, labelFont, labelBrush, labelRect, sf);
}

string outPath = args.Length > 0 ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "tray-preview.png");

outPath = Path.GetFullPath(outPath);
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
composite.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
Console.WriteLine($"Saved: {outPath}");

// ── Rendering ────────────────────────────────────────────────────────────────

static Bitmap RenderIcon(double percentage, int size)
{
    var bmp = new Bitmap(size, size);
    using var g = Graphics.FromImage(bmp);
    g.TextRenderingHint = TextRenderingHint.AntiAlias;
    g.Clear(Color.Transparent);

    using var brush = new SolidBrush(GetStatusColor(percentage));
    g.FillRectangle(brush, 0, 0, size, size);

    var text     = GetDisplayText(percentage);
    var fontSize = GetFontSize(text, size);
    using var font      = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
    using var textBrush = new SolidBrush(Color.White);
    using var sf = new StringFormat
    {
        Alignment     = StringAlignment.Center,
        LineAlignment = StringAlignment.Center,
    };
    g.DrawString(text, font, textBrush, new RectangleF(0, 0, size, size), sf);

    return bmp;
}

static string GetDisplayText(double pct)
{
    if (pct < 0)    return "?";
    if (pct >= 100) return "!";
    return ((int)Math.Round(pct)).ToString();
}

static float GetFontSize(string text, int iconSize)
{
    var fraction = text.Length switch
    {
        >= 3 => 0.55f,
        2    => 0.65f,
        _    => 0.72f,
    };
    return Math.Max(iconSize * fraction, 8f);
}

static Color GetStatusColor(double pct)
{
    if (pct < 0)    return Color.FromArgb(158, 158, 158);
    if (pct <= 50)  return Color.FromArgb(76,  175, 80);
    if (pct <= 80)  return Color.FromArgb(255, 152, 0);
    return                 Color.FromArgb(244, 67,  54);
}
