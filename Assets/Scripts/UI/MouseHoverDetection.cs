using UnityEngine;
using UnityEngine.EventSystems;

//super duper complex system here, you have to be a giga genius to understand
public class MouseHoverDetection : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static bool MouseHovering;

    void Awake() {
        MouseHovering = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MouseHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseHovering = false;
    }
}
