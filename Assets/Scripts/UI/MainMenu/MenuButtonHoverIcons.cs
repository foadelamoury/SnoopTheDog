using UnityEngine;
using UnityEngine.EventSystems;

namespace BarkAndDeliver.UI
{
    public class MenuButtonHoverIcons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject arrowLeft;
        [SerializeField] private GameObject arrowRight;

        private void Awake() => SetIcons(false);

        public void OnPointerEnter(PointerEventData eventData) => SetIcons(true);
        public void OnPointerExit(PointerEventData eventData) => SetIcons(false);

        private void SetIcons(bool active)
        {
            if (arrowLeft != null) arrowLeft.SetActive(active);
            if (arrowRight != null) arrowRight.SetActive(active);
        }
    }
}
