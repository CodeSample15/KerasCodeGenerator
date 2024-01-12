using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SizeInputField : MonoBehaviour
{
    public GameObject inputField; //prefab
    private TMP_InputField[] inputFields;

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
            res.sizes[i] = inputFields[i].text.Equals("") ? 0 : Int32.Parse(inputFields[i].text);
        }

        return res;
    }
}
