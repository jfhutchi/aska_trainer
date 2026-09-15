namespace HutchASKA.Core.Player;

public enum MovementBlockReason
{
    None = -1,
    Airborne,
    Raven,
    Swimming,
    Climbing,
    Sliding,
    Carting,
    Rowing,
    PlayerInputDisabled,
    Idle
}

public static class MovementEligibility
{
    public static MovementBlockReason GetBlockReason(bool grounded, bool raven, bool swimming,
        bool climbing, bool sliding, bool carting, bool rowing, bool playerInputEnabled, float squaredInput) =>
        !grounded ? MovementBlockReason.Airborne : raven ? MovementBlockReason.Raven
        : swimming ? MovementBlockReason.Swimming : climbing ? MovementBlockReason.Climbing
        : sliding ? MovementBlockReason.Sliding : carting ? MovementBlockReason.Carting
        : rowing ? MovementBlockReason.Rowing : !playerInputEnabled ? MovementBlockReason.PlayerInputDisabled
        : !float.IsFinite(squaredInput) || squaredInput <= 0 ? MovementBlockReason.Idle
        : MovementBlockReason.None;
}
