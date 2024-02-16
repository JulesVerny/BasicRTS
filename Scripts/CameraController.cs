using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // ===========================================================================================
    private Vector3 CameraPosition;

    [Header("Camera Settings")]
    public float CameraSpeed;
    // ===========================================================================================
    // Start is called before the first frame update
    void Start()
    {
        CameraPosition = transform.position;
    }
    // ===========================================================================================
    // Update is called once per frame
    void Update()
    {
        CameraPosition = transform.position;
        // Keyboard 
        if (Input.GetKey(KeyCode.UpArrow))
        {

            if(CameraPosition.x>-325.0f) CameraPosition.x = CameraPosition.x - CameraSpeed / 1.0f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (CameraPosition.x < 500.0f) CameraPosition.x = CameraPosition.x + CameraSpeed / 1.0f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            if (CameraPosition.z < 410.0f) CameraPosition.z = CameraPosition.z + CameraSpeed / 1.0f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (CameraPosition.z >-410.0f) CameraPosition.z = CameraPosition.z - CameraSpeed / 1.0f;
        }
        transform.position = CameraPosition;

    }
    // ===========================================================================================
}
