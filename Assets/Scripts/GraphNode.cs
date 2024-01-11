using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//this file is a mess, but just roll with it, this is a rush job meant to be finished hopefully during winter break
[RequireComponent(typeof(Button))]
public class GraphNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IComparable<GraphNode>
{
    [Header("Cosmetic")]

    [SerializeField] public Color32 NormalColor;
    [SerializeField] private Color32 SelectedColor;
    [SerializeField] private Color32 EditingColor;
    [SerializeField] private float colorFadeTime;
    private float colorFadeVelocity; 
    private float colorFadeAmount;

    [Space]

    [Header("Functional")]
    [SerializeField] public string NodeName;

    [Space]

    [SerializeField] public string[] NodeOptions;

    [Space]

    [SerializeField] public List<GameObject> NodeInputs; //these are the actual gameobjects that represent the visuals of the inputs and outputs of the node
    [SerializeField] public List<GameObject> NodeOutputs;

    [Space]

    [Tooltip("The thing the user drags out from the node that will have the line connected to it.")]
    [SerializeField] private GameObject NodeConnectionDot;
    [SerializeField] private GameObject fancyLine;

    public static GraphNode SelectedNode;


    //creating a doubly linked list to build the tree structure that will represent a graph (data structures galore right here)
    public List<GraphNode> InputConnections;
    public List<GraphNode> OutputConnections;


    public string InputShape;
    public List<string> OutputShapes;

    public GameObject lineHolder; //to hold a new line when one is drawn


    private Button MainButton;
    private Button OptionsButton;


    private Vector2 curPosition;
    private Vector2 clickStartPosition;
    private Vector2 lastPosition;


    private bool dragging; //for dragging the node itelf with the mouse
    private bool draggingConnection; //for dragging a connection of the node
    private bool movedDuringDrag;
    public bool editing;
    private GameObject draggedConnectionFROM; //the origin of the new connection
    private GameObject draggedConnecitonTO; //this is more of a temp object for the button that's going to follow the mouse

    public static string[] possibleActivations = {"relu", "LeakyReLU", "linear", "tanh", "sigmoid"};

    //Every. Possible. Node. Option.
    //I'm reusing this script for every single node variation. That means I need to have as many arguments for the keras layer api represented here as possible
    public Size inputsize;

    public int units;
    public string activation;
    public float dropout;
    public Size kernelShape;
    public Size strides;
    public bool returnSequences;
    public bool returnState; //maybe, MAYBE, I'll implement this for the alpha version
    public float recurrentDropout;


    void Awake() {
        curPosition = transform.position;
        clickStartPosition = Vector2.zero;
        dragging = false;
        editing = false;

        foreach(GameObject c in NodeInputs)
            c.GetComponent<GraphConnector>().isInputNode = true;

        foreach(GameObject c in NodeOutputs)
            c.GetComponent<GraphConnector>().isInputNode = false;
    }

    void Update() {
        //handle mouse manipulation
        if(dragging) {
            Vector2 mouseDiff = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - clickStartPosition;
            transform.position = curPosition + mouseDiff;

            if(!transform.position.Equals(lastPosition))
                movedDuringDrag = true;
        }
        else
            curPosition = transform.position;

        //handle when a new connection is made
        if(!draggingConnection) {
            CheckForConnectionDragging(NodeInputs, true);
            CheckForConnectionDragging(NodeOutputs, false);
        }
        else {
            if(!draggedConnectionFROM.GetComponent<GraphConnector>().Pressed) {
                GraphNode temp = CheckForHighlightedConnection();
        
                if(temp != null) {
                    //mouse is currently dropping a connection node onto a different graph

                    //check to make sure that if an input is being dragged, an output is slected and vise versa
                    if(MouseInventory.isInputNode && !MouseInventory.highlightingInputNode) {
                        //compatible, make connection
                        lineHolder.GetComponent<FancyLine>().EndPos = MouseInventory.HighlightedGraphConnection;
                        lineHolder = null;

                        OutputConnections.Add(MouseInventory.HighlightedConnection);
                        MouseInventory.HighlightedConnection.InputConnections.Add(this);
                    }
                    else if(!MouseInventory.isInputNode && MouseInventory.highlightingInputNode) {
                        //compatible, make connection
                        lineHolder.GetComponent<FancyLine>().EndPos = MouseInventory.HighlightedGraphConnection;
                        lineHolder = null;

                        InputConnections.Add(MouseInventory.HighlightedConnection);
                        MouseInventory.HighlightedConnection.OutputConnections.Add(this);
                    }
                }

                //clean up
                Destroy(draggedConnecitonTO);
                Destroy(lineHolder);
                draggingConnection = false;
                MouseInventory.clearInventory();
            }
            else {
                draggedConnecitonTO.transform.localScale = draggedConnectionFROM.transform.localScale;
                draggedConnecitonTO.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
        }

        //handle cosmetic changes
        if(!editing) {
            GetComponent<Image>().color = Color32.Lerp(NormalColor, SelectedColor, colorFadeAmount);
            colorFadeAmount = Mathf.SmoothDamp(colorFadeAmount, SelectedNode==this ? 1 : 0, ref colorFadeVelocity, colorFadeTime);
        }
    }

    private void CheckForConnectionDragging(List<GameObject> connections, bool inputNode) {
        if(!draggingConnection) {
            foreach(GameObject connection in connections) {
                if(connection.GetComponent<GraphConnector>().Pressed) {
                    draggedConnecitonTO = Instantiate(NodeConnectionDot, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity);
                    draggedConnecitonTO.GetComponent<GraphConnector>().TempConnector = true;
                    draggedConnecitonTO.transform.SetParent(transform, false);

                    draggedConnectionFROM = connection;
                    draggingConnection = true;

                    //for the line that draws in between the two nodes
                    lineHolder = Instantiate(fancyLine, Vector2.zero, Quaternion.identity);
                    lineHolder.GetComponent<FancyLine>().StartPos = draggedConnectionFROM;
                    lineHolder.GetComponent<FancyLine>().EndPos = draggedConnecitonTO;

                    //tell the mouse script that it's holding something
                    MouseInventory.HeldConnection = this;
                    MouseInventory.isInputNode = inputNode;

                    break; //no reason to keep going
                }
            }
        }
    }

    public void ShowOptionsMenu() 
    {
        //deselect everything and highlight this node
        SelectedNode = null;
        FancyLine.selectedLine = null;

        editing = true;
        GetComponent<Image>().color = EditingColor;

        ScreenSpaceUI.staticRef.showNodeOptionsMenu(this);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if(eventData.button.Equals(PointerEventData.InputButton.Left)) {
            clickStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            lastPosition = transform.position;
            dragging = true;
            movedDuringDrag = false;
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        dragging = false;

        if(eventData.button.Equals(PointerEventData.InputButton.Left) && !movedDuringDrag) {
            //mouse clicked the node, not dragged it
            //select the node
            SelectedNode = this;
            FancyLine.selectedLine = null;
        }
    }

    private GraphNode CheckForHighlightedConnection() {
        if(MouseInventory.HighlightedConnection != null && MouseInventory.HighlightedConnection != this)
            return MouseInventory.HighlightedConnection;
        return null;
    }

    public void Delete() {
        foreach(GraphNode node in InputConnections)
            node.OutputConnections.Remove(this);
        foreach(GraphNode node in OutputConnections)
            node.InputConnections.Remove(this);

        GraphController.staticReference.GraphNodes.Remove(this);

        Destroy(gameObject);
    }

    public int CompareTo(GraphNode other)
    {
        return NodeName.CompareTo(other.NodeName);
    }
}
