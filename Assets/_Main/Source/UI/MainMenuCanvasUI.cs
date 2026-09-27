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
        private int m_LevelNumber;
        private bool m_CanPlay;
        private int m_ShownHearts = -1;
        private bool m_PlayRequested;
        private float m_NextRefreshTime;

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

            if (Time.unscaledTime < m_NextRefreshTime)
                return;

            m_NextRefreshTime = Time.unscaledTime + 1f;
            Refresh();
        }

        public void Bind(ISaveService save, int levelNumber, Action play, Action shop, Action settings)
        {
            m_Save = save;
            m_Play = play;
            m_Shop = shop;
            m_Settings = settings;
            m_LevelNumber = levelNumber;
            m_PlayRequested = false;
            m_ShownHearts = -1;

            m_TotalScore.text = "Score " + save.GetTotalScore();

            Refresh();
        }

        private void Refresh()
        {
            m_Save.RefreshHearts();
            int hearts = m_Save.Hearts;
            int maxHearts = m_Save.MaxHearts;
            bool canPlay = hearts > 0;

            if (hearts != m_ShownHearts || canPlay != m_CanPlay)
            {
                m_ShownHearts = hearts;
                m_CanPlay = canPlay;
                ShowHearts(hearts, maxHearts);
                ShowPlayLabel();
            }

            ShowHeartTimer(hearts, maxHearts, m_Save.SecondsUntilNextHeart);
            m_PlayButton.interactable = m_CanPlay && !m_PlayRequested;
            UiText.ShowImmortal(m_Immortal, m_Save.ImmortalRemainingSeconds);
        }

        private void ShowHearts(int hearts, int maxHearts)
        {
            UiText.Begin();
            UiText.Append("Hearts ");
            if (hearts > maxHearts)
            {
                UiText.Append(maxHearts);
                UiText.Append("(+");
                UiText.Append(hearts - maxHearts);
                UiText.Append(')');
            }
            else
            {
                UiText.Append(hearts);
                UiText.Append('/');
                UiText.Append(maxHearts);
            }

            UiText.ApplyTo(m_Hearts);
        }

        private void ShowPlayLabel()
        {
            if (m_CanPlay)
                UiText.SetValue(m_PlayLabel, "LEVEL ", m_LevelNumber);
            else
                m_PlayLabel.text = "NO HEARTS";
        }

        private void ShowHeartTimer(int hearts, int maxHearts, long secondsUntilNext)
        {
            if (hearts < maxHearts && secondsUntilNext > 0)
            {
                UiText.Begin();
                UiText.Append("Next heart ");
                UiText.AppendClock(secondsUntilNext);
                UiText.ApplyTo(m_HeartTimer);
                return;
            }

            m_HeartTimer.text = hearts <= 0 ? "No hearts - visit Shop" : string.Empty;
        }

        private void OnPlayClicked()
        {
            if (m_PlayRequested)
                return;

            m_PlayRequested = true;
            m_PlayButton.interactable = false;
            m_Play.Invoke();
        }

        private void OnShopClicked()
        {
            m_Shop.Invoke();
        }

        private void OnSettingsClicked()
        {
            m_Settings.Invoke();
        }
    }
}
