using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializedGraphNode
{
    //contains all of the settings found in a graph node as well as the position for the graphnode
    public float posX;
    public float posY;

    public string type;

    public Shape inputShape;
    public int units;
    public string activation;
    public float dropout;
    public Shape kernelShape;
    public Shape strides;
    public bool returnSequences;
    public bool returnState;
    public float recurrentDropout;

    public List<int> Inputs;
    public List<int> InputIndexes;
    public List<int> Outputs;
    public List<int> OutputIndexes;

    public SerializedGraphNode(GraphNode node) {
        posX = node.transform.position.x;
        posY = node.transform.position.y;

        type = node.NodeName;

        //maybe in the future develop a singular class (NodeSettings) that holds all of these options so I don't need to update multiple files when adding more options to the nodes
        inputShape = node.inputShape;
        units = node.units;
        activation = node.activation;
        dropout = node.dropout;
        kernelShape = node.kernelShape;
        strides = node.strides;
        returnSequences = node.returnSequences;
        returnState = node.returnState;
        recurrentDropout = node.recurrentDropout;

        Inputs = new List<int>();
        Outputs = new List<int>();
    }
}
