using UnityEngine;
using UnityEngine.UI;
using System;
using BarkAndDeliver.Delivery;

/// <summary>
/// Delivery countdown timer with a spinning clock visual.
/// Pure view (MVC) that implements IDeliveryView.
/// Driven entirely by the DeliveryController.
/// </summary>
public class DeliveryTimer : MonoBehaviour, IDeliveryView
{
    // ──────────────────────────────────────────────
    //  CONFIGURATION
    // ──────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("The parent GameObject holding the clock background (so it hides when inactive).")]
    [SerializeField] private GameObject backgroundPanel;

    [Tooltip("The RectTransform of the clock hand / dial image that will rotate.")]
    [SerializeField] private RectTransform clockHandTransform;

    [Tooltip("(Optional) A Text or TextMeshPro component to display remaining time.")]
    [SerializeField] private TMPro.TextMeshProUGUI timerText;

    [Tooltip("(Optional) An Image component to use as a radial fill indicator.")]
    [SerializeField] private Image radialFillImage;

    [Header("Visual Polish")]
    [Tooltip("Pulse the clock hand color when time is running low.")]
    [SerializeField] private bool enableUrgencyPulse = true;

    [Tooltip("Time threshold (in seconds) to start the urgency pulse.")]
    [SerializeField] private float urgencyThreshold = 10f;

    [Tooltip("Color to pulse toward when time is running out.")]
    [SerializeField] private Color urgencyColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Tooltip("Speed of the urgency pulse oscillation.")]
    [SerializeField] private float pulseSpeed = 4f;

    // ──────────────────────────────────────────────
    //  RUNTIME STATE
    // ──────────────────────────────────────────────

    private Color originalHandColor;
    private Image clockHandImage;
    private bool isVisible = false;

    // ──────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // Auto-find ClockBackground if the user forgot to assign it!
        if (backgroundPanel == null)
        {
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allTransforms)
            {
                if (t.name == "ClockBackground" && t.parent != null && t.parent.name.Contains("UI"))
                {
                    backgroundPanel = t.gameObject;
                    break;
                }
            }
        }

        if (clockHandTransform != null)
        {
            clockHandImage = clockHandTransform.GetComponent<Image>();
            if (clockHandImage != null)
                originalHandColor = clockHandImage.color;
        }

        if (radialFillImage != null)
        {
            radialFillImage.type = Image.Type.Filled;
            radialFillImage.fillMethod = Image.FillMethod.Radial360;
            radialFillImage.fillOrigin = (int)Image.Origin360.Top;
            radialFillImage.fillClockwise = false;
            radialFillImage.fillAmount = 1f;
        }
        
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        
        // Toggle UI elements
        if (backgroundPanel != null) backgroundPanel.SetActive(visible);
        if (clockHandTransform != null) clockHandTransform.gameObject.SetActive(visible);
        if (timerText != null) timerText.gameObject.SetActive(visible);
        if (radialFillImage != null) radialFillImage.gameObject.SetActive(visible);
    }

    // ──────────────────────────────────────────────
    //  IDeliveryView Implementation
    // ──────────────────────────────────────────────

    public void OnDeliveryStarted(DeliveryModel model)
    {
        Debug.Log("🔔 [DeliveryTimer] OnDeliveryStarted was triggered! The UI should now become visible.");
        SetVisible(true);
        if (clockHandImage != null) clockHandImage.color = originalHandColor;
        UpdateVisuals(model.RemainingTime, 0f);
    }

    public void OnDeliveryCompleted(DeliveryModel model)
    {
        SetVisible(false);
    }

    public void OnDeliveryFailed(DeliveryModel model)
    {
        SetVisible(false);
    }

    public void OnTimerTick(float remainingTime, float normalized)
    {
        if (!isVisible) return;
        UpdateVisuals(remainingTime, normalized);
    }

    // ──────────────────────────────────────────────
    //  VISUAL UPDATES
    // ──────────────────────────────────────────────

    private void UpdateVisuals(float remainingTime, float progress)
    {
        UpdateClockHandRotation(progress);
        UpdateTimerText(remainingTime);

        if (radialFillImage != null)
            radialFillImage.fillAmount = 1f - progress;

        // Urgency pulse
        if (enableUrgencyPulse && clockHandImage != null && remainingTime <= urgencyThreshold)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            clockHandImage.color = Color.Lerp(originalHandColor, urgencyColor, t);
        }
    }

    private void UpdateClockHandRotation(float progress)
    {
        if (clockHandTransform == null) return;

        // One full 360° sweep mapped to the delivery duration.
        // 0% progress = 0°, 100% progress = -360° (clockwise).
        float targetAngle = -progress * 360f;
        clockHandTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    private void UpdateTimerText(float remainingTime)
    {
        if (timerText == null) return;

        if (remainingTime >= 60f)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
        else
        {
            // Show one decimal when under a minute for extra tension
            timerText.text = remainingTime.ToString("F1") + "s";
        }
    }
}
