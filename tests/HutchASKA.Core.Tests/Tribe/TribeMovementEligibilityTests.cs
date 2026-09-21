using HutchASKA.Core.Tribe;

namespace HutchASKA.Core.Tests.Tribe;

public sealed class TribeMovementEligibilityTests
{
    [Fact]
    public void OwnedVillagerOnOrdinaryNavmeshMovementCanBeScaled() =>
        Assert.True(TribeMovementEligibility.CanScale(
            hasControlledVillager: true,
            isOwnedVillager: true,
            isSwimming: false,
            isOnLadder: false,
            isInVehicle: false,
            isTraversingLink: false));

    [Theory]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(true, true, false, true, false, false)]
    [InlineData(true, true, false, false, true, false)]
    [InlineData(true, true, false, false, false, true)]
    public void NonOwnedOrSpecialMovementStaysNative(bool hasControlledVillager, bool isOwnedVillager,
        bool isSwimming, bool isOnLadder, bool isInVehicle, bool isTraversingLink) =>
        Assert.False(TribeMovementEligibility.CanScale(hasControlledVillager, isOwnedVillager,
            isSwimming, isOnLadder, isInVehicle, isTraversingLink));
}
