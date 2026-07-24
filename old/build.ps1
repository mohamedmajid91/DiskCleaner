<#
  build.ps1 - يبني DiskCleaner.exe من CleanApp.ps1
  المتطلبات: Install-Module ps2exe -Scope CurrentUser
#>
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$ps1  = Join-Path $here 'CleanApp.ps1'
$exe  = Join-Path $here 'DiskCleaner.exe'
$ico  = Join-Path $here 'icon.ico'

# استخراج رقم الإصدار من السكربت
$ver = (Select-String -Path $ps1 -Pattern '\$AppVersion\s*=\s*"([\d\.]+)"').Matches[0].Groups[1].Value
Write-Host "Building version $ver ..." -ForegroundColor Cyan

# ضمان ترميز UTF-8 with BOM (مهم للعربية)
$txt = [System.IO.File]::ReadAllText($ps1, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText($ps1, $txt, (New-Object System.Text.UTF8Encoding($true)))

Import-Module ps2exe
$args = @{
    inputFile   = $ps1
    outputFile  = $exe
    noConsole   = $true
    requireAdmin= $true
    title       = 'Disk & RAM Cleaner'
    description = 'Disk & RAM Cleaner by Mohammed Majid'
    company     = 'Mohammed Majid'
    product     = 'Disk Cleaner'
    copyright   = "(c) 2026 Mohammed Majid"
    version     = $ver
}
if (Test-Path $ico) { $args['iconFile'] = $ico }
Invoke-ps2exe @args

if (Test-Path $exe) {
    Write-Host "OK -> $exe ($([math]::Round((Get-Item $exe).Length/1KB,0)) KB)" -ForegroundColor Green
} else {
    Write-Host "BUILD FAILED" -ForegroundColor Red
}
