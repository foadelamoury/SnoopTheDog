using UnityEngine;
using System;

namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Runtime model holding the state of a single delivery quest.
    /// The 'M' in MVC. Instantiated at runtime by the Spawner.
    /// </summary>
    public class DeliveryModel
    {
        // ── Data ──────────────────────────────────────────────────
        public DeliveryItemSO ItemData { get; private set; }
        
        public Transform StartPoint { get; private set; }
        public Transform EndPoint { get; private set; }

        public DeliveryState State { get; private set; }
        public float RemainingTime { get; private set; }

        // ── Events ────────────────────────────────────────────────
        public event Action<DeliveryModel> OnStateChanged;
        public event Action<float, float> OnTimerTick;

        public DeliveryModel(DeliveryItemSO itemData, Transform startPoint, Transform endPoint)
        {
            ItemData = itemData;
            StartPoint = startPoint;
            EndPoint = endPoint;
            State = DeliveryState.Idle;
            RemainingTime = itemData.CountdownTime;
        }

        public void ChangeState(DeliveryState newState)
        {
            if (State == newState) return;
            
            State = newState;
            OnStateChanged?.Invoke(this);
        }

        public void Tick(float deltaTime)
        {
            if (State == DeliveryState.PickedUp || State == DeliveryState.InTransit)
            {
                RemainingTime -= deltaTime;
                if (RemainingTime <= 0f)
                {
                    RemainingTime = 0f;
                    ChangeState(DeliveryState.Failed);
                }

                // normalized time: 0 (start) to 1 (expired)
                float normalized = 1f - Mathf.Clamp01(RemainingTime / ItemData.CountdownTime);
                OnTimerTick?.Invoke(RemainingTime, normalized);
            }
        }
    }
}
