using System;
using System.Collections.Generic;
using System.Diagnostics;

//will record the current state of the graph and will be used to save the data to the disk

[System.Serializable]
public class GraphState
{
    public List<SerializedGraphNode> nodes;

    public GraphState() {
        nodes = new List<SerializedGraphNode>();
    }

    public GraphState(List<GraphNode> nodes) {
        this.nodes = new List<SerializedGraphNode>();

        //add all the nodes and their settings
        foreach(GraphNode node in nodes) {
            //convert the node into a serialized node
            this.nodes.Add(new SerializedGraphNode(node));
        }

        //link the connections
        int index = 0;
        foreach(GraphNode node in nodes) {
            int conIndex = 0;
            foreach(GraphNode input in node.InputConnections) {
                if(nodes.Contains(input)) {
                    this.nodes[index].Inputs.Add(nodes.IndexOf(input)); //nodes.IndexOf(input) should be the ID of the input node saved in the serialized graph node
                }
                else {
                    this.nodes[index].Inputs.Add(-1);
                }

                conIndex++;
            }

            conIndex = 0;
            foreach(GraphNode output in node.OutputConnections) {
                if(nodes.Contains(output)) {
                    this.nodes[index].Outputs.Add(nodes.IndexOf(output));
                }
                else {
                    this.nodes[index].Outputs.Add(-1);
                }

                conIndex++;
            }

            index++;
        }
    }
}
