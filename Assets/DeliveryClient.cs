using UnityEngine;
using UnityEngine.Events;

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
}
