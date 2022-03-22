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
    public bool blank = false;
    public List<Tile> adjacencyList = new List<Tile>();
    public List<Tile> diagonalList = new List<Tile>();

    //
    public bool visited = false;
    public Tile parent = null; 
    public float distance = 0; 

    public int ArmorValue = 8;
    public int StructurePoints = 4;
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

    public void HitCover(AttackSequence incomingAttack)
    {
        CombatLog.Log("by getting between 0-" + incomingAttack.coverRange +  " hits, " + incomingAttack.attacker.GetName() + " hits cover!");

        int damage = incomingAttack.ActiveWeapon.GetDamage();
        int AP = incomingAttack.ActiveWeapon.Template.pen;

        int armorDice = ArmorValue - AP;

        int successes = 0;

        for(int i = 0; i < armorDice; i++)
        {
            int result = Random.Range(1,7);
            if(result > 4)
            {
                successes++;
            }
        }

        if(damage > successes)
        {
            int netDamage = TacticsAttack.AmmoExpenditure[incomingAttack.FireRate]; 
            BoardBehavior.DistributeCoverDamage(transform.position, netDamage);
        }
        else
        {
            PopUpText.CreateText("-0", Color.yellow, gameObject);
        }
    }

    // damage cover without spreading feature
    public void HitCover(Weapon incomingWeapon)
    {
        int damage = incomingWeapon.GetDamage();
        int AP = incomingWeapon.Template.pen;

        int armorDice = ArmorValue - AP;

        int successes = 0;

        for(int i = 0; i < armorDice; i++)
        {
            int result = Random.Range(1,7);
            if(result > 4)
            {
                successes++;
            }
        }

        if(damage > successes)
        {
            int strucutreDamage = incomingWeapon.GetDamage() * 2; 
            if(strucutreDamage >= StructurePoints)
            {
                GlobalManager.RemoveTile(this);
            }
        }
    }

    public int DamageCover(int damage)
    {
        // blank tiles cannot be destroyed
        if(blank)
        {
            Debug.LogError("Error: cannot damage blank tiles, is the tile selector method functioning properly?");
            return -1;
        }
        int netDamage = damage - StructurePoints;
        StructurePoints -= damage;
        PopUpText.CreateText("-" + damage, Color.red, gameObject);
        if(StructurePoints < 1)
        {
            GlobalManager.RemoveTile(this);
        }
        Debug.Log("remaining damage:" + netDamage);
        return netDamage;
    }

    public bool IsStackedTile()
    {
        Tile underTile = BoardBehavior.GetTile(transform.position + Vector3.down);
        if(underTile != null)
        {
            if (!underTile.blank)
            {
                return true;
            }
        }
        return false;
    }

    public void OnScrolled(bool on)
    {
        ScrolledBox.SetActive(on);
    }


    public void DestroyMe()
    {
        Destroy(gameObject);
    }
}
