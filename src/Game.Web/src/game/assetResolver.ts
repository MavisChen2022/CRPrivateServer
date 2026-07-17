export const LOCAL_IMPORTED_ASSET_ROOT = "/assets/imported";

export const importedAssetManifest = {
  arena: "scenes/arena.png",
  topTower: "ui/top-tower.png",
  bottomTower: "ui/bottom-tower.png",
  deploySfx: "sfx/deploy.ogg",
  spellSfx: "sfx/spell.ogg",
  battleLoopSfx: "sfx/battle-loop.ogg",
  trainingKnightCard: "cards/training-knight.png",
  trainingArcherCard: "cards/training-archer.png",
  trainingGuardCard: "cards/training-guard.png",
  trainingBoltCard: "cards/training-bolt.png",
  trainingKnightUnit: "units/training-knight.png",
  trainingArcherUnit: "units/training-archer.png",
  trainingGuardUnit: "units/training-guard.png",
  trainingBoltUnit: "units/training-bolt.png"
} as const;

export type ImportedAssetKey = keyof typeof importedAssetManifest;

export function resolveImportedAsset(key: ImportedAssetKey) {
  return `${LOCAL_IMPORTED_ASSET_ROOT}/${importedAssetManifest[key]}`;
}

export async function detectImportedBattleAssets() {
  const requiredAssets: ImportedAssetKey[] = [
    "arena",
    "topTower",
    "bottomTower",
    "trainingKnightCard",
    "trainingArcherCard",
    "trainingKnightUnit",
    "deploySfx"
  ];
  const results = await Promise.all(requiredAssets.map(assetExists));
  return results.every(Boolean);
}

async function assetExists(key: ImportedAssetKey) {
  try {
    const response = await fetch(resolveImportedAsset(key), {
      method: "HEAD",
      cache: "no-store"
    });
    const contentType = response.headers.get("content-type") ?? "";
    return response.ok && (contentType.startsWith("image/") || contentType.startsWith("audio/"));
  } catch {
    return false;
  }
}

export function cardAssetKey(cardId: string): ImportedAssetKey | undefined {
  return ({
    "training-knight": "trainingKnightCard",
    "training-archer": "trainingArcherCard",
    "training-guard": "trainingGuardCard",
    "training-bolt": "trainingBoltCard"
  } satisfies Record<string, ImportedAssetKey>)[cardId];
}

export function unitAssetKey(cardId: string): ImportedAssetKey | undefined {
  return ({
    "training-knight": "trainingKnightUnit",
    "training-archer": "trainingArcherUnit",
    "training-guard": "trainingGuardUnit",
    "training-bolt": "trainingBoltUnit"
  } satisfies Record<string, ImportedAssetKey>)[cardId];
}

export function resolveOptionalCardAsset(cardId: string) {
  const key = cardAssetKey(cardId);
  return key ? resolveImportedAsset(key) : undefined;
}

export function resolveOptionalUnitAsset(cardId: string) {
  const key = unitAssetKey(cardId);
  return key ? resolveImportedAsset(key) : undefined;
}

export function deploySfxForCard(cardId: string) {
  return resolveImportedAsset(cardId === "training-bolt" ? "spellSfx" : "deploySfx");
}
