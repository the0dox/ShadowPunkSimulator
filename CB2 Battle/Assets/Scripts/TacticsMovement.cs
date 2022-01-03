using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
// While nearly all player info is handled by Playerstats, Tacticsmovement covers all player movement.
// Note that tacticsmovement Doesn't know the gamelogic of movement, it can't calculate how far a player can move
// It needs to be supplied with the playerstats
public class TacticsMovement : MonoBehaviourPunCallbacks
{
   List<Tile> selectableTiles = new List<Tile>();
   GameObject[] tiles; 

   Stack<Vector3> path = new Stack<Vector3>();
   Tile currentTile;

   public bool moving = false!;
   public int initative = 0;
   public bool finishedMove = false;
   public float jumpHeight = 2;
   public float moveSpeed = 4; 
   public Weapon activeWeapon;
   Vector3 velocity = new Vector3();
   Vector3 heading = new Vector3();
   float halfHeight = 0;

   Vector3 jumpTick = new Vector3(0,1,0);
   [SerializeField] private PhotonView pv;
   
   void Start()
   {
      Init();
   }
   public void Init()
   {
      pv.RPC("RPC_Init",RpcTarget.All);
   }
   [PunRPC]
   void RPC_Init()
   {
      tiles = GameObject.FindGameObjectsWithTag("Tile");
      halfHeight = GetComponent<Collider>().bounds.extents.y; 
   }

   public void PaintCurrentTile(string paint)
   {
     pv.RPC("RPC_Paint",RpcTarget.All,paint);
   }

   [PunRPC]
   void RPC_Paint(string paint)
   {
      Tile targetTile = GetTargetTile(gameObject);
      targetTile.current = paint.Equals("current");
      targetTile.target = paint.Equals("target");
      targetTile.selectable = paint.Equals("selectable"); 
      targetTile.selectableRunning = paint.Equals("selectableRunning");
      targetTile.attack = paint.Equals("attack");
      targetTile.UpdateIndictator();
   }

   public void GetCurrentTile()
   {
      currentTile = GetTargetTile(gameObject);
      if(currentTile != null)
      {
         currentTile.current = true; 
         currentTile.UpdateIndictator();
      }
   }

   public Tile GetTargetTile(GameObject target)
   {
      Tile tile = null;
      Vector3 down = BoardBehavior.GetClosestTile(gameObject.transform.position + Vector3.down);
      //Debug.Log("Getting tile" + down);
      tile = BoardBehavior.GetTile(down);
      return tile;
   }

   public void ComputeAdjacencyLists()
   {
      foreach (GameObject tile in tiles)
      {
         Tile t = tile.GetComponent<Tile>();
         t.FindNeighbors(jumpHeight);
      }
   }

