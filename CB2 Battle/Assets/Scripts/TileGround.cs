using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileGround : MonoBehaviour
{
    [SerializeField] private GameObject TileRef;
    [SerializeField] private MeshRenderer myMesh;
    private static GameObject STileRef;
    private static MeshRenderer SmyMesh;
    private static Dictionary<Vector3,GameObject> myTiles = new Dictionary<Vector3, GameObject>();

    void Awake()
    {
        STileRef = TileRef;
        SmyMesh = myMesh;
        transform.position = new Vector3(0,0.5f,0);
    }

    public static Dictionary<Vector3,GameObject> LoadTiles(int X, int Z)
    {
        myTiles = new Dictionary<Vector3, GameObject>();
        for(int i = -X; i <= X; i++)
        {
            for(int j = -Z; j <= Z; j++)
            {
                Vector3 newpos = new Vector3(i,0,j);
                if(!myTiles.ContainsKey(newpos))
                {
                    GameObject newTile = Instantiate(STileRef, newpos, Quaternion.identity) as GameObject;
                    myTiles.Add(newTile.transform.position, newTile);
                }
            }
        }
        return myTiles; 
    }

    public static void SetMaterial(Material newGroundMat)
    {
        SmyMesh.material = newGroundMat;
    }

    public static Material GetMaterial()
    {
        return SmyMesh.sharedMaterial;
    }
}
