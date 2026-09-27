using System;
using UnityEngine;

namespace PillFrenzy.Gameplay
{
    [Serializable]
    public struct TutorialPage
    {
        [SerializeField] private string m_Title;
        [SerializeField, TextArea(2, 5)] private string m_Body;
        [SerializeField] private Sprite m_Image;

        public string Title => m_Title;
        public string Body => m_Body;
        public Sprite Image => m_Image;
    }
}
