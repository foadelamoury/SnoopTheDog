using UnityEngine;
using MalbersAnimations.Controller;

[RequireComponent(typeof(Collider))]
public class CustomPickUpBridge : MonoBehaviour
{
    [Tooltip("The main Animal Controller (Snoopy)")]
    public MAnimal animal;

    [Tooltip("The exact bone where the item should attach (e.g. Jaw or Head)")]
    public Transform attachBone;

    private void OnTriggerEnter(Collider other)
    {
        // Try to find a Pickable component on the object we touched
        Pickable pickable = other.GetComponentInParent<Pickable>();
        
        if (pickable != null && !pickable.IsPicked)
        {
            // 1. Tell the item who is picking it up (Snoopy)
            pickable.SetFocused(animal.gameObject, true);
            
            // 2. Force it to parent to the specific bone we want
            pickable.SetParent(attachBone);
            
            // 3. Move it to the exact center of the bone
            pickable.transform.localPosition = Vector3.zero;
            
            // 4. Tell the Malbers system that the pickup was successful
            pickable.Pick();
            
            Debug.Log($"[CustomPickUpBridge] Picked up {pickable.name} and attached to {attachBone.name}!");
        }
    }
}
