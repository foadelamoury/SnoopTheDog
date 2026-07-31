using UnityEngine;
using System.Collections.Generic;

public class DeliveryQuestManager : MonoBehaviour
{
    [Header("Quest Elements")]
    public Transform pickupItem;
    public Transform deliveryCube;
    public Transform pathParent;
    
    [Header("UI Elements")]
    public GameObject clockBackgroundUI;
    public TMPro.TextMeshProUGUI congratsText;
    public DeliveryTimer deliveryTimer;
    public MiniMap miniMap;

    [Header("Path Settings")]
    public Color waypointDotColor = Color.yellow;
    public Color waypointLineColor = new Color(1f, 0.8f, 0f, 0.7f);

    private void Start()
    {
        // Draw the full path on the minimap
        if (miniMap != null)
        {
            List<Vector3> waypoints = new List<Vector3>();
            
            // 1. Add Pickup Item position
            if (pickupItem != null) waypoints.Add(pickupItem.position);
            
            // 2. Add intermediate waypoints
            if (pathParent != null)
            {
                for (int i = 0; i < pathParent.childCount; i++)
                {
                    waypoints.Add(pathParent.GetChild(i).position);
                }
            }
            
            // 3. Add Delivery Cube position
            if (deliveryCube != null) waypoints.Add(deliveryCube.position);

            miniMap.DrawWaypointPath(waypoints, waypointDotColor, waypointLineColor);
        }

        // Initialize UI state
        if (clockBackgroundUI != null) clockBackgroundUI.SetActive(false);
        if (congratsText != null) congratsText.gameObject.SetActive(false);
    }

    public void OnQuestStarted()
    {
        Debug.Log("[DeliveryQuestManager] Quest Started!");
        if (clockBackgroundUI != null) clockBackgroundUI.SetActive(true);
        if (deliveryTimer != null) deliveryTimer.StartTimer();
    }

    public void OnQuestCompleted()
    {
        Debug.Log("[DeliveryQuestManager] Quest Completed!");
        
        // Hide clock
        if (clockBackgroundUI != null) clockBackgroundUI.SetActive(false);
        
        // Stop timer
        if (deliveryTimer != null) deliveryTimer.PauseTimer();

        // Show congrats message
        if (congratsText != null) congratsText.gameObject.SetActive(true);

        // Optionally clear path
        if (miniMap != null) miniMap.ClearWaypointPath();
    }
}
