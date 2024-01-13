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
    public int id;

    public bool TempConnector = false;
    public bool isInputNode;


    private Button button;
    private bool pressed;
    private bool highlighted;

    private FancyLine line;

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

    public void addConnection(FancyLine line) {
        if(this.line!=null)
            Destroy(this.line.gameObject);

        this.line = line;
    }

    void Awake() {
        button = GetComponent<Button>();
        id = -1;
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
