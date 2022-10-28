using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform target;

    private float finalRot, currentRot;
    private float finalHeight, currentHeight;
    public float speedRot, speedHeight, distance, height;
    public float speedMove, speedZoom, speedOrbit;
    void Update()
    {
        //moving the camera across the scene
        if(Input.GetMouseButton(2))
        {//transladar el target en base al movimiento en Y del mouse
            target.Translate(Vector3.back * Input.GetAxis("Mouse Y") * speedMove * Time.deltaTime);
            //transladar el target en base al movimiento en X del mouse
            target.Translate(Vector3.left * Input.GetAxis("Mouse X") * speedMove * Time.deltaTime);
        }

        else if(Input.GetMouseButton(1))
        {
            // modificar la altura
            height += Input.GetAxis("Mouse Y");
            if (height < 1) height = 1;
            if (height > 15) height = 15;
        }
        //Zoom
        if(Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            distance -= Input.GetAxis("Mouse ScrollWheel") * speedZoom ;
            if (distance < 1) distance = 1; //limite de zoom a 1 con el target
        }
        //rotar:
        target.Rotate(Vector3.down * speedOrbit * Input.GetAxis("Horizontal") * Time.deltaTime);




        finalRot = target.eulerAngles.y;
        currentRot = transform.eulerAngles.y;
        finalHeight = target.position.y + height;
        currentHeight = transform.position.y;

        currentRot = Mathf.LerpAngle(currentRot, finalRot, speedRot * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, finalHeight, speedHeight * Time.deltaTime);

        Quaternion tempRot = Quaternion.Euler(0, currentRot, 0);
        transform.position = target.position;
        transform.position -= tempRot * Vector3.forward * distance;//el distance es el ZOOM

        Vector3 tempPos = transform.position;
        tempPos.y = currentHeight;
        transform.position = tempPos;

        transform.LookAt(target);
    }
}
