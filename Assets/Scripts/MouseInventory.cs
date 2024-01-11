using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    Silly name for a class that does this, but I find if pretty descriptive considering the purpose of this file:
        - When a user drags our a connection node to connect one node to another, the node that the line will connect to needs to know where the line originated from
        - This file will hold what the mouse is currently holding
        - The mouse will be holding a connection node for a specific graph node
        - That graph node will be stored here by the origin graph node
        - Way easier than figuring out how to tell the destination node where a line is coming from
*/

public class MouseInventory : MonoBehaviour
{
    public static GraphNode HeldConnection; //when the user drags out a new connection node, it will "hold" the origin graph node
    public static bool isInputNode; //whether or not the held connection is an input or output node
    public static GraphNode HighlightedConnection;
    public static GameObject HighlightedGraphConnection;
    public static bool highlightingInputNode;

    public static void clearInventory() {
        HeldConnection = null;
        HighlightedConnection = null;
    }
}
