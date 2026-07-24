<#
===============================================================================
  Disk & RAM Cleaner  -  منظّف القرص والذاكرة
  -----------------------------------------------------------------------------
  * Bilingual (Arabic / English) with live language toggle
  * Safe: never touches personal files (Downloads / Documents / Desktop)
  * Analyzes sizes first, then you choose and clean
  * Frees RAM cache (working sets + standby list)
  * Built-in GitHub update check
  -----------------------------------------------------------------------------
  Author : Mohammed Majid
  Repo   : https://github.com/mohamedmajid91/DiskCleaner
===============================================================================
#>

# ==== إعدادات التحديث - عدّل هذين السطرين بعد إنشاء مستودع GitHub ============
$RepoOwner  = "mohamedmajid91"           # اسم مستخدم GitHub
$RepoName   = "DiskCleaner"
$AppVersion = "1.3.1"
# ============================================================================

# --- رفع الصلاحيات تلقائياً --------------------------------------------------
$isAdmin = ([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent()
).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Start-Process powershell.exe -Verb RunAs `
        -ArgumentList "-ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

$ErrorActionPreference = 'SilentlyContinue'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

# --- محرّك تحرير الذاكرة (Working Sets + Standby List) ----------------------
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class MemTools {
    [DllImport("psapi.dll")] public static extern bool EmptyWorkingSet(IntPtr hProcess);
    [DllImport("ntdll.dll")] public static extern int NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);
    [DllImport("advapi32.dll", SetLastError=true)] public static extern bool OpenProcessToken(IntPtr h, uint acc, out IntPtr tok);
    [DllImport("advapi32.dll", SetLastError=true)] public static extern bool LookupPrivilegeValue(string host, string name, ref long luid);
    [DllImport("advapi32.dll", SetLastError=true)] public static extern bool AdjustTokenPrivileges(IntPtr tok, bool dis, ref TOKEN_PRIVILEGES nst, int len, IntPtr prev, IntPtr rlen);
    [DllImport("kernel32.dll")] public static extern IntPtr GetCurrentProcess();
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TOKEN_PRIVILEGES { public int Count; public long Luid; public int Attr; }
    public static void EnablePrivilege(string priv) {
        IntPtr tok;
        OpenProcessToken(GetCurrentProcess(), 0x28, out tok);
        TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
        tp.Count = 1; tp.Attr = 0x2; tp.Luid = 0;
        LookupPrivilegeValue(null, priv, ref tp.Luid);
        AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
    }
    public static int PurgeStandbyList() {
        EnablePrivilege("SeProfileSingleProcessPrivilege");
        int cmd = 4;
        GCHandle h = GCHandle.Alloc(cmd, GCHandleType.Pinned);
        int res = NtSetSystemInformation(0x50, h.AddrOfPinnedObject(), Marshal.SizeOf(cmd));
        h.Free();
        return res;
    }
}
"@

# ============================================================================
#  الترجمة (عربي / إنجليزي)
# ============================================================================
$script:Lang = 'en'
$T = @{
    title        = @{ ar='منظّف القرص والذاكرة'; en='Disk & RAM Cleaner' }
    diskFree     = @{ ar='قرص C: فارغ';          en='C: free' }
    ramUsed      = @{ ar='الرام مستخدمة';         en='RAM used' }
    freeWord     = @{ ar='فارغ';                  en='free' }
    analyze      = @{ ar='تحليل';                 en='Analyze' }
    cleanSel     = @{ ar='تنظيف المحدد';          en='Clean Selected' }
    freeRam      = @{ ar='تحرير الذاكرة (RAM)';   en='Free RAM' }
    close        = @{ ar='إغلاق';                 en='Close' }
    autoRam      = @{ ar='تحرير الرام تلقائياً كل 10 دقائق (طالما البرنامج مفتوح)'; en='Auto-free RAM every 10 min (while app is open)' }
    totalClean   = @{ ar='المجموع القابل للتنظيف'; en='Total cleanable' }
    pressAnalyze = @{ ar='اضغط (تحليل) لحساب الأحجام'; en='Press (Analyze) to calculate sizes' }
    analyzing    = @{ ar='جاري تحليل';            en='Analyzing' }
    cleaning     = @{ ar='جاري تنظيف';            en='Cleaning' }
    doneAnalyze  = @{ ar='التحليل خلص. اختر الفئات واضغط تنظيف.'; en='Analysis done. Select items and clean.' }
    doneClean    = @{ ar='خلص التنظيف.';          en='Cleaning done.' }
    freeingRam   = @{ ar='جاري تحرير الذاكرة...'; en='Freeing RAM...' }
    ramDone      = @{ ar='تم تحرير الذاكرة.';     en='RAM freed.' }
    autoOn       = @{ ar='التحرير التلقائي مُفعّل (كل 10 دقائق).'; en='Auto-free enabled (every 10 min).' }
    autoOff      = @{ ar='التحرير التلقائي متوقّف.'; en='Auto-free disabled.' }
    noSelect     = @{ ar='ما اخترت ولا فئة.';     en='No category selected.' }
    warn         = @{ ar='تنبيه';                 en='Warning' }
    confirmTitle = @{ ar='تأكيد التنظيف';         en='Confirm cleanup' }
    willDelete   = @{ ar='راح ينحذف:';            en='Will be deleted:' }
    permanent    = @{ ar='هذا حذف نهائي (سلة المحذوفات تنفرغ). متأكد؟'; en='This is permanent (Recycle Bin emptied). Continue?' }
    resultTitle  = @{ ar='النتيجة';               en='Result' }
    cleanOk      = @{ ar='تم التنظيف بنجاح.';     en='Cleanup successful.' }
    before       = @{ ar='قبل';                   en='Before' }
    after        = @{ ar='بعد';                   en='After' }
    freed        = @{ ar='تم تحرير';              en='Freed' }
    ramTitle     = @{ ar='الذاكرة (RAM)';         en='Memory (RAM)' }
    used         = @{ ar='مستخدمة';               en='used' }
    checkUpdate  = @{ ar='التحقق من التحديثات';   en='Check for updates' }
    updTitle     = @{ ar='التحديثات';             en='Updates' }
    updAvail     = @{ ar='يتوفّر إصدار جديد';     en='A new version is available' }
    updLatest    = @{ ar='أنت على أحدث إصدار.';   en='You are on the latest version.' }
    updFail      = @{ ar='تعذّر التحقق من التحديثات (تأكد من الإنترنت وإعدادات المستودع).'; en='Could not check for updates (check internet / repo settings).' }
    updDownload  = @{ ar='هل تريد فتح صفحة التنزيل؟'; en='Open the download page?' }
    langBtn      = @{ ar='English';               en='عربي' }
}
function L($k) { $T[$k][$script:Lang] }

# ============================================================================
#  فئات التنظيف (كلها آمنة) - الأسماء ثنائية اللغة
# ============================================================================
$LU = $env:LOCALAPPDATA
$Categories = [ordered]@{
    'temp'        = @{ Name=@{ar='ملفات مؤقتة (Temp)';en='Temporary files'}; Paths=@("$env:TEMP","$LU\Temp","C:\Windows\Temp","C:\Windows\Prefetch") }
    'winupdate'   = @{ Name=@{ar='كاش تحديثات ويندوز';en='Windows Update cache'}; Paths=@("C:\Windows\SoftwareDistribution\Download"); Service=@('wuauserv','bits') }
    'chrome'      = @{ Name=@{ar='كاش متصفح Chrome';en='Chrome cache'}; Paths=@("$LU\Google\Chrome\User Data\Default\Cache","$LU\Google\Chrome\User Data\Default\Code Cache","$LU\Google\Chrome\User Data\Default\GPUCache") }
    'edge'        = @{ Name=@{ar='كاش متصفح Edge';en='Edge cache'}; Paths=@("$LU\Microsoft\Edge\User Data\Default\Cache","$LU\Microsoft\Edge\User Data\Default\Code Cache","$LU\Microsoft\Edge\User Data\Default\GPUCache") }
    'firefox'     = @{ Name=@{ar='كاش متصفح Firefox';en='Firefox cache'}; Dynamic={ $ff="$LU\Mozilla\Firefox\Profiles"; if(Test-Path $ff){ Get-ChildItem $ff -Directory | ForEach-Object { Join-Path $_.FullName 'cache2' } } } }
    'thumbnails'  = @{ Name=@{ar='كاش الصور المصغّرة';en='Thumbnail cache'}; Files=@("$LU\Microsoft\Windows\Explorer\thumbcache_*.db") }
    'crashdumps'  = @{ Name=@{ar='تقارير الأخطاء والكراش';en='Crash dumps & error reports'}; Paths=@("C:\ProgramData\Microsoft\Windows\WER\ReportQueue","C:\ProgramData\Microsoft\Windows\WER\ReportArchive","C:\Windows\Minidump"); Files=@("C:\Windows\MEMORY.DMP") }
    'recyclebin'  = @{ Name=@{ar='سلة المحذوفات';en='Recycle Bin'}; RecycleBin=$true }
    'deliveryopt' = @{ Name=@{ar='كاش Delivery Optimization';en='Delivery Optimization cache'}; Special='deliveryopt' }
}

# ============================================================================
#  دوال الحساب والتنظيف
# ============================================================================
function Get-FreeGB { [math]::Round((Get-PSDrive C).Free / 1GB, 2) }
function Get-RamInfo {
    $os = Get-CimInstance Win32_OperatingSystem
    $totalKB = [double]$os.TotalVisibleMemorySize
    $freeKB  = [double]$os.FreePhysicalMemory
    return @{ FreeGB=[math]::Round($freeKB/1MB,2); UsedPct=[math]::Round((($totalKB-$freeKB)/$totalKB)*100,0) }
}
function Clear-RAM {
    foreach ($p in (Get-Process)) { try { [MemTools]::EmptyWorkingSet($p.Handle) | Out-Null } catch {} }
    try { [MemTools]::PurgeStandbyList() | Out-Null } catch {}
}
function Format-Size([int64]$b) {
    if ($b -ge 1GB) { return ("{0:N2} GB" -f ($b/1GB)) }
    if ($b -ge 1MB) { return ("{0:N1} MB" -f ($b/1MB)) }
    if ($b -ge 1KB) { return ("{0:N0} KB" -f ($b/1KB)) }
    return "$b B"
}
function Resolve-Paths($cat) {
    $p=@(); if($cat.Paths){$p+=$cat.Paths}; if($cat.Dynamic){$p+=(& $cat.Dynamic)}
    return $p | Where-Object { $_ }
}
function Get-CategorySize($key,$cat) {
    switch ($key) {
        'recyclebin' {
            $b=0; 'C','D','E' | ForEach-Object { $rb="$($_):\`$Recycle.Bin"; if(Test-Path $rb){ $b+=(Get-ChildItem $rb -Recurse -File -Force -EA SilentlyContinue|Measure-Object Length -Sum).Sum } }
            return [int64]$b
        }
        'deliveryopt' { try { $st = Get-DeliveryOptimizationStatus -ErrorAction Stop 2>$null 3>$null 4>$null 5>$null 6>$null; return [int64](($st|Measure-Object FileSizeInCache -Sum).Sum) } catch { return 0 } }
        default {
            $b=0; foreach($p in (Resolve-Paths $cat)){ if(Test-Path $p){ $b+=(Get-ChildItem $p -Recurse -File -Force -EA SilentlyContinue|Measure-Object Length -Sum).Sum } }
            if($cat.Files){ foreach($f in $cat.Files){ Get-ChildItem $f -Force -EA SilentlyContinue|ForEach-Object{ $b+=$_.Length } } }
            return [int64]$b
        }
    }
}
function Invoke-Clean($key,$cat) {
    if ($cat.Service) { Stop-Service $cat.Service -Force -EA SilentlyContinue }
    switch ($key) {
        'recyclebin'  { Clear-RecycleBin -Force -Confirm:$false -EA SilentlyContinue }
        'deliveryopt' { try { Delete-DeliveryOptimizationCache -Force -ErrorAction Stop 2>$null 3>$null 4>$null 5>$null 6>$null } catch {} }
        default {
            foreach($p in (Resolve-Paths $cat)){ if(Test-Path $p){ Get-ChildItem $p -Recurse -Force -EA SilentlyContinue|Remove-Item -Recurse -Force -Confirm:$false -EA SilentlyContinue } }
            if($cat.Files){ foreach($f in $cat.Files){ Remove-Item $f -Force -EA SilentlyContinue } }
        }
    }
    if ($cat.Service) { Start-Service $cat.Service -EA SilentlyContinue }
}
function Check-Update {
    try {
        $url = "https://raw.githubusercontent.com/$RepoOwner/$RepoName/main/version.txt"
        $latest = ((Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 8).Content).Trim()
        if ([version]$latest -gt [version]$AppVersion) {
            $m = "$(L 'updAvail'): $latest`n$(L 'updDownload')"
            if ([System.Windows.Forms.MessageBox]::Show($m,(L 'updTitle'),'YesNo','Information') -eq 'Yes') {
                Start-Process "https://github.com/$RepoOwner/$RepoName/releases/latest"
            }
        } else {
            [System.Windows.Forms.MessageBox]::Show((L 'updLatest'),(L 'updTitle'),'OK','Information') | Out-Null
        }
    } catch {
        [System.Windows.Forms.MessageBox]::Show((L 'updFail'),(L 'updTitle'),'OK','Warning') | Out-Null
    }
}

