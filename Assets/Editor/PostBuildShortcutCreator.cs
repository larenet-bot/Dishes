using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class PostBuildShortcutCreator
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // Folder the build was placed in
        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);

        // EXE filename
        string exeName = Path.GetFileName(pathToBuiltProject);

        // BAT file path
        string batPath = Path.Combine(buildFolder, "Create Desktop Shortcut.bat");

        string batContents =
$@"@echo off
set TARGET=%~dp0{exeName}
set SHORTCUT=%USERPROFILE%\Desktop\{Path.GetFileNameWithoutExtension(exeName)}.lnk

powershell ""$s=(New-Object -COM WScript.Shell).CreateShortcut('%SHORTCUT%');$s.TargetPath='%TARGET%';$s.WorkingDirectory='%~dp0';$s.Save()""

echo Shortcut created on desktop.
pause";

        File.WriteAllText(batPath, batContents);

        UnityEngine.Debug.Log("Created desktop shortcut BAT file.");
    }
}