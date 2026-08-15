using UnityEngine;
using MalbersAnimations.Controller;

namespace BarkAndDeliver
{
    /// <summary>
    /// Forces the Cinemachine camera to target the dog (MainAnimal) at startup.
    /// Place this on any always-active GameObject (e.g., a Manager).
    /// </summary>
    public class ForceCameraTarget : MonoBehaviour
    {
        [Tooltip("The Cinemachine camera that should follow the dog")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera cinemachineCamera;

        private void Start()
        {
            MAnimal dog = MAnimal.MainAnimal;
            if (dog == null)
            {
                Debug.LogWarning("[ForceCameraTarget] No MainAnimal found!");
                return;
            }

            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = dog.transform;
                cinemachineCamera.LookAt = dog.transform;
                Debug.Log($"[ForceCameraTarget] Camera now targeting: {dog.name}");
            }
            else
            {
                Debug.LogWarning("[ForceCameraTarget] No CinemachineCamera assigned!");
            }
        }
    }
}
