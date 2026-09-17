$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Checked {
    param([string]$Executable, [string[]]$Arguments)
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Executable failed with exit code $LASTEXITCODE"
    }
}

function Assert-Contained {
    param([string]$Candidate, [string]$Parent)
    $fullCandidate = [IO.Path]::GetFullPath($Candidate)
    $fullParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    if (-not $fullCandidate.StartsWith($fullParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Generated path escapes its intended parent: $fullCandidate"
    }
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$distRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'dist'))
Assert-Contained -Candidate $distRoot -Parent $projectRoot
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null
if ((Get-Item -LiteralPath $distRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) {
    throw 'Refuse to stage through a redirected dist directory.'
}

$stage = Join-Path $distRoot ('.stage-' + [guid]::NewGuid().ToString('N'))
$final = Join-Path $distRoot 'RhinoIfcViewer'
Assert-Contained -Candidate $stage -Parent $distRoot
Assert-Contained -Candidate $final -Parent $distRoot

Push-Location $projectRoot
try {
    foreach ($required in @(
        'src/RhinoIfcViewer/packages.lock.json',
        'tests/RhinoIfcViewer.Tests/packages.lock.json',
        'viewer/package-lock.json',
        'README.md',
        'THIRD-PARTY-NOTICES.txt',
        'docs/DEPENDENCIES.md',
        'docs/VERIFICATION.md',
        'samples/assessment.3dm',
        'samples/assessment-mm.3dm',
        'samples/assessment-m.ifc',
        'samples/assessment-mm.ifc'
    )) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Required input missing: $required"
        }
    }
    if (-not (Test-Path -LiteralPath 'licenses' -PathType Container)) {
        throw 'Required licenses directory missing.'
    }
    if (@(Get-ChildItem -LiteralPath 'licenses' -Recurse -File).Count -eq 0) {
        throw 'The licenses directory is empty.'
    }

    Push-Location (Join-Path $projectRoot 'viewer')
    try {
        Invoke-Checked -Executable 'npm.cmd' -Arguments @('ci')
        Invoke-Checked -Executable 'npm.cmd' -Arguments @('test')
        Invoke-Checked -Executable 'npm.cmd' -Arguments @('run', 'build')
    }
    finally { Pop-Location }

    Invoke-Checked -Executable 'dotnet' -Arguments @(
        'restore', 'src/RhinoIfcViewer/RhinoIfcViewer.csproj', '--locked-mode')
    Invoke-Checked -Executable 'dotnet' -Arguments @(
        'restore', 'tests/RhinoIfcViewer.Tests/RhinoIfcViewer.Tests.csproj', '--locked-mode')
    Invoke-Checked -Executable 'dotnet' -Arguments @(
        'build', 'src/RhinoIfcViewer/RhinoIfcViewer.csproj', '-c', 'Release', '--no-restore', '-warnaserror')
    Invoke-Checked -Executable 'dotnet' -Arguments @(
        'test', 'tests/RhinoIfcViewer.Tests/RhinoIfcViewer.Tests.csproj', '-c', 'Release', '--no-restore')

    $nativeOutput = Join-Path $projectRoot 'src/RhinoIfcViewer/bin/Release/net8.0-windows'
    $webOutput = Join-Path $projectRoot 'viewer/dist'
    New-Item -ItemType Directory -Path $stage | Out-Null

    # Copy every native output item except the separately staged viewer directory.
    foreach ($item in Get-ChildItem -LiteralPath $nativeOutput -Force) {
        if ($item.Name -ne 'viewer') {
            Copy-Item -LiteralPath $item.FullName -Destination $stage -Recurse -Force
        }
    }

    $viewerStage = Join-Path $stage 'viewer'
    New-Item -ItemType Directory -Path $viewerStage | Out-Null
    foreach ($item in Get-ChildItem -LiteralPath $webOutput -Force) {
        Copy-Item -LiteralPath $item.FullName -Destination $viewerStage -Recurse -Force
    }

    foreach ($name in @('README.md', 'THIRD-PARTY-NOTICES.txt')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $stage
    }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses') -Destination $stage -Recurse
    New-Item -ItemType Directory -Path (Join-Path $stage 'docs') | Out-Null
    foreach ($name in @('DEPENDENCIES.md', 'VERIFICATION.md')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot ('docs/' + $name)) -Destination (Join-Path $stage 'docs')
    }
    New-Item -ItemType Directory -Path (Join-Path $stage 'samples') | Out-Null
    foreach ($name in @('assessment.3dm', 'assessment-mm.3dm', 'assessment-m.ifc', 'assessment-mm.ifc')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot ('samples/' + $name)) -Destination (Join-Path $stage 'samples')
    }

    foreach ($required in @(
        'RhinoIfcViewer.rhp',
        'WebView2Loader.dll',
        'Microsoft.Web.WebView2.Core.dll',
        'Microsoft.Web.WebView2.WinForms.dll',
        'viewer/index.html',
        'viewer/vendor/fragments/worker.mjs',
        'viewer/vendor/web-ifc/web-ifc.wasm'
    )) {
        if (-not (Test-Path -LiteralPath (Join-Path $stage $required) -PathType Leaf)) {
            throw "Required distribution file missing: $required"
        }
    }
    if (@(Get-ChildItem -LiteralPath $stage -Filter 'RhinoCommon.dll' -Recurse -File).Count -gt 0) {
        throw 'Do not distribute RhinoCommon.dll; Rhino provides it.'
    }

    $manifest = foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File | Sort-Object FullName) {
        $relative = $file.FullName.Substring($stage.Length + 1).Replace('\', '/')
        (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash + '  ' + $relative
    }
    $manifest | Set-Content -LiteralPath (Join-Path $stage 'manifest.sha256') -Encoding UTF8

    if (Test-Path -LiteralPath $final) {
        if ((Get-Item -LiteralPath $final).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'Refuse to move a redirected release directory.'
        }
        $previous = Join-Path $distRoot ('RhinoIfcViewer-previous-' + [guid]::NewGuid().ToString('N'))
        Assert-Contained -Candidate $previous -Parent $distRoot
        Move-Item -LiteralPath $final -Destination $previous
    }
    Move-Item -LiteralPath $stage -Destination $final
    Write-Output "Staged complete distribution: $final"
}
finally { Pop-Location }
