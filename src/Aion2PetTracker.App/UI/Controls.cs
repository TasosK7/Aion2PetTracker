using System.Drawing.Drawing2D;

namespace Aion2PetTracker.App;

/// <summary>Opaque window palette that matches the glass overlays.</summary>
internal static class Ui
{
    public static readonly Color Window = Color.FromArgb(11, 13, 19), Card = Color.FromArgb(20, 23, 31), CardHover = Color.FromArgb(26, 30, 40),
        CardBorder = Color.FromArgb(34, 39, 51), Track = Color.FromArgb(46, 52, 66), Focus = Color.FromArgb(150, 108, 214, 232),
        Good = Color.FromArgb(108, 230, 158), Warn = Color.FromArgb(255, 196, 94), Bad = Color.FromArgb(255, 112, 122);
    public static Color Of(Tone tone) => tone switch
    {
        Tone.Good => Good,
        Tone.Warn => Warn,
        Tone.Bad => Bad,
        Tone.Info => Glass.Accent,
        _ => Glass.Faint
    };
    public static void CardFill(Graphics g, RectangleF r, bool hover = false, float radius = 10)
    {
        Theme.Fill(g, r, hover ? CardHover : Card, radius);
        Theme.Stroke(g, r, CardBorder, radius);
    }
    public static void FocusRing(Graphics g, RectangleF r, float radius = 10) => Theme.Stroke(g, new(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), Focus, radius - 1, 1.5f);
}

internal enum Tone
{
    Neutral, Good, Warn, Bad, Info
}

/// <summary>A row in a <see cref="SettingsStack"/>. Heights are logical (96 DPI) units.</summary>
internal abstract class SettingsItem : UiControl
{
    protected SettingsItem() => BackColor = Ui.Window;
    public abstract float LogicalHeight
    {
        get;
    }
    /// <summary>Extra space above this item, on top of the stack's normal gap.</summary>
    public float GapAbove
    {
        get; init;
    }
    protected bool Hover
    {
        get; private set;
    }
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Hover = true;
        Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Hover = false;
        Invalidate();
    }
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
    protected bool ShowFocus => Focused && ShowFocusCues;
}

/// <summary>Vertical, DPI-aware, scrollable stack of settings items. Every child is laid out:
/// Control.Visible reads false for all children until the window is shown, so it cannot drive measurement.</summary>
internal sealed class SettingsStack : Panel
{
    public float Padding2 { get; init; } = 20;
    public float Gap { get; init; } = 6;
    public SettingsStack()
    {
        AutoScroll = true;
        BackColor = Ui.Window;
        DoubleBuffered = true;
    }
    // Dark scrollbars on Windows 10 1809+ and 11; older systems keep the default theme.
    [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? app, string? idList);
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }
    }
    public float ContentHeight
    {
        get
        {
            float y = Padding2;
            foreach (Control c in Controls)
                if (c is SettingsItem item)
                    y += item.GapAbove + item.LogicalHeight + Gap;
            return y - Gap + Padding2;
        }
    }
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        float k = DeviceDpi / 96f;
        int pad = (int)Math.Round(Padding2 * k), width = Math.Max(0, ClientSize.Width - pad * 2);
        float y = Padding2;
        foreach (Control c in Controls)
        {
            if (c is not SettingsItem item)
                continue;
            y += item.GapAbove;
            int top = (int)Math.Round(y * k) + AutoScrollPosition.Y, height = (int)Math.Round(item.LogicalHeight * k);
            if (c.Bounds != new Rectangle(pad, top, width, height))
                c.SetBounds(pad, top, width, height);
            y += item.LogicalHeight + Gap;
        }
        var min = new Size(0, (int)Math.Round(ContentHeight * k));
        if (AutoScrollMinSize != min)
            AutoScrollMinSize = min;
    }
}

internal sealed class HeaderItem(string title, string subtitle) : SettingsItem
{
    public override float LogicalHeight => 48;
    protected override void Draw(Graphics g)
    {
        Theme.TextAt(g, title, Glass.Semibold(15), Glass.Text, new(0, 0, W, 28));
        Theme.TextAt(g, subtitle, Theme.Font(9), Glass.Subtle, new(1, 28, W, 18));
    }
}

internal sealed class SectionLabel(string text) : SettingsItem
{
    public override float LogicalHeight => 22;
    protected override void Draw(Graphics g) => Theme.TextAt(g, text.ToUpperInvariant(), Glass.Semibold(7.5f), Glass.Faint, new(2, 4, W, H - 4));
}

