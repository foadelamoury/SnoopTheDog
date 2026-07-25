using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

/// <summary>
/// Delivery countdown timer with a spinning clock visual.
/// Attach to a UI GameObject and wire up the clock hand RectTransform.
/// </summary>
public class DeliveryTimer : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  CONFIGURATION
    // ──────────────────────────────────────────────

    [Header("Timer Settings")]
    [Tooltip("Total delivery time in seconds.")]
    [SerializeField] private float deliveryDuration = 60f;

    [Tooltip("If true, the clock hand makes exactly one full 360° sweep over the delivery duration.\n" +
             "If false, the hand spins continuously at a fixed RPM.")]
    [SerializeField] private bool preciseCountdownRotation = true;

    [Tooltip("Revolutions per minute when using continuous spin mode.")]
    [SerializeField] private float continuousSpinRPM = 10f;

    [Header("UI References")]
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

    [Header("Events")]
    [Tooltip("Fired when the timer reaches zero. Use this to trigger 'Delivery Failed'.")]
    public UnityEvent OnTimerExpired;

    [Tooltip("Fired every frame with the normalized progress (0 = full, 1 = expired).")]
    public UnityEvent<float> OnTimerProgressChanged;

    // ──────────────────────────────────────────────
    //  C# DELEGATE (code-side alternative to UnityEvent)
    // ──────────────────────────────────────────────

    /// <summary>Raised when time runs out. Subscribe from other scripts.</summary>
    public event Action OnDeliveryFailed;

    /// <summary>Raised every frame with (remainingTime, normalizedProgress).</summary>
    public event Action<float, float> OnTick;

    // ──────────────────────────────────────────────
    //  RUNTIME STATE
    // ──────────────────────────────────────────────

    private float remainingTime;
    private float currentHandAngle;
    private bool isRunning;
    private bool isPaused;
    private Color originalHandColor;
    private Image clockHandImage;

    // ──────────────────────────────────────────────
    //  PUBLIC PROPERTIES
    // ──────────────────────────────────────────────

    /// <summary>Remaining time in seconds.</summary>
    public float RemainingTime => remainingTime;

    /// <summary>Progress from 0 (just started) to 1 (expired).</summary>
    public float NormalizedProgress => 1f - Mathf.Clamp01(remainingTime / deliveryDuration);

    /// <summary>True if the timer is actively counting down.</summary>
    public bool IsRunning => isRunning && !isPaused;

    /// <summary>True if the timer is paused.</summary>
    public bool IsPaused => isPaused;

    // ──────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ──────────────────────────────────────────────

    private void Awake()
    {
        remainingTime = deliveryDuration;

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
    }

    private void Update()
    {
        if (!isRunning || isPaused)
            return;

        // Count down
        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Max(remainingTime, 0f);

        float progress = NormalizedProgress;

        // Rotate the clock hand
        UpdateClockHandRotation(progress);

        // Update optional text display
        UpdateTimerText();

        // Update optional radial fill
        if (radialFillImage != null)
            radialFillImage.fillAmount = 1f - progress;

        // Urgency pulse
        if (enableUrgencyPulse && clockHandImage != null && remainingTime <= urgencyThreshold)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            clockHandImage.color = Color.Lerp(originalHandColor, urgencyColor, t);
        }

        // Fire tick events
        OnTimerProgressChanged?.Invoke(progress);
        OnTick?.Invoke(remainingTime, progress);

        // Timer expired
        if (remainingTime <= 0f)
        {
            isRunning = false;
            OnTimerExpired?.Invoke();
            OnDeliveryFailed?.Invoke();
        }
    }

    // ──────────────────────────────────────────────
    //  CLOCK HAND ROTATION
    // ──────────────────────────────────────────────

    private void UpdateClockHandRotation(float progress)
    {
        if (clockHandTransform == null)
            return;

        if (preciseCountdownRotation)
        {
            // One full 360° sweep mapped to the delivery duration.
            // 0% progress = 0°, 100% progress = -360° (clockwise).
            float targetAngle = -progress * 360f;
            SetClockHandAngle(targetAngle);
        }
        else
        {
            // Continuous spin at a fixed RPM regardless of remaining time.
            float degreesPerSecond = continuousSpinRPM * 360f / 60f;
            currentHandAngle -= degreesPerSecond * Time.deltaTime;
            SetClockHandAngle(currentHandAngle);
        }
    }

    /// <summary>
    /// Sets the clock hand rotation on the Z-axis using Quaternion.Euler
    /// to avoid gimbal lock and Unity's Euler angle wrapping issues.
    /// </summary>
    private void SetClockHandAngle(float angleDegrees)
    {
        // Using Quaternion directly prevents the inspector from flipping
        // between 0°/360° and avoids gimbal-lock artifacts.
        clockHandTransform.localRotation = Quaternion.Euler(0f, 0f, angleDegrees);
    }

    // ──────────────────────────────────────────────
    //  TEXT DISPLAY
    // ──────────────────────────────────────────────

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

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

    // ──────────────────────────────────────────────
    //  PUBLIC API
    // ──────────────────────────────────────────────

    /// <summary>Starts or restarts the delivery timer.</summary>
    [ContextMenu("Start Delivery Timer")]
    public void StartTimer()
    {
        remainingTime = deliveryDuration;
        currentHandAngle = 0f;
        isRunning = true;
        isPaused = false;

        if (clockHandImage != null)
            clockHandImage.color = originalHandColor;

        SetClockHandAngle(0f);
        UpdateTimerText();

        if (radialFillImage != null)
            radialFillImage.fillAmount = 1f;
    }

    /// <summary>Starts the timer with a custom duration (overrides inspector value).</summary>
    public void StartTimer(float customDuration)
    {
        deliveryDuration = customDuration;
        StartTimer();
    }

    /// <summary>Pauses the countdown. The clock hand freezes.</summary>
    [ContextMenu("Pause Timer")]
    public void PauseTimer()
    {
        if (isRunning)
            isPaused = true;
    }

    /// <summary>Resumes a paused countdown.</summary>
    [ContextMenu("Resume Timer")]
    public void ResumeTimer()
    {
        if (isRunning)
            isPaused = false;
    }

    /// <summary>Stops the timer and resets everything to its initial state.</summary>
    [ContextMenu("Reset Timer")]
    public void ResetTimer()
    {
        isRunning = false;
        isPaused = false;
        remainingTime = deliveryDuration;
        currentHandAngle = 0f;

        if (clockHandImage != null)
            clockHandImage.color = originalHandColor;

        SetClockHandAngle(0f);
        UpdateTimerText();

        if (radialFillImage != null)
            radialFillImage.fillAmount = 1f;
    }

    /// <summary>Adds bonus time to the current countdown (e.g., for picking up a time power-up).</summary>
    public void AddBonusTime(float seconds)
    {
        remainingTime = Mathf.Min(remainingTime + seconds, deliveryDuration);
    }

    /// <summary>Immediately expires the timer (e.g., if the package is destroyed).</summary>
    public void ForceExpire()
    {
        remainingTime = 0f;
    }
}
