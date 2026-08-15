using UnityEngine;

namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Data-only ScriptableObject defining a deliverable item.
    /// Contains no scene references — only serializable metadata.
    /// Scene-level Transforms are wired at runtime via <see cref="DeliverySpawner"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Delivery Item", menuName = "Bark & Deliver/Delivery Item")]
    public class DeliveryItemSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown in the UI.")]
        [SerializeField] private string itemName = "Pizza Box";

        [Tooltip("Unique ID that must match the Pickable.ID on the in-scene item prefab.")]
        [SerializeField] private int itemId;

        [Header("Timer")]
        [Tooltip("Seconds the player has to complete this delivery.")]
        [Min(1f)]
        [SerializeField] private float countdownTime = 60f;

        public void InitFallback(int id, string name, float time)
        {
            itemId = id;
            itemName = name;
            countdownTime = time;
        }

        // ── Public Read-Only API ──────────────────────────────────
        public string ItemName => itemName;
        public int    ItemId   => itemId;
        public float  CountdownTime => countdownTime;
    }
}
