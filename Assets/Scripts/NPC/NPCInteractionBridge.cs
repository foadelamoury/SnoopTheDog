using UnityEngine;
using UnityEngine.Events;
using System;
using RootMotion.FinalIK;
using MalbersAnimations.Controller;

namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Bridges Malbers Zone events to Final IK's native InteractionSystem.
    /// Replaces NPCHandoffController + NPCDeliveryIK with a single, clean script.
    /// 
    /// GIVING NPC: Press E → NPC reaches for pizza with both hands → pauses holding it →
    ///             pizza transfers to dog → NPC retracts hands.
    /// RECEIVING NPC: Dog enters zone + presses E → NPC reaches out → grabs pizza → holds it.
    /// </summary>
    public class NPCInteractionBridge : MonoBehaviour, IHandoffHandler
    {
        [Header("Final IK References")]
        [Tooltip("The InteractionSystem on the NPC's model (e.g. boywithcap). Will auto-find if left empty.")]
        [SerializeField] private InteractionSystem interactionSystem;

        [Tooltip("The InteractionObject on the Pizza Box.")]
        [SerializeField] private InteractionObject pizzaInteractionObject;

        [Header("Handoff Settings")]
        [Tooltip("The hold point where the pizza rests when the NPC is holding it.")]
        [SerializeField] private Transform holdPoint;

        [Tooltip("The physical pizza box transform (for parenting/transfer).")]
        [SerializeField] private Transform pizzaBoxTransform;

        [Tooltip("The point on the table where the pizza box rests initially.")]
        public Transform tablePoint;

        [Tooltip("The Malbers InteractZone for this NPC.")]
        public Zone interactZone;

        [Header("Events")]
        [Tooltip("Fired after the NPC has successfully given the pizza to the dog.")]
        public UnityEvent OnGaveItemToDog;

        [Tooltip("Fired after the NPC has successfully received the pizza from the dog.")]
        public UnityEvent OnReceivedItemFromDog;

        // ── State ──────────────────────────────────────────────────
        public bool IsHandoffInProgress { get; private set; }
        private bool hasInteracted = false;
        private bool isGiver = true; // true = this NPC gives the pizza; false = this NPC receives it
        private Action currentOnComplete;

        // ── Lifting State ──
        private bool isLifting = false;
        private float liftProgress = 0f;
        private Vector3 startLiftPos;
        private Quaternion startLiftRot;
        private float liftDuration = 0.5f; // Duration of the lift animation from table to chest

        // ── Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (interactionSystem == null)
                interactionSystem = GetComponentInChildren<InteractionSystem>();
        }

        private void OnEnable()
        {
            if (interactionSystem != null)
            {
                interactionSystem.OnInteractionPause += OnInteractionPaused;
                interactionSystem.OnInteractionResume += OnInteractionResumed;
                interactionSystem.OnInteractionStop += OnInteractionStopped;
            }
        }

        private void OnDisable()
        {
            if (interactionSystem != null)
            {
                interactionSystem.OnInteractionPause -= OnInteractionPaused;
                interactionSystem.OnInteractionResume -= OnInteractionResumed;
                interactionSystem.OnInteractionStop -= OnInteractionStopped;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PUBLIC API — Called from Malbers Zone Events
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Called by Malbers InteractZone "On Zone Active (MAnimal)" when the dog presses E.
        /// Starts the two-handed interaction with the pizza box.
        /// </summary>
        public void TriggerInteraction(MAnimal dog)
        {
            if (hasInteracted || IsHandoffInProgress) return;
            if (interactionSystem == null || pizzaInteractionObject == null) return;

            hasInteracted = true;
            IsHandoffInProgress = true;
            isGiver = true;

            // Lock dog movement during handoff
            if (dog != null) dog.LockInput = true;

            Debug.Log($"[NPCInteractionBridge] Starting GIVE interaction on {gameObject.name}");

            // Start interaction with BOTH hands simultaneously
            interactionSystem.StartInteraction(FullBodyBipedEffector.LeftHand, pizzaInteractionObject, false);
            interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, pizzaInteractionObject, false);
        }

        /// <summary>
        /// Parameterless overload for zones that don't pass the animal.
        /// </summary>
        public void TriggerInteraction()
        {
            TriggerInteraction(MAnimal.MainAnimal);
        }

        /// <summary>
        /// GameObject overload for Malbers events that pass GameObject instead of MAnimal.
        /// </summary>
        public void TriggerInteraction(GameObject dogObj)
        {
            if (dogObj == null) return;
            MAnimal mAnimal = dogObj.GetComponent<MAnimal>() ?? dogObj.GetComponentInParent<MAnimal>();
            if (mAnimal != null) TriggerInteraction(mAnimal);
        }

        // ══════════════════════════════════════════════════════════
        //  IHandoffHandler — Called by DeliveryClient
        // ══════════════════════════════════════════════════════════

        public void StartGiveToDog(Transform dogHolder, Transform itemToHandOff, Action onComplete)
        {
            // This path is used when the system calls it programmatically
            TriggerInteraction(MAnimal.MainAnimal);
        }

        public void StartReceiveFromDog(Transform droppedItem, Action onComplete)
        {
            if (IsHandoffInProgress) return;
            if (interactionSystem == null) return;

            IsHandoffInProgress = true;
            isGiver = false;
            currentOnComplete = onComplete;

            // If we have a specific interaction object for the dropped item, use it
            InteractionObject intObj = null;
            if (droppedItem != null)
                intObj = droppedItem.GetComponent<InteractionObject>();

            // Fallback to the pre-assigned pizza interaction object
            if (intObj == null)
                intObj = pizzaInteractionObject;

            if (intObj == null)
            {
                Debug.LogWarning("[NPCInteractionBridge] No InteractionObject found for receiving!");
                // Fallback: just parent the item directly
                if (droppedItem != null && holdPoint != null)
                {
                    droppedItem.SetParent(holdPoint);
                    droppedItem.localPosition = Vector3.zero;
                    droppedItem.localRotation = Quaternion.identity;
                }
                IsHandoffInProgress = false;
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[NPCInteractionBridge] Starting RECEIVE interaction on {gameObject.name}");

            // Lock dog
            MAnimal dog = MAnimal.MainAnimal;
            if (dog != null) dog.LockInput = true;

            interactionSystem.StartInteraction(FullBodyBipedEffector.LeftHand, intObj, false);
            interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, intObj, false);
        }

        // ══════════════════════════════════════════════════════════
        //  FINAL IK CALLBACKS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Called by InteractionSystem when the weight curve hits a Pause event.
        /// This is when the NPC's hands are ON the pizza box.
        /// </summary>
        private void OnInteractionPaused(FullBodyBipedEffector effector, InteractionObject interactionObject)
        {
            if (interactionObject != pizzaInteractionObject) return;

            Debug.Log($"[NPCInteractionBridge] Interaction PAUSED on {effector} — isGiver: {isGiver}");

            if (isGiver)
            {
                if (isLifting) return; // Prevent double-firing
                
                // Start lifting the pizza to the hold point
                isLifting = true;
                liftProgress = 0f;
                
                if (pizzaBoxTransform != null)
                {
                    startLiftPos = pizzaBoxTransform.position;
                    startLiftRot = pizzaBoxTransform.rotation;
                    
                    var rb = pizzaBoxTransform.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
            else
            {
                if (!IsHandoffInProgress) return; // Prevent double-firing

                // NPC is RECEIVING the pizza → parent it to hold point and keep holding
                if (pizzaBoxTransform != null && holdPoint != null)
                {
                    pizzaBoxTransform.SetParent(holdPoint);
                    pizzaBoxTransform.localPosition = Vector3.zero;
                    pizzaBoxTransform.localRotation = Quaternion.identity;
                }

                // DON'T resume — keep hands holding the pizza!
                // Unlock the dog though
                MAnimal dog = MAnimal.MainAnimal;
                if (dog != null) dog.LockInput = false;

                IsHandoffInProgress = false;
                OnReceivedItemFromDog?.Invoke();
                
                currentOnComplete?.Invoke();
                currentOnComplete = null;
            }
        }

        /// <summary>
        /// Called when a paused interaction is resumed (hands retracting).
        /// </summary>
        private void OnInteractionResumed(FullBodyBipedEffector effector, InteractionObject interactionObject)
        {
            if (interactionObject != pizzaInteractionObject) return;

            Debug.Log($"[NPCInteractionBridge] Interaction RESUMED (hands retracting) — effector: {effector}");
        }

        /// <summary>
        /// Called when the interaction fully completes (hands back at sides).
        /// </summary>
        private void OnInteractionStopped(FullBodyBipedEffector effector, InteractionObject interactionObject)
        {
            if (interactionObject != pizzaInteractionObject) return;
            // Only finalize once (when both hands are done)
            if (effector != FullBodyBipedEffector.LeftHand) return;

            Debug.Log($"[NPCInteractionBridge] Interaction STOPPED (complete)");

            MAnimal dog = MAnimal.MainAnimal;
            if (dog != null) dog.LockInput = false;

            IsHandoffInProgress = false;

            if (isGiver)
            {
                OnGaveItemToDog?.Invoke();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            if (isLifting && pizzaBoxTransform != null && holdPoint != null)
            {
                liftProgress += Time.deltaTime / liftDuration;
                
                // Use smooth step for a natural lifting motion
                float t = Mathf.SmoothStep(0f, 1f, liftProgress);

                pizzaBoxTransform.position = Vector3.Lerp(startLiftPos, holdPoint.position, t);
                pizzaBoxTransform.rotation = Quaternion.Lerp(startLiftRot, holdPoint.rotation, t);

                if (liftProgress >= 1f)
                {
                    // Done lifting! Now give it to the dog.
                    isLifting = false;
                    TransferPizzaToDog();
                    
                    // After a brief moment, resume the interaction so hands retract
                    StartCoroutine(ResumeAfterDelay(0.3f));
                }
            }
        }

        /// <summary>
        /// Transfers the pizza box from the NPC to the dog using Malbers MPickUp.
        /// Also triggers the delivery timer.
        /// </summary>
        private void TransferPizzaToDog()
        {
            MAnimal dog = MAnimal.MainAnimal;
            if (dog == null)
            {
                Debug.LogWarning("[NPCInteractionBridge] No dog found for transfer!");
                return;
            }

            if (pizzaBoxTransform == null)
            {
                Debug.LogWarning("[NPCInteractionBridge] No pizza box transform assigned!");
                return;
            }

            // Un-parent pizza from NPC
            pizzaBoxTransform.SetParent(null);

            // Make visible
            SetItemVisible(pizzaBoxTransform, true);

            // Try to use Malbers MPickUp
            var pickable = pizzaBoxTransform.GetComponent<Pickable>();
            if (pickable != null)
            {
                var mPickUp = dog.GetComponentInChildren<MPickUp>();
                if (mPickUp != null)
                {
                    mPickUp.FocusedItem = pickable;
                    mPickUp.PickUpItem();
                }
                else
                {
                    // Fallback: parent to dog's head
                    Debug.LogWarning("[NPCInteractionBridge] Dog missing MPickUp! Manually attaching pizza.");
                    pizzaBoxTransform.SetParent(dog.transform);
                    pizzaBoxTransform.localPosition = Vector3.up * 0.5f;

                    // Only trigger the timer manually if MPickUp wasn't used,
                    // because MPickUp.PickUpItem() already fires its own UnityEvent
                    // which calls DeliveryController.OnItemPickedUp.
                    var deliveryController = FindAnyObjectByType<DeliveryController>();
                    if (deliveryController != null)
                    {
                        deliveryController.OnItemPickedUp(pickable.ID);
                    }
                }
            }

            Debug.Log("[NPCInteractionBridge] Pizza transferred to dog!");
        }

        private System.Collections.IEnumerator ResumeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (interactionSystem != null)
            {
                interactionSystem.ResumeAll();
            }
        }

        private void SetItemVisible(Transform item, bool visible)
        {
            if (item == null) return;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = visible;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  MISSION MANAGER API
        // ══════════════════════════════════════════════════════════
        
        public void SetupAsGiver(Transform pizzaBox)
        {
            isGiver = true;
            hasInteracted = false;
            pizzaBoxTransform = pizzaBox;
            if (pizzaBox != null)
                pizzaInteractionObject = pizzaBox.GetComponent<InteractionObject>();
                
            if (interactZone != null) interactZone.gameObject.SetActive(true);
            
            if (tablePoint != null && pizzaBox != null)
            {
                pizzaBox.SetParent(null);
                pizzaBox.position = tablePoint.position;
                pizzaBox.rotation = tablePoint.rotation;
                SetItemVisible(pizzaBox, true);
            }
        }

        public void SetupAsReceiver(Transform pizzaBox)
        {
            isGiver = false;
            hasInteracted = false;
            pizzaBoxTransform = pizzaBox;
            if (pizzaBox != null)
                pizzaInteractionObject = pizzaBox.GetComponent<InteractionObject>();
                
            if (interactZone != null) interactZone.gameObject.SetActive(true);
        }
        
        public void DisableInteraction()
        {
            hasInteracted = false;
            if (interactZone != null) interactZone.gameObject.SetActive(false);
        }
    }
}
