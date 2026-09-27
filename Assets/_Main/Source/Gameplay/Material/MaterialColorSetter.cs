using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    public sealed class MaterialColorSetter : MonoBehaviour
    {
        [SerializeField] private Renderer m_Renderer;
        [SerializeField] private Color[] m_Colors;
        [SerializeField] private string m_MaterialPropertyName = "_BaseColor";

        private MaterialPropertyBlock m_PropertyBlock;
        private bool m_HasExternalColors;
        private int m_PropertyId;
        private int m_MaterialCount = -1;

        private void Awake()
        {
            if (!m_HasExternalColors)
                ApplyColors();
        }

        private void OnValidate()
        {
            m_MaterialCount = -1;
            if (!Application.isPlaying)
                ApplyColors();
        }

        public void SetColorIndex(Color color, int index)
        {
            if (m_Colors.Length <= index)
            {
                int previousLength = m_Colors.Length;
                Array.Resize(ref m_Colors, index + 1);
                for (int i = previousLength; i < m_Colors.Length; i++)
                    m_Colors[i] = Color.white;
            }

            m_Colors[index] = color;
            m_HasExternalColors = true;
            ApplyColors();
        }

        private void ApplyColors()
        {
            if (m_MaterialCount < 0)
                CacheRendererInfo();

            if (m_Colors == null || m_Colors.Length == 0 || m_Renderer == null)
                return;

            m_PropertyBlock ??= new MaterialPropertyBlock();

            int count = Mathf.Min(m_Colors.Length, m_MaterialCount);
            for (int index = 0; index < count; index++)
            {
                m_PropertyBlock.Clear();
                m_PropertyBlock.SetColor(m_PropertyId, m_Colors[index]);
                m_Renderer.SetPropertyBlock(m_PropertyBlock, index);
            }

            m_PropertyBlock.Clear();
        }

        private void CacheRendererInfo()
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<Renderer>();

            m_PropertyId = Shader.PropertyToID(m_MaterialPropertyName);
            m_MaterialCount = m_Renderer != null ? m_Renderer.sharedMaterials.Length : 0;
        }
    }
}