# ============================================================================
#  الألوان والخطوط
# ============================================================================
$clDark   = [System.Drawing.Color]::FromArgb(28,30,38)
$clPanel  = [System.Drawing.Color]::FromArgb(40,43,52)
$clAccent = [System.Drawing.Color]::FromArgb(0,150,136)
$clAccentH= [System.Drawing.Color]::FromArgb(0,180,164)
$clPurple = [System.Drawing.Color]::FromArgb(120,80,200)
$clPurpleH= [System.Drawing.Color]::FromArgb(140,100,220)
$clGray   = [System.Drawing.Color]::FromArgb(55,60,70)
$clGrayH  = [System.Drawing.Color]::FromArgb(70,76,88)
$clText   = [System.Drawing.Color]::White
$clMuted  = [System.Drawing.Color]::FromArgb(165,172,185)
$clLink   = [System.Drawing.Color]::FromArgb(90,170,255)
$fontMain = New-Object System.Drawing.Font("Segoe UI",10)
$fontBold = New-Object System.Drawing.Font("Segoe UI",11,[System.Drawing.FontStyle]::Bold)

# ============================================================================
#  النافذة
# ============================================================================
$form = New-Object System.Windows.Forms.Form
$form.Text = "Disk & RAM Cleaner"
$form.Size = New-Object System.Drawing.Size(580,760)
$form.StartPosition = "CenterScreen"
$form.BackColor = $clDark
$form.ForeColor = $clText
$form.Font = $fontMain
$form.FormBorderStyle = 'FixedSingle'
$form.MaximizeBox = $false
try { $form.Icon = [System.Drawing.Icon]::ExtractAssociatedIcon([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) } catch {}

# --- شريط علوي متدرّج ---
$header = New-Object System.Windows.Forms.Panel
$header.Location = New-Object System.Drawing.Point(0,0)
$header.Size = New-Object System.Drawing.Size(580,66)
$header.Add_Paint({
    param($s,$e)
    $rect = $s.ClientRectangle
    if ($rect.Width -le 0 -or $rect.Height -le 0) { return }   # يمنع انهيار GDI+ عند إعادة الرسم
    try {
        $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect,$clAccent,$clPurple,0)
        $e.Graphics.FillRectangle($br,$rect); $br.Dispose()
    } catch {
        $e.Graphics.Clear($clAccent)
    }
})
$form.Controls.Add($header)

