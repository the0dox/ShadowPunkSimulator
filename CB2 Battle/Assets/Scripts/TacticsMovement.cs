using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMovement : MonoBehaviour
{
   List<Tile> selectableTiles = new List<Tile>();
   GameObject[] tiles; 

   Stack<Tile> path = new Stack<Tile>();
   Tile currentTile;

   public bool moving = false!;
   public float initative = 0;
   public bool finishedMove = false;
   public float jumpHeight = 2;
   public float moveSpeed = 4; 
   public Weapon activeWeapon;
   public bool halfMove;
   

   Vector3 velocity = new Vector3();
   Vector3 heading = new Vector3();
   float halfHeight = 0;

   Vector3 jumpTick = new Vector3(0,1,0);
   
   
   public void Init()
   {
      tiles = GameObject.FindGameObjectsWithTag("Tile");
      halfHeight = GetComponent<Collider>().bounds.extents.y; 
   }

   public void GetCurrentTile()
   {
      currentTile = GetTargetTile(gameObject);
      currentTile.current = true; 
   }

   public Tile GetTargetTile(GameObject target)
   {
      RaycastHit hit;

      Tile tile = null;
      if (Physics.Raycast(target.transform.position, -Vector3.up, out hit, 1))
      {
         tile = hit.collider.GetComponent<Tile>(); 
      }
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
      ComputeAdjacencyLists();
      GetCurrentTile();

      Queue<Tile> process = new Queue<Tile>();

      process.Enqueue(currentTile);
      currentTile.visited = true;

      while (process.Count > 0) 
      {
         Tile t = process.Dequeue();

         selectableTiles.Add(t);

         if(t.GetOccupant() == null || t.GetOccupant().GetTeam() == team){
            if (t.distance <= move)
            {
               t.selectable = true;
               t.UpdateIndictator(); 
            }
            else
            {
               t.selectableRunning = true; 
               t.UpdateIndictator();
            }
            if (t.distance < doubleMove) {     

               foreach (Tile tile in t.adjacencyList)
               {
                  
                  if (!tile.visited)
                  {

                     tile.parent = t;
                     tile.visited = true;
                     tile.distance = 1 + t.distance;
                     process.Enqueue(tile);

                  }
               }
            } 
         }
      }
   }

   public void FindSelectableTiles(int run, int team)
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
         
         if(t.GetOccupant() == null){
            t.selectableRunning = true; 
            t.UpdateIndictator();
         }

         if (t.distance < run && (t.GetOccupant() == null || t.GetOccupant().GetTeam() == team)) {     

            foreach (Tile tile in t.adjacencyList)
            {
               
               if (!tile.visited)
               {

                  tile.parent = t;
                  tile.visited = true;
                  tile.distance = 1 + t.distance;
                  process.Enqueue(tile);

               }
            }
         } 
      }
   }
   public void FindChargableTiles(int charge, int team)
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

         if(t.distance > 4) {
            List<Tile> adjacentTiles = t.adjacencyList;
            foreach (Tile adjacentTile in adjacentTiles)
            {
               if (adjacentTile.GetOccupant() != null && adjacentTile.GetOccupant().GetTeam() != team)
               {
                  t.selectableRunning = true;
                  t.UpdateIndictator();
               }
            }
         }

         if (t.distance < charge && (t.GetOccupant() == null || t.GetOccupant().GetTeam() == team)) {     

            foreach (Tile tile in t.adjacencyList)
            {
               
               if (!tile.visited)
               {

                  tile.parent = t;
                  tile.visited = true;
                  tile.distance = 1 + t.distance;
                  process.Enqueue(tile);

               }
            }
         } 
      }
   }

   public void moveToTile(Tile tile)
   {
      //action economy for full/half move
      halfMove = tile.selectable; 

      //reset any previous movement
      path.Clear();

      tile.target = true;
      tile.UpdateIndictator();
      moving = true; 
      
      Tile next = tile;
      //until we reach the location
      while (next != null)
      {
         //add current tile to pathing and move on to the next
         path.Push(next);
         next = next.parent;
      }
   }

   public void Move()
   {
      //if our stack still has move orders, we can move
      if (path.Count > 0)
      {
         Tile t = path.Peek();
         Vector3 target = t.transform.position;
         //ensure player is standing on top of the tile
         target.y += halfHeight + t.GetComponent<Collider>().bounds.extents.y;

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
         finishedMove = true;
      }
   }

   public void RemoveSelectableTiles()
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
      ComputeAdjacencyLists();
      GetCurrentTile();
      GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
      PlayerStats myStats = gameObject.GetComponent<PlayerStats>();
      foreach (GameObject p in players)
      {
         PlayerStats target = p.GetComponent<PlayerStats>();
         if(w == null && target.GetTeam() != myStats.GetTeam())
         {
            p.GetComponent<TacticsMovement>().GetTargetTile(p).attack = true;
            p.GetComponent<TacticsMovement>().GetTargetTile(p).UpdateIndictator();
         }
         else if(TacticsAttack.HasValidTarget(target,myStats,w))
         {
            p.GetComponent<TacticsMovement>().GetTargetTile(p).attack = true;
            p.GetComponent<TacticsMovement>().GetTargetTile(p).UpdateIndictator();
         }
      }
   }

   public void GetGrapplePartner(PlayerStats myStats)
   {
      ComputeAdjacencyLists();
      GetCurrentTile();
      if(myStats.grappler != null)
      {
         Tile targetTile = myStats.grappler.GetComponent<TacticsMovement>().GetTargetTile(myStats.grappler.gameObject);
         targetTile.attack = true;
         targetTile.UpdateIndictator();
      }
      if(myStats.grappleTarget != null)
      {
         Tile targetTile = myStats.grappleTarget.GetComponent<TacticsMovement>().GetTargetTile(myStats.grappleTarget.gameObject);
         targetTile.attack = true;
         targetTile.UpdateIndictator();
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
      bool output = finishedMove;
      if(output)
      {
         finishedMove = false;
         return true;
      }
      return false;
   }
}
