using UnityEngine;

/// <summary>
/// Automatically registers this object's child transforms as a waypoint path on the minimap.
/// Place on the parent of ordered waypoint GameObjects.
/// Compatible with Kurt Dekker's WaypointGizmo system.
/// </summary>
public class MiniMapWaypointRegistrar : MonoBehaviour
{
    [Tooltip("Color of the dots at each waypoint")]
    [SerializeField] private Color _dotColor = Color.yellow;

    [Tooltip("Color of the lines connecting waypoints")]
    [SerializeField] private Color _lineColor = new Color(1f, 0.8f, 0f, 0.7f); // semi-transparent gold

    [Tooltip("Optional: Direct reference to the MiniMap. If empty, finds it automatically.")]
    [SerializeField] private MiniMap _miniMap;

    private void Start()
    {
        if (_miniMap == null)
            _miniMap = FindAnyObjectByType<MiniMap>();

        if (_miniMap != null && transform.childCount > 0)
        {
            _miniMap.DrawWaypointPath(transform, _dotColor, _lineColor);
        }
        else if (_miniMap == null)
        {
            Debug.LogWarning($"MiniMapWaypointRegistrar on '{name}': No MiniMap found in scene!");
        }
    }
}
