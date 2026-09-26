using System.Collections.Generic;
using System.Text;
using PillFrenzy.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PillFrenzy.Editor
{
    [CustomEditor(typeof(LevelPath))]
    public sealed class LevelPathEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LevelPath path = (LevelPath)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Spawn Share", DescribeColors(path), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Length", path.Length.ToString("0.00") + "m");

            if (!HasSpawnableColor(path))
                EditorGUILayout.HelpBox("Add at least one color with a positive weight.", MessageType.Warning);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Edit Curve", GUILayout.Height(26f)))
                    LevelLayoutEditor.EditCurve(path);

                if (GUILayout.Button("Reverse Direction", GUILayout.Height(26f)))
                    LevelAuthoring.ReversePath(path);
            }
        }

        public static string DescribeColors(LevelPath path)
        {
            IReadOnlyList<PathColorWeight> colors = path.Colors;
            float total = 0f;
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i].Color != null)
                    total += colors[i].Weight;
            }

            if (total <= 0f)
                return "No colors";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < colors.Count; i++)
            {
                PathColorWeight entry = colors[i];
                if (entry.Color == null || entry.Weight <= 0f)
                    continue;

                if (builder.Length > 0)
                    builder.Append("  ");

                builder.Append(entry.Color.name).Append(' ').Append(Mathf.RoundToInt(entry.Weight / total * 100f)).Append('%');
            }

            return builder.ToString();
        }

        public static bool HasSpawnableColor(LevelPath path)
        {
            IReadOnlyList<PathColorWeight> colors = path.Colors;
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i].Color != null && colors[i].Weight > 0f)
                    return true;
            }

            return false;
        }
    }
}
