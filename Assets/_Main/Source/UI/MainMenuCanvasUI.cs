using System;
using PillFrenzy.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PillFrenzy.UI
{
    public sealed class MainMenuCanvasUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_TotalScore;
        [SerializeField] private TMP_Text m_Hearts;
        [SerializeField] private TMP_Text m_HeartTimer;
        [SerializeField] private TMP_Text m_Immortal;
        [SerializeField] private Button m_PlayButton;
        [SerializeField] private TMP_Text m_PlayLabel;
        [SerializeField] private Button m_ShopButton;
        [SerializeField] private Button m_SettingsButton;

        private Action m_Play;
        private Action m_Shop;
        private Action m_Settings;
        private ISaveService m_Save;
        private int m_LevelNumber = 1;
        private bool m_CanPlay;
        private bool m_PlayRequested;
        private long m_LastRefreshSecond = -1;

        private void Awake()
        {
            m_PlayButton.onClick.AddListener(OnPlayClicked);
            m_ShopButton.onClick.AddListener(OnShopClicked);
            m_SettingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void Update()
        {
            if (m_Save == null)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now == m_LastRefreshSecond)
                return;

            m_LastRefreshSecond = now;
            Refresh();
        }

        public void Bind(
            ISaveService save,
            int levelNumber,
            int totalScore,
            Action play,
            Action shop,
            Action settings)
        {
            m_Save = save;
            m_Play = play;
            m_Shop = shop;
            m_Settings = settings;
            m_LevelNumber = levelNumber;
            m_PlayRequested = false;

            m_TotalScore.text = "Score " + totalScore;
            m_ShopButton.gameObject.SetActive(shop != null);
            m_SettingsButton.gameObject.SetActive(settings != null);

            Refresh();
        }

        private void Refresh()
        {
            m_Save.RefreshHearts();
            int hearts = m_Save.Hearts;
            int maxHearts = m_Save.MaxHearts;
            m_CanPlay = hearts > 0;

            m_Hearts.text = FormatHearts(hearts, maxHearts);
            m_HeartTimer.text = FormatHeartTimer(hearts, maxHearts, m_Save.SecondsUntilNextHeart);
            m_PlayButton.interactable = m_CanPlay && !m_PlayRequested;
            m_PlayLabel.text = m_CanPlay ? "LEVEL " + m_LevelNumber : "NO HEARTS";
            UiText.ShowImmortal(m_Immortal, m_Save.ImmortalRemainingSeconds);
        }

        private static string FormatHearts(int hearts, int maxHearts)
        {
            if (maxHearts <= 0)
                return "Hearts " + hearts;

            if (hearts > maxHearts)
                return "Hearts " + maxHearts + "(+" + (hearts - maxHearts) + ")";

            return "Hearts " + hearts + "/" + maxHearts;
        }

        private static string FormatHeartTimer(int hearts, int maxHearts, long secondsUntilNext)
        {
            if (hearts < maxHearts && secondsUntilNext > 0)
                return "Next heart " + UiText.Clock(secondsUntilNext);

            return hearts <= 0 ? "No hearts - visit Shop" : string.Empty;
        }

        private void OnPlayClicked()
        {
            if (!m_CanPlay || m_PlayRequested)
                return;

            m_PlayRequested = true;
            m_PlayButton.interactable = false;
            m_Play.Invoke();
        }

        private void OnShopClicked()
        {
            m_Shop?.Invoke();
        }

        private void OnSettingsClicked()
        {
            m_Settings?.Invoke();
        }
    }
}
