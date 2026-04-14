param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\Assets\Maps\Wiki')
)

$ErrorActionPreference = 'Stop'

$entries = @(
    @{ RegionKey = 'BlackrockRegion'; PageTitle = 'Map:Blackrock'; OutputName = 'BlackrockRegion.jpg' },
    @{ RegionKey = 'KeepersPass'; PageTitle = 'Map:Keeper_pass'; OutputName = 'KeepersPass.jpg' },
    @{ RegionKey = 'WindingRiver'; PageTitle = 'Map:Winding_River'; OutputName = 'WindingRiver.png' },
    @{ RegionKey = 'FarRangeBranchLine'; PageTitle = 'Map:Far_Range_Branch_Line'; OutputName = 'FarRangeBranchLine.jpg' },
    @{ RegionKey = 'AirfieldRegion'; PageTitle = 'Map:Forsaken_Airfield'; OutputName = 'AirfieldRegion.png' },
    @{ RegionKey = 'TransferPassRegion'; PageTitle = 'Map:Transfer_Pass'; OutputName = 'TransferPassRegion.jpg' },
    @{ RegionKey = 'ZoneOfContaminationRegion'; PageTitle = 'Map:Zone_of_Contamination'; OutputName = 'ZoneOfContaminationRegion.jpg' },
    @{ RegionKey = 'SunderedPassRegion'; PageTitle = 'Map:Sundered_Pass_map'; OutputName = 'SunderedPassRegion.jpg' }
)

function Get-MapJson([string]$pageTitle) {
    $pageUrl = 'https://thelongdark.fandom.com/api.php?action=query&titles=' + [uri]::EscapeDataString($pageTitle) + '&prop=revisions&rvprop=content&format=json'
    $pageJson = Invoke-WebRequest -UseBasicParsing $pageUrl | Select-Object -ExpandProperty Content | ConvertFrom-Json
    $page = $pageJson.query.pages.PSObject.Properties.Value | Select-Object -First 1
    if ($page.pageid -eq -1 -or -not $page.revisions) {
        throw "Map page not found: $pageTitle"
    }

    return ($page.revisions[0].'*' | ConvertFrom-Json)
}

function Get-ImageUrl([string]$imageName) {
    $fileUrl = 'https://thelongdark.fandom.com/api.php?action=query&titles=File:' + [uri]::EscapeDataString($imageName) + '&prop=imageinfo&iiprop=url&format=json'
    $fileJson = Invoke-WebRequest -UseBasicParsing $fileUrl | Select-Object -ExpandProperty Content | ConvertFrom-Json
    $page = $fileJson.query.pages.PSObject.Properties.Value | Select-Object -First 1
    $imageInfo = $page.imageinfo | Select-Object -First 1
    if (-not $imageInfo.url) {
        throw "Image URL not found for $imageName"
    }

    return $imageInfo.url
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$manifest = @()

foreach ($entry in $entries) {
    $mapJson = Get-MapJson $entry.PageTitle
    $imageUrl = Get-ImageUrl $mapJson.mapImage
    $targetPath = Join-Path $OutputRoot $entry.OutputName

    Invoke-WebRequest -UseBasicParsing $imageUrl -OutFile $targetPath

    $manifest += [PSCustomObject]@{
        regionKey   = $entry.RegionKey
        pageTitle   = $entry.PageTitle
        mapImage    = $mapJson.mapImage
        outputName  = $entry.OutputName
        imageUrl    = $imageUrl
        mapBounds   = $mapJson.mapBounds
        description = $mapJson.description
    }
}

$manifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $OutputRoot 'wiki-map-manifest.json')
@"
Bundled wiki map images were fetched from The Long Dark Wiki interactive maps.
Source wiki: https://thelongdark.fandom.com/wiki/Special:AllMaps
Fandom states community content is available under CC-BY-SA unless otherwise noted.
"@ | Set-Content (Join-Path $OutputRoot 'ATTRIBUTION.txt')
