using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Aion2PetTracker.Core.Storage;

namespace Aion2PetTracker.App;

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(9, 12, 20), Sidebar = Color.FromArgb(12, 16, 27), Card = Color.FromArgb(18, 24, 37),
        Card2 = Color.FromArgb(26, 34, 51), Hover = Color.FromArgb(36, 48, 70), Line = Color.FromArgb(38, 50, 72),
        Text = Color.FromArgb(234, 239, 248), Muted = Color.FromArgb(136, 150, 176), Accent = Color.FromArgb(62, 224, 190),
        Accent2 = Color.FromArgb(70, 160, 255), Amber = Color.FromArgb(255, 196, 94), Danger = Color.FromArgb(255, 107, 120);

    private static readonly string Family = PickFamily();
    private static readonly Dictionary<(int, bool), Font> Fonts = [];
    private static string PickFamily()
    {
        try
        {
            using var fc = new InstalledFontCollection();
            return fc.Families.Any(f => f.Name == "Segoe UI Variable Text") ? "Segoe UI Variable Text" : "Segoe UI";
        }
        catch { return "Segoe UI"; }
    }
    // Fonts are cached for the process lifetime: never dispose them.
    public static Font Font(float size = 10, bool bold = false)
    {
        var key = ((int)(size * 10), bold);
        if (!Fonts.TryGetValue(key, out var f))
            Fonts[key] = f = new Font(Family, size, bold ? FontStyle.Bold : FontStyle.Regular);
        return f;
    }
    public static GraphicsPath Round(RectangleF r, float radius = 14)
    {
        var p = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0.5f)
        {
            p.AddRectangle(r);
            return p;
        }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
    public static void Fill(Graphics g, RectangleF rect, Color color, float radius = 14)
    {
        using var path = Round(rect, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }
    public static void Stroke(Graphics g, RectangleF rect, Color color, float radius = 14, float width = 1)
    {
        using var path = Round(rect, radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    private static StringFormat Format(StringAlignment a) => new()
    {
        Alignment = a,
        LineAlignment = StringAlignment.Center,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };
    private static readonly StringFormat FNear = Format(StringAlignment.Near), FCenter = Format(StringAlignment.Center), FFar = Format(StringAlignment.Far);
    public static void TextAt(Graphics g, string text, Font f, Color color, RectangleF rect, StringAlignment align = StringAlignment.Near)
    {
        using var brush = new SolidBrush(color);
        g.DrawString(text, f, brush, rect, align switch
        {
            StringAlignment.Center => FCenter,
            StringAlignment.Far => FFar,
            _ => FNear
        });
    }
    public static Label Label(string text, float size = 10, bool bold = false, Color? color = null) =>
        new()
        {
            Text = text,
            UseMnemonic = false,
            Font = Font(size, bold),
            ForeColor = color ?? Text,
            AutoSize = true,
            BackColor = Color.Transparent
        };

    private const int DwmUseDark = 20, DwmBorder = 34, DwmCaption = 35, DwmText = 36, DwmCorner = 33;
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
    private static int Colorref(Color c) => c.R | (c.G << 8) | (c.B << 16);
    public static void Chrome(Form f, bool captionToo = true)
    {
        try
        {
            int on = 1, round = 2, cap = Colorref(Ui.Window), border = Colorref(Ui.CardBorder), text = Colorref(Glass.Text);
            DwmSetWindowAttribute(f.Handle, DwmUseDark, ref on, 4);
            if (captionToo)
            {
                DwmSetWindowAttribute(f.Handle, DwmCaption, ref cap, 4);
                DwmSetWindowAttribute(f.Handle, DwmText, ref text, 4);
            }
            DwmSetWindowAttribute(f.Handle, DwmBorder, ref border, 4);
            DwmSetWindowAttribute(f.Handle, DwmCorner, ref round, 4);
        }
        catch { }
    }
}

/// <summary>Minimal glass overlay language: translucent dark surfaces, hairline borders, one accent per pet group.
/// All colours are drawn onto a premultiplied ARGB surface, so translucency comes straight from alpha.</summary>
internal static class Glass
{
    public static readonly Color Surface = Color.FromArgb(200, 13, 16, 23), SurfaceTop = Color.FromArgb(212, 27, 32, 44),
        Border = Color.FromArgb(48, 255, 255, 255), Highlight = Color.FromArgb(40, 255, 255, 255), Divider = Color.FromArgb(24, 255, 255, 255),
        Text = Color.FromArgb(244, 246, 250), Subtle = Color.FromArgb(160, 170, 188), Faint = Color.FromArgb(112, 122, 140),
        Track = Color.FromArgb(38, 255, 255, 255), Success = Color.FromArgb(108, 230, 158), Accent = Color.FromArgb(108, 214, 232);

    public static Color GroupAccent(string? genus) => genus switch
    {
        "Fera" => Color.FromArgb(255, 132, 98),
        "Cogni" => Color.FromArgb(112, 168, 255),
        "Natura" => Color.FromArgb(98, 214, 142),
        "Varian" => Color.FromArgb(186, 136, 255),
        "Special" => Color.FromArgb(255, 202, 96),
        _ => Accent
    };
    public static Color Alpha(Color c, int alpha) => Color.FromArgb(Math.Clamp(alpha, 0, 255), c.R, c.G, c.B);
    public static Color Lighten(Color c, float t) => Color.FromArgb(c.A, c.R + (int)((255 - c.R) * t), c.G + (int)((255 - c.G) * t), c.B + (int)((255 - c.B) * t));

    private static readonly string SemiboldFamily = PickSemibold();
    private static readonly Dictionary<int, Font> SemiboldFonts = [];
    private static string PickSemibold()
    {
        try
        {
            using var fc = new InstalledFontCollection();
            return fc.Families.Any(f => f.Name == "Segoe UI Semibold") ? "Segoe UI Semibold" : "";
        }
        catch { return ""; }
    }
    /// <summary>Semibold reads cleaner than bold at overlay sizes. Falls back to the regular bold face.</summary>
    public static Font Semibold(float size)
    {
        if (SemiboldFamily.Length == 0)
            return Theme.Font(size, true);
        int key = (int)(size * 10);
        if (!SemiboldFonts.TryGetValue(key, out var f))
            SemiboldFonts[key] = f = new Font(SemiboldFamily, size, FontStyle.Regular);
        return f;
    }

    /// <summary>A frosted card: translucent fill with a faint top sheen, hairline border and a lit top edge.</summary>
    public static void Panel(Graphics g, RectangleF r, float radius)
    {
        using (var path = Theme.Round(r, radius))
        using (var fill = new LinearGradientBrush(r, SurfaceTop, Surface, 90f))
            g.FillPath(fill, path);
        var sheen = new RectangleF(r.X, r.Y, r.Width, Math.Max(1, r.Height * .5f));
        using (var path = Theme.Round(r, radius))
        using (var brush = new LinearGradientBrush(new RectangleF(sheen.X, sheen.Y - 1, sheen.Width, sheen.Height + 2), Color.FromArgb(16, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
        {
            var state = g.Save();
            g.SetClip(sheen, CombineMode.Intersect);
            g.FillPath(brush, path);
            g.Restore(state);
        }
        Theme.Stroke(g, r, Border, radius, 1);
        using var top = new LinearGradientBrush(new RectangleF(r.X, r.Y, r.Width, 1), Color.FromArgb(0, 255, 255, 255), Highlight, 0f);
        var blend = new ColorBlend
        {
            Colors = [Color.FromArgb(0, 255, 255, 255), Highlight, Highlight, Color.FromArgb(0, 255, 255, 255)],
            Positions = [0f, .3f, .7f, 1f]
        };
        top.InterpolationColors = blend;
        using var pen = new Pen(top, 1);
        g.DrawLine(pen, r.X + radius, r.Y + .5f, r.Right - radius, r.Y + .5f);
    }

    /// <summary>A rounded chip with tinted fill and text. Returns the chip bounds.</summary>
    public static RectangleF Chip(Graphics g, string text, Font font, Color accent, float x, float y, float height, bool alignRight = false)
    {
        float width = (float)Math.Ceiling(g.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic).Width) + height * .9f;
        var r = new RectangleF(alignRight ? x - width : x, y, width, height);
        Theme.Fill(g, r, Alpha(accent, 40), height / 2);
        Theme.Stroke(g, r, Alpha(accent, 70), height / 2, .8f);
        Theme.TextAt(g, text, font, Lighten(accent, .25f), r, StringAlignment.Center);
        return r;
    }

    public static void Dot(Graphics g, float cx, float cy, float size, Color color)
    {
        using var b = new SolidBrush(color);
        g.FillEllipse(b, cx - size / 2, cy - size / 2, size, size);
    }

    /// <summary>A pill track with an accent gradient fill and a soft glow at the leading edge.</summary>
    public static void Progress(Graphics g, RectangleF r, float ratio, Color accent)
    {
        float radius = r.Height / 2;
        Theme.Fill(g, r, Track, radius);
        ratio = Math.Clamp(ratio, 0, 1);
        if (ratio <= 0)
            return;
        var fill = new RectangleF(r.X, r.Y, Math.Max(r.Height, r.Width * ratio), r.Height);
        using (var glowPath = Theme.Round(new RectangleF(fill.X - 1, fill.Y - 1, fill.Width + 2, fill.Height + 2), radius + 1))
        using (var glow = new SolidBrush(Alpha(accent, 46)))
            g.FillPath(glow, glowPath);
        using var path = Theme.Round(fill, radius);
        using var brush = new LinearGradientBrush(new RectangleF(fill.X - 1, fill.Y, fill.Width + 2, fill.Height), Alpha(accent, 230), Lighten(accent, .35f), 0f);
        g.FillPath(brush, path);
    }

    /// <summary>Circular portrait with an accent ring. Falls back to a centred initial.</summary>
    public static void Portrait(Graphics g, Bitmap? image, RectangleF r, Color accent, string name)
    {
        using (var glow = new Pen(Alpha(accent, 40), 4))
            g.DrawEllipse(glow, r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2);
        using (var bg = new SolidBrush(Color.FromArgb(230, 10, 13, 19)))
            g.FillEllipse(bg, r);
        if (image is not null)
        {
            var state = g.Save();
            using var circle = new GraphicsPath();
            circle.AddEllipse(r.X + 1.5f, r.Y + 1.5f, r.Width - 3, r.Height - 3);
            g.SetClip(circle, CombineMode.Intersect);
            g.DrawImage(image, new RectangleF(r.X + 1.5f, r.Y + 1.5f, r.Width - 3, r.Height - 3));
            g.Restore(state);
        }
        else
        {
            string initial = name.Length > 0 && char.IsLetter(name[0]) ? name[..1].ToUpperInvariant() : "?";
            Theme.TextAt(g, initial, Semibold(r.Height * .32f), Alpha(accent, 220), r, StringAlignment.Center);
        }
        using var ring = new Pen(Alpha(accent, 200), 1.4f);
        g.DrawEllipse(ring, r);
    }
}

internal abstract class UiControl : Control
{
    protected UiControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
    }
    protected float K => DeviceDpi / 96f;
    protected float W => Width / K;
    protected float H => Height / K;
    protected PointF L(MouseEventArgs e) => new(e.X / K, e.Y / K);
    protected sealed override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.ScaleTransform(K, K);
        Draw(g);
    }
    protected abstract void Draw(Graphics g);
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }
}
