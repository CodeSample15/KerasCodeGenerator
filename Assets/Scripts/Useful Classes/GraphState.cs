using System.Collections.Generic;

//will record the current state of the graph and will be used to save the data to the disk

[System.Serializable]
public class GraphState
{
    public List<GraphNode> nodes;

    public GraphState() {
        nodes = new List<GraphNode>();
    }

    public GraphState(List<GraphNode> nodes) {
        this.nodes = nodes;
    }
}
