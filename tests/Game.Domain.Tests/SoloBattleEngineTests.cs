using Game.Domain;
using Xunit;

namespace Game.Domain.Tests;

public sealed class SoloBattleEngineTests
{
    private readonly SoloBattleEngine _engine = new();
    private readonly Guid _playerId = Guid.NewGuid();

    [Fact]
    public void Start_CreatesDeterministicInitialState()
    {
        var battle = _engine.Start(_playerId);

        Assert.Equal(_playerId, battle.PlayerId);
        Assert.Equal("Active", battle.Status);
        Assert.Equal("None", battle.Result);
        Assert.Equal(SoloBattleRules.InitialTowerHp, battle.PlayerTowerHp);
        Assert.Equal(SoloBattleRules.InitialTowerHp, battle.CpuTowerHp);
        Assert.Equal(SoloBattleRules.InitialElixir, battle.Elixir);
        Assert.Contains(battle.StarterDeck, card => card.CardId == "training-knight");
    }

    [Fact]
    public void Deploy_WithValidCard_ConsumesElixirAndAddsUnit()
    {
        var battle = _engine.Start(_playerId);

        var result = _engine.Deploy(battle, new DeployBattleCommand("training-knight", "center", 0.5, 0.72));

        Assert.True(result.Accepted);
        Assert.Null(result.ErrorCode);
        Assert.Equal(2, result.Snapshot.Elixir);
        Assert.Single(result.Snapshot.Units);
        Assert.Equal("Training Knight", result.Snapshot.Units[0].Name);
    }

    [Theory]
    [InlineData("missing-card", "center", 0.5, 0.72, "InvalidCard")]
    [InlineData("training-knight", "river", 0.5, 0.72, "InvalidLane")]
    [InlineData("training-knight", "center", 0.5, 0.30, "InvalidPlacement")]
    public void Deploy_WithInvalidCommand_IsRejected(
        string cardId,
        string lane,
        double x,
        double y,
        string expectedCode)
    {
        var battle = _engine.Start(_playerId);

        var result = _engine.Deploy(battle, new DeployBattleCommand(cardId, lane, x, y));

        Assert.False(result.Accepted);
        Assert.Equal(expectedCode, result.ErrorCode);
        Assert.Empty(result.Snapshot.Units);
    }

    [Fact]
    public void Deploy_WithInsufficientElixir_IsRejected()
    {
        var battle = _engine.Start(_playerId) with
        {
            Elixir = 1
        };

        var result = _engine.Deploy(battle, new DeployBattleCommand("training-knight", "center", 0.5, 0.72));

        Assert.False(result.Accepted);
        Assert.Equal("InsufficientElixir", result.ErrorCode);
    }

    [Fact]
    public void Advance_MovesUnitsDamagesTowerAndRegeneratesElixir()
    {
        var battle = _engine.Start(_playerId);
        var deployed = _engine.Deploy(battle, new DeployBattleCommand("training-knight", "center", 0.5, 0.72)).Snapshot;

        var advanced = _engine.Advance(deployed, 10);

        Assert.True(advanced.Tick >= 10);
        Assert.True(advanced.CpuTowerHp < SoloBattleRules.InitialTowerHp);
        Assert.True(advanced.Elixir > deployed.Elixir);
        Assert.All(advanced.Units, unit => Assert.True(unit.Position <= 720));
    }

    [Fact]
    public void Advance_WhenTowerDestroyed_EndsWithWinAndRejectsDeploy()
    {
        var battle = _engine.Start(_playerId) with
        {
            CpuTowerHp = 100
        };
        var deployed = _engine.Deploy(battle, new DeployBattleCommand("training-bolt", "center", 0.5, 0.72)).Snapshot;

        var ended = _engine.Advance(deployed, 1);
        var rejected = _engine.Deploy(ended, new DeployBattleCommand("training-archer", "left", 0.3, 0.72));

        Assert.Equal("Ended", ended.Status);
        Assert.Equal("Win", ended.Result);
        Assert.False(rejected.Accepted);
        Assert.Equal("BattleAlreadyEnded", rejected.ErrorCode);
    }

    [Fact]
    public void Advance_WhenTimerExpires_EndsWithTimeout()
    {
        var battle = _engine.Start(_playerId);

        var ended = _engine.Advance(battle, SoloBattleRules.DurationTicks);

        Assert.Equal("Ended", ended.Status);
        Assert.Equal("Timeout", ended.Result);
    }
}
