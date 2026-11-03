Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Cap {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@ -ErrorAction Stop

$root = "C:\Users\saaho\Documents\Granny 13"
$exe = Join-Path $root "build\Granny13.exe"
$p = Start-Process -FilePath $exe -ArgumentList "--seed","777" -PassThru
Start-Sleep -Seconds 4
$p.Refresh()
if ($p.HasExited) { Write-Output "PROCESS EXITED code=$($p.ExitCode)"; exit 1 }

$h = $p.MainWindowHandle
Write-Output "hwnd=$h title=$($p.MainWindowTitle)"
[Win32Cap]::ShowWindow($h, 5) | Out-Null
[Win32Cap]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 700

$r = New-Object Win32Cap+RECT
[Win32Cap]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.Right - $r.Left; $ht = $r.Bottom - $r.Top
Write-Output "rect=$($r.Left),$($r.Top) size=${w}x${ht}"

$bmp = New-Object System.Drawing.Bitmap($w, $ht)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
$ok = [Win32Cap]::PrintWindow($h, $hdc, 2)
$g.ReleaseHdc($hdc)
$g.Dispose()
$out = Join-Path $root "shots\window_capture.png"
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "printwindow=$ok saved=$out"

# also grab the desktop in case PrintWindow is black
$sw = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width
Add-Type -AssemblyName System.Windows.Forms
$b2 = New-Object System.Drawing.Bitmap(1280, 720)
$g2 = [System.Drawing.Graphics]::FromImage($b2)
$g2.CopyFromScreen($r.Left, $r.Top, 0, 0, (New-Object System.Drawing.Size(1280, 720)))
$g2.Dispose()
$b2.Save((Join-Path $root "shots\window_screen.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$b2.Dispose()

Start-Sleep -Seconds 1
$p.Refresh()
if (-not $p.HasExited) { $p.Kill() }
Write-Output "done"
