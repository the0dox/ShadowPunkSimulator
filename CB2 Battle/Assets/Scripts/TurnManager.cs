using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

// This is the big one, the game master for active play. This script manages all the logic required for playing CB2 combat
public class TurnManager : TurnActionsSR
{
    //static means easily accessable reference for all moveable characters
    //private static Queue<TacticsMovement> InitativeOrder = new Queue<TacticsMovement>();
    [SerializeField] public GameObject DebugToolTipReference; 
    private bool gameStart = false;
    public InitativeTrackerScript It;
    public static TurnManager instance;
    public static DebugTooltip DebugTooltip;
    
    SortedList<float, PlayerStats> InitiativeSorter;
    private Queue<PlayerStats> InitiativeQueue;

    // Start is called before the first frame update
    void Awake()
    {
        if(!DmMenu.debugMode)
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
            }      
            else
            {
                if(CurrentAttack.attackRoll != null && CurrentAttack.attackRoll.Completed() && !CurrentAttack.attackRolled)
                {
                    CurrentAttack.AttackRollComplete();
                    TryReaction();
                }
                if(CurrentAttack.reactionRoll != null && CurrentAttack.reactionRoll.Completed() && !CurrentAttack.reactionRolled)
                {
                    CurrentAttack.ReactionRollComplete();
                }
            }
            //Where all possible actions take place
            if (ActivePlayer != null) 
            {
                if(ActivePlayer.finishedMoving())
                {
                    ActivePlayerStats.OnMovementEnd();
                    Cancel();
                }
            }
            if(!CameraButtons.UIActive())
            {
                CheckMouse();
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
                        if(hit.collider.tag == "Player")
                        {
                            GameObject DBT = Instantiate(DebugToolTipReference) as GameObject;
                            DBT.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
                            DBT.transform.position = Input.mousePosition;
                            DebugTooltip = DBT.GetComponent<DebugTooltip>();
                            DebugTooltip.UpdateStatIn(hit.collider.GetComponent<PlayerStats>());
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
            if(EventSystem.current.IsPointerOverGameObject())
            {
                hitUi = true;
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
        if(withinRange(ServerHitObject))
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

    public void StartTurn()
    {
        if (InitiativeQueue.Count > 0) 
        {
            ActivePlayerStats = InitiativeQueue.Peek();
            UIPlayerInfo.ShowAllInfo(ActivePlayerStats);
            ActivePlayer = ActivePlayerStats.GetComponent<TacticsMovement>();
            halfActions = 2;
            freeActions = 1;
            ActivePlayerStats.ResetActions();
            ActivePlayer.OnTurnStart();
            ApplyConditions();
            Cancel();
            PrintInitiative();
            RemoveRange(ActivePlayerStats);
            pv.RPC("RPC_SetCamera", RpcTarget.All, ActivePlayer.transform.position);
            foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
            {
                w.OnTurnStart();
            }
        }
    }
   public void StartTurn(PlayerStats newPlayer, bool end)
    {
        if(InitiativeQueue.Contains(newPlayer))
        {
            // if someone else is active, end their trun
            if(ActivePlayerStats != null && end)
            {
                ActivePlayerStats.OnTurnEnd(halfActions);
            }
            while(InitiativeQueue.Peek() != newPlayer)
            {
                InitiativeQueue.Enqueue(InitiativeQueue.Dequeue());
            }
            StartTurn();
        }
        else
        {
            Debug.LogWarning(newPlayer.GetName() + " cannot start their turn as they are not in this initative queue!");
        }
    }

    [PunRPC]
    void RPC_SetCamera(Vector3 location)
    {
        CameraButtons.SetFocus(location);
    }

    public void SubtractInitiative(PlayerStats player, int initative)
    {
        Debug.LogWarning("subtracting initative has been removed");
        /*
        if(InitiativeActiveActors.ContainsValue(player))
        {
            int indexOf = InitiativeActiveActors.IndexOfValue(player);
            float newInitative = InitiativeActiveActors.Keys[indexOf] - initative;
            InitiativeActiveActors.RemoveAt(indexOf);
            if(newInitative < 1)
            {
                newInitative = 0;
                while(InitiativeFinishedActors.ContainsKey(newInitative))
                {
                    newInitative += 0.01f;
                }
                InitiativeFinishedActors.Add(newInitative, player);
            } 
            else
            {   
                InitiativeActiveActors.Add(newInitative, player);
            }   
        }
        else if(InitiativeFinishedActors.ContainsValue(player))
        {
            int indexOf = InitiativeFinishedActors.IndexOfValue(player);
            float newInitative = InitiativeFinishedActors.Keys[indexOf] - initative;
            InitiativeFinishedActors.RemoveAt(indexOf);
            if(newInitative < 1)
            {
                newInitative = 0;
            }
            while(InitiativeFinishedActors.ContainsKey(newInitative))
            {
                newInitative += 0.01f;
            }
            InitiativeFinishedActors.Add(newInitative,player);
        }
        else
        {
            Debug.LogWarning("Error: player not present in initative order!");
        }
        PrintInitiative();   
        */
    }


    public void IncreaseInitiative(PlayerStats player, int initative)
    {
        Debug.LogWarning("increaseinitiative is depreciated");
        if(InitiativeSorter.ContainsValue(player))
        {
            int indexOf = InitiativeSorter.IndexOfValue(player);
            float newInitative = InitiativeSorter.Keys[indexOf] + initative;
            InitiativeSorter.RemoveAt(indexOf);
            while(InitiativeSorter.ContainsKey(newInitative))
            {
                newInitative += 0.01f;
            }
            InitiativeSorter.Add(newInitative, player);
        }
        else
        {
            Debug.LogError("Error: player not present in initative order!");
        }
        PrintInitiative();   
    }

    //sorts initative order at the start of the new round. Rounds are larger than passes 
    public void GameStart()
    {
        gameStart = true;

        It.UpdateRound();
        InitiativeSorter = new SortedList<float, PlayerStats>();
        InitiativeQueue = new Queue<PlayerStats>();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log("player count" + players.Length);
        //adds all values in to sorted list in descending order
        foreach (GameObject p in players)
        {
            PlayerStats ps = p.GetComponent<PlayerStats>();
            if(!ps.myData.isMinion)
            {
                float initiative = ps.RollInitaitve() + (float)ps.GetStat(AttributeKey.Agility)/10f;
                while(InitiativeSorter.ContainsKey(initiative))
                {
                    initiative += 0.01f;
                }
                InitiativeSorter.Add(initiative,ps); 
            }
        }
        Stack<PlayerStats> tempStack = new Stack<PlayerStats>();
        foreach(PlayerStats ps in InitiativeSorter.Values)
        {
            tempStack.Push(ps);
        }
        Debug.Log("tempstacks length" + tempStack.Count);
        while(tempStack.Count > 0)
        {
            InitiativeQueue.Enqueue(tempStack.Pop());
        }
        Debug.Log("initiative length" + InitiativeQueue.Count);
        if(InitiativeQueue.Count > 0)
        {
            StartTurn();
        }
    }

    private float RollInitaitve(PlayerStats ps)
    {
        float initiative = ps.RollInitaitve() + (float)ps.GetStat(AttributeKey.Agility)/10f;
        return initiative;
    }

    public GameObject GetActivePlayer()
    {
        return ActivePlayer.gameObject; 
    }

    //Button to bring up player sheet
    public void DisplayCharacterSheet()
    {
        if(!CameraButtons.UIActive())
        {
            int myID = PhotonNetwork.LocalPlayer.ActorNumber;
            pv.RPC("RPC_CharacterSheet",RpcTarget.MasterClient, myID);
        }
    }

    [PunRPC]
    void RPC_CharacterSheet(int callingPlayerID)
    {
        if(ActivePlayerStats != null)
        {
            GameObject newSheet = PhotonNetwork.Instantiate("CharacterSheet", new Vector3(), Quaternion.identity);
            newSheet.GetComponent<CharacterSheet>().UpdateStatsIn(ActivePlayerStats, callingPlayerID);
        }
    }

    // called by the player manually ends the turn and transfers the activeplayer to the inactive Initiative list
    public void EndTurn(){
        if(ActivePlayerStats != null)
        {
            ActivePlayerStats.OnTurnEnd(halfActions);
            CheckRoundEnd();
            // minions do not participate in the initative order
            if(InitiativeQueue.Contains(ActivePlayerStats))
            {
                InitiativeQueue.Enqueue(InitiativeQueue.Dequeue());
            }
        }
        StartTurn();
    }

    public void CheckRoundEnd()
    {
        PlayerStats lastPlayer = InitiativeSorter.Values[0];
        if(lastPlayer == ActivePlayerStats)
        {
            It.UpdateRound();
        }
    }

    public void PrintInitiative()
    {
        if(gameStart)
        {
            Debug.Log("printing InitiativeQueue with size: " + InitiativeQueue.Count);
            Queue<PlayerStats> tempQueue = new Queue<PlayerStats>();
            List<string> entries = new List<string>();
            while(InitiativeQueue.Count > 0)
            {
                PlayerStats currentPlayer = InitiativeQueue.Dequeue();
                float Initiative = InitiativeSorter.Keys[InitiativeSorter.IndexOfValue(currentPlayer)];
                entries.Add(currentPlayer + ": " + Initiative);
                tempQueue.Enqueue(currentPlayer);
            }
            while(tempQueue.Count > 0)
            {
                InitiativeQueue.Enqueue(tempQueue.Dequeue());
            }
            It.UpdateList(entries.ToArray());
        }
        
    }

    //any special effects from conditions
    public void ApplyConditions()
    {
        if(ActivePlayerStats.Grappling())
        {
            PopUpText.CreateText("Grappling!", Color.red, ActivePlayerStats.gameObject);
            ActivePlayerStats.SpendAction("reaction");
            currentAction = "Grapple";
        }
        if(ActivePlayerStats.hasCondition(Condition.Broken))
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " is broken and cannot take actions!");
            halfActions = 0;
            freeActions = 0;
            ActivePlayerStats.healStress(99);
            PopUpText.CreateText("Broken", Color.red, ActivePlayer.gameObject);
            ActivePlayerStats.RemoveCondition(Condition.Broken);
        }
    }

    public void RollToHit(PlayerStats target, string ROF, Weapon w, PlayerStats attacker)
    {
        FireRate = ROF;
        this.target = target; 
        ActiveWeapon = w;
        //only a valid target if on diferent teams (los restriction has been lifted temporarily)
        //if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        //{
        if(target.GetTeam() != attacker.GetTeam())
        {
            AttackSequence newAttack = TacticsAttack.Attack(target, attacker, ActiveWeapon, ROF);
            AttackQueue.Enqueue(newAttack);
            currentAction = null;
            // if its not the attackers turn (overwatch) then don't subtract half actions
            if(ActivePlayerStats == attacker)
            {
                halfActions--;
                if(w.IsWeaponClass(WeaponClass.melee) || ROF.Equals("SAB") || ROF.Equals("LB") || ROF.Equals("FAB"))
                {
                    halfActions--;
                }
            }
        }   
        //}
    }

    public void FallComplete(TacticsMovement tm, int distance)
    {
        TokenDragBehavior.ToggleMovement(true);
        PlayerStats fallingPlayer = tm.GetComponent<PlayerStats>();
        if(distance > 3)
        {
            CombatLog.Log(fallingPlayer.GetName() + " falls " + distance + " meters and must resist fall damage equal to the distance fallen!");       
            AttackQueue.Enqueue(new AttackSequence(fallingPlayer, distance, 4));
        }
    }   

    public void RollToGrapple(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            if(!target.Grappling()){
                StartCoroutine(grappleDelay(target,attacker));
            }
            else
            {
                CombatLog.Log("Target is already grappling!");
            }
        }
    }

