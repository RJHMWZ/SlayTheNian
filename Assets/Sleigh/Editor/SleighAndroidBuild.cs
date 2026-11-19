#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sleigh.Editor
{
    // Uses the build pipeline directly, without the Android device-list IMGUI window.
    public static class SleighAndroidBuild
    {
        [MenuItem("Sleigh/Android/配置手机横屏")]
        public static void ConfigureLandscape()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.maxAspectRatio = 3;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            if (string.IsNullOrWhiteSpace(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)))
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.sleigh.slaythenian");
            AssetDatabase.SaveAssets();
            Debug.Log("Android landscape configuration ready.");
        }

        [MenuItem("Sleigh/Android/构建横屏测试APK")]
        public static void BuildTestApk()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Stop Play mode and wait for script compilation before building Android.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, Android SDK & NDK Tools and OpenJDK for this Unity Editor in Unity Hub.");
            if (!File.Exists(SleighProjectSetup.ScenePath))
                throw new FileNotFoundException("The saved game scene is missing.", SleighProjectSetup.ScenePath);

            ConfigureLandscape();
            Directory.CreateDirectory("Builds/Android");
            bool previousBundle = EditorUserBuildSettings.buildAppBundle;
            bool previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { SleighProjectSetup.ScenePath },
                    locationPathName = "Builds/Android/SlayTheNian-test.apk",
                    target = BuildTarget.Android,
                    options = BuildOptions.Development
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Android APK build failed. See the first build error in the Console. Result: " + report.summary.result);
                Debug.Log("Android test APK ready: " + Path.GetFullPath(report.summary.outputPath));
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousBundle;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport;
            }
        }
    }
}
#endif
