using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Object", menuName ="Object/New Object")]
public class ObjectProperties : ScriptableObject
{
    public int id;
    public ObjectType type;
    public int[] textureId;
    public string propName;
}
