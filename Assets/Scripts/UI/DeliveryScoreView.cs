using UnityEngine;
using TMPro;
using BarkAndDeliver.Delivery;

namespace BarkAndDeliver.UI
{
    /// <summary>
    /// Updates a TextMeshProUGUI element with the current delivery score.
    /// </summary>
    public class DeliveryScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private string prefix = "Score: ";

        private DeliveryController controller;

        private void Start()
        {
            if (scoreText == null) scoreText = GetComponent<TextMeshProUGUI>();

            controller = FindAnyObjectByType<DeliveryController>();
            if (controller != null)
            {
                controller.OnScoreChanged += UpdateScoreDisplay;
                UpdateScoreDisplay(controller.Score);
            }
            else
            {
                Debug.LogWarning("[DeliveryScoreView] No DeliveryController found!");
            }
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnScoreChanged -= UpdateScoreDisplay;
            }
        }

        private void UpdateScoreDisplay(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{prefix}{newScore}";
            }
        }
    }
}
