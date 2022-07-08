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
        //ComputeAdjacencyLists(1);
    }

    public static void ClearBoard()
    {    
        foreach (Vector3 key in Tiles.Keys)
        {
            Tiles[key].reset();
        }
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

    // returns true if there is an valid tile in the direction dir relative to parent position
    public static bool ValidNeighbor(Vector3 parent, Vector3 dir)
    {
        Vector3 newPos = parent + dir;
        // return false if tile doesn't exist
        if(!Tiles.ContainsKey(newPos))
        {
            return false;
        }
        // return false if tile has another tile stacked on top of it
        if(Tiles.ContainsKey(newPos + Vector3.up))
        {
            return false;
        }
        // return false if direction is diagonal and both paths to that tile are blocked
        if(dir.x != 0 && dir.z != 0)
        {
            return (ValidNeighbor(parent,new Vector3(dir.x,0,0)) || ValidNeighbor(parent, new Vector3(0,0,dir.z)));
        }
        return true;
    }

    public static bool InCover(GameObject player)
    {
        Vector3 location = player.transform.position;
        location.y = Mathf.Floor(location.y);
        
        if(Tiles.ContainsKey(location + Vector3.forward))
        {
            return true;
        }
        if(Tiles.ContainsKey(location + Vector3.left))
        {
            return true;
        }
        if(Tiles.ContainsKey(location + Vector3.right))
        {
            return true;
        }
        if(Tiles.ContainsKey(location + Vector3.back))
        {
            return true;
        }
        return false;
    }

    public static void RemoveTile(Vector3 removeKey)
    {
        Tile removedTile = Tiles[removeKey];
        Tiles.Remove(removeKey);
        removedTile.DestroyMe();
    }

    public static void CreateTile(Vector3 locationKey, string TileName)
    {
        GameObject newTileReference = TileReference.Tile(TileName);
        if(newTileReference != null && !Tiles.ContainsKey(locationKey))
        {
            GameObject newTile = Instantiate(newTileReference, locationKey, Quaternion.identity);
            Tile newTileScript = newTile.GetComponent<Tile>();
            Tiles.Add(locationKey, newTileScript);
        }
    }

    public static void ComputeAdjacencyLists(int jumpHeight)
    {
      foreach(Vector3 key in Tiles.Keys)
      {
         Tile t = Tiles[key];
         t.FindNeighbors(jumpHeight);
      }
    }

    public static void DistributeCoverDamage(Vector3 originalPos, int Damage)
    {
        Tile originalTile = GetTile(originalPos);
        Damage = originalTile.DamageCover(Damage);

        List<Vector3> destructableNeighbors = new List<Vector3>();
        destructableNeighbors = ValidDestructableNeighbors(destructableNeighbors, originalPos);

        Debug.Log("valid neighbors:" + destructableNeighbors.Count);

        

        // countine damaging cover until all damage has been applied or all cover is gone
        while(Damage > 0 && destructableNeighbors.Count > 0)
        {
            // select random tile within neighbors and apply remaing damage to it
            int randomIndex = Random.Range(0, destructableNeighbors.Count); 
            Vector3 randomPosition = destructableNeighbors[randomIndex];
            Tile randomTile = GetTile(randomPosition);

            if(randomTile != null)
            {
                Damage = randomTile.DamageCover(Damage);
            }

            // if tile is destroyed remove it from the list
            if(randomTile == null)
            {
                destructableNeighbors.Remove(randomPosition);
                Debug.Log("I'm destroyed and removed");
            }
            destructableNeighbors = ValidDestructableNeighbors(destructableNeighbors, randomPosition);
        }
        Debug.Log("finished");
        //RemoveIllogicalTiles();
    }

    private static List<Vector3> ValidDestructableNeighbors(List<Vector3> currentTiles, Vector3 newlyDestroyedTile)
    {
        for(int x = -1; x < 2; x++)
        {
            for(int y = -1; y < 2; y++)
            {
                for(int z = -1; z < 2; z++)
                {
                    Vector3 potentialTileposition = newlyDestroyedTile + new Vector3(x,y,z);
                    Tile potentialTile = GetTile(potentialTileposition);
                    // new tile must not be a duplicate, exist, and also not be an indestructable floor tile
                    if(!currentTiles.Contains(potentialTileposition) && potentialTile != null && !potentialTile.blank)
                    {
                        currentTiles.Add(potentialTileposition);
                    }
                }
            }
        }
        return currentTiles;
    }

    private static void RemoveIllogicalTiles()
    {
        foreach(Vector3 key in Tiles.Keys)
        {
            Tile currentTile = Tiles[key];
            if(!currentTile.blank)
            {
                if(Tiles.ContainsKey(key + Vector3.forward))
                {
                    break;
                }
                
                if(Tiles.ContainsKey(key + Vector3.back))
                {
                    break;
                }

                if(Tiles.ContainsKey(key + Vector3.left))
                {
                    break;
                }
                if(Tiles.ContainsKey(key + Vector3.right))
                {
                    break;
                }
                if(Tiles.ContainsKey(key + Vector3.up))
                {
                    break;
                }
                if(Tiles.ContainsKey(key + Vector3.down))
                {
                    break;
                }
                Debug.Log("illogical tile detected");
                GlobalManager.RemoveTile(currentTile);
            }
        }
    }
}
