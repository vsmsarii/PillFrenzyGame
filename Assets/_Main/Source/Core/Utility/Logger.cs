using System.Diagnostics;

public enum ELogLevel
{
    Log = 0,
    Warning,
    Error,
    None
}

public static class Logger
{
    private static ELogLevel s_MinimumLevel = ELogLevel.Log;

    public static void SetMinimumLevel(ELogLevel level)
    {
        s_MinimumLevel = level;
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context = null)
    {
        if (s_MinimumLevel <= ELogLevel.Log)
            UnityEngine.Debug.Log(message, context);
    }

    public static void Warning(object message, UnityEngine.Object context = null)
    {
        if (s_MinimumLevel <= ELogLevel.Warning)
            UnityEngine.Debug.LogWarning(message, context);
    }

    public static void Error(object message, UnityEngine.Object context = null)
    {
        if (s_MinimumLevel <= ELogLevel.Error)
            UnityEngine.Debug.LogError(message, context);
    }
}
