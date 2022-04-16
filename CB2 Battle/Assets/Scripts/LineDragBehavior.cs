using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
    private float distance;
    Vector3 root;
    bool rootSet = false;
    public bool finished = false;
    Tile rootTile; 

    void Awake()
    {
        instance = this;
    }

    // called when created, sets position on pos and creates
    public void SetParameters(int maximumDistance, Vector3 root, Photon.Realtime.Player owner)
    {
        instance = this;
        pv.RPC("RPC_SetParameters",RpcTarget.All, maximumDistance, root, owner.ActorNumber);
    }

    public void RemoveLine()
    {
        ClearPreviousPath();
        PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    void RPC_SetParameters(int maximumDistance, Vector3 root, int actorID)
    {
        // if this is my player let me control this
        if(PhotonNetwork.LocalPlayer.ActorNumber == actorID || pv.IsMine)
        {
            SetRoot(root);
            this.distance = maximumDistance;
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

            // if a token is being held, draw a path
            if(rootSet)
            {
                UpdatePath();
            }

            // start drag on mouse down
            if(Input.GetMouseButtonDown(0))
            {
                if(!rootSet)
                {
                    // start drag
                    SetRoot(viablePos);
                }
            }
            else if(rootSet && Input.GetMouseButtonUp(0))
            {
                FinishedDrag();
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
                distance = Mathf.CeilToInt(next.distance);
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
                distance = 0;
                //TooltipSystem.show("Invalid move!", "");
            }         
        }
        previousPosition = startingPos;
    }

    // On releasing the mouse button, send the drag position to all clients  
    private void PlaceToken()
    {
        if(myToken != null)
        {
            PlayerStats myStats = myToken.GetComponent<PlayerStats>();
            myToken.GetComponent<TacticsMovement>().PaintCurrentTile("");
            int id = myStats.GetID();
            pv.RPC("RPC_PlaceToken", RpcTarget.All, id, myPath.ToArray(), distance);
        }
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

    // Places the token in all clients
    [PunRPC]
    void RPC_PlaceToken(int tokenID, Vector3[] Path, float distance)
    {
        myToken = PlayerSpawner.IDtoPlayer(tokenID).GetComponent<TacticsMovement>();
        myToken.GetComponent<PlayerStats>().SpendMovement(distance);
        myToken.moveToTile(Path);
    }

    // On holding the mouse down, save the player and begin dragging them
    private void SetRoot(Vector3 newRoot)
    {
        if(newRoot != Vector3.one)
        {
            Debug.Log("setting root at " + newRoot);
            this.root = newRoot;
            rootSet = true;
            Vector3 startingPos = BoardBehavior.GetClosestTile(transform.position);
            //dont want to call this every frame, only calculate path if this position is different from last frame
            rootTile = BoardBehavior.GetTile(startingPos);  
            BoardBehavior.ComputeAdjacencyLists(1);
            Queue<Tile> process = new Queue<Tile>();
            process.Enqueue(rootTile);
            if(rootTile != null)
            {
                rootTile.visited = true;

                while (process.Count > 0) 
                {
                    Tile t = process.Dequeue();
                    if (t.distance < distance) 
                    {    
                        foreach (Tile tile in t.adjacencyList)
                        {
                            PlayerStats occupant = tile.GetOccupant();
                            if(occupant == null)
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
                                    if(tile.distance <= distance)
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
    }

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
