using UnityEngine;

namespace BarkAndDeliver.Delivery
{
    /// <summary>
    /// Helper MonoBehaviour to quickly wire up an NPC's components in the inspector.
    /// Place this on the root of your Humanoid NPC GameObject.
    /// </summary>
    [RequireComponent(typeof(NPCInteractionBridge))]
    public class NPCSetup : MonoBehaviour
    {
        [Header("NPC Parts")]
        [Tooltip("The transform representing the chest/bag area where items are held")]
        public Transform holdPoint;
        
        [Header("Auto-wired Components")]
        public Animator animator;
        public NPCInteractionBridge interactionBridge;

        private void Reset()
        {
            animator = GetComponent<Animator>();
            interactionBridge = GetComponent<NPCInteractionBridge>();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (interactionBridge == null) interactionBridge = GetComponent<NPCInteractionBridge>();
        }
    }
}
