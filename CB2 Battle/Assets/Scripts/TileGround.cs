using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileGround : MonoBehaviour
{
    [SerializeField] private GameObject TileRef;
    private static GameObject STileRef;
    private static Dictionary<Vector3,GameObject> myTiles = new Dictionary<Vector3, GameObject>();

    void Awake()
    {
        STileRef = TileRef;
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
}
