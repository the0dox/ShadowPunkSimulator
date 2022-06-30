using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// used for drawing line effects such as creating walls or magic attacks
public class LineDragBehavior : MonoBehaviour
{
    // used for static reference
    private static LineDragBehavior instance;

    // Used to send info to multiple clients
    [SerializeField] private PhotonView pv;
    // The path that the player takes 
    private List<Vector3> myPath = new List<Vector3>();
    // A reference to the token I'm holding
    private TacticsMovement myToken;
    // A reference to the previous mouse position
    private Vector3 previousPosition = new Vector3();
    // the length of the line effect
    private float length;
    // beginning of the line, can be but is not the same as the owner
    Vector3 root;
    // if a root hasn't been set, tells mouse to place a new root
    bool rootSet = false;
    // read by turn actions to read targets
    public bool finished = false;
    // Tile at the root location
    Tile rootTile; 
    // if true, the line effect cannot cross over players
    bool avoidPlayers = true;
    // how far a root can be placed from the owner, set to -1 if there is no limit
    int maxRootDistance = -1;
    // reference to the owners location
    Vector3 ownerLocation;
    // height of the drag behavior
    private int height;
    // reference to an indicator for the range 
    [SerializeField] private GameObject rangeIndicator; 

    void Awake()
    {
        instance = this;
    }

    // set parameters for a line that originates from the owner
    public void SetParameters(Photon.Realtime.Player owner, int maximumLength, Vector3 ownerLocation , bool avoidPlayers, int height = 1)
    {
        instance = this;
        pv.RPC("RPC_SetParameters",RpcTarget.All, maximumLength, false, ownerLocation,  avoidPlayers, -1, owner.ActorNumber, height);
    }

    // set parameters for a line that doesnt have a specified root
    public void SetParameters(Photon.Realtime.Player owner, int maximumLength, Vector3 ownerLocation , bool avoidPlayers, int maxRootDistance, int height)
    {
        instance = this;
        pv.RPC("RPC_SetParameters",RpcTarget.All, maximumLength, true, ownerLocation,  avoidPlayers, maxRootDistance, owner.ActorNumber, height);
    }

    // called by turn actions to delete this object
    public void RemoveLine()
    {
        ClearPreviousPath();
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    void RPC_SetParameters(int maximumLength, bool Freeroot, Vector3 ownerLocation, bool avoidPlayers, int maxDistance, int actorID, int height)
    {
        // if this is my player let me control this
        if(PhotonNetwork.LocalPlayer.ActorNumber == actorID || pv.IsMine)
        {
            this.ownerLocation = ownerLocation;
            this.length = maximumLength;
            this.avoidPlayers = avoidPlayers;
            this.maxRootDistance = maxDistance;
            this.height = height;
            if(Freeroot)
            {
                rangeIndicator.SetActive(true);
                rangeIndicator.transform.localScale = new Vector3((maxDistance*2) + 1f, 2, (maxDistance*2) + 1f);
            }
            else
            {
                Debug.Log("set root");
                rangeIndicator.SetActive(false);
                SetRoot(ownerLocation,true);
            }
        }
        // else hide me
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!finished)
        {
            CheckMouse();       
        }
    }
    
