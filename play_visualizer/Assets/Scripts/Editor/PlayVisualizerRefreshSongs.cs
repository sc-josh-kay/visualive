using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PlayVisualizer.Gameplay;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Repopulates the SongLibrary from Assets/Audio/Tracks WITHOUT rebuilding the start screen.
    /// Run this after adding/renaming/removing tracks; the menu buttons are generated at runtime
    /// from the library, so refreshing the asset is all that's needed.
    ///
    /// Menu: PlayVisualizer → Refresh Song List
    /// </summary>
    public static class PlayVisualizerRefreshSongs
    {
        private const string LibraryPath = "Assets/ScriptableObjects/SongLibrary.asset";
        private const string TracksFolder = "Assets/Audio/Tracks";

        [MenuItem("PlayVisualizer/Refresh Song List")]
        public static void Refresh()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SongLibrary>(LibraryPath);
            if (lib == null)
            {
                Debug.LogError($"PlayVisualizer: no SongLibrary at {LibraryPath}. " +
                               "Run 'Build Start Screen (Visualive)' once first.");
                return;
            }

            var clips = new List<AudioClip>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { TracksFolder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip != null) clips.Add(clip);
            }
            clips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            lib.Tracks = clips.ToArray();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();

            Debug.Log($"PlayVisualizer: Song list refreshed — {clips.Count} track(s).");
        }
    }
}
