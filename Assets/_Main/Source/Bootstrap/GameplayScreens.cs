using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PillFrenzy.Core;
using PillFrenzy.Gameplay;
using PillFrenzy.UI;
using UnityEngine;

namespace PillFrenzy.Bootstrap
{
    public static class GameplayScreens
    {
        private const int HudLayer = 0;
        private const int ResultLayer = 1;

        public static async UniTask<bool> OpenHudAsync(GameplaySession session, int levelNumber, ISaveService save, Action openSettings)
        {
            GameObject panel = await UIPanels.OpenAsync(EUIPanel.Gameplay, HudLayer, cancellationToken: session.Token);
            if (panel == null)
                return false;

            GameplayCanvasUI hud = panel.GetComponent<GameplayCanvasUI>();
            hud.BindLevel(levelNumber);
            hud.BindPowers(session.Powers, save);
            hud.BindSettings(openSettings);
            return true;
        }

        public static async UniTask ShowResultAsync(RunEnded result, Action onContinue, Action onRetry, Action onMenu, CancellationToken token)
        {
            EUIPanel panelId = result.IsComplete ? EUIPanel.Win : EUIPanel.Lose;
            GameObject panel = await UIPanels.OpenAsync(panelId, ResultLayer, cancellationToken: token);
            if (panel == null)
                return;

            if (result.IsComplete)
                panel.GetComponent<WinCanvasUI>().Show(result.Score, result.BestCombo, onContinue);
            else
                panel.GetComponent<LoseCanvasUI>().Show(result.Score, result.BestCombo, onRetry, onMenu);
        }
    }
}