$lblTitle = New-Object System.Windows.Forms.Label
$lblTitle.Font = New-Object System.Drawing.Font("Segoe UI",15,[System.Drawing.FontStyle]::Bold)
$lblTitle.ForeColor = $clText
$lblTitle.BackColor = [System.Drawing.Color]::Transparent
$lblTitle.Location = New-Object System.Drawing.Point(20,16)
$lblTitle.Size = New-Object System.Drawing.Size(400,36)
$header.Controls.Add($lblTitle)

# زر تبديل اللغة
$btnLang = New-Object System.Windows.Forms.Button
$btnLang.Size = New-Object System.Drawing.Size(90,32)
$btnLang.Location = New-Object System.Drawing.Point(460,17)
$btnLang.FlatStyle='Flat'; $btnLang.FlatAppearance.BorderColor=[System.Drawing.Color]::White; $btnLang.FlatAppearance.BorderSize=1
$btnLang.BackColor=[System.Drawing.Color]::Transparent; $btnLang.ForeColor=$clText
$btnLang.Font=$fontMain; $btnLang.Cursor='Hand'
$header.Controls.Add($btnLang)

# --- سطر المعلومات (قرص + رام) ---
$lblInfo = New-Object System.Windows.Forms.Label
$lblInfo.ForeColor = $clMuted
$lblInfo.Location = New-Object System.Drawing.Point(20,78)
$lblInfo.Size = New-Object System.Drawing.Size(540,24)
$form.Controls.Add($lblInfo)

