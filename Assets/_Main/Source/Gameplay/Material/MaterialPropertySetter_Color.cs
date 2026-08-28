using UnityEngine;

namespace PillFrenzy.Material
{
    
    public class MaterialPropertySetter_Color : MonoBehaviour
    {
        [SerializeField] private Renderer m_Renderer;

        [SerializeField] private Color[] m_Colors;
        [SerializeField] private string m_MaterialPropertyName = "_BaseColor";

        private MaterialPropertyBlock m_PropertyBlock;
        private bool m_HasExternalColors;

        private void Awake()
        {
            if (!m_HasExternalColors)
                ApplyColors();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                ApplyColors();
        }

        private void ApplyColors()
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<Renderer>();

            if (m_Colors == null || m_Colors.Length == 0 || m_Renderer == null)
                return;

            m_PropertyBlock ??= new MaterialPropertyBlock();

            int materialCount = m_Renderer.sharedMaterials.Length;
            for (int index = 0; index < m_Colors.Length; index++)
            {
                if (materialCount <= index)
                    break;

                m_PropertyBlock.Clear();
                m_PropertyBlock.SetColor(m_MaterialPropertyName, m_Colors[index]);
                m_Renderer.SetPropertyBlock(m_PropertyBlock, index);
            }

            m_PropertyBlock.Clear();
        }

        public void SetColors(Color[] colors)
        {
            m_Colors = colors;
            m_HasExternalColors = colors != null && colors.Length > 0;
            ApplyColors();
        }
        public void SetColorIndex(Color color, int index)
        {
            if (index < 0)
                return;

            if (m_Colors == null || m_Colors.Length <= index)
            {
                Color[] colors = new Color[index + 1];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.white;

                if (m_Colors != null && m_Colors.Length > 0)
                    System.Array.Copy(m_Colors, colors, m_Colors.Length);

                m_Colors = colors;
            }

            m_Colors[index] = color;
            m_HasExternalColors = true;
            ApplyColors();
        }
    }
}
