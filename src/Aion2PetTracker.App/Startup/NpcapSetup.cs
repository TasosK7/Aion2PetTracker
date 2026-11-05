using System.Diagnostics;
using Aion2PetTracker.Core.Capture;

namespace Aion2PetTracker.App;

/// <summary>
/// First-run guidance for the separately licensed capture driver. Nothing is
/// downloaded, installed or elevated by this application. The user opens the
/// official site and accepts the vendor's installer and Windows prompts.
/// </summary>
internal sealed class NpcapSetup : Form
{
    private readonly NoteItem _status = new("", Tone.Warn);

    internal NpcapSetup(bool preview = false)
    {
        Text = "Aion2 Pet Tracker · Setup";
        Icon = Program.LoadIcon();
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ui.Window;
        ForeColor = Glass.Text;
        var stack = new SettingsStack { Dock = DockStyle.Fill, Padding2 = 24, Gap = 8 };
        stack.Controls.Add(new HeaderItem("One-time setup", "Npcap lets the tracker receive game messages."));
        stack.Controls.Add(new StepsItem(
            "Open the official Npcap website and download its installer.",
            "Follow the installer and the Windows permission prompts.",
            "Click Check again, then start the game.") { GapAbove = 6 });
        stack.Controls.Add(_status);
        stack.Controls.Add(new ButtonBar(
            new ButtonBar.Item("Open Npcap download", OpenDownload, Primary: true),
            new ButtonBar.Item("Check again", CheckInstallation),
            new ButtonBar.Item("Exit", Cancel)) { GapAbove = 4 });
        Controls.Add(stack);
        float k = DeviceDpi / 96f;
        ClientSize = new((int)Math.Round(500 * k), (int)Math.Round(stack.ContentHeight * k));
        Shown += (_, _) => { Theme.Chrome(this); if (!preview) CheckInstallation(); };
    }

    private void OpenDownload()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://npcap.com/#download") { UseShellExecute = true });
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            _status.Set("Open https://npcap.com in your browser.", Tone.Warn);
        }
    }

    private void Cancel()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    // Enter checks again and Escape exits, as the former Accept/Cancel buttons did.
    protected override bool ProcessDialogKey(Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                Cancel();
                return true;
            case Keys.Enter:
                CheckInstallation();
                return true;
        }
        return base.ProcessDialogKey(keyData);
    }

    private void CheckInstallation()
    {
        if (LiveSniffer.IsNpcapInstalled())
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }
        _status.Set("Npcap is not installed yet.", Tone.Warn);
    }

    internal static bool EnsureInstalled()
    {
        if (LiveSniffer.IsNpcapInstalled())
            return true;
        using var setup = new NpcapSetup();
        return setup.ShowDialog() == DialogResult.OK;
    }

    internal static void RenderPreview(string path)
    {
        using var setup = new NpcapSetup(preview: true);
        setup._status.Set("Npcap is not installed yet.", Tone.Warn);
        setup.StartPosition = FormStartPosition.Manual;
        setup.Location = new(-10000, -10000);
        setup.Show();
        Application.DoEvents();
        using var bitmap = new Bitmap(setup.Width, setup.Height);
        setup.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        setup.Close();
    }
}
