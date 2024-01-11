using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphObject : MonoBehaviour
{
    public const float moveSpeed = 0.002f; 

    private bool moving = false;
    private Vector2 lastMousePosition = Vector2.zero;

    void Update() {
        if(Input.GetMouseButtonDown(1))
            moving = true;
        if(Input.GetMouseButtonUp(1))
            moving = false;

        if(moving)
            transform.Translate(((Vector2)Input.mousePosition - lastMousePosition) * moveSpeed * Mathf.Pow(Camera.main.orthographicSize, 1.4f));
        lastMousePosition = Input.mousePosition;
    }
}
