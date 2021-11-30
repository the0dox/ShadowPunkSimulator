using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Used to drag tokens around
public class TokenDragBehavior : MonoBehaviourPunCallbacks
{
    // Used to represent the dragged model
    [SerializeField] private MeshFilter myFilter;
    // Used to send info to multiple clients
    [SerializeField] private PhotonView pv;
    // The path that the player takes 
    private List<Vector3> myPath = new List<Vector3>();
    // A reference to the token I'm holding
    private TacticsMovement myToken;
    // A reference to the previous mouse position
    private Vector3 previousPosition = new Vector3();

    // Update is called once per frame
    void Update()
    {
        CheckMouse();       
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
            if(myToken != null)
            {
                UpdatePath();
            }

            // start drag on mouse down
            if(Input.GetMouseButtonDown(0))
            {
                if(hit.collider.tag == "Player")
                {
                    AddToken(hit.collider.gameObject);
                }
                else
                {
                    ClearToken();
                }
            }
            else if(Input.GetMouseButtonUp(0))
            {
                PlaceToken();
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
            
            ClearIndicators();

            myPath.Clear();
            
            if(next.parent != null)
            {
                int distance = Mathf.CeilToInt(next.distance);
                TooltipSystem.show("     " + distance + " MP", "");
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
                TooltipSystem.show("Invalid move!", "");
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
            int id = myStats.GetID();
            pv.RPC("RPC_PlaceToken", RpcTarget.All, id, myPath.ToArray());
        }
    }

    // Places the token in all clients
    [PunRPC]
    void RPC_PlaceToken(int tokenID, Vector3[] Path)
    {
        myToken = PlayerSpawner.IDtoPlayer(tokenID).GetComponent<TacticsMovement>();
        myToken.moveToTile(Path);
        ClearToken();
    }

    // On holding the mouse down, save the player and begin dragging them
    private void AddToken(GameObject player)
    {
        TacticsMovement newToken = player.GetComponent<TacticsMovement>();
        if(!newToken.moving)
        {
            myToken = newToken;
            myToken.FindTiles();
            myFilter.mesh = player.GetComponentInChildren<MeshFilter>().mesh;
        }
    }

    // Clears all of the path indicators
    private void ClearIndicators()
    {
        foreach(Vector3 tilepos in myPath)
        {
            Tile oldTile = BoardBehavior.GetTile(tilepos);
            oldTile.OnScrolled(false);
        }
    }

    // Clears the previously held player
    private void ClearToken()
    {
        ClearIndicators();
        TooltipSystem.hide();
        myToken = null;
        myFilter.mesh = null;
    }
    
}