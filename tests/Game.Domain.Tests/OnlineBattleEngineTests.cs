using Game.Domain;
using Xunit;

namespace Game.Domain.Tests;

public sealed class OnlineBattleEngineTests
{
    private readonly OnlineBattleEngine _engine = new();
    private readonly Guid _blue = Guid.NewGuid();
    private readonly Guid _red = Guid.NewGuid();

    [Fact]
    public void Start_CreatesTwoPlayerInitialState()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer");

        Assert.Equal("Active", battle.Status);
        Assert.Equal("Blue", battle.PlayerOne.Side);
        Assert.Equal("Red", battle.PlayerTwo.Side);
        Assert.Equal(1000, battle.PlayerOne.TowerHp);
        Assert.Equal(1000, battle.PlayerTwo.TowerHp);
        Assert.Contains(battle.StarterDeck, card => card.CardId == "training-knight");
    }

    [Fact]
    public void Deploy_WithValidBlueCommand_ConsumesBlueElixirAndAddsUnit()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer");

        var result = _engine.Deploy(battle, _blue, new DeployBattleCommand("training-knight", "center", 0.5, 0.72));

        Assert.True(result.Accepted);
        Assert.Equal(2, result.Snapshot.PlayerOne.Elixir);
        Assert.Equal(5, result.Snapshot.PlayerTwo.Elixir);
        var unit = Assert.Single(result.Snapshot.Units);
        Assert.Equal(_blue, unit.OwnerPlayerId);
        Assert.Equal("Blue", unit.Side);
    }

    [Fact]
    public void Deploy_WithValidRedCommand_ConsumesRedElixirAndAddsUnit()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer");

        var result = _engine.Deploy(battle, _red, new DeployBattleCommand("training-archer", "left", 0.4, 0.28));

        Assert.True(result.Accepted);
        Assert.Equal(5, result.Snapshot.PlayerOne.Elixir);
        Assert.Equal(3, result.Snapshot.PlayerTwo.Elixir);
        Assert.Equal("Red", Assert.Single(result.Snapshot.Units).Side);
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
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer");

        var result = _engine.Deploy(battle, _blue, new DeployBattleCommand(cardId, lane, x, y));

        Assert.False(result.Accepted);
        Assert.Equal(expectedCode, result.ErrorCode);
        Assert.Empty(result.Snapshot.Units);
    }

    [Fact]
    public void Deploy_WithNonParticipant_IsRejected()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer");

        var result = _engine.Deploy(battle, Guid.NewGuid(), new DeployBattleCommand("training-knight", "center", 0.5, 0.72));

        Assert.False(result.Accepted);
        Assert.Equal("OnlineBattleForbidden", result.ErrorCode);
    }

    [Fact]
    public void Advance_MovesUnitsAndDamagesOpponentTower()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer");
        var deployed = _engine.Deploy(battle, _blue, new DeployBattleCommand("training-knight", "center", 0.5, 0.72)).Snapshot;

        var advanced = _engine.Advance(deployed, 10);

        Assert.True(advanced.Tick >= 10);
        Assert.True(advanced.PlayerTwo.TowerHp < battle.PlayerTwo.TowerHp);
        Assert.True(advanced.PlayerOne.Elixir > deployed.PlayerOne.Elixir);
    }

    [Fact]
    public void Advance_WhenTowerDestroyed_EndsWithPlayerOneWin()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer") with
        {
            PlayerTwo = new OnlineBattleParticipantSnapshot(_red, "RedPlayer", "Red", 100, 5, 10)
        };
        var deployed = _engine.Deploy(battle, _blue, new DeployBattleCommand("training-bolt", "center", 0.5, 0.72)).Snapshot;

        Assert.Equal("Ended", deployed.Status);
        Assert.Equal("PlayerOneWin", deployed.Result);
    }

    [Fact]
    public void Advance_WhenTimerExpires_UsesTowerHpResult()
    {
        var battle = _engine.Start(_blue, "BluePlayer", _red, "RedPlayer") with
        {
            PlayerTwo = new OnlineBattleParticipantSnapshot(_red, "RedPlayer", "Red", 900, 5, 10)
        };

        var ended = _engine.Advance(battle, SoloBattleRules.DurationTicks);

        Assert.Equal("Ended", ended.Status);
        Assert.Equal("PlayerOneWin", ended.Result);
    }
}
