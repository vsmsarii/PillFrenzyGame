using PillFrenzy.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TutorialOverlayUI : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_Body;
        [SerializeField] private Image m_Image;

        [Header("Prompt")]
        [SerializeField] private TMP_Text m_TapPrompt;
        [SerializeField] private string m_TapPromptText = "TAP TO CONTINUE";

        [Header("Pointer")]
        [SerializeField] private RectTransform m_Pointer;
        [SerializeField] private Vector2 m_PointerOffset;

        private Canvas m_RootCanvas;
        private Camera m_WorldCamera;
        private Transform m_Anchor;

        private void Awake()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            m_RootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        private void OnDisable()
        {
            m_Anchor = null;
        }

        private void LateUpdate()
        {
            UpdatePointer();
        }

        public void Show(TutorialMomentView view)
        {
            TutorialPage page = view.Page;

            m_Title.gameObject.SetActive(!string.IsNullOrEmpty(page.Title));
            m_Title.text = page.Title;
            m_Body.text = page.Body;
            m_Image.gameObject.SetActive(page.Image != null);
            m_Image.sprite = page.Image;
            m_TapPrompt.gameObject.SetActive(view.ShowTapPrompt);
            m_TapPrompt.text = m_TapPromptText;

            m_Anchor = view.ShowPointer ? view.Anchor : null;
            UpdatePointer();
        }

        private void UpdatePointer()
        {
            Vector2 localPoint = default;
            bool visible = m_Anchor != null
                && m_Anchor.gameObject.activeInHierarchy
                && TryGetLocalPoint(m_Anchor.position, out localPoint);

            if (m_Pointer.gameObject.activeSelf != visible)
                m_Pointer.gameObject.SetActive(visible);

            if (visible)
                m_Pointer.localPosition = localPoint + m_PointerOffset;
        }

        private bool TryGetLocalPoint(Vector3 worldPosition, out Vector2 localPoint)
        {
            localPoint = default;
            if (m_WorldCamera == null)
                m_WorldCamera = Camera.main;

            if (m_WorldCamera == null || !(m_Pointer.parent is RectTransform parent))
                return false;

            Vector3 screenPoint = m_WorldCamera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0f)
                return false;

            Camera uiCamera = m_RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : m_RootCanvas.worldCamera;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out localPoint);
        }
    }
}
