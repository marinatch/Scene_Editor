using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO; // --> nos va a permitir acceder a los documentos del ordenador
using System.Xml; // --> Nos va a permitir trabajar con achivos XML
using System.Xml.Serialization; //--> Nos va a permitir transformar datos de Unity a XMLL

//los iferentes estados de monupulacion
public enum ObjectType { None, Walls, Props, Floors }

public class AppManager : MonoBehaviour
{
    public List<ObjectControl> saveObjects; //= new list<ObjectControl>();

    public Transform mouseGizmo;
    private RaycastHit hit, hitSelection;
    private Ray ray, raySelection;
    public LayerMask groundMask, objectMask;

    public GameObject wall;
    private Transform currentWall;
    private Vector3 initWallPos;

    private bool configureObject;
    public GameObject objectInfo;
    private ObjectControl currentObject;
    public ObjectType currentType = ObjectType.None;

    public Transform grid, gridObjects;
    public GameObject baseTexture;

    //vertices
    public List<Vector3> vertexPosition;
    private Vector3 centerFloor;
    private float right, left, forward, back, totalWidth, totalHeight;
    public Material floorMaterial;
    private List<GameObject> gizmoFirstVertex = new List<GameObject>();

    private GameObject ghostProp, finalProp;
    private int currentProp;//como referencia al ID
    public GameObject moveButton;

    void Start()
    {
       ObjectProperties[] totalObjects = Resources.LoadAll<ObjectProperties>("Objects");
         for (int i = 0; i < totalObjects.Length; i++)
         {
             GameObject newObj = Instantiate(baseTexture, gridObjects);
             ObjectProperties tempObj = totalObjects[i];
             newObj.GetComponent<Button>().onClick.AddListener(delegate { ChoosePrefab(tempObj); });
         }
    }

    private void ChoosePrefab(ObjectProperties _obj)
    {
        if (currentWall != null) Destroy(currentWall.gameObject);
       // PrintButtons(_obj.id);
        currentType = _obj.type;
        if (currentType == ObjectType.Props)
        {
            currentProp = _obj.id;
        }

    }

    void Update()
    {
        GizmoPosition();
        if (configureObject == false)
        {
            switch (currentType)
            {
                case ObjectType.None:
                    SelectUpdate();
                    break;
                case ObjectType.Walls:
                    CreateWall();
                    break;
                case ObjectType.Floors:
                    CreateFloor();
                    break;
                case ObjectType.Props:
                    PropsUpdate();
                    break;
            }
        }
        else
        {
            
        }
        if (Input.GetKeyDown(KeyCode.Escape)) CancelSelection();
    }

    void PropsUpdate()
    {
        if(finalProp == null)
        {
            if(ghostProp == null)
            {
                GameObject findProp = Resources.Load<GameObject>("Prefabs/" + currentProp);
                ghostProp = Instantiate(findProp, mouseGizmo.position, Quaternion.Euler(0, 0, 0));
            }
            else //ya tengo ghostProp
            {
                ghostProp.transform.position = mouseGizmo.position;
                if (Input.GetMouseButtonDown(0))
                {
                    finalProp = ghostProp;
                    ghostProp = null;
                }
            }
        }
        else // ya tengo finalProp
        { //girar el objeto
            if (Input.GetMouseButton(0))
            {
                finalProp.transform.LookAt(mouseGizmo.position);
                if (Input.GetKey(KeyCode.Space))
                {
                    finalProp.transform.position = mouseGizmo.position;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                saveObjects.Add(finalProp.GetComponent<ObjectControl>());
                CancelSelection();
                currentType = ObjectType.None;
                finalProp = null;
            }

        }
    }

    void SelectUpdate()
    {
        //vamos a hacerlo atravez de los rayos
        if(Input.GetMouseButtonDown(0))
        {
            raySelection = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(raySelection, out hitSelection, Mathf.Infinity, (1 << 0))) //ponemos el LayerMask y así se crea desde el script en el escenario
            {//1. si estas colisionando contra algo...
                if(hitSelection.collider.GetComponent<ObjectControl>() != null)
                {//2. ...seleccionalo
                    SelectObject(hitSelection.collider.GetComponent<ObjectControl>());
                }
            }
        }
    }

