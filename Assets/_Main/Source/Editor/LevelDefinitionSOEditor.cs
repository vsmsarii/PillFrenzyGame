using System.Collections.Generic;
using System.Text;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PillFrenzy.Editor
{
    [CustomEditor(typeof(LevelDefinitionSO))]
    public sealed class LevelDefinitionSOEditor : UnityEditor.Editor
    {
        private const string LevelFolder = "Assets/_Main/SO/Level";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelDefinitionSO definition = (LevelDefinitionSO)target;
            EditorGUILayout.Space(12f);
            DrawQueueSummary(definition);

            EditorGUILayout.Space(12f);
            using (new EditorGUILayout.HorizontalScope())
            {
                LevelLayout layout = LoadLayout(definition);
                if (layout == null)
                {
                    if (GUILayout.Button("Create Layout", GUILayout.Height(32f)))
                        LevelAuthoring.CreateLayoutFor(definition);
                }
                else
                {
                    if (GUILayout.Button("Edit Layout", GUILayout.Height(32f)))
                        AssetDatabase.OpenAsset(layout.gameObject);

                    if (GUILayout.Button("New Layout", GUILayout.Height(32f)))
                        LevelAuthoring.CreateLayoutFor(definition);
                }
            }

            if (GUILayout.Button("Create Next Level", GUILayout.Height(32f)))
                CreateNextLevel(definition);
        }

        private static void DrawQueueSummary(LevelDefinitionSO definition)
        {
            EditorGUILayout.LabelField("Target Queue", EditorStyles.boldLabel);

            TargetQuota[] queue = definition.TargetQueue;
            if (queue.Length == 0)
            {
                EditorGUILayout.HelpBox("Target queue is empty. The level can never complete.", MessageType.Error);
                return;
            }

            Dictionary<CapsuleColorSO, int> required = new();
            bool hasMissingColor = false;
            for (int i = 0; i < queue.Length; i++)
            {
                if (queue[i].Color == null)
                {
                    hasMissingColor = true;
                    continue;
                }

                required.TryGetValue(queue[i].Color, out int count);
                required[queue[i].Color] = count + (int)queue[i].Capacity;
            }

            StringBuilder summary = new StringBuilder(queue.Length + " boxes");
            foreach (KeyValuePair<CapsuleColorSO, int> pair in required)
                summary.Append("   ").Append(pair.Key.name).Append(' ').Append(pair.Value);

            EditorGUILayout.LabelField(summary.ToString(), EditorStyles.wordWrappedMiniLabel);

            if (hasMissingColor)
                EditorGUILayout.HelpBox("Some boxes have no color assigned.", MessageType.Error);

            LevelLayout layout = LoadLayout(definition);
            if (layout == null)
            {
                EditorGUILayout.HelpBox("Level has no layout. The manifest default layout is used until you create one.", MessageType.Info);
                return;
            }

            HashSet<CapsuleColorSO> spawned = LevelAuthoring.CollectSpawnedColors(layout);
            foreach (CapsuleColorSO color in required.Keys)
            {
                if (!spawned.Contains(color))
                    EditorGUILayout.HelpBox("No path in " + layout.name + " spawns " + color.name + ".", MessageType.Error);
            }
        }

        private static LevelLayout LoadLayout(LevelDefinitionSO definition)
        {
            GameObject prefab = definition.Layout.editorAsset;
            return prefab != null ? prefab.GetComponent<LevelLayout>() : null;
        }

        private static void CreateNextLevel(LevelDefinitionSO source)
        {
            LevelManifestSO manifest = LevelAuthoring.FindManifest();
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Level Manifest", "LevelManifestSO asset not found.", "OK");
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            bool sourceWasLast = IsLastEntry(manifest, source);
            int nextNumber = manifest.LevelCount + 1;
            string newPath = AssetDatabase.GenerateUniqueAssetPath(LevelFolder + "/Level" + nextNumber.ToString("00") + ".asset");
            if (!AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                EditorUtility.DisplayDialog("Create Next Level", "Could not copy level asset.", "OK");
                return;
            }

            LevelDefinitionSO created = AssetDatabase.LoadAssetAtPath<LevelDefinitionSO>(newPath);
            string newGuid = AssetDatabase.AssetPathToGUID(newPath);
            AppendToManifest(manifest, newGuid);
            LevelAuthoring.EnsureAddressable(newGuid, AddressableKeys.DefLevel(nextNumber - 1));

            if (sourceWasLast && source.ReturnToMenu)
            {
                SerializedObject serializedSource = new SerializedObject(source);
                serializedSource.FindProperty("m_ReturnToMenu").boolValue = false;
                serializedSource.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        private static bool IsLastEntry(LevelManifestSO manifest, LevelDefinitionSO definition)
        {
            List<LevelDefinitionSO> levels = LevelAuthoring.LoadLevels(manifest);
            return levels.Count > 0 && levels[levels.Count - 1] == definition;
        }

        private static void AppendToManifest(LevelManifestSO manifest, string assetGuid)
        {
            SerializedObject serializedManifest = new SerializedObject(manifest);
            SerializedProperty levels = serializedManifest.FindProperty("m_Levels");
            levels.arraySize++;
            levels.GetArrayElementAtIndex(levels.arraySize - 1).FindPropertyRelative("m_AssetGUID").stringValue = assetGuid;
            serializedManifest.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
