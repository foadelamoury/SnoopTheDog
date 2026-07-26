using UnityEngine;

public class SimpleBoneJiggle : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("How strongly the bone tries to return to its original position.")]
    [Range(0.01f, 1f)] public float stiffness = 0.2f;
    
    [Tooltip("How much the movement is slowed down (prevents infinite bouncing).")]
    [Range(0.01f, 1f)] public float damping = 0.1f;

    [Header("Bone Setup")]
    [Tooltip("The local axis that points down the length of the bone (usually Y or X depending on your 3D software).")]
    public Vector3 boneAxis = Vector3.up;
    
    [Tooltip("The length of the bone. Adjust if the jiggle feels too subtle or extreme.")]
    public float boneLength = 0.5f;

    private Vector3 simulatedTipPos;
    private Vector3 velocity;

    void Start()
    {
        // Initialize the simulated tip position to its starting rest position
        simulatedTipPos = transform.position + transform.TransformDirection(boneAxis.normalized) * boneLength;
    }

    void LateUpdate()
    {
        // 1. Where the tip "wants" to be based on current animations and head movement
        Vector3 targetTipPos = transform.position + transform.TransformDirection(boneAxis.normalized) * boneLength;

        // 2. Spring physics: Pull the simulated tip towards the target tip
        Vector3 force = (targetTipPos - simulatedTipPos) * stiffness;
        velocity = (velocity + force) * (1f - damping);
        simulatedTipPos += velocity;

        // 3. Prevent the bone from stretching by forcing the simulated tip to stay at 'boneLength' distance
        Vector3 jiggleDir = (simulatedTipPos - transform.position).normalized;
        simulatedTipPos = transform.position + jiggleDir * boneLength;

        // 4. Calculate the rotation difference and apply it
        Quaternion jiggleRotation = Quaternion.FromToRotation(transform.TransformDirection(boneAxis.normalized), jiggleDir);
        transform.rotation = jiggleRotation * transform.rotation;
    }
}