/// <summary>Plain lines of muted text, optionally with a status dot.</summary>
internal sealed class NoteItem : SettingsItem
{
    private string[] _lines;
    private Tone? _tone;
    public NoteItem(string text, Tone? tone = null)
    {
        _lines = text.Split('\n');
        _tone = tone;
    }
    public override float LogicalHeight => Math.Max(1, _lines.Length) * 17 + 2;
    public void Set(string text, Tone? tone)
    {
        var lines = text.Split('\n');
        if (lines.SequenceEqual(_lines) && tone == _tone)
            return;
        bool relayout = lines.Length != _lines.Length;
        _lines = lines;
        _tone = tone;
        Invalidate();
        if (relayout)
            Parent?.PerformLayout();
    }
    protected override void Draw(Graphics g)
    {
        float x = 2;
        if (_tone is { } tone && _lines.Any(l => l.Length > 0))
        {
            Glass.Dot(g, 6, 9.5f, 7, Ui.Of(tone));
            x = 16;
        }
        for (int i = 0; i < _lines.Length; i++)
            Theme.TextAt(g, _lines[i], Theme.Font(8.5f), _tone is { } t ? Ui.Of(t) : Glass.Subtle, new(x, i * 17 + 1, W - x, 17));
    }
}

/// <summary>Card with a title, optional description and an iOS-style switch. Space or Enter toggles.</summary>
internal sealed class ToggleRow : SettingsItem
{
    private readonly string _title;
    private readonly string? _description;
    private bool _checked;
    public event Action<bool>? Changed;
    public ToggleRow(string title, string? description, bool initial)
    {
        _title = title;
        _description = description;
        _checked = initial;
        TabStop = true;
        Cursor = Cursors.Hand;
        // Every click toggles; otherwise a quick second click becomes a DoubleClick and is lost.
        SetStyle(ControlStyles.StandardDoubleClick, false);
        AccessibleRole = AccessibleRole.CheckButton;
        AccessibleName = title;
    }
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            _checked = value;
            AccessibleDescription = value ? "On" : "Off";
            Invalidate();
            Changed?.Invoke(value);
        }
    }
    public override float LogicalHeight => _description is null ? 44 : 58;
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        Checked = !Checked;
    }
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
        }
    }
    protected override void Draw(Graphics g)
    {
        var card = new RectangleF(.5f, .5f, W - 1, H - 1);
        Ui.CardFill(g, card, Hover);
        if (ShowFocus)
            Ui.FocusRing(g, card);
        float textWidth = W - 80;
        if (_description is null)
            Theme.TextAt(g, _title, Glass.Semibold(9.5f), Glass.Text, new(14, 0, textWidth, H));
        else
        {
            Theme.TextAt(g, _title, Glass.Semibold(9.5f), Glass.Text, new(14, 9, textWidth, 20));
            Theme.TextAt(g, _description, Theme.Font(8.2f), Glass.Subtle, new(14, 29, textWidth, 18));
        }
        var track = new RectangleF(W - 54, (H - 22) / 2, 40, 22);
        Theme.Fill(g, track, _checked ? Glass.Accent : Ui.Track, 11);
        float knob = 16, x = _checked ? track.Right - knob - 3 : track.X + 3;
        using var shadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0));
        g.FillEllipse(shadow, x, track.Y + 4, knob, knob);
        using var brush = new SolidBrush(_checked ? Color.FromArgb(8, 22, 26) : Glass.Text);
        g.FillEllipse(brush, x, track.Y + 3, knob, knob);
    }
}

