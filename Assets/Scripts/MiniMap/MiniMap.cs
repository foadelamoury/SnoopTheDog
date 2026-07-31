using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MalbersAnimations.Controller;

/// <summary>
/// 2D UI Mini-Map that scrolls a world sprite based on player position.
/// Supports waypoint path rendering.
/// </summary>
public class MiniMap : MonoBehaviour
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

    /// <summary>
    /// Scrolls the map image so the player appears centered.
    /// 3D X -> UI X, 3D Z -> UI Y (top-down projection).
    /// </summary>
    private void UpdateMapPosition(Vector3 playerWorldPos)
    {
        _mapRectTransform.localPosition = new Vector3(
            _mapSpeed * playerWorldPos.x,
            _mapSpeed * playerWorldPos.z,
            0f
        );
    }

    // ───────────────────────── Waypoint Paths ─────────────────────────

    /// <summary>
    /// Draws a series of dots and connecting lines for a waypoint path on the minimap.
    /// Call this once per path (e.g., on Start or when paths are defined).
    /// </summary>
    /// <param name="waypoints">Ordered list of world-space positions</param>
    /// <param name="dotColor">Color for waypoint dots</param>
    /// <param name="lineColor">Color for connecting lines</param>
    public void ClearWaypointPath()
    {
        if (_waypointParent == null) return;
        
        // Destroy all drawn dots and lines
        for (int i = _waypointParent.childCount - 1; i >= 0; i--)
        {
            Destroy(_waypointParent.GetChild(i).gameObject);
        }
    }

    public void DrawWaypointPath(List<Vector3> waypoints, Color dotColor, Color lineColor)
    {
        if (_waypointParent == null || waypoints == null || waypoints.Count == 0) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            // Draw dot at each waypoint
            if (_waypointDotPrefab != null)
            {
                GameObject dot = Instantiate(_waypointDotPrefab, _waypointParent);
                dot.GetComponent<RectTransform>().localPosition = WorldToMapLocal(waypoints[i]);

                if (dot.TryGetComponent<Image>(out var img))
                    img.color = dotColor;
            }

            // Draw line segment between consecutive waypoints
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

    /// <summary>
    /// Draws a waypoint path from a parent Transform's children (compatible with WaypointGizmo).
    /// </summary>
    public void DrawWaypointPath(Transform waypointParent, Color dotColor, Color lineColor)
    {
        if (waypointParent == null) return;

        List<Vector3> positions = new();
        for (int i = 0; i < waypointParent.childCount; i++)
            positions.Add(waypointParent.GetChild(i).position);

        DrawWaypointPath(positions, dotColor, lineColor);
    }

    /// <summary>
    /// Creates a stretched and rotated UI Image between two minimap-local points.
    /// </summary>
    private void DrawLineSegment(Vector3 from, Vector3 to, Color color)
    {
        GameObject line = Instantiate(_pathLinePrefab, _waypointParent);
        RectTransform rt = line.GetComponent<RectTransform>();

        // Position at midpoint
        Vector3 midpoint = (from + to) / 2f;
        rt.localPosition = midpoint;

        // Calculate length and angle
        Vector3 diff = to - from;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        // Stretch and rotate
        rt.sizeDelta = new Vector2(distance, rt.sizeDelta.y); // keep height (line thickness)
        rt.localRotation = Quaternion.Euler(0, 0, angle);

        if (line.TryGetComponent<Image>(out var img))
            img.color = color;
    }

    /// <summary>
    /// Converts a world position to a local position on the map image.
    /// Since waypoints are children of the map, they don't need the scroll offset.
    /// </summary>
    private Vector3 WorldToMapLocal(Vector3 worldPos)
    {
        // mapSpeed is negative, so we negate it here because child elements
        // already move with the parent map image
        return new Vector3(
            -_mapSpeed * worldPos.x,
            -_mapSpeed * worldPos.z,
            0f
        );
    }
}
