param(
    [string] $AssetRoot = "C:\Users\User\Desktop\CR\Clash-Royale-assets",
    [string] $SfxRoot = "C:\Users\User\Desktop\CR\Clash-Royale-SFX-master"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$targetRoot = Join-Path $repoRoot "src\Game.Web\public\assets\imported"

$copies = @(
    @{
        Source = Join-Path $AssetRoot "sc3d\arena_params_s2.png"
        Target = "scenes\arena.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\knight.png"
        Target = "cards\training-knight.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\archers.png"
        Target = "cards\training-archer.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\guards_dl.png"
        Target = "cards\training-guard.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\zap_dl.png"
        Target = "cards\training-bolt.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\knight.png"
        Target = "units\training-knight.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\archers.png"
        Target = "units\training-archer.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\guards_dl.png"
        Target = "units\training-guard.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\zap_dl.png"
        Target = "units\training-bolt.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\princess_dl.png"
        Target = "ui\top-tower.png"
    },
    @{
        Source = Join-Path $AssetRoot "image\chr\royal_recruits_dl.png"
        Target = "ui\bottom-tower.png"
    },
    @{
        Source = Join-Path $SfxRoot "Cards\Archers\clash_archer_deploy_01.ogg"
        Target = "sfx\deploy.ogg"
    },
    @{
        Source = Join-Path $SfxRoot "Cards\0_Special\spellcast01.ogg"
        Target = "sfx\spell.ogg"
    },
    @{
        Source = Join-Path $SfxRoot "Arenas\General\2min_loop_battle_01.ogg"
        Target = "sfx\battle-loop.ogg"
    }
)

foreach ($copy in $copies) {
    if (-not (Test-Path -LiteralPath $copy.Source)) {
        Write-Warning "Missing local asset, fallback will be used: $($copy.Source)"
        continue
    }

    $destination = Join-Path $targetRoot $copy.Target
    $destinationDirectory = Split-Path -Parent $destination
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Copy-Item -LiteralPath $copy.Source -Destination $destination -Force
}

Write-Host "Imported available local Clash Royale assets into $targetRoot"
