using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Diagnostics;

class Uninst
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\EMTweaks";

    static void Main()
    {
        string dir = null;
        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(KeyPath))
                if (k != null) dir = k.GetValue("InstallLocation") as string;
        }
        catch { }

        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        try
        {
            DeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "EMTweaks.lnk"));
            DeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "EMTweaks.lnk"));
        }
        catch { }

        try { Registry.CurrentUser.DeleteSubKeyTree(KeyPath, false); }
        catch { }

        try
        {
            string self = Assembly.GetExecutingAssembly().Location;
            foreach (string f in Directory.GetFiles(dir))
                if (!string.Equals(f, self, StringComparison.OrdinalIgnoreCase)) DeleteFile(f);
            foreach (string d in Directory.GetDirectories(dir)) DeleteDir(d);
        }
        catch { }

        try
        {
            string self = Assembly.GetExecutingAssembly().Location;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c ping 127.0.0.1 -n 2 > nul & del /f /q \"" + self + "\"";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            Process.Start(psi);
        }
        catch { }
    }

    static void DeleteFile(string p) { try { if (File.Exists(p)) File.Delete(p); } catch { } }
    static void DeleteDir(string p) { try { if (Directory.Exists(p)) Directory.Delete(p, true); } catch { } }
}
