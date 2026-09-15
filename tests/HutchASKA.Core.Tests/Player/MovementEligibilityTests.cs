using HutchASKA.Core.Player;

namespace HutchASKA.Core.Tests.Player;

public sealed class MovementEligibilityTests
{
    [Fact]
    public void NormalPlayerDrivePermissionAllowsGroundedWalking()
    {
        // Actual 0.1.4 retest: local commands, grounded input, hasExternalControl=true.
        Assert.Equal(MovementBlockReason.None,
            MovementEligibility.GetBlockReason(true, false, false, false, false, false, false, true, 1));
    }

    [Fact]
    public void ScriptedTargetMatchingStillBlocksBoostWithNonzeroInput()
    {
        // Native matching clears hasExternalControl; cached input can remain nonzero.
        Assert.Equal(MovementBlockReason.PlayerInputDisabled,
            MovementEligibility.GetBlockReason(true, false, false, false, false, false, false, false, 1));
    }

    [Theory]
    [InlineData(false, false, false, false, false, false, false, MovementBlockReason.Airborne)]
    [InlineData(true, true, false, false, false, false, false, MovementBlockReason.Raven)]
    [InlineData(true, false, true, false, false, false, false, MovementBlockReason.Swimming)]
    [InlineData(true, false, false, true, false, false, false, MovementBlockReason.Climbing)]
    [InlineData(true, false, false, false, true, false, false, MovementBlockReason.Sliding)]
    [InlineData(true, false, false, false, false, true, false, MovementBlockReason.Carting)]
    [InlineData(true, false, false, false, false, false, true, MovementBlockReason.Rowing)]
    public void PlayerPermissionDoesNotBypassSpecialMovementExclusions(bool grounded, bool raven, bool swimming,
        bool climbing, bool sliding, bool carting, bool rowing, MovementBlockReason expected) =>
        Assert.Equal(expected, MovementEligibility.GetBlockReason(grounded, raven, swimming, climbing, sliding, carting, rowing, true, 1));

    [Theory]
    [InlineData(0)]
    [InlineData(float.NaN)]
    public void IdleOrInvalidInputCannotBoostActionMotion(float squaredInput) =>
        Assert.Equal(MovementBlockReason.Idle,
            MovementEligibility.GetBlockReason(true, false, false, false, false, false, false, true, squaredInput));
}