/// <summary>Card with a title, live value and a draggable track. Arrow keys step by 5, Home/End jump.</summary>
internal sealed class SliderRow : SettingsItem
{
    private readonly string _title;
    private readonly int _min, _max;
    private int _value;
    private bool _dragging;
    public event Action<int>? Changed;
    public SliderRow(string title, int min, int max, int value)
    {
        _title = title;
        _min = min;
        _max = max;
        _value = Math.Clamp(value, min, max);
        TabStop = true;
        AccessibleRole = AccessibleRole.Slider;
        AccessibleName = title;
    }
    public int Value
    {
        get => _value;
        set
        {
            value = Math.Clamp(value, _min, _max);
            if (_value == value)
                return;
            _value = value;
            AccessibleDescription = value + "%";
            Invalidate();
            Changed?.Invoke(value);
        }
    }
    public override float LogicalHeight => 66;
    private RectangleF TrackBounds => new(14, 44, W - 28, 4);
    private void SetFrom(MouseEventArgs e)
    {
        var t = TrackBounds;
        Value = _min + (int)Math.Round(Math.Clamp((L(e).X - t.X) / t.Width, 0, 1) * (_max - _min));
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        Focus();
        _dragging = true;
        Capture = true;
        SetFrom(e);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
            SetFrom(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
        Capture = false;
    }
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int? next = e.KeyCode switch
        {
            Keys.Left or Keys.Down => _value - 5,
            Keys.Right or Keys.Up => _value + 5,
            Keys.Home => _min,
            Keys.End => _max,
            _ => null
        };
        if (next is { } v)
        {
            Value = v;
            e.Handled = true;
        }
    }
    protected override void Draw(Graphics g)
    {
        var card = new RectangleF(.5f, .5f, W - 1, H - 1);
        Ui.CardFill(g, card, Hover || _dragging);
        if (ShowFocus)
            Ui.FocusRing(g, card);
        Theme.TextAt(g, _title, Glass.Semibold(9.5f), Glass.Text, new(14, 9, W - 90, 20));
        Theme.TextAt(g, _value + "%", Glass.Semibold(9), Glass.Subtle, new(W - 80, 9, 66, 20), StringAlignment.Far);
        var t = TrackBounds;
        float ratio = (float)(_value - _min) / Math.Max(1, _max - _min);
        Theme.Fill(g, t, Ui.Track, 2);
        Theme.Fill(g, new(t.X, t.Y, Math.Max(4, t.Width * ratio), t.Height), Glass.Accent, 2);
        float cx = t.X + t.Width * ratio, cy = t.Y + t.Height / 2, r = _dragging ? 8 : 7;
        using (var glow = new SolidBrush(Glass.Alpha(Glass.Accent, 50)))
            g.FillEllipse(glow, cx - r - 3, cy - r - 3, (r + 3) * 2, (r + 3) * 2);
        using var knob = new SolidBrush(Glass.Text);
        g.FillEllipse(knob, cx - r, cy - r, r * 2, r * 2);
    }
}

/// <summary>Live status: character, overall state and a few toned lines.</summary>
internal sealed class StatusCard : SettingsItem
{
    private string _title = "", _subtitle = "";
    private readonly (Tone Tone, string Text)[] _lines;
    public StatusCard(int lines) => _lines = Enumerable.Repeat((Tone.Neutral, ""), lines).ToArray();
    public override float LogicalHeight => 64 + _lines.Length * 21 + 6;
    public void SetHeading(string title, string subtitle)
    {
        if (_title == title && _subtitle == subtitle)
            return;
        _title = title;
        _subtitle = subtitle;
        Invalidate();
    }
    public void SetLine(int index, Tone tone, string text)
    {
        if (_lines[index] == (tone, text))
            return;
        _lines[index] = (tone, text);
        Invalidate();
    }
    protected override void Draw(Graphics g)
    {
        var card = new RectangleF(.5f, .5f, W - 1, H - 1);
        Ui.CardFill(g, card, false, 12);
        using (var path = Theme.Round(card, 12))
        using (var sheen = new LinearGradientBrush(card, Glass.Alpha(Glass.Accent, 18), Color.FromArgb(0, Glass.Accent), 90f))
            g.FillPath(sheen, path);
        Theme.TextAt(g, _title, Glass.Semibold(12), Glass.Text, new(16, 12, W - 32, 24));
        Theme.TextAt(g, _subtitle, Theme.Font(8.5f), Glass.Subtle, new(16, 36, W - 32, 18));
        using (var divider = new Pen(Ui.CardBorder, 1))
            g.DrawLine(divider, 16, 60.5f, W - 16, 60.5f);
        for (int i = 0; i < _lines.Length; i++)
        {
            float y = 66 + i * 21;
            var (tone, text) = _lines[i];
            if (text.Length == 0)
                continue;
            Glass.Dot(g, 21, y + 10.5f, 7, Ui.Of(tone));
            Theme.TextAt(g, text, Theme.Font(8.5f), tone == Tone.Neutral ? Glass.Subtle : Glass.Text, new(32, y, W - 48, 21));
        }
    }
}

