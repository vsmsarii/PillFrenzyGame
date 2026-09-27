using TMPro;

namespace PillFrenzy.UI
{
    public static class UiText
    {
        private const long SecondsPerHour = 3600;
        private const long SecondsPerMinute = 60;

        private static readonly char[] s_Buffer = new char[256];
        private static readonly char[] s_Digits = new char[20];
        private static int s_Length;

        public static void Begin()
        {
            s_Length = 0;
        }

        public static void Append(string text)
        {
            for (int i = 0; i < text.Length; i++)
                s_Buffer[s_Length++] = text[i];
        }

        public static void Append(char character)
        {
            s_Buffer[s_Length++] = character;
        }

        public static void Append(long value, int minDigits = 1)
        {
            if (value < 0)
            {
                Append('-');
                value = -value;
            }

            int digitCount = 0;
            do
            {
                s_Digits[digitCount++] = (char)('0' + value % 10);
                value /= 10;
            }
            while (value > 0 || digitCount < minDigits);

            while (digitCount > 0)
                s_Buffer[s_Length++] = s_Digits[--digitCount];
        }

        public static void AppendClock(long totalSeconds)
        {
            long hours = totalSeconds / SecondsPerHour;
            if (hours > 0)
            {
                Append(hours);
                Append(':');
            }

            Append(totalSeconds % SecondsPerHour / SecondsPerMinute, 2);
            Append(':');
            Append(totalSeconds % SecondsPerMinute, 2);
        }

        public static void ApplyTo(TMP_Text label)
        {
            label.SetText(s_Buffer, 0, s_Length);
        }

        public static void SetValue(TMP_Text label, string prefix, long value)
        {
            Begin();
            Append(prefix);
            Append(value);
            ApplyTo(label);
        }

        public static void ShowImmortal(TMP_Text label, long remainingSeconds)
        {
            bool active = remainingSeconds > 0;
            if (label.gameObject.activeSelf != active)
                label.gameObject.SetActive(active);

            if (!active)
                return;

            Begin();
            Append("Immortal ");
            AppendClock(remainingSeconds);
            ApplyTo(label);
        }
    }
}
