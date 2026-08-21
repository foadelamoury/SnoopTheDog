using UnityEngine;
using System.Collections;

namespace BarkAndDeliver.UI
{
    public class HideAfterDelay : MonoBehaviour
    {
        [Tooltip("How many seconds to wait before hiding the object.")]
        public float delay = 1f;

        private void OnEnable()
        {
            StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}
