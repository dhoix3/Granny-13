# Build Granny 13 (C# / .NET Framework 4.x, no external dependencies)
param(
    [switch]$Run,
    [switch]$Shots
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
if (-not (Test-Path $csc)) { throw "csc.exe not found" }

$src = Join-Path $root "src\*.cs"
$out = Join-Path $root "Granny13.exe"
$assets = Join-Path $root "assets"

$refs = @("/r:System.dll", "/r:System.Core.dll", "/r:System.Drawing.dll", "/r:System.Windows.Forms.dll")

$res = @(
    "/win32icon:$assets\icon.ico",
    "/resource:$assets\granny.png,granny.png",
    "/resource:$assets\granny_face.png,granny_face.png",
    "/resource:$assets\marusya.png,marusya.png",
    "/resource:$assets\marusya_angry.png,marusya_angry.png",
    "/resource:$assets\wallpaper.png,wallpaper.png",
    "/resource:$assets\items.png,items.png",
    "/resource:$assets\icon.png,icon.png"
)

$args = @("/nologo", "/target:winexe", "/unsafe+", "/codepage:65001", "/optimize+", "/platform:anycpu", "/out:$out") + $refs + $res + @($src)
Write-Host "Compiling..." -ForegroundColor Cyan
& $csc $args
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; exit 1 }
Write-Host "OK -> $out" -ForegroundColor Green

if ($Shots) {
    & $out --shots --nosound --out (Join-Path $root "shots")
    Write-Host "shots exit=$LASTEXITCODE"
}
if ($Run) { & $out }
