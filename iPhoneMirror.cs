using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

class MainForm : Form
{
    TextBox name, log;
    ComboBox size;
    CheckBox fs, rec;
    Button start, stop;
    Label status;
    Process proc;
    string engine;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }

    MainForm()
    {
        engine = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "engine");
        Text = "iPhone Mirror";
        ClientSize = new Size(460, 400);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(new Label { Text = "PC name (shows on iPhone)", Left = 16, Top = 14, Width = 220 });
        name = new TextBox { Text = Environment.MachineName, Left = 16, Top = 36, Width = 200 };
        Controls.Add(name);

        Controls.Add(new Label { Text = "Max resolution", Left = 236, Top = 14, Width = 160 });
        size = new ComboBox { Left = 236, Top = 36, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        size.Items.AddRange(new object[] { "1920x1080@60", "1920x1080@30", "1280x720@60", "1280x720@30" });
        size.SelectedIndex = 0;
        Controls.Add(size);

        fs = new CheckBox { Text = "Fullscreen", Left = 16, Top = 72, Width = 140 };
        rec = new CheckBox { Text = "Record to MP4", Left = 160, Top = 72, Width = 180 };
        Controls.Add(fs); Controls.Add(rec);

        start = new Button { Text = "Start", Left = 16, Top = 104, Width = 120, Height = 34 };
        stop = new Button { Text = "Stop", Left = 146, Top = 104, Width = 120, Height = 34, Enabled = false };
        start.Click += delegate { StartMirror(); };
        stop.Click += delegate { StopMirror(); };
        Controls.Add(start); Controls.Add(stop);

        status = new Label { Text = "Stopped", Left = 290, Top = 112, Width = 150, ForeColor = Color.Firebrick };
        Controls.Add(status);

        Controls.Add(new Label { Text = "iPhone: Control Center > Screen Mirroring > choose this PC name", Left = 16, Top = 150, Width = 430 });

        log = new TextBox { Left = 16, Top = 176, Width = 428, Height = 210, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 8.5f) };
        Controls.Add(log);

        FormClosing += delegate { StopMirror(); };
    }

    void Log(string s)
    {
        if (s == null) return;
        if (InvokeRequired) { BeginInvoke(new Action<string>(Log), s); return; }
        log.AppendText(s + "\r\n");
    }

    void StartMirror()
    {
        string exe = Path.Combine(engine, "uxplay.exe");
        if (!File.Exists(exe)) { MessageBox.Show("engine\\uxplay.exe not found next to this app."); return; }

        string nm = name.Text.Replace("\"", "").Trim();
        if (nm.Length == 0) nm = "My-PC";
        string args = "-n \"" + nm + "\" -nh -s " + size.SelectedItem;
        if (fs.Checked) args += " -fs";
        if (rec.Checked)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "iPhoneMirror");
            Directory.CreateDirectory(dir);
            args += " -mp4 \"" + Path.Combine(dir, "mirror_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp4") + "\"";
        }

        var psi = new ProcessStartInfo(exe, args);
        psi.WorkingDirectory = engine;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.EnvironmentVariables["PATH"] = engine + ";" + Environment.GetEnvironmentVariable("PATH");
        string plugins = Path.Combine(engine, "plugins");
        psi.EnvironmentVariables["GST_PLUGIN_PATH_1_0"] = plugins;
        psi.EnvironmentVariables["GST_PLUGIN_SYSTEM_PATH_1_0"] = plugins;
        psi.EnvironmentVariables["GST_REGISTRY_1_0"] = Path.Combine(Path.GetTempPath(), "iphonemirror-registry.bin");
        string scanner = Path.Combine(engine, "gst-plugin-scanner.exe");
        if (File.Exists(scanner)) psi.EnvironmentVariables["GST_PLUGIN_SCANNER"] = scanner;

        try
        {
            proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.OutputDataReceived += delegate (object s, DataReceivedEventArgs e) { Log(e.Data); };
            proc.ErrorDataReceived += delegate (object s, DataReceivedEventArgs e) { Log(e.Data); };
            proc.Exited += delegate { BeginInvoke(new Action(SetStopped)); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            Log("> uxplay " + args);
            start.Enabled = false; stop.Enabled = true;
            status.Text = "Running"; status.ForeColor = Color.ForestGreen;
        }
        catch (Exception ex) { MessageBox.Show("Could not start: " + ex.Message); }
    }

    void SetStopped()
    {
        start.Enabled = true; stop.Enabled = false;
        status.Text = "Stopped"; status.ForeColor = Color.Firebrick;
    }

    void StopMirror()
    {
        try { if (proc != null && !proc.HasExited) proc.Kill(); } catch { }
    }
}
