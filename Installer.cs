using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO.Compression;
using System.Windows.Forms;

class Installer
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\EMTweaks";

    static string InstallDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "EMTweaks"); }
    }

    [STAThread]
    static void Main(string[] args)
    {
        bool silent = false;
        foreach (string a in args)
            if (a == "/S" || a == "-silent" || a == "--silent") silent = true;

        try
        {
            DoInstall();
            if (!silent)
                MessageBox.Show("EMTweaks installed.\n\n" + InstallDir, "Install complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            if (!silent)
                MessageBox.Show("Install failed:\n" + ex.Message, "EMTweaks",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }

    static void DoInstall()
    {
        foreach (Process p in Process.GetProcessesByName("EMTweaks"))
        {
            try { p.Kill(); }
            catch { }
        }

        string exe = Assembly.GetExecutingAssembly().Location;
        byte[] all = File.ReadAllBytes(exe);
        byte[] magic = System.Text.Encoding.ASCII.GetBytes("EMTPAYLOADv1");
        int footer = magic.Length + 8;
        if (all.Length < footer) throw new Exception("bad payload");

        int m = all.Length - footer;
        for (int i = 0; i < magic.Length; i++)
            if (all[m + i] != magic[i]) throw new Exception("bad payload magic");

        long len = BitConverter.ToInt64(all, m + magic.Length);
        int start = (int)(all.Length - footer - len);
        if (start < 0 || len <= 0) throw new Exception("bad payload length");

        string tmp = Path.Combine(Path.GetTempPath(), "emtsetup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.WriteAllBytes(Path.Combine(tmp, "payload.zip"), Sub(all, start, (int)len));
            ZipFile.ExtractToDirectory(Path.Combine(tmp, "payload.zip"), tmp);

            string src = Path.Combine(tmp, "EMTweaks");
            string dst = InstallDir;
            Directory.CreateDirectory(dst);
            CopyTree(src, dst);

            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("emtuninst"))
            {
                if (s != null)
                    using (FileStream fs = File.Create(Path.Combine(dst, "uninstall.exe")))
                        s.CopyTo(fs);
            }

            MakeShortcuts(dst);
            WriteRegistry(dst);
        }
        finally
        {
            try { Directory.Delete(tmp, true); }
            catch { }
        }
    }

    static byte[] Sub(byte[] a, int off, int len)
    {
        byte[] r = new byte[len];
        Array.Copy(a, off, r, 0, len);
        return r;
    }

    static void CopyTree(string src, string dst)
    {
        foreach (string f in Directory.GetFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
        foreach (string d in Directory.GetDirectories(src))
            CopyTree(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    static void MakeShortcuts(string dir)
    {
        string target = Path.Combine(dir, "EMTweaks.exe");
        Type t = Type.GetTypeFromProgID("WScript.Shell");
        if (t == null) return;
        object sh = Activator.CreateInstance(t);
        MakeLink(sh, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "EMTweaks.lnk"), target, dir);
        MakeLink(sh, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "EMTweaks.lnk"), target, dir);
    }

    static void MakeLink(object sh, string lnk, string target, string workdir)
    {
        try
        {
            object sc = sh.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
            sc.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
            sc.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workdir });
            sc.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { target + ",0" });
            sc.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
        }
        catch { }
    }

    static void WriteRegistry(string dir)
    {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(KeyPath))
        {
            k.SetValue("DisplayName", "EMTweaks");
            k.SetValue("DisplayVersion", "1.4.6");
            k.SetValue("Publisher", "EMTweaks");
            k.SetValue("InstallLocation", dir);
            k.SetValue("UninstallString", "\"" + Path.Combine(dir, "uninstall.exe") + "\"");
            k.SetValue("DisplayIcon", Path.Combine(dir, "EMTweaks.exe") + ",0");
            k.SetValue("NoModify", 1);
            k.SetValue("NoRepair", 1);
        }
    }
}
