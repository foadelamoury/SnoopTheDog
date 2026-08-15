using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// The 'C' in MVC. Orchestrates quest flow, listens to Malbers interactions,
    /// and updates views.
    /// </summary>
    public class DeliveryController : MonoBehaviour
    {
        public int Score { get; private set; }
        public event Action<int> OnScoreChanged;

        private List<DeliveryModel> allDeliveries = new List<DeliveryModel>();
        private List<IDeliveryView> views = new List<IDeliveryView>();

        public DeliveryModel ActiveDelivery => activeDelivery;
        private DeliveryModel activeDelivery;

        private void Awake()
        {
            // Find all views in the scene
            views = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).OfType<IDeliveryView>().ToList();
        }

        private void Update()
        {
            if (activeDelivery != null && (activeDelivery.State == DeliveryState.PickedUp || activeDelivery.State == DeliveryState.InTransit))
            {
                activeDelivery.Tick(Time.deltaTime);
                
                // Tick might have caused the delivery to fail (time ran out), which sets activeDelivery to null!
                if (activeDelivery == null) return;
                
                // Update views
                foreach (var view in views)
                {
                    if (view == null) continue;
                    
                    float countdown = (activeDelivery.ItemData != null && activeDelivery.ItemData.CountdownTime > 0) 
                                      ? activeDelivery.ItemData.CountdownTime 
                                      : 1f;

                    view.OnTimerTick(activeDelivery.RemainingTime, 1f - (activeDelivery.RemainingTime / countdown));
                }
            }
        }

        public void RegisterDelivery(DeliveryModel model)
        {
            allDeliveries.Add(model);
            model.OnStateChanged += HandleStateChanged;
            Debug.Log($"Registered delivery for: {model.ItemData.ItemName}");
        }

        private void HandleStateChanged(DeliveryModel model)
        {
            switch (model.State)
            {
                case DeliveryState.PickedUp:
                    activeDelivery = model;
                    foreach (var view in views) view.OnDeliveryStarted(model);
                    model.ChangeState(DeliveryState.InTransit);
                    break;
                
                case DeliveryState.Completed:
                    Score++;
                    OnScoreChanged?.Invoke(Score);
                    foreach (var view in views) view.OnDeliveryCompleted(model);
                    activeDelivery = null;
                    break;

                case DeliveryState.Failed:
                    foreach (var view in views) view.OnDeliveryFailed(model);
                    activeDelivery = null;
                    break;
            }
        }

        // ── Public API for Interactions ───────────────────────────

        /// <summary>
        /// Called when the dog picks up an item (from MPickUp).
        /// </summary>
        public void OnItemPickedUp(int itemId)
        {
            // Just in case views weren't found in Awake (e.g., if they were disabled), find them again!
            if (views.Count == 0)
            {
                views = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<IDeliveryView>().ToList();
            }

            var model = allDeliveries.FirstOrDefault(d => d.ItemData.ItemId == itemId && d.State == DeliveryState.Idle);
            
            // BULLETPROOF FALLBACK: If the DeliverySpawner was not set up correctly (missing Start/End points),
            // the model will be null. Let's auto-generate a fallback delivery so the timer STILL works!
            if (model == null)
            {
                Debug.LogWarning($"[DeliveryController] Delivery with ID {itemId} was not registered! Auto-generating a fallback delivery so the timer works.");
                
                // We'll create a dummy SO if we can't find one
                var dummySO = ScriptableObject.CreateInstance<DeliveryItemSO>();
                dummySO.InitFallback(itemId, "Fallback Item", 60f);

                model = new DeliveryModel(dummySO, null, null);
                RegisterDelivery(model);
            }

            if (model != null)
            {
                model.ChangeState(DeliveryState.PickedUp);
            }
        }

        /// <summary>
        /// Called when the dog drops an item.
        /// </summary>
        public void OnItemDropped(int itemId)
        {
            if (activeDelivery != null && activeDelivery.ItemData.ItemId == itemId)
            {
                // For now, if dropped and not delivered, it fails.
                // Later this could be "Lost" state to allow picking up again.
                // Or if it was dropped for handoff, the handoff will change the state.
            }
        }

        /// <summary>
        /// Call to officially complete the delivery.
        /// </summary>
        public void CompleteDelivery(int itemId)
        {
            if (activeDelivery != null && activeDelivery.ItemData.ItemId == itemId)
            {
                activeDelivery.ChangeState(DeliveryState.Completed);
            }
        }
    }
}
