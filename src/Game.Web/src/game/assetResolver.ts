export const LOCAL_IMPORTED_ASSET_ROOT = "/assets/imported";

export const importedAssetManifest = {
  arena: "scenes/arena.png",
  topTower: "ui/top-tower.png",
  bottomTower: "ui/bottom-tower.png",
  deploySfx: "sfx/deploy.mp3"
} as const;

export type ImportedAssetKey = keyof typeof importedAssetManifest;

export function resolveImportedAsset(key: ImportedAssetKey) {
  return `${LOCAL_IMPORTED_ASSET_ROOT}/${importedAssetManifest[key]}`;
}

export async function detectImportedBattleAssets() {
  const requiredAssets: ImportedAssetKey[] = ["arena", "topTower", "bottomTower"];
  const results = await Promise.all(requiredAssets.map(assetExists));
  return results.every(Boolean);
}

async function assetExists(key: ImportedAssetKey) {
  try {
    const response = await fetch(resolveImportedAsset(key), {
      method: "HEAD",
      cache: "no-store"
    });
    return response.ok && response.headers.get("content-type")?.startsWith("image/") === true;
  } catch {
    return false;
  }
}
