using UnityEngine;
using System;
using System.IO;
using System.Text;

public class ExeFolderLogger : MonoBehaviour
{
    private string logPath;

    void Awake()
    {
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        logPath = Path.Combine(exeDir, "log.txt");

        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string log, string stack, LogType type)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] [{type}] {log}");

        if (type == LogType.Error || type == LogType.Exception)
            sb.AppendLine(stack);

        File.AppendAllText(logPath, sb.ToString());
    }
}
