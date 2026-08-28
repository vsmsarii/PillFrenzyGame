using PillFrenzy.Core;
using UnityEditor;
using UnityEngine;

namespace Utilities
{
    public static class ResetSaveMenu
    {
        [MenuItem("PillFrenzy/Reset Save")]
        public static void ResetSave()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged += DeleteAfterExitingPlayMode;
                EditorApplication.isPlaying = false;
                return;
            }

            DeleteSaveFiles();
        }

        private static void DeleteAfterExitingPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.playModeStateChanged -= DeleteAfterExitingPlayMode;
            DeleteSaveFiles();
        }

        private static void DeleteSaveFiles()
        {
            SaveService.DeleteSaveFiles();
            Debug.Log("Save reset: " + SaveService.FilePath);
        }
    }
}
