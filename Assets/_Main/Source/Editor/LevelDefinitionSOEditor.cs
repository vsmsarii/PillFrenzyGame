using System.IO;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
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

            EditorGUILayout.Space(12f);
            if (!GUILayout.Button("Create Next Level", GUILayout.Height(32f)))
                return;

            LevelDefinitionSO source = (LevelDefinitionSO)target;
            CreateNextLevel(source);
        }

        private static void CreateNextLevel(LevelDefinitionSO source)
        {
            if (source == null)
                return;

            LevelManifestSO manifest = FindManifest();
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Level Manifest", "LevelManifestSO asset not found.", "OK");
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath))
            {
                EditorUtility.DisplayDialog("Create Next Level", "Save the Level Definition asset first.", "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(LevelFolder))
                Directory.CreateDirectory(LevelFolder);

            int nextNumber = manifest.LevelCount + 1;
            string newPath = AssetDatabase.GenerateUniqueAssetPath(LevelFolder + "/Level" + nextNumber.ToString("00") + ".asset");
            if (!AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                EditorUtility.DisplayDialog("Create Next Level", "Could not copy level asset.", "OK");
                return;
            }

            AssetDatabase.ImportAsset(newPath);
            LevelDefinitionSO created = AssetDatabase.LoadAssetAtPath<LevelDefinitionSO>(newPath);
            if (created == null)
            {
                EditorUtility.DisplayDialog("Create Next Level", "Copied asset failed to load.", "OK");
                return;
            }

            string newGuid = AssetDatabase.AssetPathToGUID(newPath);
            AppendToManifest(manifest, newGuid);
            EnsureAddressable(newGuid, AddressableKeys.DefLevel(nextNumber - 1));

            if (IsLastEntry(manifest, source) && source.ReturnToMenu)
            {
                SerializedObject sourceSo = new SerializedObject(source);
                sourceSo.FindProperty("m_ReturnToMenu").boolValue = false;
                sourceSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(source);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        private static LevelManifestSO FindManifest()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelManifestSO");
            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LevelManifestSO>(path);
        }

        private static bool IsLastEntry(LevelManifestSO manifest, LevelDefinitionSO definition)
        {
            if (manifest == null || definition == null || manifest.LevelCount < 1)
                return false;

            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            SerializedObject so = new SerializedObject(manifest);
            SerializedProperty levels = so.FindProperty("m_Levels");
            if (levels == null || levels.arraySize < 1)
                return false;

            SerializedProperty last = levels.GetArrayElementAtIndex(levels.arraySize - 1);
            SerializedProperty guidProp = last.FindPropertyRelative("m_AssetGUID");
            return guidProp != null && guidProp.stringValue == definitionGuid;
        }

        private static void AppendToManifest(LevelManifestSO manifest, string assetGuid)
        {
            SerializedObject so = new SerializedObject(manifest);
            SerializedProperty levels = so.FindProperty("m_Levels");
            levels.arraySize++;
            SerializedProperty element = levels.GetArrayElementAtIndex(levels.arraySize - 1);
            SerializedProperty guidProp = element.FindPropertyRelative("m_AssetGUID");
            if (guidProp != null)
                guidProp.stringValue = assetGuid;

            SerializedProperty subName = element.FindPropertyRelative("m_SubObjectName");
            if (subName != null)
                subName.stringValue = string.Empty;

            SerializedProperty subType = element.FindPropertyRelative("m_SubObjectType");
            if (subType != null)
                subType.stringValue = string.Empty;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manifest);
        }

        private static void EnsureAddressable(string guid, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null || string.IsNullOrEmpty(guid))
                return;

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null)
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);

            if (entry != null && entry.address != address)
                entry.SetAddress(address, false);

            EditorUtility.SetDirty(settings);
        }
    }
}
