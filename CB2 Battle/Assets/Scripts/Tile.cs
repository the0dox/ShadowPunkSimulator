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
    public List<Tile> diagonalList = new List<Tile>();

    //
    public bool visited = false;
    public Tile parent = null; 
    public float distance = 0; 

    public int ArmorValue = 8;
    public int TotalAV;
    public Vector3 MapPosition;

    public static Vector3 topright = new Vector3(1,0,1);
    
    public static Vector3 topleft = new Vector3(1,0,-1);
    public GameObject indicator;
    private MeshFilter myFilter;
    [SerializeField] private Material transMat;
    [SerializeField] private Material solidMat;
    [SerializeField] private GameObject ScrolledBox;

    void Start()
    {
        MapPosition = transform.position;
        TotalAV = ArmorValue;
        ScrolledBox.SetActive(false);
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
                indicator.GetComponent<Renderer>().material = solidMat;
                indicator.GetComponent<Renderer>().material.color =  new Color(0,1,0,0.75f);
            }
            else if (target)
            {   
                indicator.GetComponent<Renderer>().material = solidMat;
                indicator.GetComponent<Renderer>().material.color =  new Color(0,1,0,0.75f);
            }
            else if (selectable)
            {
                indicator.GetComponent<Renderer>().material = solidMat;
                indicator.GetComponent<Renderer>().material.color = new Color(0,1,1,0.75f);
            }
            else if (attack)
            {
                indicator.GetComponent<Renderer>().material = solidMat;
                indicator.GetComponent<Renderer>().material.color = new Color(1,0,0,0.75f);
            }
            else if (selectableRunning)
            {
                indicator.GetComponent<Renderer>().material = solidMat;
                indicator.GetComponent<Renderer>().material.color = new Color(1,0.92f,0.016f,0.75f);
            }
            else
            {
                indicator.GetComponent<Renderer>().material = transMat;
                currentColor = indicator.GetComponent<Renderer>().material.color;
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
        diagonalList = new List<Tile>();
        ScrolledBox.SetActive(false);
        UpdateIndictator();
    }

    //adds neighbors to adjacency list
    public void FindNeighbors(float jumpHeight)
    {
        reset();
        CheckTile(Vector3.forward, jumpHeight,false);
        CheckTile(-Vector3.forward, jumpHeight,false);
        CheckTile(Vector3.right, jumpHeight,false);
        CheckTile(-Vector3.right, jumpHeight,false);
        //diagonal movement
        CheckTile(topright,jumpHeight,true);
        CheckTile(topleft,jumpHeight,true);
        CheckTile(-topright,jumpHeight,true);
        CheckTile(-topleft,jumpHeight,true);
    }

    public void CheckTile(Vector3 direction, float jumpHeight, bool diagonal)
    {
        for(int i = -1; i < jumpHeight; i++)
        {
            Vector3 currentDir = new Vector3(direction.x, direction.y + i, direction.z);
            if(BoardBehavior.ValidNeighbor(transform.position, currentDir))
            {
                Tile tile = BoardBehavior.GetTile(transform.position + currentDir);
                if(diagonal)
                {
                    diagonalList.Add(tile);
                }
                adjacencyList.Add(tile);    
            }
        }
    }

    public PlayerStats GetOccupant()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.up, out hit, 1, LayerMask.GetMask("Character")))
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

    public void OnScrolled(bool on)
    {
        ScrolledBox.SetActive(on);
    }


    public void DestroyMe()
    {
        DestroyImmediate(this);
    }
}