    IEnumerator grappleDelay(PlayerStats target, PlayerStats attacker)
    {
            RollResult opposedStrengthcheck = ActivePlayerStats.AbilityCheck(AttributeKey.UnarmedCombat);
            opposedStrengthcheck.OpposedRoll(target.AbilityCheck(AttributeKey.UnarmedCombat));
            while(!opposedStrengthcheck.Completed())
            {
                yield return new WaitForSeconds(0.5f);
            }
            if (opposedStrengthcheck.Passed())
            {
                AttackQueue.Enqueue(new AttackSequence(target,ActivePlayerStats,ActiveWeapon,"Grapple",1,true));
            }
            halfActions -= 2;
            Cancel();
    }

    public void AttackOfOppertunity()
    {
        if(ActivePlayer.GetAdjacentEnemies(ActivePlayerStats.GetTeam()) > 0)
        {
            List<PlayerStats> adjacentEnemies = ActivePlayer.AdjacentPlayers();
            target = ActivePlayerStats;
            FireRate = "Free";
            foreach(PlayerStats enemy in adjacentEnemies)
            {
                Weapon currentWeapon = null;
                if(enemy.PrimaryWeapon != null && enemy.PrimaryWeapon.IsWeaponClass(WeaponClass.melee))
                {
                    currentWeapon = enemy.PrimaryWeapon;
                }
                else if (enemy.SecondaryWeapon != null && enemy.SecondaryWeapon.IsWeaponClass(WeaponClass.melee))
                {
                    currentWeapon = enemy.SecondaryWeapon;
                }
                if(currentWeapon != null)
                {    
                    RollToHit(target,FireRate,currentWeapon,enemy);
                }
            }
        }
    }

