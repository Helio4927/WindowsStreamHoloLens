using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Logger : MonoBehaviourInstance<Logger>
{
    public bool showOnConsole;
    public bool captureConsole;

    public UnityEvent<string> OnLog;
    public UnityEvent<string> OnLogWarning;
    public UnityEvent<string> OnLogError;

    private void OnEnable()
    {
        Application.logMessageReceived += FromConsole;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= FromConsole;
    }

    private void FromConsole(string logString, string stackTrace, LogType type)
    {
        if (captureConsole)
        {
            switch (type)
            {
                case LogType.Log:
                    Log(logString, true);
                    break;
                case LogType.Warning:
                    LogWarning(logString, true);
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    LogError(logString, true);
                    break;
            }
        }
    }

    public void Log(string s)
    {
        Log(s, false);
    }

    private void Log(string s, bool fromConsole)
    {
        if (showOnConsole && !fromConsole)
        {
            Debug.Log(s);
        }
        OnLog.Invoke(s);
    }

    public void LogWarning(string s)
    {
        LogWarning(s, false);
    }

    private void LogWarning(string s, bool fromConsole)
    {
        if (showOnConsole && !fromConsole)
        {
            Debug.LogWarning(s);
        }
        OnLogWarning.Invoke(s);
    }

    public void LogError(string s)
    {
        LogError(s, false);
    }

    private void LogError(string s, bool fromConsole)
    {
        if (showOnConsole && !fromConsole)
        {
            Debug.LogError(s);
        }
        OnLogError.Invoke(s);
    }
}
