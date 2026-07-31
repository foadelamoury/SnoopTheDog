using MalbersAnimations.Controller;
using UnityEngine;

/// <summary>
/// Sits on a trigger collider representing the dog's carry/backpack zone.
/// Bridges Malbers' MPickUp drop event with whichever DeliveryClient is currently in range,
/// so a drop only completes a delivery when it happens inside the correct client's zone.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickupCollision : MonoBehaviour
{
    [SerializeField] private MPickUp pickUp;

    private DeliveryClient currentClient;

    private void Awake()
    {
        // MPickUp lives on Wolf Main's "Interaction" child, not the root, so a plain
        // GetComponentInParent from a sibling trigger like this one would miss it.
        if (pickUp == null) pickUp = GetComponentInParent<MPickUp>();
        if (pickUp == null) pickUp = transform.root.GetComponentInChildren<MPickUp>();
    }

    private void OnEnable()
    {
        if (pickUp != null) pickUp.OnItemDrop.AddListener(HandleItemDropped);
    }

    private void OnDisable()
    {
        if (pickUp != null) pickUp.OnItemDrop.RemoveListener(HandleItemDropped);
    }

    private void OnTriggerEnter(Collider other)
    {
        var client = other.GetComponentInParent<DeliveryClient>();
        if (client != null) currentClient = client;
    }

    private void OnTriggerExit(Collider other)
    {
        var client = other.GetComponentInParent<DeliveryClient>();
        if (client != null && client == currentClient) currentClient = null;
    }

    private void HandleItemDropped(GameObject item)
    {
        if (currentClient == null) return;

        var pickable = item.GetComponent<Pickable>();
        if (pickable != null && currentClient.CanAccept(pickable.ID))
        {
            currentClient.CompleteDelivery(item);
        }
    }
}
