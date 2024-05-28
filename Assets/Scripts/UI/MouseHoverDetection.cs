using UnityEngine;
using UnityEngine.EventSystems;

//super duper complex system here, you have to be a giga genius to understand
//if the mouse is hovering over any UI object with this script, MouseHovering will be true.
//usefull for detecting when the mouse is hovering over a menu, instead of clicking on the background
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