# --- لوحة الفئات ---
$panel = New-Object System.Windows.Forms.Panel
$panel.Location = New-Object System.Drawing.Point(20,110)
$panel.Size = New-Object System.Drawing.Size(540,300)
$panel.BackColor = $clPanel
$panel.AutoScroll = $true
$form.Controls.Add($panel)

$checkboxes = @{}
$sizeLabels = @{}
$y = 12
foreach ($key in $Categories.Keys) {
    $cb = New-Object System.Windows.Forms.CheckBox
    $cb.ForeColor = $clText; $cb.Font = $fontMain
    $cb.Location = New-Object System.Drawing.Point(16,$y)
    $cb.Size = New-Object System.Drawing.Size(320,26)
    $cb.Checked = $true
    $panel.Controls.Add($cb); $checkboxes[$key] = $cb

    $sl = New-Object System.Windows.Forms.Label
    $sl.Text = "--"; $sl.ForeColor = $clMuted; $sl.TextAlign='MiddleRight'
    $sl.Location = New-Object System.Drawing.Point(360,$y)
    $sl.Size = New-Object System.Drawing.Size(150,26)
    $panel.Controls.Add($sl); $sizeLabels[$key] = $sl
    $y += 32
}

# --- المجموع ---
$lblTotal = New-Object System.Windows.Forms.Label
$lblTotal.Font = $fontBold; $lblTotal.ForeColor = $clAccentH
$lblTotal.Location = New-Object System.Drawing.Point(20,420)
$lblTotal.Size = New-Object System.Drawing.Size(540,26)
$form.Controls.Add($lblTotal)

