using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class PostBuildShortcutCreator
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // Only create the shortcut script for Windows builds
        if (target != BuildTarget.StandaloneWindows &&
            target != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        string exeName = Path.GetFileName(pathToBuiltProject);
        string gameName = Path.GetFileNameWithoutExtension(pathToBuiltProject);

        string batPath = Path.Combine(
            buildFolder,
            "Create Desktop Shortcut.bat"
        );

        string batContents = $@"@echo off
setlocal

set ""GAME_EXE=%~dp0{exeName}""
set ""SHORTCUT=%USERPROFILE%\Desktop\{gameName}.lnk""
set ""GAME_FOLDER=%~dp0""

powershell -NoProfile -ExecutionPolicy Bypass -Command ""$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%USERPROFILE%\Desktop\{gameName}.lnk'); $Shortcut.TargetPath = '%~dp0{exeName}'; $Shortcut.WorkingDirectory = '%~dp0'; $Shortcut.IconLocation = '%~dp0{exeName},0'; $Shortcut.Save()""

if exist ""%SHORTCUT%"" (
    echo.
    echo Desktop shortcut created successfully!
    echo.
    echo Shortcut:
    echo %SHORTCUT%
) else (
    echo.
    echo ERROR: Shortcut could not be created.
)

pause
";

        File.WriteAllText(batPath, batContents);

        UnityEngine.Debug.Log(
            $"Created desktop shortcut script: {batPath}"
        );
    }
}