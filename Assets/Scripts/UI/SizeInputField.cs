using System;
using UnityEngine;
using TMPro;

public class SizeInputField : MonoBehaviour
{
    public GameObject inputField; //prefab
    private TMP_InputField[] inputFields;
    private TMP_InputField monitoringDim;

    void Update() 
    {
        //suuuuper duper inefficient, but who tf gives a shit anyways
        //ill replace the array of input fields with a list if this project gains any popularity whatsoever (i doubt it)
        if(monitoringDim != null) {
            int newDim = monitoringDim.text.Equals("") || int.Parse(monitoringDim.text) < 1 || int.Parse(monitoringDim.text) > 4 ? 1 : int.Parse(monitoringDim.text);

            if(newDim != inputFields.Length) {
                foreach(TMP_InputField field in inputFields) {
                    Destroy(field.gameObject);
                }

                inputFields = new TMP_InputField[newDim];
                for(int i=0; i<newDim; i++) {
                    GameObject temp = Instantiate(inputField);
                    temp.transform.SetParent(transform, false);
                    inputFields[i] = temp.GetComponent<TMP_InputField>();
                }
            }
        }
    }

    public void setDimWatch(TMP_InputField dim) {
        monitoringDim = dim;
    }

    public void init(int size) {
        inputFields = new TMP_InputField[size];

        for(int i=0; i<size; i++) {
            GameObject temp = Instantiate(inputField);
            temp.transform.SetParent(transform, false);
            inputFields[i] = temp.GetComponent<TMP_InputField>();
        }
    }

    public void populate(string[] values) 
    {
        if(values.Length != inputFields.Length)
            return;

        for(int i=0; i<values.Length; i++) {
            inputFields[i].SetTextWithoutNotify(values[i]);
        }
    }

    public void populate(Shape values) 
    {
        if(values.sizes.Length != inputFields.Length)
            return;

        for(int i=0; i<values.sizes.Length; i++) {
            inputFields[i].SetTextWithoutNotify(values.sizes[i].ToString());
        }
    }

    public Shape getResult() 
    {
        Shape res = new Shape(inputFields.Length);

        for(int i=0; i<inputFields.Length; i++) {
            res.sizes[i] = inputFields[i].text.Equals("") || Int32.Parse(inputFields[i].text) < 0 ? 0 : Int32.Parse(inputFields[i].text);
        }

        return res;
    }
}
