using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{

    public bool walkable = true; 
    public bool current = false;
    public bool target = false;
    public bool selectable = false; 
    public bool attack = false;
    public bool selectableRunning = false; 
    public List<Tile> adjacencyList = new List<Tile>();

    //
    public bool visited = false;
    public Tile parent = null; 
    public int distance = 0; 

    public int ArmorValue = 8;
    public int TotalAV;

    public static Vector3 topright = new Vector3(1,0,1);
    
    public static Vector3 topleft = new Vector3(1,0,-1);
    public GameObject indicator;
    private MeshFilter myFilter;

    void Start()
    {
        TotalAV = ArmorValue;
    }
    // Update is called once per frame
    public void  UpdateIndictator()
    {
        if(indicator != null)
        {
            indicator.GetComponent<Renderer>().material.color = Color.white;
            Color currentColor = indicator.GetComponent<Renderer>().material.color;
            indicator.GetComponent<Renderer>().material.color = currentColor;
            if (current)
            {
                indicator.GetComponent<Renderer>().material.color =  new Color(0,1,0,0.75f);;
            }
            else if (target)
            {
                indicator.GetComponent<Renderer>().material.color =  new Color(0,1,0,0.75f);;
            }
            else if (selectable)
            {
                indicator.GetComponent<Renderer>().material.color = new Color(0,1,1,0.75f);;
            }
            else if (attack)
            {
                indicator.GetComponent<Renderer>().material.color = new Color(1,0,0,0.75f);;
            }
            else if (selectableRunning)
            {
                indicator.GetComponent<Renderer>().material.color = new Color(1,0.92f,0.016f,0.75f);
            }
            else
            {
                currentColor = indicator.GetComponent<Renderer>().material.color;
                currentColor.a = 0.25f;
                indicator.GetComponent<Renderer>().material.color = currentColor;
            }
        
        }
    }

    //reset all the variables back
    public void reset()
    {
        current = false;
        target = false;
        selectable = false; 
        selectableRunning = false;
        attack = false;
        visited = false;
        parent = null; 
        distance = 0; 
        adjacencyList = new List<Tile>();
        UpdateIndictator();
    }

    //adds neighbors to adjacency list
    public void FindNeighbors(float jumpHeight)
    {
        reset();
        CheckTile(Vector3.forward, jumpHeight);
        CheckTile(-Vector3.forward, jumpHeight);
        CheckTile(Vector3.right, jumpHeight);
        CheckTile(-Vector3.right, jumpHeight);
        //diagonal movement
        CheckTile(topright,jumpHeight);
        CheckTile(topleft,jumpHeight);
        CheckTile(-topright,jumpHeight);
        CheckTile(-topleft,jumpHeight);
    }

    public void CheckTile(Vector3 direction, float jumpHeight)
    {
        Vector3 halfExtents = new Vector3(0.25f, (1 + jumpHeight) / 2.0f, 0.25f);
        Collider[] colliders = Physics.OverlapBox(transform.position + direction, halfExtents);

        foreach (Collider item in colliders)
        {
            Tile tile = item.GetComponent<Tile>();
            // if the collided object has the Tile script and that script is walkable
            if (tile != null && tile.walkable) 
            {
                
                RaycastHit hit;
                Physics.Raycast(tile.transform.position,Vector3.up, out hit, 1);
                //if there is nothing on top
                if (hit.collider == null || hit.collider.tag != "Tile")
                {
                    adjacencyList.Add(tile);      
                }
            }
        }
    }

    public PlayerStats GetOccupant()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit, 1))
        {
            return hit.collider.GetComponent<PlayerStats>();
        }
        return null; 
    }

    public int CoverReduction(int damage, int AP)
    {
        if(ArmorValue < (damage + AP))
        {
            ArmorValue--;
            float percentage = ((float)TotalAV)/ArmorValue;
            
            if(ArmorValue < 1)
            {
                CombatLog.Log("Cover is sufficiently damaged that it provides no AP");
            }
            else
            {
                CombatLog.Log("Cover is reduced to " + ArmorValue + "AP");
            }
        return ArmorValue + 1;
        }
        return ArmorValue;
    }


    public void DestroyMe()
    {
        DestroyImmediate(this);
    }
}
