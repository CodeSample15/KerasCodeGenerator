using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SizeInputField : MonoBehaviour
{
    public GameObject inputField; //prefab
    private InputField[] inputFields;

    public void init(int size) {
        inputFields = new InputField[size];

        for(int i=0; i<size; i++) {
            GameObject temp = Instantiate(inputField);
            temp.transform.SetParent(transform);
            inputFields[i] = temp.GetComponent<InputField>();
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
