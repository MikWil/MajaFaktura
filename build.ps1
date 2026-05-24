<#
.SYNOPSIS
    Builds MajaFaktura and packages it as a signed Windows MSI installer.

.DESCRIPTION
    1. Regenerates Assets\applogo.ico from Assets\applogo.png (multi-size: 256/48/32/16).
    2. Publishes the WPF app as a self-contained win-x64 executable via dotnet publish.
    3. Packages the output into an MSI using WiX 6 (wix.exe must be in PATH).
    4. Signs the MSI with a self-signed code-signing certificate (created on first run).
    Output: MajaFaktura\installer\MajaFaktura.msi

.REQUIREMENTS
    - .NET 10 SDK
    - WiX Toolset v6  (dotnet tool install --global wix)
#>
$ErrorActionPreference = "Stop"

$projectDir   = "$PSScriptRoot\MajaFaktura"
$publishDir   = "$projectDir\publish"
$installerDir = "$projectDir\installer"
$wxsFile      = "$installerDir\Package.wxs"
$msiOut       = "$installerDir\MajaFaktura.msi"
$icoSrc       = "$projectDir\Assets\applogo.png"
$icoOut       = "$projectDir\Assets\applogo.ico"

$signtool     = (Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Shared\NuGetPackages\microsoft.windows.sdk.buildtools" `
                    -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
                 Where-Object { $_.FullName -match "x64" } |
                 Select-Object -First 1).FullName

# ---------------------------------------------------------------------------
# 1. Regenerate ICO
# ---------------------------------------------------------------------------
Write-Host "==> Generating ICO..."
Add-Type -AssemblyName System.Drawing

function ConvertTo-IcoBytes([string]$srcPng) {
    $src   = [System.Drawing.Bitmap]::new($srcPng)
    $sizes = @(256, 48, 32, 16)
    $entries = [System.Collections.Generic.List[object]]::new()

    foreach ($sz in $sizes) {
        $bmp = [System.Drawing.Bitmap]::new($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g   = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.DrawImage($src, 0, 0, $sz, $sz)
        $g.Dispose()

        if ($sz -eq 256) {
            # 256x256 stored as PNG inside ICO (Vista+)
            $ms = [System.IO.MemoryStream]::new()
            $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $entries.Add(@{ Size = $sz; Data = $ms.ToArray() })
        } else {
            # Smaller sizes stored as BMP/DIB (32-bit BGRA, bottom-up)
            $pixels = [byte[]]::new($sz * $sz * 4)
            for ($y = 0; $y -lt $sz; $y++) {
                for ($x = 0; $x -lt $sz; $x++) {
                    $c   = $bmp.GetPixel($x, $y)
                    $row = $sz - 1 - $y          # bottom-up
                    $idx = ($row * $sz + $x) * 4
                    $pixels[$idx]     = $c.B
                    $pixels[$idx + 1] = $c.G
                    $pixels[$idx + 2] = $c.R
                    $pixels[$idx + 3] = $c.A
                }
            }
            $rowBytes = [int][Math]::Ceiling($sz / 32.0) * 4
            $andMask  = [byte[]]::new($rowBytes * $sz)   # all zeros = alpha handles transparency

            $ms = [System.IO.MemoryStream]::new()
            $bw = [System.IO.BinaryWriter]::new($ms)
            $bw.Write([uint32]40)              # BITMAPINFOHEADER biSize
            $bw.Write([int32]$sz)              # biWidth
            $bw.Write([int32]($sz * 2))        # biHeight (XOR + AND, ICO convention)
            $bw.Write([uint16]1)               # biPlanes
            $bw.Write([uint16]32)              # biBitCount
            $bw.Write([uint32]0)               # biCompression (BI_RGB)
            $bw.Write([uint32]0)               # biSizeImage
            $bw.Write([int32]0)                # biXPelsPerMeter
            $bw.Write([int32]0)                # biYPelsPerMeter
            $bw.Write([uint32]0)               # biClrUsed
            $bw.Write([uint32]0)               # biClrImportant
            $bw.Write($pixels)
            $bw.Write($andMask)
            $bw.Flush()
            $entries.Add(@{ Size = $sz; Data = $ms.ToArray() })
        }
        $bmp.Dispose()
    }
    $src.Dispose()

    # Build ICO file
    $n          = $entries.Count
    $headerSize = 6 + $n * 16
    $offsets    = [int[]]::new($n)
    $off        = $headerSize
    for ($i = 0; $i -lt $n; $i++) { $offsets[$i] = $off; $off += $entries[$i].Data.Length }

    $ico = [System.IO.MemoryStream]::new()
    $bw  = [System.IO.BinaryWriter]::new($ico)
    $bw.Write([uint16]0)    # reserved
    $bw.Write([uint16]1)    # type: ICO
    $bw.Write([uint16]$n)

    for ($i = 0; $i -lt $n; $i++) {
        $sz  = $entries[$i].Size
        if ($sz -eq 256) { $dim = [byte]0 } else { $dim = [byte]$sz }
        $bw.Write($dim)  # width  (0 = 256)
        $bw.Write($dim)  # height (0 = 256)
        $bw.Write([byte]0)     # color count
        $bw.Write([byte]0)     # reserved
        $bw.Write([uint16]1)   # planes
        $bw.Write([uint16]32)  # bit count
        $bw.Write([uint32]$entries[$i].Data.Length)
        $bw.Write([uint32]$offsets[$i])
    }
    foreach ($e in $entries) { $bw.Write($e.Data) }
    $bw.Flush()
    return $ico.ToArray()
}

$icoBytes = ConvertTo-IcoBytes $icoSrc
[System.IO.File]::WriteAllBytes($icoOut, $icoBytes)
Write-Host "  $icoOut ($($icoBytes.Length) bytes)"

# ---------------------------------------------------------------------------
# 2. Publish app
# ---------------------------------------------------------------------------
Write-Host "==> Publishing app..."
dotnet publish "$projectDir\MajaFaktura.csproj" `
    -c Release -r win-x64 --self-contained true `
    -o "$publishDir"

# ---------------------------------------------------------------------------
# 3. Build MSI
# ---------------------------------------------------------------------------
Write-Host "==> Building MSI..."
wix build "$wxsFile" `
    -d "ProjectDir=$projectDir\" `
    -o "$msiOut"

# ---------------------------------------------------------------------------
# 4. Sign MSI
# ---------------------------------------------------------------------------
Write-Host "==> Signing MSI..."
$certSubject = "MajaFaktura"
$pfxPath     = "$PSScriptRoot\MajaFaktura-codesign.pfx"
$pfxPass     = "MajaFaktura"

$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue |
        Where-Object { $_.Subject -eq "CN=$certSubject" } |
        Select-Object -First 1

if (-not $cert) {
    Write-Host "  Creating self-signed code-signing certificate..."
    $cert = New-SelfSignedCertificate `
        -Subject        "CN=$certSubject" `
        -Type           CodeSigning `
        -KeyUsage       DigitalSignature `
        -KeyAlgorithm   RSA `
        -KeyLength      2048 `
        -HashAlgorithm  SHA256 `
        -NotAfter       (Get-Date).AddYears(10) `
        -CertStoreLocation Cert:\CurrentUser\My
}

if (-not (Test-Path $pfxPath)) {
    $secPass = ConvertTo-SecureString $pfxPass -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secPass | Out-Null
    Write-Host "  Certificate exported to $pfxPath"
}

& $signtool sign /f $pfxPath /p $pfxPass /fd sha256 /tr "http://timestamp.digicert.com" /td sha256 "$msiOut"

Write-Host "==> Done: $msiOut"
