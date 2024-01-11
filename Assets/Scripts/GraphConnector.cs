using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GraphConnector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string Shape;
    public string Name;

    public bool TempConnector = false;
    public bool isInputNode;


    private Button button;
    private bool pressed;
    private bool highlighted;

    public bool Pressed {
        get { return pressed; }
    }

    public bool Highlighted {
        get { return highlighted; }
    }

    public void OnPointerDown(PointerEventData eventData) {
        pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData) {
        pressed = false;
    }

    void Awake() {
        button = GetComponent<Button>();
    }

    void Update() {
        if(!TempConnector && Vector2.Distance(transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition)) < 0.3f) {
            button.Select();
            highlighted = true;
            MouseInventory.HighlightedGraphConnection = gameObject;
            MouseInventory.HighlightedConnection = GetComponentInParent<GraphNode>();
            MouseInventory.highlightingInputNode = isInputNode;
        }
        else
            highlighted = false;
    }
}
