using UnityEngine;
using UnityEngine.Events;
using MalbersAnimations.Controller;

/// <summary>NPC waiting for a specific package (matched by its Pickable.ID) to be delivered.</summary>
public class DeliveryClient : MonoBehaviour
{
    [SerializeField] private int requiredPackageId;
    [SerializeField] private bool consumePackageOnDelivery = true;

    public UnityEvent OnDeliveryCompleted;

    public bool IsWaitingForDelivery { get; private set; } = true;

    public bool CanAccept(int packageId) => IsWaitingForDelivery && packageId == requiredPackageId;

    public void CompleteDelivery(GameObject package)
    {
        if (!IsWaitingForDelivery) return;

        IsWaitingForDelivery = false;
        OnDeliveryCompleted.Invoke();

        if (consumePackageOnDelivery) Destroy(package);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsWaitingForDelivery) return;

        // See if the object entering the cube belongs to a character with MPickUp
        var dogPickUp = other.transform.root.GetComponentInChildren<MPickUp>();
        if (dogPickUp != null && dogPickUp.Has_Item)
        {
            // Check if the item the dog is holding matches the required ID
            var heldItem = dogPickUp.Item as Pickable;
            if (heldItem != null && heldItem.ID == requiredPackageId)
            {
                // Force the dog to drop the item so we can deliver it!
                dogPickUp.DropItem();
                
                // Complete the delivery instantly!
                CompleteDelivery(heldItem.gameObject);
            }
        }
    }
}
