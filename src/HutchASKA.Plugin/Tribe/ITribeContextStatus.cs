namespace HutchASKA.Plugin.Tribe;

internal interface ITribeContextStatus
{
    string? LastError { get; }
    bool HasNativeFailure { get; }
}