    // All mouse behavior called every frame
    public void CheckMouse()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //if the mouse is pointed at a thing
        if (Physics.Raycast(ray, out hit))
        {
            // position is normalized to fit the board
            Vector3 viablePos = hit.point;
            viablePos.x = Mathf.RoundToInt(viablePos.x);
            viablePos.y = Mathf.RoundToInt(viablePos.y);
            viablePos.z = Mathf.RoundToInt(viablePos.z);  
            transform.position = viablePos;

            rangeIndicator.transform.position = ownerLocation;

            // if a token is being held, draw a path
            if(rootSet)
            {
                UpdatePath();
            }

            // start drag if a root isn't set on mouse click
            if(Input.GetMouseButtonUp(0))
            {
                if(!rootSet)
                {
                    SetRoot(viablePos,false);
                }
                else
                {
                    FinishedDrag();
                }
            }
            // right click will cancel current root
            else if(Input.GetMouseButtonUp(1))
            {
                // can't cancel owner root line behavior
                if(rootSet && !root.Equals(ownerLocation))
                {
                    rootSet = false;
                    ClearPreviousPath();
                }
            }
        }
    }

    // If my mouse position is different from the previous frame, calculate mypath and draw the grid 
    private void UpdatePath()
    {
        Vector3 startingPos = BoardBehavior.GetClosestTile(transform.position);
        //dont want to call this every frame, only calculate path if this position is different from last frame
        Tile next = BoardBehavior.GetTile(startingPos);
        if(startingPos != previousPosition)
        {
            if(next.parent != null)
            {    
                ClearPreviousPath();
                length = Mathf.CeilToInt(next.distance);
                //TooltipSystem.show("     " + distance + " MP", "");
                while (next != null)
                {
                    //add current tile to pathing and move on to the next
                    next.OnScrolled(true);
                    myPath.Add(next.transform.position);
                    next = next.parent;
                }
            }   
            else
            {
                length = 0;
                //TooltipSystem.show("Invalid move!", "");
            }         
        }
        previousPosition = startingPos;
    }

    // Clears all of the path indicators
    private void ClearPreviousPath()
    {
        foreach(Vector3 tilepos in myPath)
        {
            Tile oldTile = BoardBehavior.GetTile(tilepos);
            if(oldTile != null)
            {
                oldTile.OnScrolled(false);
            }
        }
        myPath.Clear();
    }

    // On holding the mouse down, save the player and begin dragging them
    private void SetRoot(Vector3 newRoot, bool ownerRoot)
    {
        if(ownerRoot)
        {
            rootSet = true;
            this.root = ownerLocation;
            rootTile = BoardBehavior.GetTile(BoardBehavior.GetClosestTile(ownerLocation + Vector3.down)); 
        }
        // only take in valid locations
        else if(newRoot != Vector3.one)
        {
            Vector3 startingPos = BoardBehavior.GetClosestTile(transform.position);
            
            // if a maxrootdistance is specified enforce distance
            if(maxRootDistance < 0 || Mathf.FloorToInt(Vector3.Distance(startingPos, ownerLocation)) <= (maxRootDistance))
            {
                
                rootTile = BoardBehavior.GetTile(startingPos);  

                // only set if either I don't care to avoid players, or the tile selected is null
                if(rootTile.GetOccupant() == null || !avoidPlayers) 
                {
                    Debug.Log("setting root at " + newRoot);
                    this.root = startingPos;
                    rootSet = true;
                    //dont want to call this every frame, only calculate path if this position is different from last frame
                }
            }
        } 
        BoardBehavior.ComputeAdjacencyLists(height);
        Queue<Tile> process = new Queue<Tile>();
        process.Enqueue(rootTile);
        if(rootTile != null)
        {
            rootTile.visited = true;

            while (process.Count > 0) 
            {
                Tile t = process.Dequeue();
                if (t.distance < length) 
                {    
                    foreach (Tile tile in t.adjacencyList)
                    {
                        PlayerStats occupant = tile.GetOccupant();
                        if(occupant == null && ValidTile(tile))
                        { 
                            if (!tile.visited)
                            {
                                //selectableTiles.Add(t);
                                tile.visited = true;
                                float distanceCost = 1;
                                if(t.diagonalList.Contains(tile))
                                {
                                distanceCost = 1.5f;
                                }
                                tile.distance = distanceCost + t.distance;
                                if(tile.distance <= length)
                                {
                                tile.parent = t;
                                process.Enqueue(tile);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // returns true if tile t is within maxmium length
    public bool ValidTile(Tile t)
    {
        Vector3 normalizedOwner = new Vector3(ownerLocation.x, 0, ownerLocation.z);
        Vector3 normalizedTarget = new Vector3(t.transform.position.x, 0, t.transform.position.z);
        if(maxRootDistance == -1 || Mathf.FloorToInt(Vector3.Distance(normalizedOwner, normalizedTarget)) <= maxRootDistance)
        {
            return t.distance < length;
        }
        return false;
    }

    // tells turn actions that the action is finished
    private void FinishedDrag()
    {
        if(!pv.IsMine)
        {
            List<string> code = new List<string>();
            foreach(Vector3 location in myPath)
            {
                code.Add(location.x + "," + location.y + "," + location.z);
            } 
            pv.RPC("RPC_FinishedDrag", RpcTarget.MasterClient, code.ToArray());
        }
        else
        {
            finished = true; 
        }   
    }
    
    [PunRPC]
    void RPC_FinishedDrag(string[] incomingCode)
    {
        myPath.Clear();
        for(int i = 0; i < incomingCode.Length; i++)
        {   
            string[] decoded = incomingCode[i].Split(',');
            myPath.Add (new Vector3(float.Parse(decoded[0]), float.Parse(decoded[1]), float.Parse(decoded[2])));
        }
        finished = true;
    }

    public List<Vector3> GetPath()
    {
        return myPath;
    }
}
