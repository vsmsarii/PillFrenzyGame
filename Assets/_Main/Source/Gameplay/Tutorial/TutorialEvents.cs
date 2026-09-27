namespace PillFrenzy.Gameplay
{
    public readonly struct TutorialFinished
    {
        public readonly TutorialDefinitionSO Tutorial;

        public TutorialFinished(TutorialDefinitionSO tutorial)
        {
            Tutorial = tutorial;
        }
    }
}
