using UnityEditor;
using UnityEngine;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// iOS port M1: configures Player Settings for the iPhone build — landscape-only orientation,
    /// bundle identifier, minimum iOS version, iPhone target, and automatic signing. Safe to run
    /// before the iOS Build Support module is installed (these are just settings). Re-runnable.
    ///
    /// Menu: PlayVisualizer → iOS: Configure Player Settings (M1)
    /// </summary>
    public static class PlayVisualizerIosM1
    {
        // Change this if you want a different id; must be unique to your Apple ID.
        private const string BundleId = "com.josh.visualive";

        [MenuItem("PlayVisualizer/iOS: Configure Player Settings (M1)")]
        public static void Configure()
        {
            PlayerSettings.productName = "Visualive";

            // Landscape only (both landscape orientations, auto-rotate between them).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.useAnimatedAutorotation = true;

            // iOS identity / target.
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
            PlayerSettings.iOS.targetOSVersionString = "16.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;

            // Free personal team: let Xcode manage signing (you'll pick your team in Xcode).
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"PlayVisualizer: iOS Player Settings configured — landscape-only, bundle '{BundleId}', " +
                      "min iOS 16.0, iPhone. Now install the iOS Build Support module (Unity Hub) and switch platform to iOS.");
        }
    }
}
