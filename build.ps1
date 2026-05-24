<#
.SYNOPSIS
    Builds MajaFaktura and packages it as a Windows MSI installer.

.DESCRIPTION
    1. Publishes the WPF app as a self-contained win-x64 executable via dotnet publish.
    2. Packages the output into an MSI using WiX 6 (wix.exe must be in PATH).
    Output: MajaFaktura\installer\MajaFaktura.msi

.REQUIREMENTS
    - .NET 10 SDK
    - WiX Toolset v6  (dotnet tool install --global wix)
#>
$ErrorActionPreference = "Stop"

$projectDir  = "$PSScriptRoot\MajaFaktura"
$publishDir  = "$projectDir\publish"
$installerDir = "$projectDir\installer"
$wxsFile     = "$installerDir\Package.wxs"
$msiOut      = "$installerDir\MajaFaktura.msi"

Write-Host "==> Publishing app..."
dotnet publish "$projectDir\MajaFaktura.csproj" `
    -c Release -r win-x64 --self-contained true `
    -o "$publishDir"

Write-Host "==> Building MSI..."
wix build "$wxsFile" `
    -d "ProjectDir=$projectDir\" `
    -o "$msiOut"

Write-Host "==> Done: $msiOut"
