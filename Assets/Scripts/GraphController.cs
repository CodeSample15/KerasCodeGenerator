using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GraphController : MonoBehaviour
{
    public static GraphController staticReference;
    public List<GraphNode> GraphNodes;

    public List<GraphNode> visitedNodes;
    private List<GraphNode> checkPoints;

    void Update() 
    {
        staticReference = this;

        ManageInteractivity();
    }

    public void CompileGraph() 
    {
        //HOOO BOY HERE COMES A BIG BOY ALGORITHM
        if(graphCanBeCompiled()) {
            Debug.Log("yipeee");
        }
    }

    private bool graphCanBeCompiled() 
    {
        //check to make sure graph can be compiled

        //trace graphns to check for infinite loops:
        visitedNodes = new List<GraphNode>();
        checkPoints = new List<GraphNode>();

        bool goodToCompile = true;
        foreach(GraphNode node in GraphNodes) {
            if(node.NodeName.ToLower().Equals("input"))
                goodToCompile = goodToCompile && canBeCompiled(node);
        }

        //loop until there are no more checkpoints
        while(checkPoints.Count() > 0) {
            GraphNode temp = checkPoints[0];
            checkPoints.RemoveAt(0);
            goodToCompile = goodToCompile && canBeCompiled(temp.OutputConnections[0]);
        }

        return goodToCompile;
    }

    private bool canBeCompiled(GraphNode node) 
    {
        if(node == null)
            return true;

        if(node.OutputConnections.Count() > 1 || node.InputConnections.Count() > 1) {
            //make sure all of the inputs are satisfied
            foreach(GraphNode inputN in node.InputConnections)
                if(inputN == null || !canBeTracedUpToInput(inputN))
                    return false;

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

        return canBeCompiled(node.OutputConnections[0]);
    }

    private bool canBeTracedUpToInput(GraphNode node) 
    {
        if(node == null)
            return false;
        if(node.NodeName.ToLower().Equals("input"))
            return true;

        bool canBeTraced = true;
        foreach(GraphNode inputN in node.InputConnections) {
            if(inputN == null)
                return false;

            canBeTraced = canBeTraced && canBeTracedUpToInput(inputN);
        }

        return canBeTraced;
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