    void CreateFloor()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 currentPoint = mouseGizmo.position;
            currentPoint.y = 0;

            if (hasVertex(currentPoint))
            {
                vertexPosition.Add(currentPoint);
                //Generar geometría
                GenerateFloor();
                //limpiar la pantalla de los puntos de gizmo
                for (int i = gizmoFirstVertex.Count - 1; i >= 0; i--)
                {
                    Destroy(gizmoFirstVertex[i]);
                }
                //gizmoFirstVertex.Clear();
                gizmoFirstVertex = new List<GameObject>();
            }
            else
            {
                //sino existe guardalo
                vertexPosition.Add(currentPoint);
                gizmoFirstVertex.Add(Instantiate(mouseGizmo, currentPoint, Quaternion.identity).gameObject);
            }
        }
    }

    void GenerateFloor()
    {
        centerFloor = GetCenterFloor();
        GameObject newFloor = new GameObject("FloorMesh");
        MeshFilter filter = newFloor.AddComponent<MeshFilter>();
        //acceder al MeshRenderer y cambiar el scale
        MeshRenderer rend = newFloor.AddComponent<MeshRenderer>();
       rend.material = floorMaterial;
        rend.material.mainTextureScale = new Vector2(totalWidth, totalHeight);
        
        //crear la geometría //convertir los vertices un una red de Mesh
        Mesh newMesh = new Mesh();

        List<Vector3> vertex = vertexPosition;
        //el lastVertex es el centro
        vertex.Add(centerFloor); 
        int lastVertex = vertex.Count - 1; //calcular cuantos vertices tengo sin contar el primero ni el ultimo
        newMesh.vertices = vertex.ToArray();

        //el orden de dibujo
        List<int> tris = new List<int>();
        //los tris son los triangulos strips, los creamos con for recursivo, de esta manera invertimos la colisión del suelo y asi lo podemos pulsar y seleccionar
        for (int i = vertexPosition.Count - 1; i >= 0; i--)
        {
            int currentVertex = i;
            int nextVertex = i + 1;
            if (nextVertex >= vertexPosition.Count) nextVertex = 0;

            tris.Add(lastVertex);
            tris.Add(nextVertex);
            tris.Add(currentVertex);
        }
        //tris.Reverse();//Solución a que el suelo mire hacia abajo

        newMesh.triangles = tris.ToArray();

        //normal map, los normales (hacia donde calcular la illuminacion) a ser plano esta siempre por arriba
        List<Vector3> normals = new List<Vector3>();
        for (int i = 0; i < vertex.Count; i++)
        {
            normals.Add(Vector3.up);
        }
        newMesh.normals = normals.ToArray();

        //UVs
        List<Vector2> uv = new List<Vector2>();
        for (int i = 0; i < vertex.Count; i++)
        {
            Vector2 coord = GetUvCoord(vertex[i]);
            uv.Add(coord);
        }
        newMesh.uv = uv.ToArray();

        filter.mesh = newMesh;
        //añadir la colision al suelo, para poder cambiar la textura
        MeshCollider col = newFloor.AddComponent<MeshCollider>();
        print(filter.mesh.triangles[1]);

        ObjectControl control = newFloor.AddComponent<ObjectControl>();
        control.element = Resources.Load < ObjectProperties>("Objects/1_Floor");
        control.objMaterial = rend;

        //añadimos el saveVertex que hemos creado en ObjectControl
        control.SaveVertex(vertex);

        //antes de borrar la lista de vértices, vamos a guardar el control (hacemos lo mismo con los muros 377)
        saveObjects.Add(control);
        vertexPosition = new List<Vector3>();

    }
    Vector2 GetUvCoord(Vector3 pos)
    {
        float distanceWidth = right - pos.x;//la distancia de la derecha a el punto mas lejos
        float totalPercentX = distanceWidth / totalWidth;
        //Invertir valores

        float distanceHeight = forward - pos.z;
        float totalPercentZ = distanceHeight / totalHeight;
        //Invertir valores

        return new Vector2(totalPercentX, totalPercentZ);
    }
    Vector3 GetCenterFloor() //obtener el centro de la pantalla
    {
        right = -Mathf.Infinity;
        for (int i = 0; i < vertexPosition.Count; i++)
        {
            if(vertexPosition[i].x > right)
            {
                right = vertexPosition[i].x;
            }
        }
        left = Mathf.Infinity;
        for (int i = 0; i < vertexPosition.Count; i++)
        {
            if (vertexPosition[i].x < left)
            {
                left = vertexPosition[i].x;
            }
        }
        forward = -Mathf.Infinity;
        for (int i = 0; i < vertexPosition.Count; i++)
        {
            if (vertexPosition[i].z > forward)
            {
                forward = vertexPosition[i].z;
            }
        }
        back = Mathf.Infinity;
        for (int i = 0; i < vertexPosition.Count; i++)
        {
            if (vertexPosition[i].z < back)
            {
                back = vertexPosition[i].z;
            }
        }
        float centerX = left + ((right - left) / 2);
        float centerZ = back + ((forward - back) / 2);
        Vector3 finalCenter = new Vector3(centerX, 0, centerZ);

        //Width y Height UV's
        totalWidth = right - left;
        totalHeight = forward - back;

        return finalCenter;
    }
    bool hasVertex(Vector3 _vertex) //preguntar si el vertice ya existe
    {
        for (int i = 0; i < vertexPosition.Count; i++)
        {
            if (vertexPosition[i] == _vertex) return true;
        }
        return false;
    }
    public void SelectObject(ObjectControl _obj)
    {
        //limpiar el grid y no ripitir las texturas cuando volvemos a pulsar la tecla 1
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Destroy(grid.GetChild(i).gameObject);
        }
        moveButton.SetActive(_obj.element.type == ObjectType.Props);

        print(_obj.element.id);
        currentObject = _obj;
        configureObject = true;
        objectInfo.SetActive(true);

        for (int i = 0; i < currentObject.element.textureId.Length; i++)
        {
            GameObject newTexture = Instantiate(baseTexture, grid);
            //crear un int temporal para assignar el id como int
            int tempId = currentObject.element.textureId[i];
            newTexture.transform.GetChild(0).GetComponent<Image>().sprite =
                Resources.Load<Sprite>("UITextures/" + tempId);
            newTexture.GetComponent<Button>().onClick.AddListener(delegate { AssignTexture(tempId); });
        }
    }

    //llamar el butos por scripting al script ObjectControl
    private void AssignTexture(int _idTexture)
    {
        if (_idTexture != -1)
        {
            Texture2D getTexture = Resources.Load<Texture2D>("Textures/" + _idTexture);
            //añadimos el _idTexture ya que en el objectControl hemos asignado que le llegará un ID
            currentObject.GetComponent<ObjectControl>().SetTexture(getTexture, _idTexture);
        }

    }

    public void CancelSelection()
    {
        
        currentObject = null;
        configureObject = false;
        objectInfo.SetActive(false);
        currentType = ObjectType.None;
    }
    public void TrashObject()
    {
        if (currentObject != null)
        {
            currentObject.DestroyObject();
        }
        CancelSelection();
    }
    public void MoveObject()
    {
        ghostProp = currentObject.gameObject;
        configureObject = false;
        currentType = ObjectType.Props;
    }
    void CreateWall()
    {
        if (Input.GetMouseButtonDown(0))
        {

            currentWall = Instantiate(wall, mouseGizmo.position, Quaternion.identity).transform;
            initWallPos = mouseGizmo.position;

        }
        else if (Input.GetMouseButton(0))
        {
            if (currentWall != null)
            {
                currentWall.LookAt(mouseGizmo.position);
                //ampliar el escale
                Vector3 finalScale = currentWall.localScale;
                finalScale.z = Vector3.Distance(currentWall.position, mouseGizmo.position);
                currentWall.localScale = finalScale;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (currentWall != null)
            {
                if (mouseGizmo.position == initWallPos)
                {
                    //Destroy(currentWall.gameObject);
                    currentWall.GetComponent<ObjectControl>().DestroyObject();
                }
                else
                {
                    saveObjects.Add(currentWall.GetComponent<ObjectControl>());
                }
            }
            currentWall = null;
        }

    }
    void GizmoPosition()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundMask))
        {
            Vector3 finalPos = new Vector3((int)hit.point.x, (int)hit.point.y, (int)hit.point.z);
            //obligar el usuario a trabajar en positivo moviendo todos los elementos del escinario
            /*o hacemos el seguiente
            if(hit.point.x > 0)
            {
                if (hit.point.x > ((int)hit.point.x + 0.5f)) finalPos.x += 1;
            }
            if (hit.point.x < 0)
            {
                if (hit.point.x > ((int)hit.point.x + 0.5f)) finalPos.x -= 1;
            }
            if (hit.point.z > 0)
            {
                if (hit.point.z > ((int)hit.point.z + 0.5f)) finalPos.z += 1;
            }
            if (hit.point.z < 0)
            {
                if (hit.point.z > ((int)hit.point.z + 0.5f)) finalPos.z -= 1;
            }*/
            if (hit.point.x > ((int)hit.point.x + 0.5f)) finalPos.x += 1;
            if (hit.point.z > ((int)hit.point.z + 0.5f)) finalPos.z += 1;

            mouseGizmo.position = finalPos;
        }
    }

    public void saveProject()
    {
        SaveData newSave = new SaveData();
        newSave.allObjects = new List<SaveObjectProperties>();
        //recorrer todos los elementos y asignarles sus valores a los elementos de la lista
        for (int i = 0; i < saveObjects.Count; i++)
        {
            SaveObjectProperties newObject = new SaveObjectProperties()
            {
                id = saveObjects[i].element.id,
                position = saveObjects[i].transform.position,
                rotation = saveObjects[i].transform.rotation,
                scale = saveObjects[i].transform.localScale,
                type = saveObjects[i].element.type,
                vertexFloor = saveObjects[i].allVertex,
                currentTexture = saveObjects[i].currentIdTexture
            };
            //añadimos cada objeto a la lista de SaveData
            newSave.allObjects.Add(newObject);
        }
        //crear el documento XML
        //asignar al XmlSerializer que cree un documento de tipo SaveData 
        XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
        using (StringWriter sw = new StringWriter())
        {
            serializer.Serialize(sw, newSave);
            File.WriteAllText(Application.dataPath + "/Saved/SaveProject.marina", sw.ToString());
            Application.OpenURL(Application.dataPath);
        }

    }
    public void loadProject()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(SaveData));
        string readFile = File.ReadAllText(Application.dataPath + "/Saved/SaveProject.marina");
        if (readFile.Length > 0)
        {
            using (StringReader sr = new StringReader(readFile))
            {
                SaveData newLoad = serializer.Deserialize(sr) as SaveData;
                for (int i = 0; i < newLoad.allObjects.Count; i++)
                {
                    GameObject findObject = Resources.Load<GameObject>("Prefabs/" + newLoad.allObjects[i].id);
                    if (findObject != null)
                    {
                        GameObject newObject = Instantiate(findObject,
                            newLoad.allObjects[i].position,
                            newLoad.allObjects[i].rotation);

                        newObject.transform.localScale = newLoad.allObjects[i].scale;
                        currentObject = newObject.GetComponent<ObjectControl>();
                        AssignTexture(newLoad.allObjects[i].currentTexture);

                        saveObjects.Add(currentObject);
                    }
                    else
                    {
                        if (newLoad.allObjects[i].type == ObjectType.Floors)
                        {
                            vertexPosition = newLoad.allObjects[i].vertexFloor;
                            GenerateFloor();
                        }
                    }
                }
            }
        }

    }
}

[System.Serializable]
public class SaveData  //convertir toda la data en una lista
{
    public List<SaveObjectProperties> allObjects;
}


[System.Serializable]
public class SaveObjectProperties
{
    public int id;
    public ObjectType type;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public List<Vector3> vertexFloor;
    public int currentTexture;

    //creamos un constructor vacío ya que con el constructor vacío vamos a poder serializar estos datos y sin él daría errores.
    //declaramos el constructor vacío --> necesario para poder Serializar
    public SaveObjectProperties() { }

    //creamos un constructor con todos los valores que van a llegar a esta clase
    public SaveObjectProperties(int _id, ObjectType _type, Vector3 _position,
              Quaternion _rotation, Vector3 _scale, List<Vector3> _vertex, int _textureID)
    {
        id = _id;
        type = _type;
        position = _position;
        rotation = _rotation;
        scale = _scale;
        vertexFloor = _vertex;
        currentTexture = _textureID;
    }
}
