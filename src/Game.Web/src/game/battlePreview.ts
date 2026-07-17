import Phaser from "phaser";

type PreviewOptions = {
  reducedMotion: boolean;
};

export function createBattlePreview(containerId: string, options: PreviewOptions) {
  const scene = new Phaser.Scene("BattlePreview");

  scene.create = function create() {
    const width = 360;
    const height = 520;
    this.add.rectangle(width / 2, height / 2, width, height, 0x244f7a);
    this.add.rectangle(width / 2, height / 2, width - 56, height - 56, 0x6dbb6d);
    this.add.rectangle(width / 2, height / 2, 18, height - 64, 0x4e86c7);

    const topTower = this.add.rectangle(width / 2, 76, 72, 72, 0xc94848);
    const bottomTower = this.add.rectangle(width / 2, height - 76, 72, 72, 0x4f7bd9);
    this.add.text(width / 2 - 22, 66, "CPU", { color: "#fff", fontSize: "14px" });
    this.add.text(width / 2 - 28, height - 86, "YOU", { color: "#fff", fontSize: "14px" });

    if (!options.reducedMotion) {
      this.tweens.add({
        targets: [topTower, bottomTower],
        scale: 1.06,
        duration: 850,
        yoyo: true,
        repeat: -1
      });
    }
  };

  const game = new Phaser.Game({
    type: Phaser.AUTO,
    parent: containerId,
    width: 360,
    height: 520,
    backgroundColor: "#15334d",
    scene
  });

  return () => {
    game.destroy(true);
  };
}

