using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSpaceUI : MonoBehaviour
{
    public static ScreenSpaceUI staticRef;

    [Header("Prefabs:")]
    [SerializeField] private List<GraphNode> possibleNodes; //possible nodes that can be added to the graph (basically all of the nodes that I've implemented so far)
    [SerializeField] private GameObject addNodeMenuButtonTemplate;
    [SerializeField] private GameObject checkBox;
    [SerializeField] private GameObject shapeInputField;
    [SerializeField] private GameObject textInput;
    [SerializeField] private GameObject dropDown;
    [SerializeField] private GameObject integerOnlyInputField;
    [SerializeField] private GameObject floatOnlyInputField;


    [Header("In scene objects:")]
    [SerializeField] private GameObject worldSpaceUI;
    [SerializeField] private GameObject addNodeMenuListObject;
    [SerializeField] private GameObject OptionsMenu;
    [SerializeField] private TextMeshProUGUI OptionsMenuTitle;


    [Header("Animations:")]
    [SerializeField] private Animator AddNodeMenuAnims;

    private bool showingAddNodeMenu; //so I don't have to do an ugly meno.getbool() line
    private GraphNode currentOptionsNode;

    void Awake() 
    {
        showingAddNodeMenu = false;
        AddNodeMenuAnims.SetBool("Showing", false);
        OptionsMenu.SetActive(false);
        currentOptionsNode = null;

        staticRef = this;

        InitNodeAddMenu();
    }

    public void showNodeOptionsMenu(GraphNode node) 
    {
        AddNodeMenuAnims.SetBool("Showing", false);
        showingAddNodeMenu = false;

        currentOptionsNode = node;

        OptionsMenu.SetActive(true);

        //add all the settings to the menu (nightmare nightmare nightmare nightmare nightmare nightmare nightmare nightmare nightmare nightmare nightmare nightmare)
        foreach(string setting in node.NodeOptions) {
            switch(setting.ToLower()) 
            {
                case "input size":
                    break;

                case "units":
                    break;

                case "activation":
                    break;

                case "dropout":
                    break;

                case "kernel shape":
                    break;

                case "strides":
                    break;

                case "return sequences":
                    break;

                case "recurrent dropout":
                    break;
            }
        }
    }

    public void hideNodeOptionsMenu() 
    {
        OptionsMenu.SetActive(false);

        currentOptionsNode.GetComponent<Image>().color = currentOptionsNode.NormalColor;
        currentOptionsNode.editing = false;
        currentOptionsNode = null;
    }

    private void InitNodeAddMenu()
    {
        //sort nodes in template list by name
        possibleNodes.Sort();

        foreach(GraphNode n in possibleNodes) {
            //create button for each node type
            GameObject temp = Instantiate(addNodeMenuButtonTemplate, Vector2.zero, Quaternion.identity);
            temp.GetComponentInChildren<TextMeshProUGUI>().SetText(n.NodeName);
            temp.GetComponent<Button>().onClick.AddListener(() => addNewNodeToGraph(n));

            //add button to scrollable menu
            temp.transform.SetParent(addNodeMenuListObject.transform, false);
        }
    }

    public void toggleAddNodeMenu() {
        showingAddNodeMenu = !showingAddNodeMenu;
        AddNodeMenuAnims.SetBool("Showing", showingAddNodeMenu);
    }

    private void addNewNodeToGraph(GraphNode node) 
    {
        GameObject temp = Instantiate(node.gameObject, Vector2.zero, Quaternion.identity);
        temp.transform.SetParent(worldSpaceUI.transform, false);

        GraphController.staticReference.GraphNodes.Add(temp.GetComponent<GraphNode>()); //update the graph
    }
}
