using System.Collections.Generic;
using System.Linq;
using PillFrenzy.Gameplay;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Splines;

namespace PillFrenzy.Editor
{
    public static class LevelAuthoring
    {
        public const string BeltMaterialPath = "Assets/_Main/Art/3D/Material/M_Belt.mat";
        public const string LayoutFolder = "Assets/_Main/Prefab/Level";
        public const string ColorFolder = "Assets/_Main/SO/Color";

        private static readonly Vector3[] s_DefaultKnots =
        {
            new(-4f, 0f, 0f),
            new(0f, 0f, 1f),
            new(4f, 0f, 0f)
        };

        public static LevelPath CreatePath(LevelLayout layout, IReadOnlyList<Vector3> localKnots = null)
        {
            IReadOnlyList<LevelPath> existing = layout.Paths;
            GameObject go = new GameObject("Path " + (existing.Count + 1));
            Undo.RegisterCreatedObjectUndo(go, "Add Path");
            go.transform.SetParent(layout.transform, false);

            if (localKnots == null)
            {
                Vector3 offset = Vector3.forward * (existing.Count * 2f);
                localKnots = s_DefaultKnots.Select(knot => knot + offset).ToArray();
            }

            SplineContainer container = go.AddComponent<SplineContainer>();
            Spline spline = container.Spline;
            spline.Clear();
            for (int i = 0; i < localKnots.Count; i++)
                spline.Add((float3)localKnots[i], TangentMode.AutoSmooth);

            LevelPath path = go.AddComponent<LevelPath>();
            if (existing.Count > 0)
                path.SetColors(existing[existing.Count - 1].Colors);

            go.AddComponent<PathMesh>();
            go.GetComponent<MeshRenderer>().sharedMaterial = LoadBeltMaterial();
            return path;
        }

        public static void RemovePath(LevelPath path)
        {
            Undo.DestroyObjectImmediate(path.gameObject);
        }

        public static void ReversePath(LevelPath path)
        {
            Undo.RecordObject(path.Container, "Reverse Path");
            path.Container.ReverseFlow(0);
            EditorUtility.SetDirty(path.Container);
        }

        public static void RebuildMeshes(LevelLayout layout)
        {
            foreach (PathMesh mesh in layout.GetComponentsInChildren<PathMesh>())
                mesh.Rebuild();
        }

        public static HashSet<CapsuleColorSO> CollectSpawnedColors(LevelLayout layout)
        {
            HashSet<CapsuleColorSO> colors = new();
            foreach (LevelPath path in layout.Paths)
            {
                foreach (PathColorWeight weight in path.Colors)
                {
                    if (weight.Color != null && weight.Weight > 0f)
                        colors.Add(weight.Color);
                }
            }

            return colors;
        }

        public static LevelManifestSO FindManifest()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelManifestSO");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<LevelManifestSO>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        public static List<LevelDefinitionSO> LoadLevels(LevelManifestSO manifest)
        {
            List<LevelDefinitionSO> levels = new();
            SerializedProperty entries = new SerializedObject(manifest).FindProperty("m_Levels");
            for (int i = 0; i < entries.arraySize; i++)
            {
                string guid = entries.GetArrayElementAtIndex(i).FindPropertyRelative("m_AssetGUID").stringValue;
                levels.Add(AssetDatabase.LoadAssetAtPath<LevelDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return levels;
        }

        public static GameObject LoadDefaultLayout(LevelManifestSO manifest)
        {
            string guid = new SerializedObject(manifest).FindProperty("m_DefaultLayout.m_AssetGUID").stringValue;
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static GameObject CreateLayoutFor(LevelDefinitionSO definition)
        {
            LevelManifestSO manifest = FindManifest();
            GameObject template = definition.Layout.editorAsset != null ? definition.Layout.editorAsset : LoadDefaultLayout(manifest);

            if (template == null)
            {
                EditorUtility.DisplayDialog("Create Layout", "No layout to use as a template. Assign a default layout in the level manifest.", "OK");
                return null;
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(LayoutFolder + "/" + definition.name + ".prefab");
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), path))
                return null;

            int levelNumber = LoadLevels(manifest).IndexOf(definition) + 1;
            string address = levelNumber > 0 ? "layout.level." + levelNumber : "layout." + definition.name.ToLowerInvariant();
            string guid = AssetDatabase.AssetPathToGUID(path);
            EnsureAddressable(guid, address);

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("m_Layout.m_AssetGUID").stringValue = guid;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            GameObject created = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            AssetDatabase.OpenAsset(created);
            return created;
        }

        public static CapsuleColorSO CreateColor(string colorName, Color color)
        {
            if (!AssetDatabase.IsValidFolder(ColorFolder))
                AssetDatabase.CreateFolder("Assets/_Main/SO", "Color");

            CapsuleColorSO asset = ScriptableObject.CreateInstance<CapsuleColorSO>();
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Color").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(ColorFolder + "/" + colorName + ".asset"));
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static List<CapsuleColorSO> LoadColors()
        {
            return AssetDatabase.FindAssets("t:CapsuleColorSO")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CapsuleColorSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(color => color.name)
                .ToList();
        }

        public static void EnsureAddressable(string guid, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetEntry entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
            if (entry.address != address)
                entry.SetAddress(address, false);

            EditorUtility.SetDirty(settings);
        }

        public static Material LoadBeltMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BeltMaterialPath);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.32f, 0.34f, 0.38f) };
            AssetDatabase.CreateAsset(material, BeltMaterialPath);
            return material;
        }
    }
}
