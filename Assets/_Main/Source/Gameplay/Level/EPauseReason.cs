using System;

namespace PillFrenzy.Gameplay
{
    [Flags]
    public enum EPauseReason
    {
        None = 0,
        Menu = 1 << 0,
        Application = 1 << 1,
        Tutorial = 1 << 2
    }
}
