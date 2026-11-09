namespace LabIdentity.Configuration;

public sealed class LabSettings
{
    public AppSettings App { get; set; } = new();
    public PollingSettings Polling { get; set; } = new();
    public DatabaseSettings ConnectionStrings { get; set; } = new();
}

public sealed class AppSettings
{
    public string ApplicationName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class PollingSettings
{
    public int MaxOptionsPerPoll { get; set; }
}

public sealed class DatabaseSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}