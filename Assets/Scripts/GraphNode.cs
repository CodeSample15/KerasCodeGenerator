using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//this file is a mess, but just roll with it, this is a rush job meant to be finished hopefully during winter break
[RequireComponent(typeof(Button))]
public class GraphNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IComparable<GraphNode>
{
    public int id = -1; //for keeping track of which node is in which model

    [Header("Cosmetic")]

    [SerializeField] public Color32 NormalColor;
    [SerializeField] private Color32 SelectedColor;
    [SerializeField] private Color32 EditingColor;
    [SerializeField] private float colorFadeTime;
    private float colorFadeVelocity; 
    private float colorFadeAmount;

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

    [HideInInspector] public static GraphNode SelectedNode;


    //creating a doubly linked list to build the tree structure that will represent a graph (data structures galore right here)
    [HideInInspector] public GraphNode[] InputConnections;
    [HideInInspector] public GraphNode[] OutputConnections;

    [HideInInspector] public List<string> OutputShapes;

    [HideInInspector] public GameObject lineHolder; //to hold a new line when one is drawn


    private Button MainButton;
    private Button OptionsButton;
    private TextMeshProUGUI descriptionText;


    private Vector2 curPosition;
    private Vector2 clickStartPosition;
    private Vector2 lastPosition;


    private bool dragging; //for dragging the node itelf with the mouse
    private bool draggingConnection; //for dragging a connection of the node
    private bool movedDuringDrag;
    [HideInInspector] public bool editing;
    private GameObject draggedConnectionFROM; //the origin of the new connection
    private GameObject draggedConnecitonTO; //this is more of a temp object for the button that's going to follow the mouse

    public static string[] possibleActivations = {"relu", "linear", "tanh", "sigmoid"}; //leaky relu is going to be an actual layer

    //Every. Possible. Node. Option.
    //I'm reusing this script for every single node variation. That means I need to have as many arguments for the keras layer api represented here as possible
    [HideInInspector] public Shape inputShape;

    [HideInInspector] public int units;
    [HideInInspector] public string activation;
    [HideInInspector] public float dropout;
    [HideInInspector] public Shape kernelShape;
    [HideInInspector] public Shape strides;
    [HideInInspector] public bool returnSequences;
    [HideInInspector] public bool returnState; //hah not even close to being implemented
    [HideInInspector] public float recurrentDropout;


    void Awake() {
        curPosition = transform.position;
        clickStartPosition = Vector2.zero;
        dragging = false;
        editing = false;

        InputConnections = new GraphNode[NodeInputs.Count];
        OutputConnections = new GraphNode[NodeOutputs.Count];

        initBlankOptions();

        foreach(GameObject c in NodeInputs)
            c.GetComponent<GraphConnector>().isInputNode = true;

        foreach(GameObject c in NodeOutputs)
            c.GetComponent<GraphConnector>().isInputNode = false;

        Transform tempTextHolder = transform.Find("Info Box");
        descriptionText = tempTextHolder != null ? tempTextHolder.GetComponent<TextMeshProUGUI>() : null;
    }

    void Start() 
    {
        //set up the ids of all of the graph connectors
        for(int i=0; i<NodeInputs.Count; i++)
            NodeInputs[i].GetComponent<GraphConnector>().id = i;
        for(int i=0; i<NodeOutputs.Count; i++)
            NodeOutputs[i].GetComponent<GraphConnector>().id = i;

        updateDescription();
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
            //forgive me lord, for I have written this spaghetti code:
            if(!draggedConnectionFROM.GetComponent<GraphConnector>().Pressed) {
                GraphNode temp = CheckForHighlightedConnection();
        
                if(temp != null && lineHolder != null) {
                    //mouse is currently dropping a connection node onto a different graph
                    FancyLine lineHolderLine = lineHolder.GetComponent<FancyLine>();
                    GraphConnector fromConnector = lineHolderLine.StartPos.GetComponent<GraphConnector>();
                    GraphConnector toConnector = MouseInventory.HighlightedGraphConnection.GetComponent<GraphConnector>();

                    //check to make sure that if an input is being dragged, an output is slected and vise versa
                    if(MouseInventory.isInputNode && !MouseInventory.highlightingInputNode) {
                        //compatible, make connection
                        lineHolderLine.EndPos = MouseInventory.HighlightedGraphConnection;
                        toConnector.addConnection(lineHolderLine);
                        fromConnector.addConnection(lineHolderLine);
                        lineHolder = null;
  
                        InputConnections[fromConnector.id] = MouseInventory.HighlightedConnection;
                        MouseInventory.HighlightedConnection.OutputConnections[toConnector.id] = this;
                    }
                    else if(!MouseInventory.isInputNode && MouseInventory.highlightingInputNode) {
                        //compatible, make connection
                        lineHolderLine.EndPos = MouseInventory.HighlightedGraphConnection;
                        toConnector.addConnection(lineHolderLine);
                        fromConnector.addConnection(lineHolderLine);
                        lineHolder = null;

                        OutputConnections[fromConnector.id] = MouseInventory.HighlightedConnection;
                        MouseInventory.HighlightedConnection.InputConnections[toConnector.id] = this;
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

    public void updateDescription() 
    {
        if(descriptionText != null) {
            descriptionText.text = "";

            foreach(string setting in NodeOptions) {
                switch(setting) {
                    case "input size":
                        descriptionText.text += "Input size: (" + inputShape.toString() + ")\n";
                        break;

                    case "units":
                        descriptionText.text += "Units: " + units.ToString() + "\n";
                        break;

                    case "activation":
                        descriptionText.text += "Activation: " + activation + "\n";
                        break;

                    case "dropout":
                        descriptionText.text += "Dropout: " + dropout + "\n";
                        break;

                    case "kernel shape":
                        descriptionText.text += "Kernel shape: (" + kernelShape.toString() + ")\n";
                        break;

                    case "strides":
                        descriptionText.text += "Strides: (" + strides.toString() + ")\n";
                        break;

                    case "return sequences":
                        descriptionText.text += "Return seqs: " + returnSequences + "\n";
                        break;

                    case "recurrent dropout":
                        descriptionText.text += "Recurrent drop: " + recurrentDropout + "\n";
                        break;
                }
            }
        }
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

    private void initBlankOptions() 
    {
        //Let's assume we're not loading from a save file, since that's not in my time budget for this winter break
        //initialize all of the options with default parameters
        inputShape = new Shape(2);
        units = 10;
        activation = "relu";
        dropout = 0.0f;
        
        kernelShape = new Shape(2);
        kernelShape.sizes[0] = 1;
        kernelShape.sizes[1] = 1;

        strides = new Shape(2);
        returnSequences = false;
        returnState = false;
        recurrentDropout = 0.0f;
    }

    public void removeConnection(GraphNode node) 
    {
        for(int i=0; i<InputConnections.Length; i++)
            if(InputConnections[i] == node)
                InputConnections[i] = null;

        for(int i=0; i<OutputConnections.Length; i++)
            if(OutputConnections[i] == node)
                OutputConnections[i] = null;
    }

    public void Delete() {
        foreach(GraphNode node in InputConnections)
            if(node != null)
                node.removeConnection(this);
        foreach(GraphNode node in OutputConnections)
            if(node != null)
                node.removeConnection(this);

        GraphController.staticReference.GraphNodes.Remove(this);

        Destroy(gameObject);
    }

    public int CompareTo(GraphNode other)
    {
        return NodeName.CompareTo(other.NodeName);
    }
}
