using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using Aion2PetTracker.Core.Pets;
namespace Aion2PetTracker.App;

internal sealed class MerchantOverlay : Form
{
    private readonly TrackerPreferences _prefs;
    private readonly Action _save;
    private readonly Dictionary<int, Bitmap> _portraits = [];
    private readonly OverlayRecovery _recovery = new();
    private MerchantView? _view;
    private Bitmap? _surface;
    private long _dismissed;
    private bool _locked, _dirty;
    private float _scale;
    private byte _alpha;
    private int _rowsPerColumn;
    internal MerchantOverlay(TrackerPreferences prefs, Action save)
    {
        _prefs = prefs;
        _save = save;
        Text = "Aion2 Pet Tracker · Merchant";
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = prefs.AlwaysOnTop;
        StartPosition = FormStartPosition.Manual;
        MouseDown += (_, e) =>
        {
            if (_prefs.PetSoulsLocked || e.Button != MouseButtons.Left)
                return;
            float x = e.X / _scale, y = e.Y / _scale;
            if (y < 34 && x > Width / _scale - 30)
            {
                _dismissed = _view?.ReceivedMs ?? 0;
                Hide();
                return;
            }
            if (y > 40)
                return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
            _prefs.MerchantX = Left;
            _prefs.MerchantY = Top;
            _save();
        };
    }
    protected override bool ShowWithoutActivation => true;
    internal void Reopen()
    {
        _dismissed = 0;
        _dirty = true;
        _recovery.Request();
    }
    protected override CreateParams CreateParams
    {
        get
        {
            var p = base.CreateParams;
            p.ExStyle |= 0x08000000 | 0x80 | 0x80000;
            if (_prefs?.PetSoulsLocked == true)
                p.ExStyle |= 0x20;
            return p;
        }
    }
    internal void RefreshState(MerchantView? view, MerchantScene scene, bool enabled, bool preview = false, float scale = 1)
    {
        if (!enabled || view is null || (!preview && ((!scene.GameFocused && !_prefs.AlwaysOnTop) || !scene.ShopVisible || view.ReceivedMs == _dismissed)))
        {
            if (Visible)
                Hide();
            return;
        }
        if (_locked != _prefs.PetSoulsLocked)
        {
            _locked = _prefs.PetSoulsLocked;
            if (IsHandleCreated)
                RecreateHandle();
            _dirty = true;
        }
        if (!preview)
            _recovery.Maintain(this, _prefs.AlwaysOnTop, Present);
        float k = preview ? scale : DeviceDpi / 96f;
        var area = scene.Bounds.IsEmpty ? Screen.PrimaryScreen!.WorkingArea : Rectangle.Intersect(scene.Bounds, Screen.FromRectangle(scene.Bounds).WorkingArea);
        if (_prefs.MerchantX is { } savedX && _prefs.MerchantY is { } savedY)
            area = Screen.FromPoint(new(savedX, savedY)).WorkingArea;
        if (area.Width <= 0 || area.Height <= 0)
            area = Screen.PrimaryScreen!.WorkingArea;
        _rowsPerColumn = Math.Max(1, (int)(area.Height / k - 108) / 54);
        int columns = (view.Rows.Count + _rowsPerColumn - 1) / _rowsPerColumn;
        int rows = Math.Min(_rowsPerColumn, view.Rows.Count);
        var size = new Size((int)Math.Round((columns * 298 - 8) * k), (int)Math.Round((rows * 54 + 70) * k));
        byte alpha = (byte)Math.Clamp((int)Math.Round(_prefs.OverlayOpacity * 2.55), 0, 255);
        bool changed = _view is null || _view.Tab != view.Tab || _view.ReceivedMs != view.ReceivedMs || _view.Character != view.Character || _view.CharacterConfirmed != view.CharacterConfirmed || !_view.Rows.SequenceEqual(view.Rows) || Size != size || _scale != k || _alpha != alpha || _dirty;
        _view = view;
        Size = size;
        _scale = k;
        _alpha = alpha;
        var wanted = new Point(_prefs.MerchantX ?? area.Right - Width - 12, _prefs.MerchantY ?? area.Top + (int)(162 * k));
        Location = new(Math.Clamp(wanted.X, area.Left, Math.Max(area.Left, area.Right - Width)), Math.Clamp(wanted.Y, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        if (changed || _surface is null)
        {
            var bitmap = DrawSurface();
            var old = _surface;
            _surface = bitmap;
            old?.Dispose();
        }
        if (preview)
            return;
        if (!Visible)
        {
            Show();
            _recovery.Request();
            changed = true;
        }
        if (changed || _dirty)
        {
            Present();
            _dirty = false;
        }
        _recovery.Maintain(this, _prefs.AlwaysOnTop, Present);
    }
    private Bitmap? Portrait(int id)
    {
        if (_portraits.TryGetValue(id, out var image))
            return image;
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"peticon/{id}.png");
        if (stream is null)
            return null;
        using var decoded = Image.FromStream(stream);
        if (_portraits.Count >= 32)
        {
            var first = _portraits.First();
            first.Value.Dispose();
            _portraits.Remove(first.Key);
        }
        return _portraits[id] = new Bitmap(decoded);
    }
    private Bitmap DrawSurface()
    {
        var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.ScaleTransform(_scale, _scale);
        float w = Width / _scale, h = Height / _scale;
        var frame = new RectangleF(1.5f, 3, w - 3, h - 6);
        Glass.Panel(g, frame, 14);
        // Title with a tab chip, character underneath.
        var titleFont = Glass.Semibold(9.5f);
        Theme.TextAt(g, "Cloud Merchant", titleFont, Glass.Text, new(14, 8, w - 60, 19));
        float titleWidth = g.MeasureString("Cloud Merchant", titleFont, PointF.Empty, StringFormat.GenericTypographic).Width;
        Glass.Chip(g, _view!.Tab == MerchantTab.Special ? "SPECIAL" : "PETS", Glass.Semibold(6.5f), Glass.Accent, 14 + titleWidth + 8, 10, 15);
        string character = _view.Character is { } name ? (_view.CharacterConfirmed ? name : "Last: " + name) : "Detecting character";
        Theme.TextAt(g, character, Theme.Font(7.5f), Glass.Subtle, new(14, 26, w - 28, 15));
        if (!_prefs.PetSoulsLocked)
        {
            // Hit area stays the original top-right 30 x 34 region.
            var close = new RectangleF(w - 28, 9, 18, 18);
            using (var bg = new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
                g.FillEllipse(bg, close);
            using var cross = new Pen(Glass.Subtle, 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(cross, close.X + 6, close.Y + 6, close.Right - 6, close.Bottom - 6);
            g.DrawLine(cross, close.Right - 6, close.Y + 6, close.X + 6, close.Bottom - 6);
        }
        using (var divider = new Pen(Glass.Divider, 1))
            g.DrawLine(divider, 12, 43.5f, w - 12, 43.5f);
        var culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        for (int i = 0; i < _view.Rows.Count; i++)
        {
            var row = _view.Rows[i];
            float x = (i / _rowsPerColumn) * 298, y = 46 + (i % _rowsPerColumn) * 54;
            if (i % _rowsPerColumn > 0)
            {
                using var line = new Pen(Glass.Divider, 1);
                g.DrawLine(line, x + 54, y - 1.5f, x + 280, y - 1.5f);
            }
            var accent = Glass.GroupAccent(row.Genus);
            bool max = row.Progress?.Level == 3;
            Glass.Portrait(g, Portrait(row.PetId), new(x + 13, y + 8, 33, 33), accent, row.Name);
            Theme.TextAt(g, row.Name, Glass.Semibold(9), Glass.Text, new(x + 54, y + 1, 226, 18));
            Glass.Dot(g, x + 57, y + 26, 5, accent);
            Theme.TextAt(g, row.Genus, Theme.Font(7.5f), Glass.Subtle, new(x + 63, y + 18, 70, 15));
            string progress = row.Progress is { } p ? (row.Uncertain ? "≈ " : "") + p.Label : "Total unknown";
            Theme.TextAt(g, progress, Glass.Semibold(7.8f), row.Progress is null ? Glass.Faint : max ? Glass.Success : Glass.Text, new(x + 130, y + 18, 150, 16), StringAlignment.Far);
            string total = row.Progress is { } t ? "Total " + (row.Uncertain ? "≈ " : "") + t.Total.ToString("N0", culture) : "No saved data yet";
            Theme.TextAt(g, total, Theme.Font(7.5f), Glass.Faint, new(x + 54, y + 34, 120, 16));
            if (row.Progress is { } bar)
                Glass.Progress(g, new(x + 190, y + 40, 90, 4), max ? 1 : (float)bar.Current / bar.Goal, max ? Glass.Success : accent);
        }
        Theme.TextAt(g, _view.Rows.Any(r => r.Uncertain) ? "≈ Possible missed changes" : _view.Rows.Any(r => r.Cached) ? "Saved data" : "Your soul balance", Theme.Font(7.5f), Glass.Faint, new(12, h - 25, w - 24, 17), StringAlignment.Center);
        return bitmap;
    }
    internal void RenderPreview(string path, MerchantView view, float scale = 1)
    {
        RefreshState(view, new(true, true, new(0, 0, 1920, 1080), 1), true, true, scale);
        _surface!.Save(path, ImageFormat.Png);
        int[] corners = [_surface.GetPixel(0, 0).A, _surface.GetPixel(Width - 1, 0).A, _surface.GetPixel(0, Height - 1).A, _surface.GetPixel(Width - 1, Height - 1).A];
        if (corners.Any(a => a != 0))
            throw new InvalidOperationException("Merchant corners must be transparent");
        _alpha = 0;
        Show();
        Present();
        bool pinned = _prefs.AlwaysOnTop;
        _prefs.AlwaysOnTop = false;
        RefreshState(view, new(false, true, Rectangle.Empty, 0), true);
        if (Visible)
            throw new InvalidOperationException("Merchant must hide outside the game");
        _prefs.AlwaysOnTop = pinned;
        Show();
        Present();
        RefreshState(view, new(true, false, Rectangle.Empty, 0), true);
        if (Visible)
            throw new InvalidOperationException("Merchant must hide outside the shop");
        Show();
        Present();
        RefreshState(view, new(true, true, Rectangle.Empty, 0), false);
        if (Visible)
            throw new InvalidOperationException("Merchant switch must hide popup");
        File.WriteAllText(Path.ChangeExtension(path, "checks.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            scale,
            rows = view.Rows.Count,
            width = Width,
            height = Height,
            corners,
            nativeAlphaUpload = true,
            contextHideChecks = true
        }));
    }
    private void Present()
    {
        if (IsHandleCreated && _surface is not null)
            LayeredSurface.Present(Handle, _surface, Location, _alpha);
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _dirty = true;
        _recovery.Request();
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        _dirty = true;
        _recovery.Request();
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }
    protected override void OnPaint(PaintEventArgs e) => Present();
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _surface?.Dispose();
            foreach (var image in _portraits.Values)
                image.Dispose();
        }
        base.Dispose(disposing);
    }
    protected override void WndProc(ref Message m)
    {
        if (OverlayRecovery.IsRecoveryMessage(m.Msg))
        {
            _dirty = true;
            _recovery.Request();
        }
        if (m.Msg == 0x21)
        {
            m.Result = new IntPtr(3);
            return;
        }
        base.WndProc(ref m);
    }
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
}
