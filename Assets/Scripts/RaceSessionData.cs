public static class RaceSessionData
{
    public static float BestLapTime { get; private set; } = float.MaxValue;

    public static void SubmitLapTime(float lapTime)
    {
        if (lapTime < BestLapTime)
        {
            BestLapTime = lapTime;
        }
    }

    public static void Clear()
    {
        BestLapTime = float.MaxValue;
    }
}