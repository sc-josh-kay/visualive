using UnityEngine;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// The list of selectable tracks, populated from Assets/Audio/Tracks by the editor setup.
    /// The start screen reads this to build the song menu.
    /// </summary>
    [CreateAssetMenu(fileName = "SongLibrary", menuName = "PlayVisualizer/Song Library")]
    public class SongLibrary : ScriptableObject
    {
        public AudioClip[] Tracks;
    }
}
