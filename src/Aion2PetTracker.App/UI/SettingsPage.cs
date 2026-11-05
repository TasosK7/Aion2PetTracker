namespace Aion2PetTracker.App;

/// <summary>Settings content without the capture service, so it can be rendered for previews.</summary>
internal sealed class SettingsPage
{
    public const int CaptureLine = 0, StorageLine = 1, ShopLine = 2;
    public readonly SettingsStack Stack = new() { Dock = DockStyle.Fill };
    public readonly StatusCard Status = new(3);

    public SettingsPage(TrackerPreferences prefs, Action save, Action restoreOverlays, Action reopenMerchant)
    {
        var items = new List<SettingsItem>
        {
            new HeaderItem("Aion2 Pet Tracker", "Pet souls and Cloud Merchant overlays"),
            Status,
            new SectionLabel("Overlays") { GapAbove = 10 }
        };
        ToggleRow Toggle(string title, string? description, bool value, Action<bool> changed)
        {
            var row = new ToggleRow(title, description, value);
            row.Changed += changed;
            items.Add(row);
            return row;
        }
        Toggle("Show pet overlay", "Wallet and recent pet cards", prefs.PetSouls, v => { prefs.PetSouls = v; save(); });
        Toggle("Lock overlays", "Click through to the game", prefs.PetSoulsLocked, v => { prefs.PetSoulsLocked = v; save(); });
        Toggle("Always on top", "Keep overlays visible after Alt-Tab", prefs.AlwaysOnTop, v => { prefs.AlwaysOnTop = v; restoreOverlays(); save(); });
        var opacity = new SliderRow("Overlay opacity", 30, 100, prefs.OverlayOpacity);
        opacity.Changed += v => { prefs.OverlayOpacity = v; save(); };
        items.Add(opacity);
        items.Add(new SectionLabel("Cloud Merchant") { GapAbove = 10 });
        Toggle("Show pet details at Cloud Merchant", "Your saved progress next to each offer", prefs.MerchantInfo, v => { prefs.MerchantInfo = v; reopenMerchant(); save(); });
        Toggle("Manual mode", "Fallback if the shop is not detected · uses the last offers", prefs.MerchantManualDisplay, v => { prefs.MerchantManualDisplay = v; reopenMerchant(); save(); });
        items.Add(new ButtonBar(new ButtonBar.Item("Restore overlays", restoreOverlays)) { GapAbove = 10 });
        items.Add(new NoteItem("Saved balances are kept between sessions.\n≈ is shown only after capture gaps or unsupported updates.\nClosing settings minimizes to the taskbar. Use the tray menu to exit.") { GapAbove = 8 });
        Stack.SuspendLayout();
        foreach (var item in items)
            Stack.Controls.Add(item);
        Stack.ResumeLayout();
    }

    /// <summary>Window client size in pixels for the current DPI, sized to fit without scrolling where possible.</summary>
    public Size PreferredClientSize(float k) => new((int)Math.Round(500 * k), (int)Math.Round(Math.Min(Stack.ContentHeight, 760) * k));

    /// <summary>Renders the page with demo status, without a capture service.</summary>
    internal static void RenderPreview(string path)
    {
        var prefs = new TrackerPreferences();
        using var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new(-10000, -10000),
            BackColor = Ui.Window,
            AutoScaleMode = AutoScaleMode.None
        };
        var page = new SettingsPage(prefs, () => { }, () => { }, () => { });
        page.Status.SetHeading("Preview · Server 2011", "Wallet synchronized");
        page.Status.SetLine(CaptureLine, Tone.Good, "Receiving game data");
        page.Status.SetLine(StorageLine, Tone.Neutral, "Autosave active · 12,408 packets received");
        page.Status.SetLine(ShopLine, Tone.Neutral, "Waiting for shop offers · open Pets or Special");
        form.Controls.Add(page.Stack);
        form.ClientSize = page.PreferredClientSize(1);
        form.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        form.Close();
    }
}