# --- شريط التقدّم ---
$progress = New-Object System.Windows.Forms.ProgressBar
$progress.Location = New-Object System.Drawing.Point(20,452)
$progress.Size = New-Object System.Drawing.Size(540,18)
$progress.Style = 'Continuous'
$form.Controls.Add($progress)

# --- الحالة ---
$lblStatus = New-Object System.Windows.Forms.Label
$lblStatus.ForeColor = $clMuted
$lblStatus.Location = New-Object System.Drawing.Point(20,476)
$lblStatus.Size = New-Object System.Drawing.Size(540,22)
$form.Controls.Add($lblStatus)

# --- الأزرار ---
function New-Btn($x,$y,$w,$color,$hover) {
    $b = New-Object System.Windows.Forms.Button
    $b.Size=New-Object System.Drawing.Size($w,44)
    $b.Location=New-Object System.Drawing.Point($x,$y)
    $b.FlatStyle='Flat'; $b.FlatAppearance.BorderSize=0
    $b.BackColor=$color; $b.ForeColor=[System.Drawing.Color]::White
    $b.Font=$fontBold; $b.Cursor='Hand'
    $b.Tag=@{base=$color;hover=$hover}
    $b.Add_MouseEnter({ $this.BackColor=$this.Tag.hover })
    $b.Add_MouseLeave({ $this.BackColor=$this.Tag.base })
    return $b
}
$btnClean   = New-Btn 300 508 260 $clAccent $clAccentH
$btnAnalyze = New-Btn 20  508 260 $clGray   $clGrayH
$btnRam     = New-Btn 300 560 260 $clPurple $clPurpleH
$btnClose   = New-Btn 20  560 260 $clGray   $clGrayH
$form.Controls.Add($btnClean); $form.Controls.Add($btnAnalyze)
$form.Controls.Add($btnRam);   $form.Controls.Add($btnClose)

