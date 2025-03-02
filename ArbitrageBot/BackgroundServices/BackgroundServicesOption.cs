namespace ArbitrageBot.BackgroundServices;

public class BackgroundServicesOption
{
    public const string SectionKey = "BackgroundServices";
    public required TimeSpan AssetsBackgroundService { get; set; }
}