    public void PlacePlayer(GameObject newPlayer)
    {
        if(newPlayer != null)
        {
            Cancel();
            ClearActions();
            UIPlayerInfo.UpdateCustomCommand("Placing new token");
            StartCoroutine(PlacePlayer_Delay(newPlayer));
        }
    }

    IEnumerator PlacePlayer_Delay(GameObject newPlayer)
    {
        currentAction = "SelectLocation";
        while(multipleTargets.Count == 0)
        {
            yield return new WaitForSeconds(0.2f);
        }
        Vector3 spawnPosition = multipleTargets[0].transform.position + new Vector3(0, 1.5f, 0);
        newPlayer.GetComponent<TacticsMovement>().SetPosition(spawnPosition);
        AddPlayer(newPlayer);
        Cancel();
        PrintInitiative();
    }

    public void AddPlayer(GameObject newPlayer)
    {
        if(gameStart)
        {
            PlayerStats newps = newPlayer.GetComponent<PlayerStats>();
            if(!newps.myData.isMinion)
            {
                float initiative = newps.RollInitaitve() + (float)newps.GetStat(AttributeKey.Agility)/10f;
                while(InitiativeSorter.ContainsKey(initiative))
                {
                    initiative += 0.01f;
                }
                InitiativeSorter.Add(initiative, newps);
                ResetInitiativeAt(ActivePlayerStats);
            }
        }
    }

