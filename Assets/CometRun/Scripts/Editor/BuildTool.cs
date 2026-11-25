using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CometRun.EditorTools
{
    // Menu button so I don't have to set up the Android build settings every time.
    // Comet Run > Build Android APK, the apk ends up in Builds/Android/CometRun.apk
    public static class BuildTool
    {
        const string ApkPath = "Builds/Android/CometRun.apk";

        [MenuItem("Comet Run/Build Android APK")]
        public static void BuildAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.productName = "Comet Run";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.itu.cometrun");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuSceneBuilder.ScenePath, GameSceneBuilder.ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                long bytes = new System.IO.FileInfo(ApkPath).Length;
                Debug.Log("Comet Run: APK built at " + ApkPath + " (" + bytes / (1024 * 1024) + " MB)");
            }
            else
            {
                Debug.LogError("Comet Run: build failed (" + report.summary.result + ")");
                if (Application.isBatchMode) EditorApplication.Exit(1); // so a command line build actually reports the fail
            }
        }
    }
}
