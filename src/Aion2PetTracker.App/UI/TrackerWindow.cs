using System.Text.Json;
using System.Diagnostics;
namespace Aion2PetTracker.App;

internal sealed class TrackerWindow : Form
{
    private readonly PetTrackerService _service;
    private readonly TrackerPreferences _prefs;
    private readonly PetSoulsOverlay _overlay;
    private readonly MerchantOverlay _merchant;
    private readonly MerchantSceneProbe _merchantScene = new();
    private MerchantScene _scene = new(false, false, Rectangle.Empty, 0);
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 250 };
    private readonly SettingsPage _page;
    private StatusCard Status => _page.Status;
    private readonly string? _statusFile;
    private int _tick;
    private bool _exit;
    internal TrackerWindow(PetTrackerService service, TrackerPreferences prefs, string? statusFile)
    {
        _service = service;
        _prefs = prefs;
        _statusFile = statusFile;
        Text = "Aion2 Pet Tracker";
        Icon = Program.LoadIcon();
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Ui.Window;
        ForeColor = Glass.Text;
        StartPosition = FormStartPosition.CenterScreen;
        _overlay = new(prefs, Save);
        _merchant = new(prefs, Save);
        _page = new(prefs, Save, RestoreOverlays, () => _merchant.Reopen());
        Status.SetHeading("Detecting character automatically", "Waiting for game data");
        Controls.Add(_page.Stack);
        float k = DeviceDpi / 96f;
        ClientSize = _page.PreferredClientSize(k);
        MinimumSize = new((int)Math.Round(440 * k), (int)Math.Round(420 * k));
        var menu = new ContextMenuStrip { Renderer = new DarkMenuRenderer(), ShowImageMargin = false };
        menu.Items.Add("Show settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Restore overlays", null, (_, _) => RestoreOverlays());
        menu.Items.Add("Open data folder", null, (_, _) => Process.Start(new ProcessStartInfo(TrackerPreferences.Folder) { UseShellExecute = true }));
        menu.Items.Add("Copy shop diagnostics", null, (_, _) => CopyShopDiagnostics());
        menu.Items.Add("Exit", null, (_, _) => { _exit = true; Close(); });
        _tray = new()
        {
            Icon = Icon,
            Text = "Aion2 Pet Tracker",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _timer.Tick += (_, _) => UpdateState();
        _timer.Start();
        Shown += (_, _) => { Theme.Chrome(this); UpdateState(); };
        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Normal)
                RestoreOverlays();
        };
    }
    private void ShowSettings()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        RestoreOverlays();
    }
    private void RestoreOverlays()
    {
        _overlay.RestoreOverlay();
        _merchant.Reopen();
        UpdateState();
    }
    private void Save()
    {
        try
        {
            _prefs.Save();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Status.SetLine(SettingsPage.StorageLine, Tone.Bad, e is UnauthorizedAccessException ? "Could not save settings: access denied." : "Could not save settings: the file is unavailable.");
        }
    }
    private void CopyShopDiagnostics()
    {
        var s = _service.Snapshot();
        // Deliberately exclude character, server, balances, addresses and raw traffic.
        try
        {
            Clipboard.SetText(JsonSerializer.Serialize(new
            {
                AppVersion = Application.ProductVersion,
                s.Shop,
                _scene.GameFocused,
                _scene.ShopVisible,
                ClientWidth = _scene.Bounds.Width,
                ClientHeight = _scene.Bounds.Height,
                _scene.Score,
                _scene.Scale,
                _scene.OffsetX,
                _scene.OffsetY,
                Dpi = DeviceDpi,
                _prefs.MerchantInfo,
                _prefs.MerchantManualDisplay,
                _prefs.AlwaysOnTop,
                HasPetOffers = s.Merchant is not null,
                PanelVisible = _merchant.Visible
            }, new JsonSerializerOptions { WriteIndented = true }));
            Status.SetLine(SettingsPage.ShopLine, Tone.Good, "Shop diagnostics copied · no character or account data included");
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            Status.SetLine(SettingsPage.ShopLine, Tone.Warn, "Clipboard unavailable · try Copy shop diagnostics again");
        }
    }
    private void UpdateState()
    {
        var s = _service.Snapshot();
        _overlay.RefreshState(s.Pets, true, false, s.Wallet);
        bool merchantEnabled = _prefs.PetSouls && _prefs.MerchantInfo;
        _scene = _merchantScene.Update(merchantEnabled && s.Merchant is not null, PetTrackerService.Now, _prefs.MerchantManualDisplay);
        _merchant.RefreshState(s.Merchant, _scene, merchantEnabled);
        if (_tick++ % 4 != 0)
            return;
        Status.SetHeading(s.Character is { } name ? $"{name} · Server {s.Server}" : "Detecting character automatically",
            s.Identified ? s.Wallet.Status : s.Wallet.Cached ? "Saved data · Waiting for character information" : "The character is detected automatically from game data");
        bool npcap = Aion2PetTracker.Core.Capture.LiveSniffer.IsNpcapInstalled();
        Status.SetLine(SettingsPage.CaptureLine, !npcap ? Tone.Bad : s.Packets > 0 && !s.Capture.Contains("not running") ? Tone.Good : Tone.Warn,
            !npcap ? "Npcap is missing · See README for setup" : s.Capture.Contains("not running") ? "Waiting for AION 2" : s.Packets > 0 ? "Receiving game data" : "Waiting for game data");
        Status.SetLine(SettingsPage.StorageLine, s.SaveError is not null ? Tone.Bad : Tone.Neutral,
            s.SaveError ?? (s.SavePending ? "Autosave pending" : "Autosave active · " + s.Packets.ToString("N0") + " packets received"));
        Status.SetLine(SettingsPage.ShopLine, !merchantEnabled ? Tone.Neutral : _merchant.Visible ? Tone.Info : s.Merchant is not null && _scene.GameFocused ? Tone.Warn : Tone.Neutral,
            !merchantEnabled ? "Merchant panel disabled" : s.Merchant is null ?
            (s.Shop.LastMessageRejected ? "Shop data unavailable · reopen Pets or Special" : "Waiting for shop offers · open Pets or Special") :
            _merchant.Visible ? (!_scene.GameFocused ? "Merchant visible · last observed shop offers" :
            _prefs.MerchantManualDisplay ? "Merchant displayed · manual mode" : "Merchant displayed · automatic detection") :
            !_scene.GameFocused ? "Merchant ready · return to the game" : "Shop not detected · try Manual mode");
        if (_statusFile is not null)
            try
            {
                File.WriteAllText(_statusFile, JsonSerializer.Serialize(new
                {
                    release = Application.ProductVersion,
                    at = PetTrackerService.Now,
                    Snapshot = s,
                    MerchantScene = _scene,
                    MerchantVisible = _merchant.Visible
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (IOException) { }
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            return;
        }
        base.OnFormClosing(e);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _overlay.Dispose();
            _merchant.Dispose();
            _tray.Visible = false;
            _tray.ContextMenuStrip?.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}
