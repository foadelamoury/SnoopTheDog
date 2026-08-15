using UnityEngine;
using UnityEngine.Events;
using MalbersAnimations.Controller;

/// <summary>NPC waiting for a delivery. Completes the mission if they are the target destination.</summary>
public class DeliveryClient : MonoBehaviour
{
    [Tooltip("If true, the pizza box is destroyed when the delivery is complete.")]
    [SerializeField] private bool consumePackageOnDelivery = true;

    public UnityEvent OnDeliveryCompleted;

    private BarkAndDeliver.Delivery.DeliveryController deliveryController;

    private void Start()
    {
        deliveryController = FindAnyObjectByType<BarkAndDeliver.Delivery.DeliveryController>();
    }

    /// <summary>
    /// Can be called directly from an InteractZone Unity Event (OnZoneEnter or OnZoneActive)
    /// </summary>
    public void TryCompleteDelivery()
    {
        if (deliveryController == null || deliveryController.ActiveDelivery == null)
            return;

        // Ensure this NPC is the actual destination of the active mission
        if (deliveryController.ActiveDelivery.EndPoint != this.transform)
        {
            Debug.Log("[DeliveryClient] Not the destination for the current delivery.");
            return;
        }

        bool hasItem = false;
        GameObject itemObj = null;

        // 1. Check if Malbers MPickUp has the item
        var dogPickUp = FindAnyObjectByType<MPickUp>();
        if (dogPickUp != null && dogPickUp.Has_Item && dogPickUp.Item != null)
        {
            hasItem = true;
            itemObj = (dogPickUp.Item as Component)?.gameObject;
            dogPickUp.DropItem(); // Force drop
        }
        else
        {
            // 2. Fallback: The delivery controller knows we picked it up, even if Malbers lost track of it!
            hasItem = true; 
        }

        if (hasItem)
        {
            // See if this NPC has a NPCInteractionBridge to play the IK receive animation
            var interactionBridge = GetComponent<BarkAndDeliver.Delivery.NPCInteractionBridge>();
            if (interactionBridge != null)
            {
                // If we don't have the physical item object because of the fallback, pass a dummy or null
                Transform itemTransform = itemObj != null ? itemObj.transform : this.transform; 
                
                interactionBridge.StartReceiveFromDog(itemTransform, () => 
                {
                    CompleteDelivery(itemObj);
                });
            }
            else
            {
                CompleteDelivery(itemObj);
            }
        }
    }

    private void CompleteDelivery(GameObject package)
    {
        if (deliveryController != null && deliveryController.ActiveDelivery != null)
        {
            // Complete by passing the Item ID of the active delivery
            deliveryController.CompleteDelivery(deliveryController.ActiveDelivery.ItemData.ItemId);
        }

        OnDeliveryCompleted.Invoke();

        if (consumePackageOnDelivery && package != null) 
            Destroy(package);
    }
}
