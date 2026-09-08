#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Globalization;

[InitializeOnLoad]
public static class ProjectTimer
{
    private const string LAST_CHECK_KEY = "PTimer_LastCheckTime";
    private static readonly double intervalSecs = 600.0;
    private static string folderPath = Application.dataPath + "/Editor/ZProjectTimeTracker";
    private static string logFilePath = folderPath + "/ProjectTimeLog.txt";

    static ProjectTimer()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        if (!File.Exists(logFilePath))
        {
            File.WriteAllText(logFilePath, $"=== {Application.productName} PROJECT TIME LOG ===\n");
        }
        float seshTime = SessionState.GetFloat(LAST_CHECK_KEY, 0);
        if (seshTime == 0)
        {
            SessionState.SetFloat(LAST_CHECK_KEY, (float)EditorApplication.timeSinceStartup);
        }

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.quitting += OnQuitting;
    }

    static void OnEditorUpdate()
    {
        double lastCheck = SessionState.GetFloat(LAST_CHECK_KEY, 0);

        //Update the log file every 10 minutes to avoid crash data loss
        if (EditorApplication.timeSinceStartup - lastCheck < intervalSecs) return;

        SaveToLog();
    }

    static void OnQuitting()
    {
        SaveToLog();
    }

    static void SaveToLog()
    {
        double lastCheck = SessionState.GetFloat(LAST_CHECK_KEY, 0);
        if (lastCheck == 0) return;

        double elapsed = EditorApplication.timeSinceStartup - lastCheck;
        if (elapsed <= 10) return;   //for instant crash on load

        SessionState.SetFloat(LAST_CHECK_KEY, (float)EditorApplication.timeSinceStartup);

        double totalTime = ParseTotalTimeFromLog();

        totalTime += elapsed;

        string today = DateTime.Now.ToString("dd/MM/yyyy");
        ParseDataFromFile(out int val, out string last);
        if (last != today)
        {
            val++;
        }

        TimeSpan timeSpan = TimeSpan.FromSeconds(totalTime);
        //The file layout
        string[] fileContent = new string[]
        {
            $"=== {Application.productName} PROJECT TIME LOG ===\n",
            $"Total time spent on this project : {Mathf.FloorToInt((float)timeSpan.TotalHours)}h : {timeSpan.Minutes:D2}m\n",
            "IGNORE THE LAST PART!!\n",
            $"{DateTime.Now:dd/MM/yyyy}",
            $"{val}",
            $"RAW_TOTAL_SECONDS:{totalTime.ToString(CultureInfo.InvariantCulture)}"
        };

        File.WriteAllLines(logFilePath, fileContent);
        AssetDatabase.ImportAsset("Assets/Editor/ZProjectTimeTracker/ProjectTimeLog.txt", ImportAssetOptions.ForceUpdate);
    }

    static double ParseTotalTimeFromLog()
    {
        if (!File.Exists(logFilePath)) return 0;

        try
        {
            string[] lines = File.ReadAllLines(logFilePath);

            //Parse the last line as it has the total time value for easy reading
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].StartsWith("RAW_TOTAL_SECONDS:"))
                {
                    string[] split = lines[i].Split(':');
                    if (split.Length == 2 && double.TryParse(split[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedSeconds))
                    {
                        return parsedSeconds;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ProjectTimer failed to parse previous time: " + e.Message);
        }

        return 0;
    }

    static void ParseDataFromFile(out int val, out string last)
    {
        val = 0;
        last = DateTime.Now.ToString("dd/MM/yyyy");

        if (!File.Exists(logFilePath)) return;

        try
        {
            string[] lines = File.ReadAllLines(logFilePath);
            if (lines.Length >= 3) //so that we know file has been updated atleast once
            {
                int.TryParse(lines[lines.Length - 2].Trim(), out val);
                if (val <= 0) val = 1;

                last = lines[lines.Length - 3].Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("ProjectTimer Parse Error: " + e.Message);
        }
    }
}
#endif