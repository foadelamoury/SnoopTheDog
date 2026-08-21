using UnityEngine;

namespace BarkAndDeliver.UI
{
    public class UI : MonoBehaviour
    {
        protected virtual void Awake() { }

        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
        public virtual void Close() => Hide();
    }
}
