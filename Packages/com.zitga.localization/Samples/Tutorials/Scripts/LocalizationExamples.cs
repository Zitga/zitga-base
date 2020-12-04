using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Zitga.LocalizeTools.Tutorials
{
    public class LocalizationExamples : MonoBehaviour
    {
        [SerializeField]
        private Button english;
        [SerializeField]
        private Button indonesia;
        [SerializeField]
        private Text content;

        private int score = 0;

        private Localization localization;

        private EventHandler languageChanged;

        void Awake ()
        {
            localization = Localization.Current;
            
            localization.CultureInfo = Locale.GetCultureInfoByLanguage(SystemLanguage.English);
            
            InitButton();

            InitListener();
        }

        private void InitListener()
        {
            languageChanged = (sender, e) =>
            {
                UpdateText(sender, e).Forget();
            };

            localization.CultureInfoChanged += languageChanged;
        }

        private void InitButton()
        {
            english.onClick.AddListener(OnClickEn);
            indonesia.onClick.AddListener(OnClickId);
        }

        private void OnClickEn()
        {
            localization.CultureInfo = Locale.GetCultureInfoByLanguage(SystemLanguage.English);
        }

        private void OnClickId()
        {
            localization.CultureInfo = Locale.GetCultureInfoByLanguage(SystemLanguage.Indonesian);
        }
        
        private async UniTaskVoid UpdateText(object sender, EventArgs e)
        {
            var value = await R.mail.event_arena_subject;
            score++;
            content.text = value + " " + score;
        }
    }
}