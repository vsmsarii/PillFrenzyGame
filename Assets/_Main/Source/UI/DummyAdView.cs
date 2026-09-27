using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class DummyAdView : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text m_Title;
        [SerializeField] private TMP_Text m_AdUnit;

        [Header("Close")]
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private TMP_Text m_CloseLabel;

        [Header("Labels")]
        [SerializeField] private string m_TestPrefix = "TEST";
        [SerializeField] private string m_InterstitialCloseText = "CLOSE";
        [SerializeField] private string m_RewardedCloseText = "CLAIM REWARD";
        [SerializeField] private string m_MissingAdUnitText = "no ad unit id";

        public UniTask ShowAsync(EAdType type, string adUnitId, bool testAds, CancellationToken cancellationToken)
        {
            string adName = type.ToString().ToUpperInvariant() + " AD";
            m_Title.text = testAds ? m_TestPrefix + " " + adName : adName;
            m_AdUnit.text = string.IsNullOrEmpty(adUnitId) ? m_MissingAdUnitText : adUnitId;
            m_CloseLabel.text = type == EAdType.Rewarded ? m_RewardedCloseText : m_InterstitialCloseText;

            UniTaskCompletionSource closed = new UniTaskCompletionSource();
            m_CloseButton.onClick.AddListener(() => closed.TrySetResult());
            return closed.Task.AttachExternalCancellation(cancellationToken);
        }
    }
}
