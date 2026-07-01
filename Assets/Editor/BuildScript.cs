// BuildScript.cs — Unity batch-mode Android build script
// Usage: Unity.exe -batchmode -quit -projectPath <path> -executeMethod BuildScript.BuildAndroid
// Output: Builds/<timestamp>/SlitherRoyale.apk

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/Init.unity"
    };

    public static void BuildAndroid()
    {
        Debug.Log("[BuildScript] Starting Android APK build...");

        // ── SDK / NDK / JDK paths (bundled with Unity) ─────────────────────
        string editorRoot = Path.GetDirectoryName(EditorApplication.applicationPath);
        string androidPlayer = Path.Combine(editorRoot, "Data", "PlaybackEngines", "AndroidPlayer");

        string sdkPath = Path.Combine(androidPlayer, "SDK");
        string ndkPath = Path.Combine(androidPlayer, "NDK");
        string jdkPath = Path.Combine(androidPlayer, "OpenJDK");

        EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
        EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
        EditorPrefs.SetString("JdkPath",        jdkPath);

        Debug.Log($"[BuildScript] SDK: {sdkPath}");
        Debug.Log($"[BuildScript] NDK: {ndkPath}");
        Debug.Log($"[BuildScript] JDK: {jdkPath}");

        // ── Player settings ────────────────────────────────────────────────
        PlayerSettings.companyName             = "SlitherRoyale";
        PlayerSettings.productName             = "Slither Royale";
        PlayerSettings.applicationIdentifier   = "com.slitherroyale.game";
        PlayerSettings.bundleVersion           = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        PlayerSettings.Android.minSdkVersion          = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0
        PlayerSettings.Android.targetSdkVersion       = AndroidSdkVersions.AndroidApiLevel34; // Android 14
        PlayerSettings.Android.targetArchitectures    = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.optimizedFramePacing   = true;

        // Screen orientation — portrait only (mobile game)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

        // ── Keystore (debug self-signed — replace for production) ──────────
        string projectRoot  = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string keystorePath = Path.Combine(projectRoot, "Builds", "debug.keystore");
        Directory.CreateDirectory(Path.GetDirectoryName(keystorePath));

        if (!File.Exists(keystorePath))
        {
            Debug.Log("[BuildScript] No keystore found — Unity will use default debug signing.");
        }

        PlayerSettings.Android.useCustomKeystore = File.Exists(keystorePath);
        if (PlayerSettings.Android.useCustomKeystore)
        {
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = "slitherroyale";
            PlayerSettings.Android.keyaliasName = "slitherroyale";
            PlayerSettings.Android.keyaliasPass = "slitherroyale";
        }

        // ── Output path ────────────────────────────────────────────────────
        string timestamp  = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputDir  = Path.Combine(projectRoot, "Builds", timestamp);
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, "SlitherRoyale.apk");

        // ── Build ──────────────────────────────────────────────────────────
        var options = new BuildPlayerOptions
        {
            scenes           = Scenes,
            locationPathName = outputPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        Debug.Log($"[BuildScript] Building to: {outputPath}");

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] BUILD SUCCEEDED: {outputPath} ({summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"[BuildScript] BUILD FAILED: {summary.result} — {summary.totalErrors} errors");
            EditorApplication.Exit(1);
        }
    }
}
