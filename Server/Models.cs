namespace StattrackServer;

using SPTarkov.Server.Core.Models.Utils;

public record StatTrackData
{
    public int Kills  { get; set; }
    public int BossKills { get; set; }
    public int HeadshotKills { get; set; }
    public int TotalShots { get; set; }
    public int TotalHits { get; set; }
    public int TotalHeadHits { get; set; }
    public float TotalDamage { get; set; }
    public int TimesLost { get; set; }
}


public record StatTrackStats : IRequestData
{
    public Dictionary<string, StatTrackData> Data { get; set; }
    public string ProfileId { get; set; }
}