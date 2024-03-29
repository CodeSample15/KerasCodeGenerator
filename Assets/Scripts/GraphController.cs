using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphController : MonoBehaviour
{
    public static GraphController staticReference;

    //for notifying the user of their code generation status
    [SerializeField] private Animator compiledNotification;
    [SerializeField] private Animator failedNotification;

    public List<GraphNode> GraphNodes;

    private List<GraphNode> visitedNodes;
    private List<GraphNode> checkPoints;
    private List<string> requiredLayers;

    private string compiledCode;
    private int numModels; //the number of individual, non connected models in the graph

    private string reasonForFailing = "";

    void Awake() 
    {
        compiledCode = "";
    }

    void Update() 
    {
        staticReference = this;

        ManageInteractivity();
    }

    public void CompileGraph() 
    {
        //data structures and algorithms hell:

        requiredLayers = new List<string>();
        
        if(graphCanBeCompiled()) { //also counts how many individual models are in the graph and assigns the value to numModels
            //reset the output string
            compiledCode = "";
            addToCode("from keras.models import Model");
            addToCode("from keras.layers import Input", false);
            foreach(string layer in requiredLayers)
                addToCode(", " + layer, false);
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
                    addToCode("");

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

            addToCode(""); //spacing

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

            Debug.Log(compiledCode);
        }
        else {
            failedNotification.SetTrigger("Show");

            Debug.LogError("Unable to compile! Reason:\n" + reasonForFailing);
        }
    }

    //traverse the model until a split or join is made in the graph
    private void traverseModel(GraphNode start, string inputName, string modelName, List<string[]> merges, List<string>[] output_names, int modelIndex) 
    {
        //trace the model
        GraphNode current = start.OutputConnections[0];
        bool firstLayer = true;
        while(current != null && current.OutputConnections.Length==1 && current.InputConnections.Length==1) {
            addToCode(modelName + " = " + getCodeForNode(current) + "(" + (firstLayer ? inputName : modelName) + ")");
            current = current.OutputConnections[0];

            firstLayer = false;
        }

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
            output_names[modelIndex].Add(modelName);
        }
    }

    private bool graphCanBeCompiled() 
    {
        //check to make sure graph can be compiled

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

        if(node.OutputConnections.Length > 1 || node.InputConnections.Length > 1) {
            //make sure all of the inputs are satisfied
            foreach(GraphNode inputN in node.InputConnections) {
                if(inputN != null)
                    inputN.id = id; //link input node to the rest of the model

                if(inputN == null || !canBeTracedUpToInput(inputN, id)) {
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

        if(node.OutputConnections[0] == null)
            return true;

        return canBeCompiled(node.OutputConnections[0], id);
    }

    private bool canBeTracedUpToInput(GraphNode node, int sourceID) 
    {
        if(node == null)
            return false;
        if(node.NodeName.ToLower().Equals("input"))
            return true;

        bool canBeTraced = true;
        foreach(GraphNode inputN in node.InputConnections) {
            if(inputN == null)
                return false;

            inputN.id = sourceID;
            canBeTraced = canBeTraced && canBeTracedUpToInput(inputN, sourceID);
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
                Debug.LogWarning("Node not found to convert to code.");
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
            if(GraphNode.SelectedNode != null)
                GraphNode.SelectedNode.Delete();
            if(FancyLine.selectedLine != null)
                FancyLine.selectedLine.Delete();
        }
    }
}
