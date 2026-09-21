namespace HutchASKA.Core.Tribe;

public static class TribeMovementEligibility
{
    public static bool CanScale(bool hasControlledVillager, bool isOwnedVillager, bool isSwimming,
        bool isOnLadder, bool isInVehicle, bool isTraversingLink) =>
        hasControlledVillager && isOwnedVillager && !isSwimming && !isOnLadder && !isInVehicle && !isTraversingLink;
}
