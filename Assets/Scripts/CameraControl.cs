using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraControl : MonoBehaviour
{
    [SerializeField] private float ScrollSpeed;
    [SerializeField] private float MaxSize;
    [SerializeField] private float MinSize;

    private Camera cam;

    void Awake() {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        //only scroll if mouse is on screen and not hovering over a menu
        if(!MouseHoverDetection.MouseHovering && Input.mousePosition.x > 0 && Input.mousePosition.x < Screen.width && Input.mousePosition.y > 0 && Input.mousePosition.y < Screen.height) {
            cam.orthographicSize -= Input.mouseScrollDelta.y * ScrollSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, MinSize, MaxSize);
        }
    }
}
