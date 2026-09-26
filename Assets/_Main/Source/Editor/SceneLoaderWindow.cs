using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PillFrenzy.Editor
{
    public sealed class SceneLoaderWindow : EditorWindow
    {
        private const string TestScenePath = "Assets/_Main/Scene/Test.unity";

        private Vector2 m_Scroll;

        [MenuItem("PillFrenzy/Scene Loader")]
        public static void ShowWindow()
        {
            GetWindow<SceneLoaderWindow>("Scene Loader");
        }

        private void OnGUI()
        {
            GUILayout.Label("Scenes", EditorStyles.boldLabel);
            GUILayout.Space(5f);

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorGUILayout.HelpBox("No scenes in Build Settings.", MessageType.Warning);
            }
            else
            {
                m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (!scenes[i].enabled)
                        continue;

                    string sceneName = Path.GetFileNameWithoutExtension(scenes[i].path);
                    if (GUILayout.Button(sceneName, GUILayout.Height(28f)))
                        OpenScene(scenes[i].path);
                }

                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(12f);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            GUILayout.Space(8f);

            if (GUILayout.Button("TEST", GUILayout.Height(32f)))
                OpenScene(TestScenePath);
        }

        private static void OpenScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Scene Loader", "Scene not found:\n" + scenePath, "OK");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(scenePath);
        }
    }
}
