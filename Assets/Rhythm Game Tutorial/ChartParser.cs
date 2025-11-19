using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;

public static class ChartParser
{
    /// Removes all comments from a text file and returns clean JSON
    public static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "{}";

        string[] lines = raw.Split('\n');
        StringBuilder sb = new StringBuilder();

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            // Skip full-line comments
            if (trimmed.StartsWith("//")) continue;

            // Remove inline comments
            string noInline = Regex.Replace(trimmed, @"\/\/.*$", "");

            if (!string.IsNullOrWhiteSpace(noInline))
                sb.AppendLine(noInline);
        }

        return sb.ToString();
    }
}