/// <summary>A row of rounded buttons. Tab focuses the row, Left/Right moves, Enter/Space activates.</summary>
internal sealed class ButtonBar : SettingsItem
{
    internal sealed record Item(string Text, Action Click, bool Primary = false);
    private readonly Item[] _items;
    private RectangleF[] _bounds = [];
    private int _hover = -1, _pressed = -1, _selected;
    public ButtonBar(params Item[] items)
    {
        _items = items;
        TabStop = true;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleName = string.Join(", ", items.Select(i => i.Text));
    }
    public override float LogicalHeight => 38;
    private void Measure(Graphics g)
    {
        _bounds = new RectangleF[_items.Length];
        float x = .5f;
        for (int i = 0; i < _items.Length; i++)
        {
            float w = g.MeasureString(_items[i].Text, Glass.Semibold(9), PointF.Empty, StringFormat.GenericTypographic).Width + 32;
            _bounds[i] = new(x, .5f, w, H - 1);
            x += w + 8;
        }
    }
    private int HitTest(MouseEventArgs e)
    {
        var p = L(e);
        return Array.FindIndex(_bounds, b => b.Contains(p));
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int hit = HitTest(e);
        Cursor = hit >= 0 ? Cursors.Hand : Cursors.Default;
        if (hit != _hover)
        {
            _hover = hit;
            Invalidate();
        }
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        base.OnMouseLeave(e);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
            return;
        _pressed = HitTest(e);
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        int hit = HitTest(e), pressed = _pressed;
        _pressed = -1;
        Invalidate();
        if (hit >= 0 && hit == pressed)
        {
            _selected = hit;
            _items[hit].Click();
        }
    }
    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Enter or Keys.Space || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Left:
                _selected = Math.Max(0, _selected - 1);
                break;
            case Keys.Right:
                _selected = Math.Min(_items.Length - 1, _selected + 1);
                break;
            case Keys.Enter or Keys.Space:
                _items[_selected].Click();
                break;
            default:
                return;
        }
        e.Handled = true;
        Invalidate();
    }
    protected override void Draw(Graphics g)
    {
        Measure(g);
        for (int i = 0; i < _items.Length; i++)
        {
            var b = _bounds[i];
            var item = _items[i];
            bool hot = i == _hover, down = i == _pressed;
            float radius = b.Height / 2;
            if (item.Primary)
            {
                var fill = down ? Glass.Alpha(Glass.Accent, 200) : hot ? Glass.Lighten(Glass.Accent, .15f) : Glass.Accent;
                Theme.Fill(g, b, fill, radius);
                Theme.TextAt(g, item.Text, Glass.Semibold(9), Color.FromArgb(8, 22, 26), b, StringAlignment.Center);
            }
            else
            {
                Theme.Fill(g, b, down ? Ui.Track : hot ? Ui.CardHover : Ui.Card, radius);
                Theme.Stroke(g, b, Ui.CardBorder, radius);
                Theme.TextAt(g, item.Text, Glass.Semibold(9), Glass.Text, b, StringAlignment.Center);
            }
            if (ShowFocus && i == _selected)
                Ui.FocusRing(g, b, radius);
        }
    }
}

/// <summary>Dark tray and context menus.</summary>
internal sealed class DarkMenuRenderer() : ToolStripProfessionalRenderer(new Palette())
{
    private sealed class Palette : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Ui.Card;
        public override Color ImageMarginGradientBegin => Ui.Card;
        public override Color ImageMarginGradientMiddle => Ui.Card;
        public override Color ImageMarginGradientEnd => Ui.Card;
        public override Color MenuBorder => Ui.CardBorder;
        public override Color MenuItemBorder => Ui.CardHover;
        public override Color MenuItemSelected => Ui.CardHover;
        public override Color MenuItemSelectedGradientBegin => Ui.CardHover;
        public override Color MenuItemSelectedGradientEnd => Ui.CardHover;
        public override Color SeparatorDark => Ui.CardBorder;
        public override Color SeparatorLight => Ui.CardBorder;
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Glass.Text;
        base.OnRenderItemText(e);
    }
}

/// <summary>Numbered steps with accent badges.</summary>
internal sealed class StepsItem(params string[] steps) : SettingsItem
{
    public override float LogicalHeight => steps.Length * 34 - 6;
    protected override void Draw(Graphics g)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            float y = i * 34;
            var badge = new RectangleF(0, y + 2, 24, 24);
            Theme.Fill(g, badge, Glass.Alpha(Glass.Accent, 40), 12);
            Theme.Stroke(g, badge, Glass.Alpha(Glass.Accent, 90), 12);
            Theme.TextAt(g, (i + 1).ToString(), Glass.Semibold(8.5f), Glass.Lighten(Glass.Accent, .25f), badge, StringAlignment.Center);
            Theme.TextAt(g, steps[i], Theme.Font(9.5f), Glass.Text, new(36, y, W - 36, 28));
        }
    }
}
