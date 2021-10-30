using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardBehavior : MonoBehaviour
{
    private static Dictionary<Vector3, Tile> Tiles;

    public static void Init()
    {
        Tiles = new Dictionary<Vector3, Tile>();
        GameObject[] tileObjects = GameObject.FindGameObjectsWithTag("Tile");
        foreach(GameObject g in tileObjects)
        {
            if(g.TryGetComponent<Tile>(out Tile t))
            {
                Tiles.Add(t.transform.position, t);
            }
        }
        ComputeAdjacencyLists(1);
    }

    public static Tile GetTile(Vector3 pos)
    {
        if(Tiles.ContainsKey(pos))
        {
            return Tiles[pos];
        }
        else
        {
            return null;
        }
    }
    
    public static Vector3 GetClosestTile(Vector3 hit)
    {
        
        //Debug.Log("Getting closest tile to " + hit);
        float closestDist = 100f;
        Vector3 ClosestPos = new Vector3();
        foreach(Vector3 pos in Tiles.Keys)
        {
            float currentDist = Vector3.Distance(pos, hit);
            if(ClosestPos == null || closestDist > currentDist)
            {
                closestDist = currentDist;
                ClosestPos = pos;
            }
        }
        //Debug.Log("Closest pos is " + ClosestPos + " at distance " +  closestDist);
        return ClosestPos;
    }

    // returns true if there is an empty tile in the direction dir relative to parent position
    public static bool ValidNeighbor(Vector3 parent, Vector3 dir)
    {
        Vector3 newPos = parent + dir;
        if(!Tiles.ContainsKey(newPos))
        {
            return false;
        }
        if(Tiles.ContainsKey(newPos + Vector3.up))
        {
            return false;
        }
        return true;
    }
    public static void ComputeAdjacencyLists(int jumpHeight)
    {
      foreach(Vector3 key in Tiles.Keys)
      {
         Tile t = Tiles[key];
         t.FindNeighbors(jumpHeight);
      }
    }

}
