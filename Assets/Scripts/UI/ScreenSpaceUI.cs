using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSpaceUI : MonoBehaviour
{
    public static ScreenSpaceUI staticRef;

    [Header("Prefabs:")]
    [SerializeField] public List<GraphNode> possibleNodes; //possible nodes that can be added to the graph (basically all of the nodes that I've implemented so far)
    [SerializeField] private GameObject addNodeMenuButtonTemplate;
    [SerializeField] private TextMeshProUGUI LabelText;
    [SerializeField] private GameObject checkBox;
    [SerializeField] private GameObject shapeInputField;
    [SerializeField] private GameObject textInput;
    [SerializeField] private GameObject dropDown;
    [SerializeField] private GameObject integerOnlyInputField;
    [SerializeField] private GameObject floatOnlyInputField;


    [Header("In scene objects:")]
    [SerializeField] public GameObject worldSpaceUI;
    [SerializeField] private GameObject addNodeMenuListObject;
    [SerializeField] private GameObject OptionsMenu;
    [SerializeField] private GameObject OptionsMenuSettingsGrid;
    [SerializeField] private TextMeshProUGUI OptionsMenuTitle;
    public GameObject previewCodeButton; //this, and all the other public fields, will be accessed by the GraphController and GraphNode. Just putting the references here for organization
    public TMP_InputField CodePreview;
    public GameObject returnToGraphButton;
    public GameObject loadWarning; //the thing that warns the user that their currently open graph will be deleted if they try to load one from file


    [Header("Animations:")]
    [SerializeField] private Animator AddNodeMenuAnims;

    private bool showingAddNodeMenu; //so I don't have to do an ugly meno.getbool() line
    private GraphNode currentOptionsNode;
    private GameObject[] nodeSettings;
    private GameObject[] nodeSettingsMenuObjects;

    private bool showingCodePrev;

    public bool ShowingCodePrev {
        get {return showingCodePrev; }
    }

    void Awake() 
    {
        showingAddNodeMenu = false;
        AddNodeMenuAnims.SetBool("Showing", false);
        OptionsMenu.SetActive(false);
        currentOptionsNode = null;

        staticRef = this;

        previewCodeButton.SetActive(false);
        CodePreview.gameObject.SetActive(false);

        showingCodePrev = false;

        loadWarning.SetActive(false);

        InitNodeAddMenu();
    }

    public void showNodeOptionsMenu(GraphNode node) 
    {
        AddNodeMenuAnims.SetBool("Showing", false);
        showingAddNodeMenu = false;

        currentOptionsNode = node;

        OptionsMenu.SetActive(true);
        OptionsMenuTitle.SetText(node.NodeName);

        //add all the settings to the menu
        if(!node.NodeName.ToLower().Equals("input")) {
            nodeSettings = new GameObject[node.NodeOptions.Length];
            nodeSettingsMenuObjects = new GameObject[node.NodeOptions.Length*2];

            int counter = 0;
            foreach(string setting in node.NodeOptions) {
                GameObject temp;

                switch(setting.ToLower()) 
                {
                    case "input size":
                        //label
                        addLabelToNodeSettingsMenu("Input size:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(shapeInputField, counter);
                        temp.GetComponent<SizeInputField>().init(2);
                        temp.GetComponent<SizeInputField>().populate(node.inputShape);
                        break;

                    case "units":
                        //label
                        addLabelToNodeSettingsMenu("Units:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(integerOnlyInputField, counter);
                        temp.GetComponent<TMP_InputField>().text = node.units.ToString();
                        break;

                    case "activation":
                        //label
                        addLabelToNodeSettingsMenu("Activation:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(textInput, counter);
                        temp.GetComponent<TMP_InputField>().text = node.activation;
                        break;

                    case "dropout":
                        //label
                        addLabelToNodeSettingsMenu("Dropout:", counter);
                        
                        //setting
                        temp = addSettingToNodeSettingsMenu(floatOnlyInputField, counter);
                        temp.GetComponent<TMP_InputField>().text = node.dropout.ToString();
                        break;

                    case "kernel shape":
                        //label
                        addLabelToNodeSettingsMenu("Kernel shape:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(shapeInputField, counter);
                        temp.GetComponent<SizeInputField>().init(2);
                        temp.GetComponent<SizeInputField>().populate(node.kernelShape);
                        break;

                    case "strides":
                        //label (this commenting is getting redundant)
                        addLabelToNodeSettingsMenu("Strides:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(shapeInputField, counter);
                        temp.GetComponent<SizeInputField>().init(2);
                        temp.GetComponent<SizeInputField>().populate(node.strides);
                        break;

                    case "return sequences":
                        //label
                        addLabelToNodeSettingsMenu("Return sequences:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(checkBox, counter);
                        temp.GetComponent<Toggle>().isOn = node.returnSequences;
                        break;

                    case "recurrent dropout":
                        //label
                        addLabelToNodeSettingsMenu("Recurrent dropout:", counter);

                        //setting
                        temp = addSettingToNodeSettingsMenu(floatOnlyInputField, counter);
                        temp.GetComponent<TMP_InputField>().text = node.recurrentDropout.ToString();
                        break;
                }

                counter++;
            }
        }
        else {
            nodeSettings = new GameObject[2];
            nodeSettingsMenuObjects = new GameObject[4];

            //the node is an input node, do some special shit to handle variable input size
            addLabelToNodeSettingsMenu("Input dim:", 0);
            
            GameObject tempDim = addSettingToNodeSettingsMenu(integerOnlyInputField, 0);
            tempDim.GetComponent<TMP_InputField>().text = node.inputShape.dimension().ToString();

            addLabelToNodeSettingsMenu("Input shape:", 1);

            GameObject tempShape = addSettingToNodeSettingsMenu(shapeInputField, 1);
            tempShape.GetComponent<SizeInputField>().init(node.inputShape.dimension());
            tempShape.GetComponent<SizeInputField>().populate(node.inputShape);
            tempShape.GetComponent<SizeInputField>().setDimWatch(tempDim.GetComponent<TMP_InputField>());
        }
    }

    public void hideNodeOptionsMenu() 
    {
        OptionsMenu.SetActive(false);

        //Save all of the settings the user entered
        saveCurrentNodeSettings();
        clearOptionsFromMenu();

        currentOptionsNode.GetComponent<Image>().color = currentOptionsNode.NormalColor;
        currentOptionsNode.editing = false;
        currentOptionsNode = null;
    }

    private void addLabelToNodeSettingsMenu(string label, int counter) {
        GameObject temp = Instantiate(LabelText.gameObject);
        temp.transform.SetParent(OptionsMenuSettingsGrid.transform, false);
        temp.GetComponent<TextMeshProUGUI>().SetText(label);
        nodeSettingsMenuObjects[counter*2] = temp;
    }

    private GameObject addSettingToNodeSettingsMenu(GameObject template, int counter) {
        GameObject temp = Instantiate(template);
        temp.transform.SetParent(OptionsMenuSettingsGrid.transform, false);
        nodeSettings[counter] = temp;
        nodeSettingsMenuObjects[(counter*2)+1] = temp;

        return temp;
    }

    private void clearOptionsFromMenu() 
    {
        foreach(GameObject obj in nodeSettingsMenuObjects)
            Destroy(obj);
    }

    private void saveCurrentNodeSettings()
    {
        if(!currentOptionsNode.NodeName.ToLower().Equals("input")) {
            int counter = 0;
            Shape temp;
            foreach(string setting in currentOptionsNode.NodeOptions) {
                switch(setting.ToLower()) 
                {
                    case "input size":
                        currentOptionsNode.inputShape = nodeSettings[counter].GetComponent<SizeInputField>().getResult();
                        break;

                    case "units":
                        currentOptionsNode.units = int.Parse(nodeSettings[counter].GetComponent<TMP_InputField>().text);
                        break;

                    case "activation":
                        currentOptionsNode.activation = nodeSettings[counter].GetComponent<TMP_InputField>().text;
                        break;

                    case "dropout":
                        currentOptionsNode.dropout = float.Parse(nodeSettings[counter].GetComponent<TMP_InputField>().text);
                        break;

                    case "kernel shape":
                        temp = nodeSettings[counter].GetComponent<SizeInputField>().getResult();
                        removeOOBValuesFromShape(temp, 1);
                        currentOptionsNode.kernelShape = temp;
                        break;

                    case "strides":
                        temp = nodeSettings[counter].GetComponent<SizeInputField>().getResult();
                        removeOOBValuesFromShape(temp, 0);
                        currentOptionsNode.strides = temp;
                        break;

                    case "return sequences":
                        currentOptionsNode.returnSequences = nodeSettings[counter].GetComponent<Toggle>().isOn;
                        break;

                    case "recurrent dropout":
                        currentOptionsNode.recurrentDropout = float.Parse(nodeSettings[counter].GetComponent<TMP_InputField>().text);
                        break;
                }

                counter++;
            }
        }
        else {
            currentOptionsNode.inputShape = nodeSettings[1].GetComponent<SizeInputField>().getResult();
        }

        currentOptionsNode.updateDescription(); //update the visual on the node that displays important information so editing graphs is easier
    }

    private void removeOOBValuesFromShape(Shape shape, int l_bound=0, int h_bound=-1) {
        for(int i=0; i<shape.sizes.Length; i++) {
            if(shape.sizes[i] < l_bound)
                shape.sizes[i] = l_bound;
            if(h_bound != -1 && shape.sizes[i] > h_bound)
                shape.sizes[i] = h_bound;
        }
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
            temp.GetComponent<Image>().color = n.GetComponent<Image>().color;

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

        temp.transform.position = new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(-3.5f, 3.5f)); //prevent stacking of new nodes

        GraphController.staticReference.GraphNodes.Add(temp.GetComponent<GraphNode>()); //update the graph
    }

    //these two methods will be linked to the code preview button through the editor
    public void showCodePreview() 
    {
        CodePreview.gameObject.SetActive(true);
        showingCodePrev = true;
    }

    public void hideCodePreview() 
    {
        CodePreview.gameObject.SetActive(false);
        showingCodePrev = false;
    }

    //will be linked to the cancel button on the load warning gameobject (referenced in this file)
    public void hideLoadWarning() {
        loadWarning.SetActive(false);
    }
}