# --- خيار تحرير الرام التلقائي ---
$chkAuto = New-Object System.Windows.Forms.CheckBox
$chkAuto.ForeColor = $clText; $chkAuto.Font = $fontMain
$chkAuto.Location = New-Object System.Drawing.Point(20,616)
$chkAuto.Size = New-Object System.Drawing.Size(540,26)
$form.Controls.Add($chkAuto)

# --- تذييل: تحديثات + توقيع ---
$lnkUpdate = New-Object System.Windows.Forms.LinkLabel
$lnkUpdate.LinkColor = $clLink; $lnkUpdate.ActiveLinkColor=$clAccentH
$lnkUpdate.Font = $fontMain
$lnkUpdate.Location = New-Object System.Drawing.Point(20,650)
$lnkUpdate.Size = New-Object System.Drawing.Size(240,22)
$form.Controls.Add($lnkUpdate)

$lblCredit = New-Object System.Windows.Forms.Label
$lblCredit.Text = "v$AppVersion  •  by Mohammed Majid"
$lblCredit.ForeColor = $clMuted
$lblCredit.Font = New-Object System.Drawing.Font("Segoe UI",8)
$lblCredit.TextAlign = 'MiddleRight'
$lblCredit.Location = New-Object System.Drawing.Point(300,650)
$lblCredit.Size = New-Object System.Drawing.Size(260,22)
$form.Controls.Add($lblCredit)

# ============================================================================
#  اللغة + تحديث المعلومات
# ============================================================================
function Update-Header {
    $r = Get-RamInfo
    $lblInfo.Text = "$(L 'diskFree') $(Get-FreeGB) GB    |    $(L 'ramUsed') $($r.UsedPct)%  ($(L 'freeWord') $($r.FreeGB) GB)"
}
$script:analyzed = $false
$script:lastTotal = [int64]0
function Apply-Language {
    try {
        $ar = ($script:Lang -eq 'ar')
        $form.SuspendLayout()
        $form.RightToLeft = if($ar){'Yes'}else{'No'}
        $form.RightToLeftLayout = $ar
        $header.RightToLeft = if($ar){'Yes'}else{'No'}
        $lblTitle.Text = L 'title'
        $btnLang.Text  = L 'langBtn'
        $btnAnalyze.Text = L 'analyze'
        $btnClean.Text   = L 'cleanSel'
        $btnRam.Text     = L 'freeRam'
        $btnClose.Text   = L 'close'
        $chkAuto.Text    = L 'autoRam'
        $lnkUpdate.Text  = L 'checkUpdate'
        foreach ($k in $Categories.Keys) { $checkboxes[$k].Text = $Categories[$k].Name[$script:Lang] }
        if ($script:analyzed) { $lblTotal.Text = "$(L 'totalClean'): $(Format-Size $script:lastTotal)" }
        else { $lblTotal.Text = L 'pressAnalyze' }
        Update-Header
        $form.ResumeLayout()
        $form.Refresh()
    } catch {
        try { $form.ResumeLayout() } catch {}
    }
}

# ============================================================================
#  السلوك
# ============================================================================
$btnLang.Add_Click({ $script:Lang = if($script:Lang -eq 'ar'){'en'}else{'ar'}; Apply-Language })

