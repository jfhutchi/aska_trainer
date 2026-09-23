namespace HutchASKA.Plugin.Tribe;

internal interface ITribeContextStatus
{
    string? LastError { get; }
    string? LastDiscoverySummary { get; }
    bool HasNativeFailure { get; }
}
