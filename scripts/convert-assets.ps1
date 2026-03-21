# convert-assets.ps1
# Converts SVG logo files into multi-resolution .ico files and a banner.png
# Requires: ImageMagick (magick command)

$ErrorActionPreference = 'Stop'

$assetsDir = Join-Path $PSScriptRoot '..' 'assets'
$assetsDir = Resolve-Path $assetsDir

$sizes = @(16, 32, 48, 256)
$svgs = @(
    'logo-connected-darktheme',
    'logo-connected-lighttheme',
    'logo-disconnected-darktheme',
    'logo-disconnected-lighttheme'
)

foreach ($name in $svgs) {
    $svgPath = Join-Path $assetsDir "$name.svg"
    $icoName = $name -replace '^logo-', 'app-'
    $icoPath = Join-Path $assetsDir "$icoName.ico"

    if (-not (Test-Path $svgPath)) {
        Write-Error "SVG not found: $svgPath"
        continue
    }

    $pngFiles = @()
    foreach ($size in $sizes) {
        $png = Join-Path $assetsDir "${icoName}-${size}.png"
        & magick -background none -density 384 $svgPath -resize "${size}x${size}" $png
        $pngFiles += $png
    }

    & magick @pngFiles $icoPath
    Write-Host "Created: $icoPath"

    # Clean up intermediate PNGs
    foreach ($png in $pngFiles) {
        Remove-Item $png
    }
}

# Generate banner.png from connected-darktheme
$bannerSvg = Join-Path $assetsDir 'logo-connected-darktheme.svg'
$bannerPng = Join-Path $assetsDir 'banner.png'
& magick -background none -density 384 $bannerSvg -resize 256x256 $bannerPng
Write-Host "Created: $bannerPng"

Write-Host 'Asset conversion complete.'
