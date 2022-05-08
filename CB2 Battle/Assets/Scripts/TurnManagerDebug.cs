using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

// This is the big one, the game master for active play. This script manages all the logic required for playing CB2 combat
public class TurnManagerDebug : TurnManager
{
    //static means easily accessable reference for all moveable characters
    //private static Queue<TacticsMovement> InitativeOrder = new Queue<TacticsMovement>();
    private bool gameStart = false;
    SortedList<float, PlayerStats> IntiativeActiveActors = new SortedList<float, PlayerStats>(); 
    SortedList<float, PlayerStats> IntiativeFinishedActors = new SortedList<float, PlayerStats>(); 

    int hits = 0;
    int misses = 0; 
    int kills = 0;

    // Start is called before the first frame update
    void Awake()
    {
        if(DmMenu.debugMode)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update ()
    {
        // If client, ignore game logic just check mouse
        if(!pv.IsMine)
        {
            CheckMouse();
        }
        // If server, game logic is ok
        else
        {
            if(CurrentAttack == null)
            {
                if(AttackQueue.Count > 0)
                {
                    CurrentAttack = AttackQueue.Dequeue();
                }
            }
            else if(CurrentAttack.Completed)
            {
                CurrentAttack = null;
                Cancel();
                
                if(AttackQueue.Count == 0)
                {
                    Debug.Log("hits: " + hits + "misses: " + misses + "kills: " + kills + "ATTK: " + (200f/kills) + " AHTK: " + ((float)hits/kills));
                }
            }      
            else
            {
                if(CurrentAttack.attackRoll != null && CurrentAttack.attackRoll.Completed() && !CurrentAttack.attackRolled)
                {
                    CurrentAttack.AttackRollComplete();
                    NoReaction();
                }
                if(CurrentAttack.reactionRoll != null && CurrentAttack.reactionRoll.Completed() && !CurrentAttack.reactionRolled)
                {
                    if(CurrentAttack.GetNetHits() > 0)
                    {
                        hits++;
                    }
                    else
                    {
                        misses++;
                    }
                    CurrentAttack.ReactionRollComplete();
                    if(CurrentAttack.target.getWounds() > CurrentAttack.target.myData.GetAttribute(AttributeKey.PhysicalHealth))
                    {
                        kills++;
                        CurrentAttack.target.myData.SetAttribute(AttributeKey.PDamage, 0, false);
                        CurrentAttack.target.ResetHealth();
                    }
                }
            }
            //Where all possible actions take place
            if (ActivePlayer != null && CurrentAttack == null && !CameraButtons.UIActive()) 
            {
            if(ActivePlayer.finishedMoving())
            {
                ActivePlayerStats.OnMovementEnd();
                Cancel();
            }
            CheckMouse();
            switch(currentAction)
            {
                case "Move":
                break;
                case "Stand":
                    ActivePlayerStats.RemoveCondition(Condition.Prone);
                    PopUpText.CreateText("Standing!", Color.green, ActivePlayerStats.gameObject);
                    halfActions--;
                    Cancel();
                break;
                default:
                    break;
                }
            }
        }
    }

    void CheckMouse()
    {
        if(Input.GetMouseButtonUp(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            //if the mouse clicked on a thing
            if (Physics.Raycast(ray, out hit))
            {
                switch(currentAction)
                    {
                    case "Move":
                        if(hit.collider.tag == "Player" && !ActivePlayer.moving)
                        {
                            GameObject DBT = Instantiate(DebugToolTipReference) as GameObject;
                            DBT.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
                            DBT.transform.position = Input.mousePosition;
                            DBT.GetComponent<DebugTooltip>().UpdateStatIn(hit.collider.GetComponent<PlayerStats>());
                        }
                    break;
                    default:
                    break;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool hitUi = false;
            GameObject[] ui = GameObject.FindGameObjectsWithTag("ActionInput");
            foreach(GameObject g in ui)
            {
                if(EventSystem.current.IsPointerOverGameObject())
                {
                    hitUi = true;
                }
            }
            //if the mouse clicked on a thing
            if (Physics.Raycast(ray, out hit) && !hitUi )
            {
                Vector3 SentPos;
                if(hit.collider.tag.Equals("Player") || hit.collider.tag.Equals("Tile"))
                {
                    SentPos = hit.collider.transform.position;
                }
                else
                {
                    SentPos = BoardBehavior.GetClosestTile(hit.point);
                }
                pv.RPC("RPC_Send_Hit_Left",RpcTarget.MasterClient, SentPos);
            }
        }
    }

    
    void ServerLeftClick(GameObject ServerHitObject)
    {
            if (ServerHitObject.tag == "Player")
            {
                PlayerStats ps = ServerHitObject.GetComponent<PlayerStats>();
                switch(currentAction)
                {
                    case "Attack":
                    RollToHit(ps, FireRate, ActiveWeapon, ActivePlayerStats);
                    break;
                    case "SelectSingleEnemy":
                    if(ps.GetTeam() != ActivePlayerStats.GetTeam())
                    {
                        target = ps;
                    }
                    break;
                    case "SelectSingleAlly":
                    if(ps.GetTeam() == ActivePlayerStats.GetTeam())
                    {
                        target = ps;
                    }
                    break;
                }
            }
            else if(ServerHitObject.tag == "Tile")
            {
                switch(currentAction)
                {
                    case "SelectTargetsTiles":
                    if(multipleTargets.Count < multipleTargetsLimit && !multipleTargets.Contains(ServerHitObject) && !ServerHitObject.GetComponent<Tile>().blank)
                    {
                        CombatLog.Log("Selected tile at " + ServerHitObject.transform.position);
                        multipleTargets.Add(ServerHitObject);
                    }
                    break;
                    case "SelectLocation":
                        multipleTargets.Add(ServerHitObject);
                    break;
                }
            }
    }

    [PunRPC]
    void RPC_Send_Hit_Left(Vector3 ObjectLocation)
    {
        GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject p in Players)
        {
            if(p.transform.position == ObjectLocation)
            {
                ServerLeftClick(p);
                break;
            }
        }
        Tile myTile = BoardBehavior.GetTile(ObjectLocation);
        if(myTile != null)
        {
            ServerLeftClick(myTile.gameObject);
        }
    }

    
    public new void StartTurn()
    {
        if (IntiativeActiveActors.Count > 0) 
        {
            ActivePlayerStats = IntiativeActiveActors[IntiativeActiveActors.Keys[IntiativeActiveActors.Count-1]];
            UIPlayerInfo.ShowAllInfo(ActivePlayerStats);
            ActivePlayer = ActivePlayerStats.GetComponent<TacticsMovement>();
            halfActions = 100;
            freeActions = 100;
            ActivePlayerStats.ResetActions();
            ActivePlayer.OnTurnStart();
            ApplyConditions();
            Cancel();
            PrintInitiative();
            RemoveRange(ActivePlayerStats);
            //pv.RPC("RPC_SetCamera", RpcTarget.All, ActivePlayer.transform.position);
            foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
            {
                w.OnTurnStart();
            }
        }
        else
        {
            CompletePass();
        }
    }

   public new void StartTurn(PlayerStats newPlayer)
    {
        // if someone else is active, end their trun
        if(ActivePlayerStats != null && IntiativeActiveActors.ContainsValue(ActivePlayerStats))
        {
            ActivePlayerStats.OnTurnEnd();
            float initative = IntiativeActiveActors.Keys[IntiativeActiveActors.IndexOfValue(ActivePlayerStats)];
            IntiativeFinishedActors.Add(initative,ActivePlayerStats);
            IntiativeActiveActors.RemoveAt(IntiativeActiveActors.IndexOfValue(ActivePlayerStats));
        }
        ActivePlayerStats = newPlayer;
        UIPlayerInfo.ShowAllInfo(ActivePlayerStats);
        ActivePlayer = ActivePlayerStats.GetComponent<TacticsMovement>();
        halfActions = 100;
        freeActions = 100;
        ActivePlayerStats.ResetActions();
        ActivePlayer.OnTurnStart();
        ApplyConditions();
        PrintInitiative();
        Cancel();
        //PrintInitiative();
        RemoveRange(ActivePlayerStats);
        //pv.RPC("RPC_SetCamera", RpcTarget.All, ActivePlayer.transform.position);
        foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
        {
            w.OnTurnStart();
        }
    }

    private void CompletePass()
    {
        SortedList<float, PlayerStats> TempSorter = new SortedList<float, PlayerStats>(); 

        foreach (KeyValuePair<float, PlayerStats> kvp in IntiativeFinishedActors) 
        {
            // NEW INITATIVE STYLE SUBTRACT ON PASS NOT ON TURN END
            float initative = kvp.Key - 10;
            if(Mathf.FloorToInt(initative) >= 1)
            {
                IntiativeActiveActors.Add(initative, kvp.Value);
            }
            else{
                initative = 0;
                while(TempSorter.ContainsKey(initative))
                {
                    initative += 0.01f;
                }
                TempSorter.Add(initative, kvp.Value);
            }
        }

        IntiativeFinishedActors = TempSorter;

        if(IntiativeActiveActors.Count > 0)
        {
            StartTurn();
        }
        else
        {
            StartNewRound();
        }
    }

    public new void RollToHit(PlayerStats target, string ROF, Weapon w, PlayerStats attacker)
    {
        FireRate = ROF;
        this.target = target; 
        ActiveWeapon = w;
        //only a valid target if on diferent teams (los restriction has been lifted temporarily)
        //if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        //{

        SkillPromptBehavior.ManualRolls = false;

        hits = 0;
        misses = 0;
        kills = 0;
        target.ResetHealth();
        for(int i = 0; i < 200; i++)
        {
            AttackSequence newAttack = TacticsAttack.Attack(target, attacker, ActiveWeapon, ROF);
            AttackQueue.Enqueue(newAttack);
        }   
    }

}