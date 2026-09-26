$ErrorActionPreference = 'Stop'
$version = (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'package.json') | ConvertFrom-Json).version
& (Join-Path $PSScriptRoot 'desktop\build.ps1')
$distPath = Join-Path $PSScriptRoot 'dist'
$chromeSource = Join-Path $PSScriptRoot 'chrome-extension'
$chromePath = Join-Path $distPath 'chrome-extension'
New-Item -ItemType Directory -Force -Path $chromePath | Out-Null
Get-ChildItem -LiteralPath $chromeSource -File -Recurse | ForEach-Object {
    $relative = $_.FullName.Substring($chromeSource.Length + 1)
    $destination = Join-Path $chromePath $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
}
$assetsPath = Join-Path $distPath 'assets'
New-Item -ItemType Directory -Force -Path $assetsPath | Out-Null
foreach ($asset in @('logo.png', 'logo.svg', 'logo-dark.svg', 'icon.svg', 'icon.png', 'FONT-NOTICE.md')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot "assets\$asset") -Destination $assetsPath -Force }
$fontNoticePath = Join-Path $assetsPath 'fonts'
New-Item -ItemType Directory -Force -Path $fontNoticePath | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\fonts\OFL.txt') -Destination $fontNoticePath -Force
# Keep release instructions separate from the contributor README and match each ZIP's layout.
$encoding = [Text.UTF8Encoding]::new($false)
$readmeAll = @(
    "# BeterEnter v$version", '',
    'Enterで改行、Ctrl+Enterで送信するためのChatGPT用ツールです。', '',
    '## Windowsアプリ', '',
    'windows\BeterEnter.exe を起動します。通知領域のアイコンを右クリックすると一時停止や終了ができます。未署名のため、Windows SmartScreenが警告を表示する場合があります。', '',
    '## Chrome拡張', '',
    '1. chrome://extensions を開き、「デベロッパー モード」を有効にします。',
    '2. 「パッケージ化されていない拡張機能を読み込む」を選び、chrome-extension フォルダーを指定します。',
    '3. ChatGPTのページを再読み込みします。対象は chatgpt.com と chat.openai.com です。', '',
    'Chrome版を削除するには chrome://extensions を開き、BeterEnterの「削除」を選びます。'
) -join "`n"
$readmeWindows = @(
    "# BeterEnter v$version — Windows版", '',
    'Enterで改行、Ctrl+Enterで送信するためのChatGPT用ツールです。', '',
    'このフォルダーにある BeterEnter.exe を起動します。通知領域のアイコンを右クリックすると一時停止や終了ができます。未署名のため、Windows SmartScreenが警告を表示する場合があります。'
) -join "`n"
$readmeChrome = @(
    "# BeterEnter v$version — Chrome版", '',
    'Enterで改行、Ctrl+Enterで送信します。対象は chatgpt.com と chat.openai.com です。', '',
    '1. chrome://extensions を開き、「デベロッパー モード」を有効にします。',
    '2. 「パッケージ化されていない拡張機能を読み込む」を選び、chrome-extension フォルダーを指定します。',
    '3. ChatGPTのページを再読み込みします。', '',
    '削除するには chrome://extensions を開き、BeterEnterの「削除」を選びます。'
) -join "`n"
$releaseReadmePath = Join-Path $distPath 'README.md'
[IO.File]::WriteAllText($releaseReadmePath, $readmeAll.Trim() + "`n", $encoding)
foreach ($document in @('LICENSE', 'CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $document) -Destination $distPath -Force }
$documents = @((Join-Path $distPath 'README.md'), (Join-Path $distPath 'LICENSE'), (Join-Path $distPath 'CHANGELOG.md'), $assetsPath)
$windowsPath = Join-Path $distPath 'windows'
$allArchive = Join-Path $distPath "BeterEnter-v$version.zip"
$windowsArchive = Join-Path $distPath "BeterEnter-v$version-windows-x64.zip"
$chromeArchive = Join-Path $distPath "BeterEnter-v$version-chrome.zip"
# Stage only the current branded executable; older releases may still exist in dist/windows.
$stagingPath = Join-Path $distPath "package-v$version"
$stagedWindowsPath = Join-Path $stagingPath 'windows'
New-Item -ItemType Directory -Force -Path $stagedWindowsPath | Out-Null
Copy-Item -LiteralPath (Join-Path $windowsPath 'BeterEnter.exe') -Destination $stagedWindowsPath -Force
$stagedWindowsStandalonePath = Join-Path $stagingPath 'windows-standalone'
New-Item -ItemType Directory -Force -Path $stagedWindowsStandalonePath | Out-Null
Copy-Item -LiteralPath (Join-Path $windowsPath 'BeterEnter.exe') -Destination $stagedWindowsStandalonePath -Force
$windowsReadmePath = Join-Path $stagedWindowsStandalonePath 'README.md'
[IO.File]::WriteAllText($windowsReadmePath, $readmeWindows.Trim() + "`n", $encoding)
$chromePackagePath = Join-Path $stagingPath 'chrome-package'
New-Item -ItemType Directory -Force -Path $chromePackagePath | Out-Null
$chromeReadmePath = Join-Path $chromePackagePath 'README.md'
[IO.File]::WriteAllText($chromeReadmePath, $readmeChrome.Trim() + "`n", $encoding)
$sharedDocuments = @((Join-Path $distPath 'LICENSE'), (Join-Path $distPath 'CHANGELOG.md'), $assetsPath)
Compress-Archive -LiteralPath (@($chromePath, $stagedWindowsPath) + $documents) -DestinationPath $allArchive -Force
Compress-Archive -LiteralPath (@((Join-Path $stagedWindowsStandalonePath 'BeterEnter.exe'), $windowsReadmePath) + $sharedDocuments) -DestinationPath $windowsArchive -Force
Compress-Archive -LiteralPath (@($chromePath, $chromeReadmePath) + $sharedDocuments) -DestinationPath $chromeArchive -Force

# Include Git configuration and omit generated files from the source archive.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceArchive = Join-Path $distPath "BeterEnter-v$version-source.zip"
$sourceStream = [IO.File]::Open($sourceArchive, [IO.FileMode]::Create)
$archive = New-Object IO.Compression.ZipArchive($sourceStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    $sourceFiles = @()
    foreach ($folder in @('assets', 'chrome-extension', 'desktop', 'scripts')) {
        $sourceFiles += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot $folder) -Recurse -Force -File | Where-Object { $_.FullName -notlike '*\desktop\build\*' }
    }
    foreach ($file in @('README.md', 'LICENSE', 'CHANGELOG.md', '.gitignore', '.gitattributes', 'package.json', 'package-lock.json', 'build-package.ps1')) {
        $sourceFiles += Get-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Force
    }
    foreach ($file in $sourceFiles | Sort-Object FullName) {
        $relative = $file.FullName.Substring($PSScriptRoot.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose(); $sourceStream.Dispose() }
$checksumLines = foreach ($artifact in @($allArchive, $windowsArchive, $chromeArchive, $sourceArchive, (Join-Path $windowsPath 'BeterEnter.exe'))) {
    $relative = $artifact.Substring($distPath.Length + 1).Replace('\', '/')
    (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $relative
}
[IO.File]::WriteAllLines((Join-Path $distPath "SHA256SUMS-v$version.txt"), [string[]]$checksumLines, [Text.Encoding]::ASCII)
Write-Output "Packages: $distPath (version $version)"
