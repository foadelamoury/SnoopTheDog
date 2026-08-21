using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BarkAndDeliver.UI
{
    public class UI_MainMenu : UI
    {
        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Main";

        [Header("Panels")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;

        public void OnPlayClicked() => SceneManager.LoadScene(gameplaySceneName);

        public void OnOptionsClicked()
        {
            if (optionsPanel != null) optionsPanel.SetActive(true);
        }

        public void OnCreditsClicked()
        {
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }
    }
}
