using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BarkAndDeliver.Delivery;

namespace BarkAndDeliver.Missions
{
    /// <summary>
    /// Replaces the static DeliverySpawner with a dynamic, endless random mission loop.
    /// Picks random senders and receivers, and automatically configures their zones and minimap waypoints.
    /// </summary>
    public class DeliveryMissionManager : MonoBehaviour
    {
        [Header("Mission Configuration")]
        [Tooltip("The pizza item data to use for deliveries.")]
        [SerializeField] private DeliveryItemSO deliveryItemData;
        
        [Tooltip("The physical pizza box in the scene.")]
        [SerializeField] private Transform pizzaBoxTransform;
        
        [Tooltip("Delay in seconds before starting a new mission after one completes.")]
        [SerializeField] private float delayBetweenMissions = 3f;

        private DeliveryController controller;
        
        [Header("NPC Pools")]
        [Tooltip("Drag the delivery men who SEND the pizza here.")]
        [SerializeField] private List<NPCInteractionBridge> senders = new List<NPCInteractionBridge>();
        
        [Tooltip("Drag the delivery men who RECEIVE the pizza here.")]
        [SerializeField] private List<NPCInteractionBridge> receivers = new List<NPCInteractionBridge>();
        
        private NPCInteractionBridge currentGiver;
        private NPCInteractionBridge currentReceiver;

        private void Start()
        {
            controller = FindAnyObjectByType<DeliveryController>();
            if (controller == null)
            {
                Debug.LogError("[DeliveryMissionManager] No DeliveryController found!");
                return;
            }

            if (senders.Count == 0 || receivers.Count == 0)
            {
                Debug.LogWarning("[DeliveryMissionManager] You must assign at least one Sender and one Receiver in the inspector!");
                return;
            }

            // Listen for completion to start the next loop
            controller.OnScoreChanged += HandleScoreChanged;

            // Start the very first mission!
            StartCoroutine(StartNewMissionCoroutine(1f));
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnScoreChanged -= HandleScoreChanged;
            }
        }

        private void HandleScoreChanged(int newScore)
        {
            // A delivery completed successfully, start a new one!
            StartCoroutine(StartNewMissionCoroutine(delayBetweenMissions));
        }

        private IEnumerator StartNewMissionCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNewMission();
        }

        private void StartNewMission()
        {
            if (senders.Count == 0 || receivers.Count == 0) return;

            // Pick a random giver
            currentGiver = senders[Random.Range(0, senders.Count)];
            
            // Pick a random receiver (could be anyone in the receiver list)
            currentReceiver = receivers[Random.Range(0, receivers.Count)];

            // Disable interaction for everyone first
            foreach (var npc in senders)
            {
                if (npc != null) npc.DisableInteraction();
            }
            foreach (var npc in receivers)
            {
                if (npc != null) npc.DisableInteraction();
            }

            // Setup the chosen ones
            currentGiver.SetupAsGiver(pizzaBoxTransform);
            currentReceiver.SetupAsReceiver(pizzaBoxTransform);

            // Register the delivery so the minimap waypoints work dynamically!
            // If we don't have an item data, create a fallback
            if (deliveryItemData == null)
            {
                deliveryItemData = ScriptableObject.CreateInstance<DeliveryItemSO>();
                deliveryItemData.InitFallback(1, "Random Pizza Delivery", 60f);
            }

            DeliveryModel model = new DeliveryModel(deliveryItemData, currentGiver.transform, currentReceiver.transform);
            controller.RegisterDelivery(model);

            Debug.Log($"[DeliveryMissionManager] Started new mission! Giver: {currentGiver.name} → Receiver: {currentReceiver.name}");
        }
    }
}
