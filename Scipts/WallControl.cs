using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallControl : MonoBehaviour
{
    private MeshRenderer render;
    void Start()
    {
        render = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        render.material.mainTextureScale = new Vector2(transform.parent.localScale.z, transform.localScale.y);
    }
}
