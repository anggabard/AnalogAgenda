namespace AnalogAgenda.Functions.Constants;

public class TimeTriggers
{
    public const string Every30Seconds = "*/30 * * * * *";
    public const string EveryMinute = "0 * * * * *";
    public const string Every2WeeksAt11AM = "0 0 11 1,15 * *";
    public const string EveryDayAt10AM = "0 0 10 * * *";
    public const string Every7DaysAt7AM = "0 0 7 7,14,21,28 * *";
}
