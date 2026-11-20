param([string]$PlanPath = 'Temp/ProjectOrganizationAudit/moves.json', [string]$WorkspaceRoot = '')
$ErrorActionPreference = 'Stop'
$workspace = if ($WorkspaceRoot) { [System.IO.Path]::GetFullPath($WorkspaceRoot).TrimEnd('\') } else { [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\') }
function Resolve-WorkspacePath([string]$relative) {
    $absolute = [System.IO.Path]::GetFullPath((Join-Path $workspace $relative))
    if (-not $absolute.StartsWith($workspace + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes workspace: $relative"
    }
    return $absolute
}
$plan = Get-Content -LiteralPath (Resolve-WorkspacePath $PlanPath) -Raw -Encoding UTF8 | ConvertFrom-Json
$moves = @{}
foreach ($entry in $plan.files.PSObject.Properties) {
    $source = Resolve-WorkspacePath $entry.Name
    $destination = Resolve-WorkspacePath $entry.Value
    if (-not $source.StartsWith($workspace + '\Assets\', [System.StringComparison]::OrdinalIgnoreCase) -or
        -not $destination.StartsWith($workspace + '\Assets\', [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Only project-owned Assets may be moved.' }
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing source: $source" }
    if (Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination" }
    $moves[$source] = $destination
}
function Ensure-AssetFolder([string]$path) {
    if (Test-Path -LiteralPath $path -PathType Container) { return }
    $parent = Split-Path -Parent $path
    Ensure-AssetFolder $parent
    New-Item -ItemType Directory -Path $path | Out-Null
    $meta = $path + '.meta'
    if (-not $moves.ContainsValue($meta) -and -not (Test-Path -LiteralPath $meta)) {
        $guid = [System.Guid]::NewGuid().ToString('N')
        [System.IO.File]::WriteAllText($meta, "fileFormatVersion: 2`nguid: $guid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
    }
}
foreach ($destination in $moves.Values) { Ensure-AssetFolder (Split-Path -Parent $destination) }
# Preserve original directory GUIDs before relocating their contents.
foreach ($entry in $plan.folders.PSObject.Properties) {
    $sourceMeta = (Resolve-WorkspacePath $entry.Name) + '.meta'
    if ($moves.ContainsKey($sourceMeta)) { Move-Item -LiteralPath $sourceMeta -Destination $moves[$sourceMeta] }
}
$count = 0
foreach ($source in @($moves.Keys | Where-Object { -not $_.EndsWith('.meta') } | Sort-Object)) {
    Move-Item -LiteralPath $source -Destination $moves[$source]
    $meta = $source + '.meta'
    if ($moves.ContainsKey($meta)) { Move-Item -LiteralPath $meta -Destination $moves[$meta] }
    $count++
}
# Remove only verified empty source directories; never recurse or delete asset contents.
foreach ($entry in @($plan.folders.PSObject.Properties | Sort-Object { $_.Name.Length } -Descending)) {
    $source = Resolve-WorkspacePath $entry.Name
    if ((Test-Path -LiteralPath $source -PathType Container) -and @(Get-ChildItem -LiteralPath $source -Force).Count -eq 0) {
        Remove-Item -LiteralPath $source
    }
}
Write-Output "Moved $count assets with original .meta files; all paths verified inside workspace."
