using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class PlaybackDataLoader
{
    public const string StreamingPlaybackFolder = "unity_playback";
    public const string DefaultMatlabPlaybackFolder = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/unity_playback";
    private const string LegacyMatlabPlaybackFolder = "C:/Users/ywc/Desktop/codex/matlab_workshop_model/output/unity_export_v2/simevents_stateflow_finaltransport_4m1agv";

    public static string ResolvePlaybackRoot(string configuredDirectory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return CombineUrl(Application.streamingAssetsPath, StreamingPlaybackFolder);
#else
        string streamingRoot = Path.Combine(Application.streamingAssetsPath, StreamingPlaybackFolder);
        bool hasConfiguredDirectory = !string.IsNullOrWhiteSpace(configuredDirectory);
        bool configuredIsLegacyDefault = string.Equals(NormalizePath(configuredDirectory), NormalizePath(LegacyMatlabPlaybackFolder), StringComparison.OrdinalIgnoreCase);

        if (hasConfiguredDirectory && !configuredIsLegacyDefault && Directory.Exists(configuredDirectory))
        {
            return configuredDirectory;
        }

        if (Directory.Exists(DefaultMatlabPlaybackFolder))
        {
            return DefaultMatlabPlaybackFolder;
        }

        if (hasConfiguredDirectory && Directory.Exists(configuredDirectory))
        {
            return configuredDirectory;
        }

        if (Directory.Exists(streamingRoot))
        {
            return streamingRoot;
        }

        return DefaultMatlabPlaybackFolder;
#endif
    }

    public static bool FileExists(string rootDirectory, string fileName)
    {
        string path = CombinePath(rootDirectory, fileName);
#if UNITY_WEBGL && !UNITY_EDITOR
        return false;
#else
        return File.Exists(path);
#endif
    }

    public static bool TryReadAllLines(string rootDirectory, string fileName, out string[] lines, out string error)
    {
        lines = Array.Empty<string>();
        error = "";

        string path = CombinePath(rootDirectory, fileName);
        try
        {
            if (!File.Exists(path))
            {
                error = "Missing file: " + path;
                return false;
            }

            lines = File.ReadAllLines(path);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static IEnumerator ReadAllLinesRoutine(string rootDirectory, string fileName, Action<string[], string> completed)
    {
        string path = CombinePath(rootDirectory, fileName);
#if UNITY_WEBGL && !UNITY_EDITOR
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                completed(Array.Empty<string>(), request.error + ": " + path);
                yield break;
            }

            completed(SplitLines(request.downloadHandler.text), "");
        }
#else
        string[] lines;
        string error;
        TryReadAllLines(rootDirectory, fileName, out lines, out error);
        completed(lines, error);
        yield break;
#endif
    }

    public static IEnumerable<Dictionary<string, string>> ReadCsv(string[] lines)
    {
        if (lines == null || lines.Length < 2)
        {
            yield break;
        }

        List<string> headers = SplitCsvLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            List<string> values = SplitCsvLine(lines[i]);
            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
            {
                row[headers[j]] = j < values.Count ? values[j] : "";
            }
            yield return row;
        }
    }

    public static string Get(Dictionary<string, string> row, string key)
    {
        string value;
        return row.TryGetValue(key, out value) ? value : "";
    }

    public static float GetFloat(Dictionary<string, string> row, string key)
    {
        string value = Get(row, key);
        float result;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }
        return 0f;
    }

    private static string CombinePath(string rootDirectory, string fileName)
    {
        if (rootDirectory.Contains("://"))
        {
            return CombineUrl(rootDirectory, fileName);
        }

        return Path.Combine(rootDirectory, fileName);
    }

    private static string CombineUrl(string root, string child)
    {
        return root.TrimEnd('/') + "/" + child.TrimStart('/');
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "" : path.Replace('\\', '/').TrimEnd('/');
    }

    private static string[] SplitLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }
}
