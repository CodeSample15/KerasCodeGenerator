using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEditor;

public class GraphController : MonoBehaviour
{
    public static GraphController staticReference;

    //for notifying the user of their code generation status
    [SerializeField] private Animator compiledNotification;
    [SerializeField] private Animator failedNotification;

    [Space]

    [SerializeField] private FancyLine fancyLinePrefab;

    public List<GraphNode> GraphNodes;

    private List<GraphNode> visitedNodes;
    private List<GraphNode> checkPoints;
    private List<string> requiredLayers;

    private string compiledCode;
    private int numModels; //the number of individual, non connected models in the graph

    private string reasonForFailing = "";

    private GraphState loadedState; //for temporarily storing loaded states from a file

    void Awake() 
    {
        compiledCode = "";
    }

    void Update() 
    {
        staticReference = this;

        ManageInteractivity();
    }

    void LateUpdate()
    {
        ScanForOnScreenNodes();
    }

    public void SaveGraph() 
    {
        string dir = EditorUtility.SaveFilePanel("Select Directory", "", "graph.kcgg", "kcgg"); //kcgg = keras code generator graph

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(dir, FileMode.Create);

        GraphState data = new GraphState(GraphNodes);
        formatter.Serialize(stream, data);
        stream.Close();
    }

    public void LoadGraph() 
    {
        string dir = EditorUtility.OpenFilePanel("Select graph file:", "", "kcgg");
        loadedState = null; //just in case, get rid of any state previously loaded in memory

        if(File.Exists(dir)) 
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(dir, FileMode.Open);

            GraphState data = formatter.Deserialize(stream) as GraphState;

            //restore graph
            if(GraphNodes.Count == 0) {
                restoreGraph(data);
            }
            else {
                //graph isn't empty, prompt to ask the user to save
                loadedState = data; //store in a global field for later use
                ScreenSpaceUI.staticRef.loadWarning.SetActive(true);
            }

            stream.Close();
        }
        else
        {
            Debug.LogError("Failed to open graph file! File not found.");
        }
    }

    public void ReturnToGraph() 
    {
        //find the nearest input node and travel to it
        GraphNode travelTo = null;

        foreach(GraphNode node in GraphNodes) {
            if(node.NodeName.Equals("Input")) {
                if(travelTo == null || node.transform.position.magnitude < travelTo.transform.position.magnitude) {
                    travelTo = node;
                }
            }
        }

        //if there are no input nodes, just travel to the closest node
        if(travelTo == null) {
            foreach(GraphNode node in GraphNodes) {
                if(travelTo == null || node.transform.position.magnitude < travelTo.transform.position.magnitude) {
                    travelTo = node;
                }
            }
        }

        //move all nodes over in the graph so the camera is looking at the graph
        foreach(GraphNode node in GraphNodes) {
            node.transform.position -= travelTo.transform.position;
        }
    }

    public void IgnoreWarningLoadGraph() //for ui buttons to access
    {
        if(loadedState != null) {
            restoreGraph(loadedState);
            ScreenSpaceUI.staticRef.hideLoadWarning();
        }
    }

    public void CompileGraph() 
    {
        //if the code preview window is open, ignore the compile button press
        if(ScreenSpaceUI.staticRef.ShowingCodePrev)
            return;

        //data structures and algorithms hell: (tracing a graph and appending the generated code in an order that makes sense)
        //combination of BFS and DFS :D

        requiredLayers = new List<string>();
        
        if(graphCanBeCompiled()) { //also counts how many individual models are in the graph and assigns the value to numModels
            //reset the output string
            compiledCode = "";
            addToCode("from keras.models import Model");
            addToCode("from keras.layers import Input", false);
            foreach(string layer in requiredLayers) {
                if(layer != "Split") //split is not an actual keras layer, just something I am using to represent a split in the model architecture
                    addToCode(", " + layer, false);
            }
            addToCode(""); //some spacing
            addToCode("");

            //each input node is a seperate model, write code for that model
            int modelCount = 0;

            checkPoints = new List<GraphNode>(); //to keep track of different model merges
            List<string[]> merges = new List<string[]>();
            List<string>[] input_names = new List<string>[numModels];
            List<string>[] output_names = new List<string>[numModels];

            initStringListArray(input_names); //we create an array of different inputs and outputs to determine which input and which output corresponds to which model in the graph (represented by the index of this list)
            initStringListArray(output_names);

            //create a list of all of the unique model ids
            List<int> uniqueModels = new List<int>();
            foreach(GraphNode n in GraphNodes) {
            if(!uniqueModels.Contains(n.id) && n.id != -1)
                uniqueModels.Add(n.id);
            }

            foreach(GraphNode node in GraphNodes) {
                if(node.NodeName.Equals("Input")) {
                    string inputName = "input_" + modelCount;
                    string modelName = "m_" + modelCount;

                    int modelIndex = uniqueModels.IndexOf(node.id);

                    input_names[modelIndex].Add(inputName);

                    //add input to model
                    addToCode(inputName + " = Input(shape=(", false);
                    addToCode(node.inputShape.toString(), false);
                    if(node.inputShape.sizes.Length == 1)
                        addToCode(",", false);
                    addToCode("))");

                    
                    traverseModel(node, inputName, modelName, merges, output_names, modelIndex);

                    modelCount++;
                }
            }

            //combine models that are joined by concat
            //repeat until no more nodes remain
            while(checkPoints.Count > 0) {
                string modelName = "m_" + modelCount;

                addToCode(modelName + " = Concatenate()([" + merges[0][0] + ", " + merges[0][1] + "])");

                int modelIndex = uniqueModels.IndexOf(checkPoints[0].id);
                traverseModel(checkPoints[0], modelName, modelName, merges, output_names, modelIndex);
                checkPoints.RemoveAt(0);
                merges.RemoveAt(0);
                
                modelCount++;
            }

            //addToCode(""); //spacing

            //combine everything into one model
            //repeat for each individual model in the graph
            for(int i=0; i<numModels; i++) {
                addToCode("model_" + i + " = Model(inputs=[", false);
                for(int j=0; j<input_names[i].Count; j++) {
                    addToCode(input_names[i][j], false);
                    if(i < input_names[i].Count-1)
                        addToCode(",", false);
                }

                addToCode("], outputs=[", false);
                for(int j=0; j<output_names[i].Count; j++) {
                    addToCode(output_names[i][j], false);
                    if(i < output_names[i].Count-1)
                        addToCode(",", false);
                }

                addToCode("])");
            }

            //copy the code to the user's clipboard and notify the user
            GUIUtility.systemCopyBuffer = compiledCode;
            compiledNotification.SetTrigger("Show");

            Debug.Log(compiledCode); //for my sanity

            //update the text in the preview window with the compiled code
            ScreenSpaceUI.staticRef.CodePreview.text = compiledCode;
            ScreenSpaceUI.staticRef.previewCodeButton.SetActive(true); //will be set false once the graph is changed
        }
        else {
            failedNotification.SetTrigger("Show");

            Debug.LogError("Unable to compile! Reason:\n" + reasonForFailing);
        }
    }

    //traverse the model until a split or join is made in the graph
    private void traverseModel(GraphNode start, string inputName, string modelName, List<string[]> merges, List<string>[] output_names, int modelIndex, bool skipFirst=true, bool startWithInput=true, bool isSplit=false, int splitId=1) 
    {
        bool codeWritten = false;

        //trace the model
        GraphNode current = skipFirst ? start.OutputConnections[0] : start;
        bool firstLayer = startWithInput;
        bool firstLoop = true;

        while(current != null && current.OutputConnections.Length==1 && current.InputConnections.Length==1) {
            string baseName = modelName;
            if(firstLoop && isSplit)
                baseName += "_s" + splitId;

            addToCode(baseName + " = " + getCodeForNode(current) + "(" + (firstLayer ? inputName : modelName) + ")");
            current = current.OutputConnections[0];
            codeWritten = true;

            if(firstLoop && isSplit)
                modelName += "_s" + splitId;
            firstLayer = false;
            firstLoop = false;
        }

        //split node
        if(current != null && current.OutputConnections.Length != 1) {
            traverseModel(current.OutputConnections[0], inputName, modelName, merges, output_names, modelIndex, false, firstLayer, true, 1);
            traverseModel(current.OutputConnections[1], inputName, modelName, merges, output_names, modelIndex, false, firstLayer, true, 2);
        }
        else {
            //concat node
            if(current != null) {
                if(checkPoints.Contains(current) && current.NodeName.Equals("Concatenate")) {
                    merges[checkPoints.IndexOf(current)][1] = firstLayer ? inputName : modelName;
                }
                else {
                    checkPoints.Add(current);

                    string[] placeHolder = new string[2];
                    placeHolder[0] = firstLayer ? inputName : modelName;
                    merges.Add(placeHolder);
                }
            }
            else {
                //output node
                output_names[modelIndex].Add(firstLayer ? inputName : modelName);
            }
        }

        if(codeWritten)
            addToCode(""); //start new line
    }

    private bool graphCanBeCompiled() 
    {
        //check to make sure graph can be compiled
        if(GraphNodes.Count == 0) {
            reasonForFailing = "Empty graph";
            return false; //just stop here, there's nothing to compile lol
        }

        //reset all ids(useful for tracing later)
        foreach(GraphNode n in GraphNodes) {
            n.id = -1;
        }

        //trace graphns to check for infinite loops:
        visitedNodes = new List<GraphNode>();
        checkPoints = new List<GraphNode>();

        bool goodToCompile = true;
        int id = 0; //to keep track of the different models that may be in a single graph
        foreach(GraphNode node in GraphNodes) {
            if(node.NodeName.ToLower().Equals("input")) {
                goodToCompile = goodToCompile && canBeCompiled(node, id++);
            }
        }

        //loop until there are no more checkpoints
        while(checkPoints.Count > 0) {
            GraphNode temp = checkPoints[0];
            checkPoints.RemoveAt(0);
            goodToCompile = goodToCompile && canBeCompiled(temp.OutputConnections[0], temp.id);
        }

        //count models
        List<int> models = new List<int>();
        foreach(GraphNode n in GraphNodes) {
            if(!models.Contains(n.id) && n.id != -1)
                models.Add(n.id);
        }
        numModels = models.Count;

        return goodToCompile;
    }

    private bool canBeCompiled(GraphNode node, int id) 
    {
        if(node == null)
            return true;
        node.id = id;

        if(!node.NodeName.Equals("Input") && !requiredLayers.Contains(node.NodeName))
            requiredLayers.Add(node.NodeName);

        if(node.InputConnections.Length > 1) {
            //make sure all of the inputs are satisfied
            foreach(GraphNode inputN in node.InputConnections) {
                if(inputN != null)
                    inputN.id = id; //link input node to the rest of the model

                List<GraphNode> traversedNodes = new List<GraphNode>();
                if(inputN == null || !canBeTracedUpToInput(inputN, id, traversedNodes)) {
                    reasonForFailing = "Incomplete graph (Concatenate doesn't have all inputs satisfied)";
                    return false;
                }
            }

            if(!checkPoints.Contains(node))
                checkPoints.Add(node);
            if(!visitedNodes.Contains(node))
                visitedNodes.Add(node);
            return true; //more than out output or input, come back to this later
        }

        /*
        if(visitedNodes.Contains(node)) {
            reasonForFailing = "Infinite loop in graph detected";
            return false; //prevents infinite loop i think
        }
        */

        visitedNodes.Add(node);

        if(node.OutputConnections.Length > 1)
            return canBeCompiled(node.OutputConnections[0], id) && canBeCompiled(node.OutputConnections[1], id);
        return canBeCompiled(node.OutputConnections[0], id);
    }

    private bool canBeTracedUpToInput(GraphNode node, int sourceID, List<GraphNode> traversedNodes) 
    {
        if(node == null)
            return false;
        if(node.NodeName.ToLower().Equals("input"))
            return true;

        bool canBeTraced = true;
        foreach(GraphNode inputN in node.InputConnections) {
            if(inputN == null || traversedNodes.Contains(inputN))
                return false;

            traversedNodes.Add(inputN);
            inputN.id = sourceID;
            canBeTraced = canBeTraced && canBeTracedUpToInput(inputN, sourceID, traversedNodes);
        }

        return canBeTraced;
    }

    private void addToCode(string next, bool addNewLine=true) {
        //helper method to make my code a little more bearable to read
        compiledCode += next;
        if(addNewLine)
            compiledCode += '\n';
    }

    private string getCodeForNode(GraphNode node) {
        string code = node.NodeName + "("; // example: Dense(

        switch(node.NodeName.ToLower()) 
        {
            case "dense":
                code += "units=" + node.units + ", activation=\'" + node.activation + "\')";
                break;

            case "conv2d":
                code += "filters=" + node.units + ", kernel_size=(" + node.kernelShape.toString() + "), ";
                if(node.strides.sizes[0] != 0 && node.strides.sizes[1] != 0)
                    code += "strides=(" + node.strides.toString() + "), ";
                code += "activation=\'" + node.activation + "\', padding=\'same\')"; //TODO: Add padding option to the node settings
                break;

            case "flatten":
                code += ")";
                break;

            default:
                code += "ERROR: NODE NOT IMPLEMENTED YET)";
                Debug.LogWarning("Node \"" + node.NodeName.ToUpper() + "\" not implemented yet.");
                break;
        }

        return code;
    }

    private void initStringListArray(List<string>[] arr) {
        for(int i=0; i<arr.Length; i++) {
            arr[i] = new List<string>();
        }
    }

    private void ManageInteractivity() 
    {
        RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));

        //handle selection clips
        if(Input.GetMouseButtonDown(0)) { 
            if(hit.collider!=null) {
                //check to see what was hit
                if(hit.collider.CompareTag("GraphLine")) {
                    FancyLine.selectedLine = hit.collider.GetComponent<FancyLine>();
                    GraphNode.SelectedNode = null;
                }
                else {
                    FancyLine.selectedLine = null;
                }
            }
            else {
                FancyLine.selectedLine = null;
                GraphNode.SelectedNode = null;
            }
        }

        //handle highlighting
        if(hit.collider!=null && hit.collider.CompareTag("GraphLine") && MouseInventory.HeldConnection == null)
            FancyLine.highlightedLine = hit.collider.GetComponent<FancyLine>();
        else
            FancyLine.highlightedLine = null;

        //handle deleting connections
        if(Input.GetKeyDown(KeyCode.Backspace)) {
            if(GraphNode.SelectedNode != null) {
                GraphNode.SelectedNode.Delete();
                ScreenSpaceUI.staticRef.previewCodeButton.SetActive(false); //graph updated, hide the preview button since the code changed
            }
            if(FancyLine.selectedLine != null) {
                FancyLine.selectedLine.Delete();
                ScreenSpaceUI.staticRef.previewCodeButton.SetActive(false);
            }
        }
    }

    private void ScanForOnScreenNodes() 
    {
        //scan for nodes in the graph that are on screen and if there are none, show the "return to graph" button to save the user from getting lost in the infinite void
        bool onScreen = GraphNodes.Count == 0;
        
        foreach(GraphNode node in GraphNodes) {
            if(node.NodeOnScreen) {
                onScreen = true;
                break;
            }
        }

        if(onScreen)
            ScreenSpaceUI.staticRef.returnToGraphButton.SetActive(false);
        else
            ScreenSpaceUI.staticRef.returnToGraphButton.SetActive(true);
    }

    public void copyCodeToClipboard() {
        GUIUtility.systemCopyBuffer = compiledCode;
        compiledNotification.SetTrigger("Show");
    }

    private void restoreGraph(GraphState state) 
    {
        //clear existing graph
        int numNodes = GraphNodes.Count;
        for(int i=0; i<numNodes; i++) {
            GraphNodes[0].Delete();
        }

        //instantiate nodes from graph node
        foreach(SerializedGraphNode node in state.nodes) {
            GameObject toInstantiate = getPrefabForNodeName(node.type);

            if(toInstantiate != null) {
                GameObject temp = Instantiate(toInstantiate, Vector2.zero, Quaternion.identity);
                temp.transform.SetParent(ScreenSpaceUI.staticRef.worldSpaceUI.transform, false);
                temp.transform.position = new Vector2(node.posX, node.posY);

                GraphNodes.Add(temp.GetComponent<GraphNode>());

                //fill in data
                TransferSettings(node, temp.GetComponent<GraphNode>());
            }
            else {
                Debug.LogError("Error loading graph node type.");
            }
        }

        //create connections
        int index = 0;
        int conIndex; //connection index
        foreach(SerializedGraphNode node in state.nodes) {
            conIndex = 0;
            foreach(int inputID in node.Inputs) {
                if(inputID != -1) {
                    GraphNodes[index].InputConnections[conIndex] = GraphNodes[inputID];
                }

                conIndex++;
            }

            conIndex = 0;
            foreach(int outputID in node.Outputs) {
                if(outputID != -1) {
                    GraphNodes[index].OutputConnections[conIndex] = GraphNodes[outputID];
                }

                conIndex++;
            }

            index++;
        }

        //create line connection visuals
        FancyLine lineHolder;
        List<int[]> madeConnections = new List<int[]>();
        
        index = 0; //to keep track of the index of the current node in the below foreach loop
        foreach(GraphNode node in GraphNodes) {
            conIndex = 0;
            foreach(GraphNode input in node.InputConnections) {
                int[] connectionQuery = {index, GraphNodes.IndexOf(input)};

                if(input != null && !findConnectionMatch(madeConnections, connectionQuery)) {
                    int outputIndex = state.nodes[GraphNodes.IndexOf(input)].Outputs.IndexOf(index);//the index of the connection coming out of the node that's feeding as input to this node

                    lineHolder = Instantiate(fancyLinePrefab, Vector2.zero, Quaternion.identity);
                    lineHolder.StartPos = node.NodeInputs[conIndex];
                    lineHolder.EndPos = input.NodeOutputs[outputIndex].gameObject;

                    madeConnections.Add(connectionQuery); //mark the connection as created to avoid duplicate connections
                }

                conIndex++;
            }

            conIndex = 0;
            foreach(GraphNode output in node.OutputConnections) {
                int[] connectionQuery = {index, GraphNodes.IndexOf(output)};

                if(output != null && !findConnectionMatch(madeConnections, connectionQuery)) {
                    int inputIndex = state.nodes[GraphNodes.IndexOf(output)].Inputs.IndexOf(index);//the index of the connection going into the node that's taking the output of this node

                    lineHolder = Instantiate(fancyLinePrefab, Vector2.zero, Quaternion.identity);
                    lineHolder.StartPos = node.NodeOutputs[conIndex];
                    lineHolder.EndPos = output.NodeInputs[inputIndex].gameObject;

                    madeConnections.Add(connectionQuery); //mark the connection as created to avoid duplicate connections
                }

                conIndex++;
            }

            index++;
        }
    }

    private bool findConnectionMatch(List<int[]> list, int[] query) {
        bool found = false;

        foreach(int[] pair in list) {
            if((query[0] == pair[0] && query[1] == pair[1]) || (query[0] == pair[1] && query[1] == pair[0])) {
                found = true;
                break;
            }
        }

        return found;
    }

    private GameObject getPrefabForNodeName(string name) 
    {
        GameObject found = null;
        foreach(GraphNode node in ScreenSpaceUI.staticRef.possibleNodes) {
            if(node.NodeName.Equals(name)) {
                found = node.gameObject;
                break;
            }
        }

        return found;
    }

    //transfers data from a serialized node to a regular graph node
    private void TransferSettings(SerializedGraphNode from, GraphNode to) {
        to.inputShape = from.inputShape;
        to.units = from.units;
        to.activation = from.activation;
        to.dropout = from.dropout;
        to.kernelShape = from.kernelShape;
        to.strides = from.strides;
        to.returnSequences = from.returnSequences;
        to.returnState = from.returnState;
        to.recurrentDropout = from.recurrentDropout;
    }
}
