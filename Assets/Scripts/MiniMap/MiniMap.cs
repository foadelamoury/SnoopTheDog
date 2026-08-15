using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MalbersAnimations.Controller;
using BarkAndDeliver.Delivery;

/// <summary>
/// 2D UI Mini-Map that scrolls a world sprite based on player position.
/// Implements IDeliveryView to dynamically draw the waypoint path ONLY when the dog holds the item.
/// </summary>
public class MiniMap : MonoBehaviour, IDeliveryView
{
    [Header("References")]
    [Tooltip("The map Image that scrolls (child of the mask)")]
    [SerializeField] private RectTransform _mapRectTransform;

    [Tooltip("Optional: Drag the player transform here. If empty, uses MAnimal.MainAnimal")]
    [SerializeField] private Transform _playerTransform;

    [Header("Map Settings")]
    [Tooltip("Scale multiplier: -(miniMapSize / worldSize). Negative = correct scroll direction")]
    [SerializeField] private float _mapSpeed = -2f;

    [Header("Waypoint Paths")]
    [Tooltip("Prefab for waypoint dot (small yellow circle UI Image)")]
    [SerializeField] private GameObject _waypointDotPrefab;

    [Tooltip("Prefab for path line segment (stretched Image between waypoints)")]
    [SerializeField] private GameObject _pathLinePrefab;

    [Tooltip("Parent transform for waypoint visuals (should be child of Map Image)")]
    [SerializeField] private RectTransform _waypointParent;
    
    [Header("Path Visuals")]
    [SerializeField] private Color dotColor = Color.yellow;
    [SerializeField] private Color lineColor = new Color(1f, 0.8f, 0f, 0.7f);

    // Cached player reference
    private Transform _cachedPlayer;

    private void Start()
    {
        // Find player via Malbers MainAnimal if not assigned
        if (_playerTransform == null && MAnimal.MainAnimal != null)
            _playerTransform = MAnimal.MainAnimal.transform;

        _cachedPlayer = _playerTransform;
    }

    private void LateUpdate()
    {
        // Fallback: try to find player if not yet found
        if (_cachedPlayer == null)
        {
            if (MAnimal.MainAnimal != null)
                _cachedPlayer = MAnimal.MainAnimal.transform;
            else
                return;
        }

        // Scroll map based on player world position
        UpdateMapPosition(_cachedPlayer.position);
    }

    private void UpdateMapPosition(Vector3 playerWorldPos)
    {
        _mapRectTransform.localPosition = new Vector3(
            _mapSpeed * playerWorldPos.x,
            _mapSpeed * playerWorldPos.z,
            0f
        );
    }

    // ── IDeliveryView Implementation ──────────────────────────────────────

    public void OnDeliveryStarted(DeliveryModel model)
    {
        // Guard: fallback deliveries may not have scene transforms
        if (model.StartPoint == null || model.EndPoint == null)
        {
            Debug.LogWarning("[MiniMap] Delivery model has no Start/End points — skipping minimap path.");
            return;
        }

        // Draw the path from Start to End when the dog picks it up!
        List<Vector3> waypoints = new List<Vector3>
        {
            model.StartPoint.position,
            model.EndPoint.position
        };
        
        DrawWaypointPath(waypoints, dotColor, lineColor);
    }

    public void OnDeliveryCompleted(DeliveryModel model)
    {
        ClearWaypointPath();
    }

    public void OnDeliveryFailed(DeliveryModel model)
    {
        ClearWaypointPath();
    }

    public void OnTimerTick(float remainingTime, float normalized)
    {
        // Minimap doesn't care about the timer ticks
    }

    // ───────────────────────── Waypoint Paths ─────────────────────────

    public void ClearWaypointPath()
    {
        if (_waypointParent == null) return;
        
        for (int i = _waypointParent.childCount - 1; i >= 0; i--)
        {
            Destroy(_waypointParent.GetChild(i).gameObject);
        }
    }

    private void DrawWaypointPath(List<Vector3> waypoints, Color dotColor, Color lineColor)
    {
        ClearWaypointPath(); // Always clear old paths first

        if (_waypointParent == null || waypoints == null || waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            // Draw dot
            if (_waypointDotPrefab != null)
            {
                GameObject dot = Instantiate(_waypointDotPrefab, _waypointParent);
                dot.GetComponent<RectTransform>().localPosition = WorldToMapLocal(waypoints[i]);

                if (dot.TryGetComponent<Image>(out var img))
                    img.color = dotColor;
            }

            // Draw line segment
            if (i > 0 && _pathLinePrefab != null)
            {
                DrawLineSegment(
                    WorldToMapLocal(waypoints[i - 1]),
                    WorldToMapLocal(waypoints[i]),
                    lineColor
                );
            }
        }
    }

    private void DrawLineSegment(Vector3 from, Vector3 to, Color color)
    {
        GameObject line = Instantiate(_pathLinePrefab, _waypointParent);
        RectTransform rt = line.GetComponent<RectTransform>();

        Vector3 midpoint = (from + to) / 2f;
        rt.localPosition = midpoint;

        Vector3 diff = to - from;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rt.sizeDelta = new Vector2(distance, rt.sizeDelta.y);
        rt.localRotation = Quaternion.Euler(0, 0, angle);

        if (line.TryGetComponent<Image>(out var img))
            img.color = color;
    }

    private Vector3 WorldToMapLocal(Vector3 worldPos)
    {
        return new Vector3(
            -_mapSpeed * worldPos.x,
            -_mapSpeed * worldPos.z,
            0f
        );
    }
}
