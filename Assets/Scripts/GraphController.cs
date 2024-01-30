using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphController : MonoBehaviour
{
    public static GraphController staticReference;
    public List<GraphNode> GraphNodes;

    private List<GraphNode> visitedNodes;
    private List<GraphNode> checkPoints;
    private List<string> requiredLayers;

    private string compiledCode;
    private int numModels; //the number of individual, non connected models in the graph

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
        
        if(graphCanBeCompiled()) {
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
            List<string> input_names = new List<string>();
            List<string> output_names = new List<string>();

            foreach(GraphNode node in GraphNodes) {
                if(node.NodeName.Equals("Input")) {
                    string inputName = "input_" + modelCount;
                    string modelName = "m_" + modelCount;

                    input_names.Add(inputName);

                    //add input to model
                    addToCode(inputName + " = Input(shape=(", false);
                    addToCode(node.inputShape.toString(), false);
                    addToCode("))");

                    
                    traverseModel(node, inputName, modelName, merges, output_names);
                    addToCode("");

                    modelCount++;
                }
            }

            //combine models that are joined by concat
            //repeat until no more nodes remain
            while(checkPoints.Count > 0) {
                string modelName = "model_" + modelCount;

                addToCode(modelName + " = Concatenate([" + merges[0][0] + ", " + merges[0][1] + "])");

                traverseModel(checkPoints[0], modelName, modelName, merges, output_names);
                checkPoints.RemoveAt(0);
                merges.RemoveAt(0);
                
                modelCount++;
            }

            //combine everything into one model
            addToCode(""); //spacing

            addToCode("model = Model(inputs = [", false);
            for(int i=0; i<input_names.Count; i++) {
                addToCode(input_names[i], false);
                if(i < input_names.Count-1)
                    addToCode(",", false);
            }

            addToCode("], outputs=[", false);
            for(int i=0; i<output_names.Count; i++) {
                addToCode(output_names[i], false);
                if(i < output_names.Count-1)
                    addToCode(",", false);
            }

            addToCode("])");

            Debug.Log(compiledCode);
        }
        else {
            Debug.Log("Unable to compile");
        }
    }

    //traverse the model until a split or join is made in the graph
    private void traverseModel(GraphNode start, string inputName, string modelName, List<string[]> merges, List<string> output_names) 
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
            output_names.Add(modelName);
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

                if(inputN == null || !canBeTracedUpToInput(inputN, id))
                    return false;
            }

            if(!checkPoints.Contains(node))
                checkPoints.Add(node);
            if(!visitedNodes.Contains(node))
                visitedNodes.Add(node);
            return true; //more than out output or input, come back to this later
        }

        if(visitedNodes.Contains(node))
            return false; //prevents infinite loop i think

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
                code += "filters=" + node.units + ", kernel_size=(" + node.kernelShape.toString() + "), strides=(" + node.strides.toString() + ")" + ", activation=" + node.activation + ")";
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