    public void RemovePlayer(GameObject Player)
    {
        Cancel();
        PlayerStats removedPlayer = Player.GetComponent<PlayerStats>();
        if(InitiativeSorter.Count == 1)
        {
            ActivePlayerStats = null;
        }
        while(ActivePlayerStats == removedPlayer)
        {
            EndTurn();
        }
        if(InitiativeSorter.ContainsValue(removedPlayer))
        {
            InitiativeSorter.RemoveAt(InitiativeSorter.IndexOfValue(removedPlayer));
        }
        ResetInitiativeAt(ActivePlayerStats);
        PhotonNetwork.Destroy(Player);
        PrintInitiative();
    }

    private void ResetInitiativeAt(PlayerStats top)
    {
        InitiativeQueue.Clear();
        Stack<PlayerStats> tempStack = new Stack<PlayerStats>();
        foreach(PlayerStats ps in InitiativeSorter.Values)
        {
            tempStack.Push(ps);
        }
        while(tempStack.Count > 0)
        {
            InitiativeQueue.Enqueue(tempStack.Pop());
        }
        if(top != null)
        {
            while(InitiativeQueue.Peek() != top)
            {
                InitiativeQueue.Enqueue(InitiativeQueue.Dequeue());
            }
        }
        else
        {
            StartTurn();
        }
    }

