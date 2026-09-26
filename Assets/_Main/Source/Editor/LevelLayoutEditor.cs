using System.Collections.Generic;
using PillFrenzy.Gameplay;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Splines;
using UnityEngine;

namespace PillFrenzy.Editor
{
    [CustomEditor(typeof(LevelLayout))]
    public sealed class LevelLayoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelLayout layout = (LevelLayout)target;
            IReadOnlyList<LevelPath> paths = layout.Paths;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);

            if (paths.Count == 0)
                EditorGUILayout.HelpBox("Layout has no paths. Capsules cannot spawn.", MessageType.Error);

            for (int i = 0; i < paths.Count; i++)
                DrawPathRow(paths[i]);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Path", GUILayout.Height(28f)))
                    Select(LevelAuthoring.CreatePath(layout));

                if (GUILayout.Button("Rebuild Meshes", GUILayout.Height(28f)))
                    LevelAuthoring.RebuildMeshes(layout);
            }
        }

        private static void DrawPathRow(LevelPath path)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(path.name, EditorStyles.boldLabel, GUILayout.Width(90f));
                    EditorGUILayout.LabelField(path.Length.ToString("0.0") + "m, " + path.Container.Spline.Count + " knots");

                    if (GUILayout.Button("Select", GUILayout.Width(60f)))
                        Select(path);

                    if (GUILayout.Button("Edit Curve", GUILayout.Width(80f)))
                        EditCurve(path);

                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        LevelAuthoring.RemovePath(path);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.LabelField(LevelPathEditor.DescribeColors(path), EditorStyles.miniLabel);

                if (path.Container.Spline.Count < 2)
                    EditorGUILayout.HelpBox("Path needs at least two knots.", MessageType.Error);

                if (!LevelPathEditor.HasSpawnableColor(path))
                    EditorGUILayout.HelpBox("Path has no color with a positive weight.", MessageType.Warning);
            }
        }

        private static void Select(LevelPath path)
        {
            Selection.activeGameObject = path.gameObject;
            EditorGUIUtility.PingObject(path.gameObject);
        }

        public static void EditCurve(LevelPath path)
        {
            Selection.activeGameObject = path.gameObject;
            ToolManager.SetActiveContext<SplineToolContext>();
        }
    }
}