   public void FindSelectableTiles(int move, int doubleMove, int team)
   {
      pv.RPC("RPC_Walk", RpcTarget.All ,move,doubleMove, team);
   }
   [PunRPC]
   void RPC_Walk(int move, int doubleMove, int team)
   {
      ComputeAdjacencyLists();
      GetCurrentTile();

      Queue<Tile> process = new Queue<Tile>();

      process.Enqueue(currentTile);
      currentTile.visited = true;

      while (process.Count > 0) 
      {
         Tile t = process.Dequeue();

         selectableTiles.Add(t);
         PlayerStats occupant = t.GetOccupant();
         if(occupant == null || occupant.GetTeam() == team)
         { 
            if (t.distance < doubleMove) {     

               foreach (Tile tile in t.adjacencyList)
               {
                  
                  if (!tile.visited)
                  {
                     tile.parent = t;
                     tile.visited = true;
                     float distanceCost = 1;
                     if(t.diagonalList.Contains(tile))
                     {
                        distanceCost = 1.5f;
                     }
                     tile.distance = distanceCost + t.distance;
                     process.Enqueue(tile);
                  }
               }
            }
         }
      }
   }
   public void FindTiles()
   {
      PlayerStats myStats = GetComponent<PlayerStats>();
      float distance = (float)myStats.GetMovement("walk");
      int team = myStats.GetTeam();
      ComputeAdjacencyLists();
      GetCurrentTile();

      Queue<Tile> process = new Queue<Tile>();

      process.Enqueue(currentTile);
      if(currentTile != null)
      {
         currentTile.visited = true;

         while (process.Count > 0) 
         {
            Tile t = process.Dequeue();

            selectableTiles.Add(t);
            PlayerStats occupant = t.GetOccupant();
            if(occupant == null || occupant.GetTeam() == team)
            { 
               if (t.distance < distance) {     

                  foreach (Tile tile in t.adjacencyList)
                  {
                     
                     if (!tile.visited)
                     {
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

   public void moveToTile(Vector3[] pathArray)
   {
      //reset any previous movement
      path.Clear();
      moving = true;
      
      //until we reach the location
      for (int i = 0; i < pathArray.Length; i++)
      {
         //add current tile to pathing and move on to the next
         path.Push(pathArray[i]);
      }
   
      StartCoroutine(moveDelay());
   }

   IEnumerator moveDelay()
   {
      while(moving)
      {  
         Move();
         yield return new WaitForEndOfFrame(); 
      }
      finishedMoving();
   }

   public void Move()
   {
      //if our stack still has move orders, we can move
      if (path.Count > 0)
      {
         Vector3 target = path.Peek();
         //ensure player is standing on top of the tile
         target.y += 1.5f;

         //if we haven't reached it yet
         if (Vector3.Distance(transform.position, target) >= 0.05f)
         {

            //Debug.Log("my position" + transform.position.y);
            //Debug.Log("target position" + target.y);
            //o implement: jumping

            //senario 1: jumping up
            if ( target.y > transform.position.y)
            { 
               Vector3 newLevel = new Vector3(transform.position.x, target.y, transform.position.z);
               transform.position = newLevel;
            }
            //senario 2: jumping down only triggers if directly above target
            else if ( target.y < transform.position.y)
            {
               Vector3 currentLevel = new Vector3 (target.x, transform.position.y, target.z);
               if ( Vector3.Distance(transform.position, currentLevel) >= 0.05f)
               {
                  CalculateHeading(currentLevel);
                  SetHoritzontalVelocity();
               }
               else
               {
                  Vector3 newLevel = new Vector3(transform.position.x, target.y, transform.position.z);
                  transform.position = newLevel;
               }
            }
            else
            {
               CalculateHeading(target);
               SetHoritzontalVelocity();
            }
            
            //look at target
            transform.forward = heading;
            //make it MOVE!
            transform.position += velocity * Time.deltaTime;
         }
         else {
            //stick player on top of tile
            transform.position = target;
            path.Pop();
         }
      }
      //stop moving
      else 
      {
         //clear selectable tiles
         selectableTiles.Clear();
         moving = false;
         
         //clear old current tile
         if (currentTile != null) 
         {
            currentTile.current = false;
            currentTile = null;
         }

         //signal to turn manager that actions need to be spent
         //finishedMoving();
      }
   }

   public void RemoveSelectableTiles()
   {
      pv.RPC("RPC_TileClear",RpcTarget.All);
   }

   [PunRPC]
   void RPC_TileClear()
   {
      foreach (Tile t in selectableTiles)
      {
         t.reset();
      }
      selectableTiles.Clear();
   }

   void CalculateHeading(Vector3 target)
   {
      //direction we travel in, normalize makes it a simple vector
      heading = target - transform.position;
      heading.Normalize();
   }

   void SetHoritzontalVelocity()
   {
      velocity = heading * moveSpeed;
   }
   public void GetValidAttackTargets(Weapon w)
   {
      RemoveSelectableTiles();
      ComputeAdjacencyLists();
      GetCurrentTile();
      GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
      PlayerStats myStats = gameObject.GetComponent<PlayerStats>();
      foreach (GameObject p in players)
      {
         PlayerStats target = p.GetComponent<PlayerStats>();
         if(w == null && target.GetTeam() != myStats.GetTeam())
         {
            p.GetComponent<TacticsMovement>().PaintCurrentTile("attack");
         }
         else if(TacticsAttack.HasValidTarget(target,myStats,w))
         {
            p.GetComponent<TacticsMovement>().PaintCurrentTile("attack");
         }
      }
   }


   public void GetGrapplePartner(PlayerStats myStats)
   {
      ComputeAdjacencyLists();
      GetCurrentTile();
      if(myStats.grappler != null)
      {
         myStats.grappler.GetComponent<TacticsMovement>().PaintCurrentTile("attack");
      }
      if(myStats.grappleTarget != null)
      {
        myStats.grappleTarget.GetComponent<TacticsMovement>().PaintCurrentTile("attack");
      }
   }

   public List<PlayerStats> AdjacentPlayers(Tile Parent)
   {
      List<PlayerStats> l = new List<PlayerStats>();
      foreach(Tile t in Parent.adjacencyList)
      {
         PlayerStats p = t.GetOccupant();
         if ( p != null)
         {
            l.Add(p);
         }
      }
      return l;
   }
   public List<PlayerStats> AdjacentPlayers()
   {
      GetCurrentTile();
      return AdjacentPlayers(currentTile);
   }

   public int GetAdjacentEnemies(int team)
   {
      int Count = 0;
      List<PlayerStats> adjacents = AdjacentPlayers();
      foreach(PlayerStats player in adjacents)
      {
         if(player.GetTeam() != team)
         {
            Count++;
         }
      }
      return Count;
   }

   //allows the player to move, called once per turn
   public void ResetMove()
   {
      finishedMove = false;
   }

   public bool finishedMoving()
   {
      if(finishedMove)
      {
         foreach (Tile t in selectableTiles)
         {
            t.reset();
         }
         selectableTiles.Clear();
         return true;
      }
      return false;
   }
}
