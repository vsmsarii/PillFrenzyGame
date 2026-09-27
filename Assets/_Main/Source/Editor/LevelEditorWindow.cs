using System.Collections.Generic;
using PillFrenzy.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PillFrenzy.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private LevelManifestSO m_Manifest;
        private List<LevelDefinitionSO> m_Levels = new();
        private List<CapsuleColorSO> m_Colors = new();
        private Vector2 m_Scroll;
        private string m_NewColorName = "NewColor";
        private Color m_NewColor = Color.white;

        [MenuItem("PillFrenzy/Level Editor")]
        public static void Open()
        {
            GetWindow<LevelEditorWindow>("Level Editor");
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void Refresh()
        {
            m_Manifest = LevelAuthoring.FindManifest();
            m_Levels = m_Manifest != null ? LevelAuthoring.LoadLevels(m_Manifest) : new List<LevelDefinitionSO>();
            m_Colors = LevelAuthoring.LoadColors();
        }

        private void OnGUI()
        {
            if (m_Manifest == null)
            {
                EditorGUILayout.HelpBox("LevelManifestSO asset not found.", MessageType.Error);
                return;
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawLevels();
            EditorGUILayout.Space(16f);
            DrawColors();
            EditorGUILayout.EndScrollView();
        }

        private void DrawLevels()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);
                if (GUILayout.Button("Manifest", GUILayout.Width(80f)))
                    Selection.activeObject = m_Manifest;
            }

            for (int i = 0; i < m_Levels.Count; i++)
                DrawLevelRow(i, m_Levels[i]);
        }

        private void DrawLevelRow(int index, LevelDefinitionSO definition)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level " + (index + 1), EditorStyles.boldLabel, GUILayout.Width(70f));
                if (definition == null)
                {
                    EditorGUILayout.LabelField("Missing definition");
                    return;
                }

                GameObject layout = definition.Layout.editorAsset;
                EditorGUILayout.LabelField((layout != null ? layout.name : "Default layout") + ", " + definition.TargetQueue.Length + " boxes");

                if (GUILayout.Button("Definition", GUILayout.Width(80f)))
                    Selection.activeObject = definition;

                GUI.enabled = layout != null;
                if (GUILayout.Button("Edit Layout", GUILayout.Width(85f)))
                    AssetDatabase.OpenAsset(layout);

                GUI.enabled = true;
                if (GUILayout.Button(layout != null ? "New Layout" : "Create Layout", GUILayout.Width(95f)))
                    LevelAuthoring.CreateLayoutFor(definition);
            }
        }

        private void DrawColors()
        {
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            for (int i = 0; i < m_Colors.Count; i++)
            {
                CapsuleColorSO color = m_Colors[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(color, typeof(CapsuleColorSO), false);
                    EditorGUILayout.ColorField(GUIContent.none, color.Color, false, false, false, GUILayout.Width(60f));
                }
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                m_NewColorName = EditorGUILayout.TextField(m_NewColorName);
                m_NewColor = EditorGUILayout.ColorField(GUIContent.none, m_NewColor, true, false, false, GUILayout.Width(60f));
                GUI.enabled = !string.IsNullOrWhiteSpace(m_NewColorName);
                if (GUILayout.Button("New Color", GUILayout.Width(85f)))
                {
                    Selection.activeObject = LevelAuthoring.CreateColor(m_NewColorName.Trim(), m_NewColor);
                    Refresh();
                }

                GUI.enabled = true;
            }
        }
    }
}