$btnAnalyze.Add_Click({
    $btnAnalyze.Enabled=$false; $btnClean.Enabled=$false
    $progress.Value=0; $progress.Maximum=$Categories.Count
    $total=[int64]0; $i=0
    foreach ($key in $Categories.Keys) {
        $i++; $lblStatus.Text="$(L 'analyzing'): $($Categories[$key].Name[$script:Lang])..."
        [System.Windows.Forms.Application]::DoEvents()
        $s = Get-CategorySize $key $Categories[$key]
        $sizeLabels[$key].Text = Format-Size $s
        $total += $s; $progress.Value=$i
        [System.Windows.Forms.Application]::DoEvents()
    }
    $script:analyzed=$true; $script:lastTotal=$total
    $lblTotal.Text = "$(L 'totalClean'): $(Format-Size $total)"
    $lblStatus.Text = L 'doneAnalyze'
    Update-Header
    $btnAnalyze.Enabled=$true; $btnClean.Enabled=$true
})

$btnClean.Add_Click({
    $sel = @($Categories.Keys | Where-Object { $checkboxes[$_].Checked })
    if ($sel.Count -eq 0) { [System.Windows.Forms.MessageBox]::Show((L 'noSelect'),(L 'warn'),'OK','Warning')|Out-Null; return }
    $names = ($sel | ForEach-Object { "• " + $Categories[$_].Name[$script:Lang] }) -join "`n"
    $msg = "$(L 'willDelete')`n`n$names`n`n$(L 'permanent')"
    if ([System.Windows.Forms.MessageBox]::Show($msg,(L 'confirmTitle'),'YesNo','Question') -ne 'Yes') { return }
    $before = Get-FreeGB
    $btnAnalyze.Enabled=$false; $btnClean.Enabled=$false
    $progress.Value=0; $progress.Maximum=$sel.Count; $i=0
    foreach ($key in $sel) {
        $i++; $lblStatus.Text="$(L 'cleaning'): $($Categories[$key].Name[$script:Lang])..."
        [System.Windows.Forms.Application]::DoEvents()
        Invoke-Clean $key $Categories[$key]
        $sizeLabels[$key].Text="0 B"; $progress.Value=$i
        [System.Windows.Forms.Application]::DoEvents()
    }
    $after = Get-FreeGB; $freed=[math]::Round($after-$before,2)
    Update-Header; $lblStatus.Text = L 'doneClean'
    $btnAnalyze.Enabled=$true; $btnClean.Enabled=$true
    $m = "$(L 'cleanOk')`n`n$(L 'before'): $before GB`n$(L 'after'): $after GB`n$(L 'freed'): $freed GB"
    [System.Windows.Forms.MessageBox]::Show($m,(L 'resultTitle'),'OK','Information')|Out-Null
})

$btnRam.Add_Click({
    $btnRam.Enabled=$false
    $b = Get-RamInfo; $lblStatus.Text = L 'freeingRam'
    [System.Windows.Forms.Application]::DoEvents()
    Clear-RAM; Start-Sleep -Milliseconds 600
    $a = Get-RamInfo; Update-Header; $lblStatus.Text = L 'ramDone'
    $btnRam.Enabled=$true
    $freed=[math]::Round($a.FreeGB-$b.FreeGB,2)
    $m = "$(L 'ramDone')`n`n$(L 'before'): $($b.FreeGB) GB ($($b.UsedPct)% $(L 'used'))`n$(L 'after'): $($a.FreeGB) GB ($($a.UsedPct)% $(L 'used'))`n$(L 'freed'): $freed GB"
    [System.Windows.Forms.MessageBox]::Show($m,(L 'ramTitle'),'OK','Information')|Out-Null
})

$btnClose.Add_Click({ $form.Close() })

$ramTimer = New-Object System.Windows.Forms.Timer
$ramTimer.Interval = 600000
$ramTimer.Add_Tick({ Clear-RAM; Update-Header; $lblStatus.Text = "$(L 'ramDone') @ $(Get-Date -Format 'HH:mm')" })
$chkAuto.Add_CheckedChanged({
    if ($chkAuto.Checked) { $ramTimer.Start(); $lblStatus.Text = L 'autoOn' }
    else { $ramTimer.Stop(); $lblStatus.Text = L 'autoOff' }
})

$lnkUpdate.Add_LinkClicked({ Check-Update })

# ============================================================================
$Apply = { Apply-Language }
& $Apply
$form.Add_Shown({ Apply-Language; $btnAnalyze.PerformClick() })
[void]$form.ShowDialog()
