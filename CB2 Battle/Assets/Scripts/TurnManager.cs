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
    
    SortedList<float, PlayerStats> IntiativeActiveActors = new SortedList<float, PlayerStats>(); 
    SortedList<float, PlayerStats> IntiativeFinishedActors = new SortedList<float, PlayerStats>(); 
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
        if (IntiativeActiveActors.Count > 0) 
        {
            ActivePlayerStats = IntiativeActiveActors[IntiativeActiveActors.Keys[IntiativeActiveActors.Count-1]];
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
        else
        {
            CompletePass();
        }
    }
   public void StartTurn(PlayerStats newPlayer)
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
        halfActions = 2;
        freeActions = 1;
        ActivePlayerStats.ResetActions();
        ActivePlayer.OnTurnStart();
        ApplyConditions();
        PrintInitiative();
        Cancel();
        //PrintInitiative();
        RemoveRange(ActivePlayerStats);
        pv.RPC("RPC_SetCamera", RpcTarget.All, ActivePlayer.transform.position);
        foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
        {
            w.OnTurnStart();
        }
    }

    [PunRPC]
    void RPC_SetCamera(Vector3 location)
    {
        CameraButtons.SetFocus(location);
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

    public void SubtractIniative(PlayerStats player, int initative)
    {
        if(IntiativeActiveActors.ContainsValue(player))
        {
            int indexOf = IntiativeActiveActors.IndexOfValue(player);
            float newInitative = IntiativeActiveActors.Keys[indexOf] - initative;
            IntiativeActiveActors.RemoveAt(indexOf);
            if(newInitative < 1)
            {
                newInitative = 0;
                while(IntiativeFinishedActors.ContainsKey(newInitative))
                {
                    newInitative += 0.01f;
                }
                IntiativeFinishedActors.Add(newInitative, player);
            } 
            else
            {   
                IntiativeActiveActors.Add(newInitative, player);
            }   
        }
        else if(IntiativeFinishedActors.ContainsValue(player))
        {
            int indexOf = IntiativeFinishedActors.IndexOfValue(player);
            float newInitative = IntiativeFinishedActors.Keys[indexOf] - initative;
            IntiativeFinishedActors.RemoveAt(indexOf);
            if(newInitative < 1)
            {
                newInitative = 0;
            }
            while(IntiativeFinishedActors.ContainsKey(newInitative))
            {
                newInitative += 0.01f;
            }
            IntiativeFinishedActors.Add(newInitative,player);
        }
        else
        {
            Debug.LogError("Error: player not present in initative order!");
        }
        PrintInitiative();   
    }


    public void IncreaseInitiative(PlayerStats player, int initative)
    {
        if(IntiativeActiveActors.ContainsValue(player))
        {
            int indexOf = IntiativeActiveActors.IndexOfValue(player);
            float newInitative = IntiativeActiveActors.Keys[indexOf] + initative;
            IntiativeActiveActors.RemoveAt(indexOf);
            while(IntiativeActiveActors.ContainsKey(newInitative))
            {
                newInitative += 0.01f;
            }
            IntiativeActiveActors.Add(newInitative, player);
        }
        else if(IntiativeFinishedActors.ContainsValue(player))
        {
            int indexOf = IntiativeFinishedActors.IndexOfValue(player);
            // only add initative to those who haven't been bumped off initative yet
            if(IntiativeFinishedActors.Keys[indexOf] >= 1)
            {
                float newInitative = IntiativeFinishedActors.Keys[indexOf] + initative;
                IntiativeFinishedActors.RemoveAt(indexOf);
                while(IntiativeFinishedActors.ContainsKey(newInitative))
                {
                    newInitative += 0.01f;
                }
                IntiativeFinishedActors.Add(newInitative,player);
            }
        }
        else
        {
            Debug.LogError("Error: player not present in initative order!");
        }
        PrintInitiative();   
    }

    //sorts initative order at the start of the new round. Rounds are larger than passes 
    public void StartNewRound()
    {
        CombatLog.Log("New Round");
        gameStart = true;
        //InitativeOrder.Clear();
        IntiativeActiveActors.Clear(); 
        IntiativeFinishedActors.Clear();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        //adds all values in to sorted list in descending order
        foreach (GameObject p in players)
        {
            PlayerStats ps = p.GetComponent<PlayerStats>();
            ps.StartRound();
            if(!ps.myData.isMinion)
            {
                float initiative = RollInitaitve(ps);
                if(Mathf.FloorToInt(initiative) >= 1)
                {
                    while(IntiativeActiveActors.ContainsKey(initiative))
                    {
                        initiative += 0.01f;
                    }
                    IntiativeActiveActors.Add(initiative,ps);
                }
                else
                {
                    while(IntiativeFinishedActors.ContainsKey(initiative))
                    {
                        initiative += 0.01f;
                    }
                    IntiativeFinishedActors.Add(initiative,ps);
                }
            }
        }

        if(IntiativeActiveActors.Count > 0)
        {
            StartTurn();
        }
    }

    private float RollInitaitve(PlayerStats ps)
    {
        float initiative = ps.RollInitaitve() + (float)ps.GetStat(AttributeKey.Agility)/10f;
        if(ps.hasCondition(Condition.Winded))
        {
            PopUpText.CreateText("Winded!", Color.yellow, ps.gameObject);
            ps.RemoveCondition(Condition.Winded);
            initiative -= 10;
            if(initiative < 0)
            {
                initiative = 0; 
            }
        }
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

    // called by the player manually ends the turn and transfers the activeplayer to the inactive iniative list
    public void EndTurn(){
        ActivePlayerStats.OnTurnEnd();
        // minions do not participate in the initative order
        if(IntiativeActiveActors.ContainsValue(ActivePlayerStats))
        {
            float initative = IntiativeActiveActors.Keys[IntiativeActiveActors.IndexOfValue(ActivePlayerStats)];
            IntiativeFinishedActors.Add(initative,ActivePlayerStats);
            IntiativeActiveActors.RemoveAt(IntiativeActiveActors.IndexOfValue(ActivePlayerStats));
        }
        /* OLD STYLE MAYBE BRING BACK?
        float newInitative = IntiativeActiveActors.Keys[IntiativeActiveActors.Count-1] - 10;
        if(Mathf.FloorToInt(newInitative) < 1)
        {
            newInitative = 0;
            while(IntiativeFinishedActors.ContainsKey(newInitative))
            {
                newInitative += 0.01f;
            }
        }
        */
        StartTurn();
    }

    /*
    public void EndTurn(TacticsMovement newPlayer){
        ActivePlayerStats.UpdateConditions(false);
        //InitativeOrder.Enqueue(InitativeOrder.Dequeue());
        ActivePlayerStats.ApplyAdvanceBonus(0);
        StartTurn(newPlayer);
    }
    */

    public void PrintInitiative()
    {
        if(gameStart)
        {
            Stack<string> outputStack = new Stack<string>();
            List<string> entries = new List<string>();
            foreach(KeyValuePair<float, PlayerStats> kvp in IntiativeActiveActors)
            {  
                outputStack.Push(kvp.Value.GetName() + ": " + Mathf.FloorToInt(kvp.Key));
            }
            while(outputStack.Count > 0)
            {
                entries.Add(outputStack.Pop());
            }
            entries.Add("-----inactive-----");
            foreach(KeyValuePair<float, PlayerStats> kvp in IntiativeFinishedActors)
            {  
                outputStack.Push(kvp.Value.GetName() + ": " + Mathf.FloorToInt(kvp.Key));
            }
            while(outputStack.Count > 0)
            {
                entries.Add(outputStack.Pop());
            }
            It.UpdateList(entries.ToArray());
        }
        
    }

    //any special effects from conditions
    public void ApplyConditions()
    {
        if(ActivePlayerStats.hasCondition(Condition.Running))
        {
            freeActions = 0;
        }
        if(ActivePlayerStats.Grappling())
        {
            PopUpText.CreateText("Grappling!", Color.red, ActivePlayerStats.gameObject);
            ActivePlayerStats.SpendAction("reaction");
            currentAction = "Grapple";
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
        if(target.GetTeam() != ActivePlayerStats.GetTeam())
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

    public void AddPlayer(GameObject newPlayer)
    {
        PlayerStats newps = newPlayer.GetComponent<PlayerStats>();
        if(!newps.myData.isMinion)
        {
            float initiative = newps.RollInitaitve() + (float)newps.GetStat(AttributeKey.Agility)/10f;
            while(IntiativeFinishedActors.ContainsKey(initiative))
            {
                initiative += 0.01f;
            }
            IntiativeFinishedActors.Add(initiative, newps);
        }
        else
        {
            StartTurn(newps);
        }
    }

    public void RemovePlayer(GameObject Player)
    {
        PlayerStats removedPlayer = Player.GetComponent<PlayerStats>();
        if(IntiativeActiveActors.Count == 1)
        {
            ActivePlayerStats = null;
        }
        while(ActivePlayerStats == removedPlayer)
        {
            EndTurn();
        }
        if(IntiativeActiveActors.ContainsValue(removedPlayer))
        {
            IntiativeActiveActors.RemoveAt(IntiativeActiveActors.IndexOfValue(removedPlayer));
        }
        if(IntiativeFinishedActors.ContainsValue(removedPlayer))
        {
            IntiativeFinishedActors.RemoveAt(IntiativeFinishedActors.IndexOfValue(removedPlayer));
        }
        PhotonNetwork.Destroy(Player);
        PrintInitiative();
    }

    public void TotalDefense()
    {
        CurrentAttack.target.SetCondition(Condition.FullDefense,1,true);
        TacticsAttack.Defend(CurrentAttack,0);
        SubtractIniative(CurrentAttack.target,10);
        ClearActions();
    }

    public void Dodge()
    {
        int gymnasticsBonus = CurrentAttack.target.myData.GetAttribute(AttributeKey.Gymnastics);
        TacticsAttack.Defend(CurrentAttack,gymnasticsBonus);
        SubtractIniative(CurrentAttack.target,5);
        ClearActions();
    }

    public void Block()
    {
        int unarmedBonus = CurrentAttack.target.myData.GetAttribute(AttributeKey.UnarmedCombat);
        TacticsAttack.Defend(CurrentAttack,unarmedBonus);
        SubtractIniative(CurrentAttack.target,5);
        ClearActions();
    }

    public void Parry()
    { 
        int parryBonus = CurrentAttack.target.myData.GetAttribute(AttributeKey.Blades);
        TacticsAttack.Defend(CurrentAttack,parryBonus);
        SubtractIniative(CurrentAttack.target,5);
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
        if(!ActiveDrone.deployed)
        {
            CreateMinion();
        }
        else
        {
            StartTurn(ActiveDrone.droneMinion);
        }
    }
}