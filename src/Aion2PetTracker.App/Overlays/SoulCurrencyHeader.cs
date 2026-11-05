using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using Aion2PetTracker.Core.Pets;

namespace Aion2PetTracker.App;

/// <summary>Permanent compact wallet. Artwork is sampled from the user's unchanged reference,
/// while all amounts are separately drawn live text. No bitmap generation or animated effects.</summary>
internal sealed class SoulCurrencyHeader : UiControl
{
    public SoulCurrencyView View = SoulCurrencyView.Unknown;
    public bool Demo;
    private Bitmap? _reference;
    private readonly ToolTip _tip = new();
    private static readonly SoulCurrencyKind[] Order = [SoulCurrencyKind.Fera, SoulCurrencyKind.Cogni, SoulCurrencyKind.Natura, SoulCurrencyKind.Varian, SoulCurrencyKind.Special, SoulCurrencyKind.Rainbow];
    // Source coordinates are within the provided 2172 x 724 image. Numbers and frame text are never sampled.
    private static readonly RectangleF[] Icons = [new(113, 285, 115, 155), new(450, 285, 120, 155), new(786, 285, 120, 155), new(1110, 285, 120, 155), new(1431, 285, 120, 155), new(1750, 285, 120, 155)];
    public SoulCurrencyHeader()
    {
        MouseMove += (_, e) =>
        {
            string text = TooltipAt(L(e).X, W);
            if (_tip.GetToolTip(this) != text)
                _tip.SetToolTip(this, text);
        };
    }
    internal string TooltipAt(float x, float width)
    {
        float firstWidth = (width - 120) / 5;
        int index = Math.Clamp((int)(x / firstWidth), 0, 5);
        var kind = Order[index];
        string age = View.VerifiedMs > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(View.VerifiedMs).ToLocalTime().ToString("MMM d, yyyy HH:mm", CultureInfo.GetCultureInfo("en-US")) : "unknown";
        return $"{(kind == SoulCurrencyKind.Rainbow ? "Rainbow Souls" : kind + " Soul Crystals")}: " + (View.Count(kind)?.ToString("N0", CultureInfo.GetCultureInfo("en-US")) ?? "Balance unknown") +
            (View.Cached ? $"\nSaved balance · last confirmed: {age}" : "") +
            (View.Uncertain ? "\n≈ Possible missed changes · Synchronization needed" : "") + (View.Character is { } name ? "\n" + name : "");
    }
    protected override void Draw(Graphics g) => DrawOverlay(g, W, H);
    internal void DrawOverlay(Graphics g, float W, float H)
    {
        var frame = new RectangleF(1, 3, W - 2, H - 6);
        Glass.Panel(g, frame, frame.Height / 2);
        if (_reference is null)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("gameicon/SoulHeaderReference.png");
            if (stream is not null)
            {
                using var decoded = Image.FromStream(stream);
                _reference = new Bitmap(decoded);
            }
        }
        using var attributes = new ImageAttributes();
        attributes.SetColorKey(Color.Black, Color.FromArgb(12, 12, 12));
        for (int i = 0; i < 6; i++)
        {
            float firstWidth = (W - 120) / 5;
            float left = i * firstWidth, right = i == 5 ? W : (i + 1) * firstWidth;
            if (i > 0)
            {
                using var line = new Pen(Glass.Divider, 1);
                g.DrawLine(line, left, 13, left, H - 13);
            }
            // The first segment starts at the rounded end of the pill; nudge its content inwards.
            float inset = i == 0 ? 9 : 6;
            if (_reference is not null)
            {
                var src = Icons[i];
                var target = new Rectangle((int)(left + inset), 11, 15, 20);
                g.DrawImage(_reference, target, src.X, src.Y, src.Width, src.Height, GraphicsUnit.Pixel, attributes);
            }
            var value = View.Count(Order[i]);
            string count = value is null ? "—" : (View.Uncertain ? "≈" : "") + value.Value.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            float textLeft = left + inset + 15, textWidth = right - textLeft - (i == 5 ? 10 : 3);
            float size = 9;
            while (size > 6.5f && g.MeasureString(count, Glass.Semibold(size)).Width > textWidth)
                size -= .5f;
            Theme.TextAt(g, count, Glass.Semibold(size), value is null ? Glass.Faint : View.Cached ? Glass.Subtle : Glass.Text, new(textLeft, 8, textWidth, H - 16), StringAlignment.Center);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _reference?.Dispose();
            _tip.Dispose();
        }
        base.Dispose(disposing);
    }
}
