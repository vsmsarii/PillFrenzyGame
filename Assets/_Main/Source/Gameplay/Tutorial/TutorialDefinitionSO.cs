using System.Collections.Generic;
using PillFrenzy.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace PillFrenzy.Gameplay
{
    [CreateAssetMenu(menuName = "PillFrenzy/Tutorial Definition", fileName = "Tutorial")]
    public sealed class TutorialDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string m_Id;
        [SerializeField] private int m_Order;

        [Header("Condition")]
        [SerializeField, FormerlySerializedAs("m_Trigger")] private ETutorialCondition m_Condition;
        [SerializeField, Min(1)] private int m_LevelNumber = 1;
        [SerializeField] private ECapsuleKind m_CapsuleKind = ECapsuleKind.Poison;
        [SerializeField] private ESpecialPowerId m_SpecialPower;
        [SerializeField] private bool m_RevealsSpecialPower;

        [Header("Moment")]
        [SerializeField] private ETutorialMoment m_Moment;
        [SerializeField, Min(0f)] private float m_EnterDistance = 1f;
        [SerializeField, Min(1)] private int m_CapsuleCount = 5;

        [Header("Spawns")]
        [SerializeField] private ForcedCapsuleSpawn[] m_ForcedSpawns;

        [Header("Presentation")]
        [SerializeField] private ETutorialPresentation m_Presentation;
        [SerializeField] private ETutorialCompletion m_Completion;
        [SerializeField] private bool m_ShowPointer = true;
        [SerializeField] private string m_ContinueLabel;

        [Header("Content")]
        [SerializeField] private TutorialPage[] m_Pages;

        public string Id => m_Id;
        public int Order => m_Order;
        public ETutorialCondition Condition => m_Condition;
        public int LevelNumber => m_LevelNumber;
        public ECapsuleKind CapsuleKind => m_CapsuleKind;
        public ESpecialPowerId SpecialPower => m_SpecialPower;
        public bool RevealsSpecialPower => m_RevealsSpecialPower
            && m_Condition == ETutorialCondition.SpecialPowerUnlocked
            && m_SpecialPower != ESpecialPowerId.None;
        public ETutorialMoment Moment => m_Moment;
        public float EnterDistance => m_EnterDistance;
        public int CapsuleCount => m_CapsuleCount;
        public IReadOnlyList<ForcedCapsuleSpawn> ForcedSpawns => m_ForcedSpawns;
        public ETutorialPresentation Presentation => IsBeforeRun ? ETutorialPresentation.Modal : m_Presentation;
        public string ContinueLabel => m_ContinueLabel;
        public ETutorialCompletion Completion => m_Completion;
        public bool ShowPointer => m_ShowPointer;
        public IReadOnlyList<TutorialPage> Pages => m_Pages;
        public bool HasContent => !string.IsNullOrEmpty(m_Id) && m_Pages.Length > 0;
        public bool IsBeforeRun => m_Moment == ETutorialMoment.BeforeRun;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(m_Id))
                m_Id = name;
        }
    }
}
