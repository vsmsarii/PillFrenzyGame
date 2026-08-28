using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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

    [ThreadStatic] private static StringBuilder s_Builder;

    public static ELogLevel MinimumLevel => s_MinimumLevel;

    public static void SetMinimumLevel(ELogLevel level)
    {
        s_MinimumLevel = level;
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context = null, bool callerInfo = true)
    {
        if (s_MinimumLevel > ELogLevel.Log)
            return;

        Write(ELogLevel.Log, message, context, callerInfo ? ResolveCaller() : null);
    }

    public static void Warning(object message, UnityEngine.Object context = null, bool callerInfo = false)
    {
        if (s_MinimumLevel > ELogLevel.Warning)
            return;

        Write(ELogLevel.Warning, message, context, callerInfo ? ResolveCaller() : null);
    }

    public static void Error(object message, UnityEngine.Object context = null, bool callerInfo = false)
    {
        if (s_MinimumLevel > ELogLevel.Error)
            return;

        Write(ELogLevel.Error, message, context, callerInfo ? ResolveCaller() : null);
    }

    private static void Write(ELogLevel level, object message, UnityEngine.Object context, string caller)
    {
        string formatted = Format(message, context, caller);
        switch (level)
        {
            case ELogLevel.Log:
                UnityEngine.Debug.Log(formatted, context);
                break;
            case ELogLevel.Warning:
                UnityEngine.Debug.LogWarning(formatted, context);
                break;
            case ELogLevel.Error:
                UnityEngine.Debug.LogError(formatted, context);
                break;
        }
    }

    private static string Format(object message, UnityEngine.Object context, string caller)
    {
        if (caller == null && context == null)
            return message != null ? message.ToString() : string.Empty;

        StringBuilder builder = s_Builder;
        if (builder == null)
        {
            builder = new StringBuilder(256);
            s_Builder = builder;
        }

        builder.Clear();

        if (caller != null)
            builder.Append('[').Append(caller).Append("] ");

        if (context != null)
            builder.Append('[').Append(context.name).Append("] ");

        builder.Append(message);
        return builder.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ResolveCaller()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        StackFrame frame = new StackTrace(2, true).GetFrame(0);
        MethodBase method = frame?.GetMethod();
        if (method?.DeclaringType == null)
            return null;

        return method.DeclaringType.Name + "." + method.Name + ":" + frame.GetFileLineNumber();
#else
        return null;
#endif
    }
}