    public void TotalDefense()
    {
        CurrentAttack.target.SetCondition(Condition.FullDefense,1,true);
        TacticsAttack.Defend(CurrentAttack,0);
        SubtractInitiative(CurrentAttack.target,10);
        ClearActions();
    }

    public void Dodge()
    {
        int gymnasticsBonus = CurrentAttack.target.myData.GetAttribute(AttributeKey.Gymnastics);
        TacticsAttack.Defend(CurrentAttack,gymnasticsBonus);
        SubtractInitiative(CurrentAttack.target,5);
        ClearActions();
    }
    
    public void BladeParry()
    {
        Debug.LogWarning("bladeparry is untested");
        Weapon targetWeapon = null;
        int weaponBonus = 0;
        if(CurrentAttack.target.PrimaryWeapon != null)
        {
            targetWeapon = CurrentAttack.target.PrimaryWeapon;
        }
        else if(CurrentAttack.target.SecondaryWeapon != null)
        {
            targetWeapon = CurrentAttack.target.SecondaryWeapon;
        }
        if(targetWeapon != null)
        {
            weaponBonus = CurrentAttack.target.myData.GetAttribute(targetWeapon.Template.WeaponSkill.skillKey);
            if(CurrentAttack.target.myData.hasSpecialization(targetWeapon))
            {
                Debug.Log("is specialized!");
                weaponBonus += 2;
            }
        } 
        TacticsAttack.Defend(CurrentAttack,weaponBonus);
        SubtractInitiative(CurrentAttack.target,5);
        ClearActions();
    }


    public void Block()
    {
        int unarmedBonus = CurrentAttack.target.myData.GetAttribute(AttributeKey.UnarmedCombat);
        TacticsAttack.Defend(CurrentAttack,unarmedBonus);
        SubtractInitiative(CurrentAttack.target,5);
        ClearActions();
    }

    public void Parry()
    { 
        int parryBonus = CurrentAttack.target.myData.GetAttribute(AttributeKey.Blades);
        TacticsAttack.Defend(CurrentAttack,parryBonus);
        SubtractInitiative(CurrentAttack.target,5);
        ClearActions();
    }
    public void NoReaction()
    {
        TacticsAttack.Defend(CurrentAttack);
        ClearActions();
    }

    private bool withinRange(GameObject target)
    {
        if(MaxSelectionRange == -1)
        {
            return true;
        }
        int distance = Mathf.FloorToInt(Vector3.Distance(ActivePlayerStats.transform.position, target.transform.position));
        if(distance <= MaxSelectionRange)
        {
            return true;
        }
        else
        {
            CombatLog.Log("Target is outside range");
            return false;
        }
    } 

    public void Drone0()
    {
        List<Drone> droneEquipment = new List<Drone>();
        foreach(Item i in ActivePlayerStats.myData.equipmentObjects)
        {
            if(i.GetType() == typeof(Drone))
            {
                droneEquipment.Add((Drone)i);
            }
        }
        ActiveDrone = droneEquipment[0];
        ActivePlayerStats.takeStun(1);
        if(!ActiveDrone.deployed)
        {
            CreateMinion();
        }
        else
        {
            StartTurn(ActiveDrone.droneMinion, false);
        }
    }

    public static void DebugTooltipToggle()
    {
        if(DebugTooltip != null)
        {
            Destroy(DebugTooltip.gameObject);
        }
    }
}