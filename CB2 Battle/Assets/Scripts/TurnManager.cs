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
    [SerializeField] private GameObject DebugToolTipReference; 
    private bool gameStart = false;
    public InitativeTrackerScript It;
    float incrementer = 0;
    
    SortedList<float, PlayerStats> IntiativeActiveActors = new SortedList<float, PlayerStats>(); 
    SortedList<float, PlayerStats> IntiativeFinishedActors = new SortedList<float, PlayerStats>(); 
    // Start is called before the first frame update

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
            else if(CurrentAttack.AttackMissed)
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
                if(CurrentAttack.soakRoll != null && CurrentAttack.soakRoll.Completed() && !CurrentAttack.soakRolled)
                {
                    CurrentAttack.SoakRollComplete();
                    ResolveHit();
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
        switch(currentAction)
        {
        case "Move":
            if (ServerHitObject.tag == "Tile" && !ActivePlayer.moving && currentAction != null)
            {
                /*
                Tile t = ServerHitObject.GetComponent<Tile>();

                //player can reach 
                if (t.selectable)
                {
                    //make tile green
                    ActivePlayer.moveToTile(t);
                    halfActions--;
                    AttackOfOppertunity();
                    ActivePlayer.RemoveSelectableTiles();

                    if(ActivePlayerStats.hasCondition("Braced"))
                    {
                    CombatLog.Log("By moving, " + ActivePlayerStats.GetName() + " loses their Brace Condition");
                    PopUpText.CreateText("Unbraced!", Color.red, ActivePlayerStats.gameObject);
                    ActivePlayerStats.RemoveCondition("Braced");
                    }
                } 
                else if (t.selectableRunning)
                {
                    //make tile green
                    ActivePlayer.moveToTile(t);
                    halfActions = 0;
                    AttackOfOppertunity();
                    ActivePlayer.RemoveSelectableTiles();

                    if(ActivePlayerStats.hasCondition("Braced"))
                    {
                    CombatLog.Log("By moving, " + ActivePlayerStats.GetName() + " loses their Brace Condition");
                    PopUpText.CreateText("Unbraced!", Color.red, ActivePlayerStats.gameObject);
                    ActivePlayerStats.RemoveCondition("Braced");
                    }
                }
                */
            }
            break;
        case "Attack":
            if (ServerHitObject.tag == "Player")
            {
                RollToHit(ServerHitObject.GetComponent<PlayerStats>(), FireRate, ActiveWeapon, ActivePlayerStats);
            }
            break;
        case "CM":
            if (ServerHitObject.tag == "Player")
            {
                PlayerStats CMtarget = ServerHitObject.GetComponent<PlayerStats>();
                if(FireRate.Equals("Grapple"))
                {
                    RollToGrapple(CMtarget, ActivePlayerStats);
                }
            }
            break;
        default:
            break;
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
            UIPlayerInfo.StartTurn(ActivePlayerStats);
            ActivePlayer = ActivePlayerStats.GetComponent<TacticsMovement>();
            halfActions = 2;
            freeActions = 1;
            ActivePlayerStats.ResetActions();
            ActivePlayer.ResetMove();
            ApplyConditions();
            Cancel();
            PrintInitiative();
            RemoveRange(ActivePlayerStats);
            CameraButtons.SetFocus(ActivePlayer.transform.position);
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
   public void StartTurn(TacticsMovement newPlayer)
    {
        if (IntiativeActiveActors.Count > 0) 
        {
            StartTurn();
        }
    }

    private void CompletePass()
    {
        SortedList<float, PlayerStats> TempSorter = new SortedList<float, PlayerStats>(); 

        foreach (KeyValuePair<float, PlayerStats> kvp in IntiativeFinishedActors) 
        {
            float initative = kvp.Key;
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
                IntiativeFinishedActors.Add(0, player);
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
            IntiativeFinishedActors.Add(newInitative,player);
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
            float initiative = ps.RollInitaitve() + (float)ps.GetStat(AttributeKey.Agility)/10f;
            if(IntiativeActiveActors.ContainsKey(initiative))
            {
                incrementer += 0.01f;
                initiative += incrementer;
            }
            IntiativeActiveActors.Add(initiative,ps);
        }

        if(IntiativeActiveActors.Count > 0)
        {
            StartTurn();
        }
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

    public void EndTurn(){
        float newInitative = IntiativeActiveActors.Keys[IntiativeActiveActors.Count-1] - 10;
        if(Mathf.FloorToInt(newInitative) < 1)
        {
            newInitative = 0;
            while(IntiativeFinishedActors.ContainsKey(newInitative))
            {
                newInitative += 0.001f;
            }
        }
        IntiativeFinishedActors.Add(newInitative,ActivePlayerStats);
        IntiativeActiveActors.RemoveAt(IntiativeActiveActors.Count-1);
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
    
    IEnumerator FireDistractionDelay()
    {
        RollResult fireroll = ActivePlayerStats.AbilityCheck("WP",0);
        while(!fireroll.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        if(!fireroll.Passed())
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " is too distracted by the fire to act!");
            halfActions = 0;
        }
        else
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " is able to act while on fire!");
        }
        Cancel();
    }

    public void RollToHit(PlayerStats target, string ROF, Weapon w, PlayerStats attacker)
    {
        FireRate = ROF;
        this.target = target; 
        ActiveWeapon = w;
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
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
            RollResult GrappleResult = attacker.AbilityCheck("WS",0);
            while(!GrappleResult.Completed())
            {
                yield return new WaitForSeconds(0.5f);
            }
            if (GrappleResult.Passed())
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

    public void AbilityCheck(string skill)
    {
        ActivePlayerStats.AbilityCheck(skill,0);
    }
    public void AbilityCheck(string skill, int value)
    {
        ActivePlayerStats.AbilityCheck(skill,value);
        Cancel();
    }

    public void AddPlayer(GameObject newPlayer)
    {
        PlayerStats newps = newPlayer.GetComponent<PlayerStats>();
        float initiative = newps.RollInitaitve() + (float)newps.GetStat(AttributeKey.Agility)/10f;
        Stack<TacticsMovement> TempStack = new Stack<TacticsMovement>();
        if(IntiativeActiveActors.ContainsKey(initiative))
        {
            incrementer += 0.01f;
            initiative += incrementer;
        }
        IntiativeFinishedActors.Add(initiative, newps);
        StartTurn();
    }

    public void RemovePlayer(GameObject Player)
    {
        PlayerStats removedPlayer = Player.GetComponent<PlayerStats>();
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

}