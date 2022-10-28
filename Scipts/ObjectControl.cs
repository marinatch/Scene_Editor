using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectControl : MonoBehaviour
{
    public ObjectProperties element;
    private AppManager manager;
    public GameObject objectDelete;
    public Renderer objMaterial;

    //necesitaremos esto para guardar la textura y los vértices
    public int currentIdTexture = -1;
    public List<Vector3> allVertex;


    private void Start()
    {
        currentIdTexture = -1;
        manager = GameObject.FindGameObjectWithTag("GameController").GetComponent<AppManager>();
    }
    /* private void OnMouseDown()
     {
         manager.SelectObject(this);
     }*/
    public void DestroyObject()
    {
        if(objectDelete != null) Destroy(objectDelete);
        else Destroy(gameObject);
    }

    //vamos a pasarle también el ID al setTexture
    public void SetTexture(Texture2D _albedo, int _id)
    {
        //vamos a asignar a nuestra variable currentIdTexture la ID 
        currentIdTexture = _id;
        objMaterial.material.SetTexture("_MainTex", _albedo);
    }

    public void SaveVertex(List<Vector3> _vertex)
    {
        allVertex = _vertex;
    }

}
