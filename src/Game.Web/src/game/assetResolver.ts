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
