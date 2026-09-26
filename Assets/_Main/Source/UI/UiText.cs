using TMPro;

namespace PillFrenzy.UI
{
    public static class UiText
    {
        public static string Clock(long totalSeconds)
        {
            return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
        }

        public static void ShowImmortal(TMP_Text label, long remainingSeconds)
        {
            bool active = remainingSeconds > 0;
            label.gameObject.SetActive(active);
            if (active)
                label.text = "Immortal " + Clock(remainingSeconds);
        }
    }
}
