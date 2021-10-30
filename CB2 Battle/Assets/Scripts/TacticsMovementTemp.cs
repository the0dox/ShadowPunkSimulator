using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMovementTemp : MonoBehaviour
{
   List<Tile> selectableTiles = new List<Tile>();
   GameObject[] tiles; 

   Stack<Tile> path = new Stack<Tile>();
   Tile currentTile;

   public bool moving = false!;
   public int move = 5;
   public float jumpHeight = 2;
   public float moveSpeed = 2; 
   public float jumpVelocity = 4.5f;

   Vector3 velocity = new Vector3();
   Vector3 heading = new Vector3();
   float halfHeight = 0;

   bool fallingDown = false;
   bool jumpingUp = false;
   bool movingEdge = false;

   Vector3 jumptarget = new Vector3();

   protected void Init()
   {
      tiles = GameObject.FindGameObjectsWithTag("Tile");

      halfHeight = GetComponent<Collider>().bounds.extents.y; 
   }

   public void GetCurrentTile()
   {
      Vector3 down = BoardBehavior.GetClosestTile(gameObject.transform.position + Vector3.down);
      Debug.Log("Getting tile" + down);
      currentTile = BoardBehavior.GetTile(down);
      currentTile.current = true; 
      currentTile.UpdateIndictator();
   }

   public void ComputeAdjacencyLists()
   {
      foreach (GameObject tile in tiles)
      {
         Tile t = tile.GetComponent<Tile>();
         t.FindNeighbors(jumpHeight);
      }
   }

   public void FindSelectableTiles()
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
         t.selectable = true; 
         t.UpdateIndictator();

         if (t.distance < move) {     

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

            //o implement: jumping
            bool jump = transform.position.y != target.y;

            if (jump)
            { 
               Jump(target);
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
            currentTile.UpdateIndictator();
            currentTile = null;
         }

      }
   }

   protected void RemoveSelectableTiles()
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

   void Jump(Vector3 target)
   {
      if (fallingDown)
      {
         FallDownward(target);
      }
      else if (jumpingUp)
      {
         JumpUpward(target);
      }
      else if (movingEdge)
      {
         MoveToEdge();
      }
      else
      {
         prepareJump(target);
      }
   }

   void prepareJump (Vector3 target)
   {
      float targetY = target.y;
      target.y = transform.position.y;

      CalculateHeading(target);

      //if the tile is bellow us
      if (transform.position.y > targetY)
      {
         fallingDown = false;
         jumpingUp = false;
         movingEdge = true;

         jumptarget = transform.position + (target - transform.position) / 2.0f;
      }
      else
      {
         fallingDown = false;
         jumpingUp = true;
         movingEdge = false;

         velocity = heading * moveSpeed / 3.0f;

         float difference = targetY - transform.position.y; 

         velocity.y = jumpVelocity * (0.5f + difference / 2.0f);
      }
   }

   void FallDownward(Vector3 target)
   {
      velocity += Physics.gravity * Time.deltaTime;

      //we have fallen bellow our target
      if (transform.position.y <= target.y)
      {
         fallingDown = false;
         Vector3 p = transform.position;
         p.y = target.y;

         velocity = new Vector3();
      }
   }
   void JumpUpward(Vector3 target)
   {
      velocity += Physics.gravity * Time.deltaTime;

      if (transform.position.y > target.y)
      {
         jumpingUp = false;
         fallingDown = true;
      }
   }
   
   void MoveToEdge()
   {
      //move until you reach edge
      if (Vector3.Distance(transform.position, jumptarget) >= 0.05f)
      {
         SetHoritzontalVelocity();
      }
      else
      {
         movingEdge = false;
         fallingDown = true;

         velocity /= 3.0f;
         velocity.y = 1.5f;
      }
   }
}
