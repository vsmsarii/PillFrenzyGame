using System.Diagnostics;
using System.Text;

public enum ELogLevel
{
    Log,
    Warning,
    Error
}

public static class Logger
{
    private static bool m_BlockLog = false;
    private static bool m_BlockWarning = false;
    private static bool m_BlockError = false;


    private static readonly StringBuilder m_StringBuilder = new StringBuilder(256);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context = null, bool callerInfo = true, bool force = false)
    {
        if(m_BlockLog && !force)
            return;

        LogInternal(ELogLevel.Log, message, context, callerInfo);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Warning(object message, UnityEngine.Object context = null, bool callerInfo = true, bool force = false)
    {
        if(m_BlockWarning && !force)
            return;

        LogInternal(ELogLevel.Warning, message, context, callerInfo);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Error(object message, UnityEngine.Object context = null, bool callerInfo = true, bool force = false)
    {
        if(m_BlockError && !force)
            return;

        LogInternal(ELogLevel.Error, message, context, callerInfo);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    private static void LogInternal(ELogLevel level, object message, UnityEngine.Object context, bool callerInfo = true)
    {

#if UNITY_EDITOR || DEVELOPMENT_BUILD

        m_StringBuilder.Clear();

        if (callerInfo)
        {
            var stackTrace = new StackTrace(2, true);
            var frame = stackTrace.GetFrame(0);

            if (frame?.GetMethod()?.DeclaringType != null)
            {
                var method = frame.GetMethod();
                m_StringBuilder.Append('[')
                            .Append(method.DeclaringType.Name)
                            .Append('.')
                            .Append(method.Name)
                            .Append(':')
                            .Append(frame.GetFileLineNumber())
                            .Append("] ");
            }
            else
                m_StringBuilder.Append("[Unknown] ");
        }

        if (context != null)
            m_StringBuilder.Append('[').Append(context.name).Append("] ");

        m_StringBuilder.Append(message);

        var formattedMessage = m_StringBuilder.ToString();
        switch (level)
        {
            case ELogLevel.Log: UnityEngine.Debug.Log(formattedMessage, context); break;
            case ELogLevel.Warning: UnityEngine.Debug.LogWarning(formattedMessage, context); break;
            case ELogLevel.Error: UnityEngine.Debug.LogError(formattedMessage, context); break;
        }

#endif
    }
}
