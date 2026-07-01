using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class Bootstrapper
{
    public static void BuildAndroid()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Init.unity" },
            locationPathName = "Builds/Android/SlitherRoyale.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.ToString());
    }

    public static void BuildiOS()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Init.unity" },
            locationPathName = "Builds/iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.ToString());
    }

    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Init.unity" },
            locationPathName = "Builds/Windows/SlitherRoyale.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(report.summary.ToString());
    }
}
