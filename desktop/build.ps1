$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $workspacePath 'dist\windows'
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkPath 'csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw '.NET Framework 4.x (64 bit) is required.' }
$compilerArgs = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/codepage:65001',
    "/out:$outputPath\BeterEnter.exe", "/win32manifest:$PSScriptRoot\app.manifest", "/win32icon:$workspacePath\assets\logo.ico",
    "/resource:$workspacePath\assets\logo.ico,BeterEnter.ActiveIcon", "/resource:$workspacePath\assets\logo-paused.ico,BeterEnter.PausedIcon",
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Runtime.Serialization.dll',
    "/reference:$frameworkPath\WPF\UIAutomationClient.dll", "/reference:$frameworkPath\WPF\UIAutomationTypes.dll",
    "/reference:$frameworkPath\WPF\WindowsBase.dll",
    "$PSScriptRoot\KeyPolicy.cs", "$PSScriptRoot\Native.cs", "$PSScriptRoot\Program.cs", "$PSScriptRoot\TrayIcons.cs", "$PSScriptRoot\AssemblyInfo.cs")
& $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built: $outputPath\BeterEnter.exe"
