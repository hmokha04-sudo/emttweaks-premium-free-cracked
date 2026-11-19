# EMTweaks Installer — source

This is the complete source for `EMTweaks-Setup.exe` and `uninstall.exe`.
It is published so anyone can read exactly what the installer does and verify
that it is a plain installer, not malware.

No network code. No downloads. No background services. No persistence beyond the
uninstall entry an ordinary application registers. No obfuscation.

## What is in here

| File | Purpose |
|---|---|
| `Installer.cs` | The installer. C# / .NET Framework 4.x, ~200 lines. |
| `Uninstaller.cs` | The uninstaller. C# / .NET Framework 4.x, ~70 lines. |
| `build.bat` | Compiles both with the .NET Framework `csc.exe` already present on Windows. |
| `append.js` | Glues `payload.zip` onto the compiled installer and writes a footer. |
| `make_payload.ps1` | Turns any EMTweaks application folder into `payload.zip`. |

`payload.zip` is **not** in this repository — it is the application itself, a
binary blob. Everything that executes is the C# above.

## What the installer actually does

Read `Installer.cs`. In order:

1. Kills any running `EMTweaks.exe` processes so files are not locked.
2. Opens **its own executable**, reads a footer at the end
   (`EMTPAYLOADv1` + an 8-byte length), and slices out the appended ZIP.
3. Extracts that ZIP to a fresh temp directory.
4. Copies the extracted `EMTweaks` folder to
   `%LOCALAPPDATA%\Programs\EMTweaks`, overwriting existing files.
   This is the same location the official app installs to. No admin rights needed.
5. Writes `uninstall.exe` (embedded in the installer as a managed resource)
   into the install folder.
6. Creates two `.lnk` shortcuts through `WScript.Shell`: Start Menu and Desktop,
   both pointing at `EMTweaks.exe`.
7. Writes one registry key under
   `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EMTweaks`
   with DisplayName, DisplayVersion, Publisher, InstallLocation,
   UninstallString and DisplayIcon. This is what makes it appear in
   **Settings → Apps**.
8. Deletes the temp directory.

That is the whole install path. No other writes.

Saved application state in `%APPDATA%\emtweaks` (session, applied-tweak state,
settings) is never touched, so reinstalling over an old copy keeps settings.

## What the uninstaller actually does

Read `Uninstaller.cs`. It:

1. Reads `InstallLocation` from the registry key above; falls back to its own
   directory if the key is missing.
2. Deletes the two shortcuts.
3. Deletes the registry key.
4. Deletes every file and subfolder in the install directory, skipping itself.
5. Spawns a one-shot `cmd.exe` command to delete its own executable after exit.

## Build it yourself

Requirements: Windows, .NET Framework 4.x (ships with Windows), 7-Zip, Node.js.

```powershell
# 1. make the payload from an EMTweaks application folder
.\make_payload.ps1 -AppDir "C:\path\to\EMTweaks" -Out payload.zip

# 2. compile installer + uninstaller
cmd /c build.bat

# 3. append payload -> EMTweaks-Setup.exe
node append.js
```

`append.js` produces `EMTweaks-Setup.exe`. It writes a 12-byte ASCII marker
`EMTPAYLOADv1` and a little-endian 64-bit payload length at the very end of the
file. The installer locates its payload by reading those last 20 bytes.

Verify a built installer:

```
node append.js   # prints "magic ok: true / length ok: true"
```

## Silent install

```
EMTweaks-Setup.exe /S
```

No dialogs, no prompts. Without the flag, a single "Install complete" message
box appears.

## Why the SmartScreen warning

The binaries are not code-signed. A signed build requires a code-signing
certificate; none is included here. Windows flags unsigned executables on first
run — "More info → Run anyway" is the normal path. Signing the two `.exe` files
with your own certificate removes the warning without changing any source.
