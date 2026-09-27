using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PillFrenzy.Core
{
    public sealed class InputService : Service, IInputService
    {
        private const string PressActionName = "Gameplay/Tap";

        private readonly IAssetProvider m_Assets;

        private InputAction m_Press;
        private bool m_HasPending;
        private Vector2 m_PendingPosition;

        public InputService(IAssetProvider assets)
        {
            m_Assets = assets;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            InputActionAsset actions = await m_Assets.LoadAsset<InputActionAsset>(AddressableKeys.InputActions, cancellationToken);
            m_Press = actions.FindAction(PressActionName, true);
            m_Press.started += OnPressed;
            m_Press.Enable();
        }

        public bool TryConsumeTap(out Vector2 screenPosition)
        {
            if (!m_HasPending)
            {
                screenPosition = default;
                return false;
            }

            m_HasPending = false;
            screenPosition = m_PendingPosition;
            return true;
        }

        protected override void OnDispose()
        {
            if (m_Press != null)
            {
                m_Press.started -= OnPressed;
                m_Press.Disable();
                m_Press = null;
            }

            m_HasPending = false;
            m_Assets.ReleaseAsset(AddressableKeys.InputActions);
        }

        private void OnPressed(InputAction.CallbackContext context)
        {
            m_HasPending = true;
            m_PendingPosition = Pointer.current.position.ReadValue();
        }
    }
}
