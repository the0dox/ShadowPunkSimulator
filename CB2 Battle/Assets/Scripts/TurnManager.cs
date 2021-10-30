using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

// This is the big one, the game master for active play. This script manages all the logic required for playing CB2 combat
public class TurnManager : TurnActions
{
    //static means easily accessable reference for all moveable characters
    static Queue<string> turnKey = new Queue<string>();
    private static Queue<TacticsMovement> InitativeOrder = new Queue<TacticsMovement>();
    [SerializeField] private GameObject DebugToolTipReference; 
    private bool gameStart = false;    
    public GameObject CharacterSheet;
    public GameObject SkillModifierScript;
    public InitativeTrackerScript It;
    public GameObject PhotonHit;
    float incrementer = 0;
    SortedList<float, TacticsMovement> Sorter = new SortedList<float, TacticsMovement>();  
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
        else
        {
            if(CurrentAttack.attackRoll != null && CurrentAttack.attackRoll.Completed() && !CurrentAttack.attackRolled)
            {
                Debug.Log("finished roll");
                CurrentAttack.AttackRollComplete();
                TryReaction();
            }
            if(CurrentAttack.reactionRoll != null && CurrentAttack.reactionRoll.Completed()&& !CurrentAttack.reactionRolled)
            {
                CurrentAttack.ReactionRollComplete();
                ResolveHit();
            }
        }
        //Where all possible actions take place
        if (ActivePlayer != null && CurrentAttack == null && !CameraButtons.UIActive()) 
        {
            if(ActivePlayer.finishedMoving())
            {
                if(!ActivePlayerStats.hasCondition("KnockDownBonus"))
                {
                    ActivePlayerStats.SetCondition("KnockDownBonus",1,false);
                }
                Cancel();
                if(!ActivePlayerStats.ValidAction("Charge"))
                {
                    Combat();
                }
            }
            CheckMouse();
            switch(currentAction)
            {
                case "Move":
                    if (ActivePlayer.moving)
                    {
                        ActivePlayer.Move();
                    }
                break;
                case "ReloadPrimaryWeapon":
                    if(halfActions < 1)
                    {
                        EndTurn();
                    }
                    else
                    {
                        if(ActivePlayerStats.PrimaryWeapon.Reload(ActivePlayerStats))
                        {
                            ActivePlayerStats.CompleteRepeatingAction();
                            PopUpText.CreateText("Reloaded!", Color.green, ActivePlayer.gameObject);
                        }
                        else
                        {
                            PopUpText.CreateText(ActivePlayerStats.PrimaryWeapon.ReloadString(), Color.yellow, ActivePlayer.gameObject);
                        }
                        halfActions--;
                        Cancel();
                    }
                break;
                case "ReloadSecondaryWeapon":
                    if(halfActions < 1)
                    {
                        EndTurn();
                    }
                    else
                    {
                        if(ActivePlayerStats.SecondaryWeapon.Reload(ActivePlayerStats))
                        {
                            ActivePlayerStats.CompleteRepeatingAction();
                            PopUpText.CreateText("Reloaded!", Color.green, ActivePlayer.gameObject);
                        }
                        else
                        {
                            PopUpText.CreateText(ActivePlayerStats.SecondaryWeapon.ReloadString(), Color.yellow, ActivePlayer.gameObject);
                        }
                        halfActions--;
                        Cancel();
                    }
                break;
                case "Stand":
                    AttackOfOppertunity();
                    ActivePlayerStats.RemoveCondition("Prone");
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
            
                Debug.Log("Sending info of my hitlocation");
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
            }
            break;
        case "Run":
            if (ServerHitObject.tag == "Tile" && !ActivePlayer.moving)
            {
                Tile t = ServerHitObject.GetComponent<Tile>();
                //player can reach 
                ActivePlayerStats.SetCondition("Running",0,true);
                if (t.selectableRunning)
                {
                    //make tile green
                    ActivePlayer.moveToTile(t);
                    AttackOfOppertunity();
                    halfActions-=2;
                    currentAction = "Move";
                    ActivePlayer.RemoveSelectableTiles();

                    
                    if(ActivePlayerStats.hasCondition("Braced"))
                    {
                        CombatLog.Log("By moving, " + ActivePlayerStats.GetName() + " loses their Brace Condition");
                        PopUpText.CreateText("Unbraced!", Color.red, ActivePlayerStats.gameObject);
                        ActivePlayerStats.RemoveCondition("Braced");
                    }
                }
            }
            break;
        case "Disengage":
            if (ServerHitObject.tag == "Tile" && !ActivePlayer.moving)
            {
                Tile t = ServerHitObject.GetComponent<Tile>();
                //player can reach 
                if (t.selectableRunning)
                {
                    //make tile green
                    ActivePlayer.moveToTile(t);
                    if(ActivePlayerStats.AbilityCheck("Acrobatics",0).Passed())
                    {
                        CombatLog.Log(ActivePlayerStats.GetName() + "'s successful acrobatics check reduces the cost of disengaging to a half action");
                        halfActions--;
                    }
                    else
                    {
                        halfActions -= 2;
                    }                  
                    currentAction = "Move";  
                    ActivePlayer.RemoveSelectableTiles();

                    if(ActivePlayerStats.hasCondition("Braced"))
                    {
                        CombatLog.Log("By moving, " + ActivePlayerStats.GetName() + " loses their Brace Condition");
                        PopUpText.CreateText("Unbraced!", Color.red, ActivePlayerStats.gameObject);
                        ActivePlayerStats.RemoveCondition("Braced");
                    }
                }
            }
            break;
        case "Advance":
            if (ServerHitObject.tag == "Tile" && !ActivePlayer.moving)
            {
                Tile t = ServerHitObject.GetComponent<Tile>();
                //player can reach 
                if (t.selectableRunning)
                {
                    //make tile green
                    ActivePlayer.moveToTile(t);
                    ActivePlayerStats.ApplyAdvanceBonus(TacticsAttack.SaveCoverBonus(ActivePlayerStats)); 
                    halfActions-=2;

                    if(ActivePlayerStats.hasCondition("Braced"))
                    {
                        CombatLog.Log("By moving, " + ActivePlayerStats.GetName() + " loses their Brace Condition");
                        PopUpText.CreateText("Unbraced!", Color.red, ActivePlayerStats.gameObject);
                        ActivePlayerStats.RemoveCondition("Braced");
                    }
                }
        }
            break;
        case "Charge":
            if (ServerHitObject.tag == "Tile" && !ActivePlayer.moving)
            {
                Tile t = ServerHitObject.GetComponent<Tile>();
                //player can reach 
                if (t.selectableRunning)
                {
                    //make tile green
                    ActivePlayer.moveToTile(t);
                    AttackOfOppertunity();
                    ActivePlayerStats.SetCondition("Charging",1,true);
                    ActivePlayerStats.SpendAction("Charge");
                    currentAction = "Move";
                    ActivePlayer.RemoveSelectableTiles();
                    halfActions -= 2;

                    if(ActivePlayerStats.hasCondition("Braced"))
                    {
                        CombatLog.Log("By moving, " + ActivePlayerStats.GetName() + " loses their Brace Condition");
                        PopUpText.CreateText("Unbraced!", Color.red, ActivePlayerStats.gameObject);
                        ActivePlayerStats.RemoveCondition("Braced");
                    }
                }
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
                if(FireRate.Equals("Stun"))
                {
                    RollToStun(CMtarget, ActivePlayerStats);
                }
                else if(FireRate.Equals("Grapple"))
                {
                    RollToGrapple(CMtarget, ActivePlayerStats);
                }
                else if(FireRate.Equals("Knock"))
                {
                    RollToKnock(CMtarget, ActivePlayerStats);
                }
                else if(FireRate.Equals("Feint"))
                {
                    RollToFeint(CMtarget, ActivePlayerStats);
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
        Debug.Log("finding object at " + ObjectLocation);
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
        if (InitativeOrder.Count != 0) 
        {
            ActivePlayer = InitativeOrder.Peek();
            ActivePlayerStats = ActivePlayer.GetComponent<PlayerStats>();
            //CombatLog.Log("player " + ActivePlayerStats.GetName() + " is starting their turn");
            halfActions = 2;
            ActivePlayerStats.ResetActions();
            ActivePlayer.ResetMove();
            ApplyConditions();
            Cancel();
            PrintInitiative();
            RemoveRange(ActivePlayerStats);
            ActivePlayerStats.UpdateConditions(true);
            CameraButtons.SetFocus(ActivePlayer.transform.position);
            foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
            {
                w.OnTurnStart();
            }
        }
    }
   public void StartTurn(TacticsMovement newPlayer)
    {
        if (InitativeOrder.Count != 0) 
        {
            while (InitativeOrder.Peek() != newPlayer)
            {
                InitativeOrder.Enqueue(InitativeOrder.Dequeue());    
            }
            ActivePlayer = newPlayer;
            ActivePlayerStats = ActivePlayer.GetComponent<PlayerStats>();
            //CombatLog.Log("player " + ActivePlayerStats.GetName() + " is starting their turn");
            halfActions = 2;
            ActivePlayerStats.ResetActions();
            ActivePlayer.ResetMove();
            ApplyConditions();
            Cancel();
            PrintInitiative();
            RemoveRange(ActivePlayerStats);
            ActivePlayerStats.UpdateConditions(true);
            foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
            {
                w.OnTurnStart();
            }
        }
    }
    //sorts initative order on beginning of game
    public void SortQueue()
    {
        gameStart = true;
        InitativeOrder.Clear();
        Sorter.Clear(); 
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Stack<TacticsMovement> TempStack = new Stack<TacticsMovement>();
        
        //adds all values in to sorted list in descending order
        foreach (GameObject p in players)
        {
            TacticsMovement tm = p.GetComponent<TacticsMovement>();
            tm.Init();
            float initiative = p.GetComponent<PlayerStats>().RollInitaitve() + (float)p.GetComponent<PlayerStats>().GetStatScore("A")/10f;
            if(Sorter.ContainsKey(initiative))
            {
                incrementer += 0.01f;
                initiative += incrementer;
            }
            tm.initative = initiative;
            Sorter.Add(initiative,tm);
        }

        //moved to a stack to reverse order 
        foreach (KeyValuePair<float, TacticsMovement> kvp in Sorter) {
            TempStack.Push(kvp.Value);
        }

        //finally added to Queue in correct order
        while(TempStack.Count != 0){
            InitativeOrder.Enqueue(TempStack.Pop());
        }
        if(InitativeOrder.Count > 0)
        {
            StartTurn();
        }
        PrintInitiative();
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
        ActivePlayerStats.UpdateConditions(false);
        InitativeOrder.Enqueue(InitativeOrder.Dequeue());
        ActivePlayerStats.ApplyAdvanceBonus(0);
        StartTurn();
    }

    public void EndTurn(TacticsMovement newPlayer){
        ActivePlayerStats.UpdateConditions(false);
        //InitativeOrder.Enqueue(InitativeOrder.Dequeue());
        ActivePlayerStats.ApplyAdvanceBonus(0);
        StartTurn(newPlayer);
    }

    public void PrintInitiative()
    {
        if(gameStart)
        {
            int iterations = InitativeOrder.Count;
            List<string> entries = new List<string>();
            while (iterations > 0)
            {
                TacticsMovement tm = InitativeOrder.Dequeue();
                InitativeOrder.Enqueue(tm);
                string name = tm.GetComponent<PlayerStats>().GetName();
                string value = "" + tm.initative;
                entries.Add(name + ": " + value);
                iterations--;
            }
            It.UpdateList(entries.ToArray());
        }
        
    }

    //any special effects from conditions
    public void ApplyConditions()
    {
        if(ActivePlayerStats.hasCondition("Pinned"))
        {
            halfActions--;
        }
        if(ActivePlayerStats.hasCondition("Stunned"))
        {
            PopUpText.CreateText("Stunned!", Color.yellow, ActivePlayerStats.gameObject);
            halfActions = 0;
            ActivePlayerStats.SpendAction("reaction");
        }
        if(ActivePlayerStats.IsHelpless())
        {
            PopUpText.CreateText("Helpess!", Color.yellow, ActivePlayerStats.gameObject);
            halfActions = 0;
            ActivePlayerStats.SpendAction("reaction");
        }
        if(ActivePlayerStats.hasCondition("On Fire"))
        {
            StartCoroutine(FireDistractionDelay());
            int damage = Random.Range(1,11);
            ActivePlayerStats.takeDamage(damage,"Body","E");
            ActivePlayerStats.takeFatigue(1);
            CombatLog.Log(ActivePlayerStats.GetName() + " takes 1d10 = " + damage + " damage from the fire!");
            PopUpText.CreateText("Burning! (-" + damage +")",Color.red,ActivePlayerStats.gameObject);
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
            target.SetCondition("Under Fire", 1,false);
            AttackSequence newAttack = TacticsAttack.Attack(target, attacker, ActiveWeapon, ROF);
            if(HitLocation != null)
            {
                newAttack.HitLocation = HitLocation;
            }
            AttackQueue.Enqueue(newAttack);
            currentAction = null;
            // if its not the attackers turn (overwatch) then don't subtract half actions
            if(ActivePlayerStats == attacker)
            {
                halfActions--;
            }
        }
    }

    public void RollToStun(PlayerStats target, PlayerStats attacker)
    {
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            StartCoroutine(StunDelay(target,attacker));
        }
    }

    IEnumerator StunDelay(PlayerStats target, PlayerStats attacker)
    {
            //to implement check talents to avoid penalty
            int modifiers = 0;
            if(!attacker.hasCondition("TakeDown"))
            {
                modifiers -= 20;
            }
            RollResult StunResult = attacker.AbilityCheck("WS",modifiers);
            while(!StunResult.Completed())
            {
                yield return new WaitForSeconds(0.5f);
            }
            if (StunResult.Passed())
            {
                AttackQueue.Enqueue(new AttackSequence(target,ActivePlayerStats,ActiveWeapon,"Stun",1,true));
            }
            halfActions -= 2;
            Cancel();
    }

    public void RollToKnock(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            //to implement check to see if you moved
            StartCoroutine(KnockDelay(target,attacker));
        }
    }

    IEnumerator KnockDelay(PlayerStats target, PlayerStats attacker)
    {
        RollResult OpposedStrengthTest = attacker.AbilityCheck("KnockDown",0,null,target);
        while(!OpposedStrengthTest.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        int result = OpposedStrengthTest.GetDOF();
        if (result > 1)
        {
            CombatLog.Log(attacker.GetName() + " knock down attempt wins by 2 DOF and takes the wind out of " + target.GetName());
            target.takeDamage(attacker.GetStatScore("S")-4,"Body");
            target.takeFatigue(1);
        }
        if (result > 0)
        {
            CombatLog.Log(attacker.GetName() + "Knocks " + target.GetName() + " Prone");
            target.SetCondition("Prone",0,true);
        }
        else if (result < -1)
        {
            CombatLog.Log(attacker.GetName() + " loses by 2 DOF and gets knocked down himself!");
            attacker.SetCondition("Prone",0,true);
        }
        halfActions--;
        Cancel();
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

    public void RollToFeint(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            FeintDelay(target,attacker);
        }
    }

    IEnumerator FeintDelay(PlayerStats target, PlayerStats attacker)
    {
        RollResult OpposedWSCheck = attacker.AbilityCheck("S",0,null,target);
        while(!OpposedWSCheck.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        int result = OpposedWSCheck.GetDOF();
        if (result > 0)
            {
                CombatLog.Log(attacker.GetName() + " next attack can't be avoided!");
                attacker.SetCondition("Feinted", 1, true);
            }
            else
            {
                 CombatLog.Log(attacker.GetName() + " fails to feint");
            }
            halfActions--;
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
                if(enemy.PrimaryWeapon != null && enemy.PrimaryWeapon.IsWeaponClass("Melee"))
                {
                    currentWeapon = enemy.PrimaryWeapon;
                }
                else if (enemy.SecondaryWeapon != null && enemy.SecondaryWeapon.IsWeaponClass("Melee"))
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
        float initiative = newPlayer.GetComponent<PlayerStats>().RollInitaitve() + (float)newPlayer.GetComponent<PlayerStats>().GetStatScore("A")/10f;
        TacticsMovement tm = newPlayer.GetComponent<TacticsMovement>();
        Stack<TacticsMovement> TempStack = new Stack<TacticsMovement>();
        if(Sorter.ContainsKey(initiative))
        {
            incrementer += 0.01f;
            initiative += incrementer;
        }
        Sorter.Add(initiative,tm);
        if(InitativeOrder.Count > 0)
        {
            TacticsMovement SavedPostion = InitativeOrder.Dequeue();
            InitativeOrder.Clear();
            //moved to a stack to reverse order 
            foreach (KeyValuePair<float, TacticsMovement> kvp in Sorter) {
                TempStack.Push(kvp.Value);
            }

            //finally added to Queue in correct order
            while(TempStack.Count != 0){
                InitativeOrder.Enqueue(TempStack.Pop());
            }

            while(InitativeOrder.Peek() != SavedPostion)
            {
                InitativeOrder.Enqueue(InitativeOrder.Dequeue());
            }
        }
        else
        {
            InitativeOrder.Enqueue(tm);
            StartTurn(tm);
        }
    }

    public void RemovePlayer(GameObject Player)
    {
        Stack<TacticsMovement> TempStack = new Stack<TacticsMovement>();
        TacticsMovement tm = Player.GetComponent<TacticsMovement>();
        if(InitativeOrder.Peek() == tm)
        {
            EndTurn();
        }
        Sorter.Remove(tm.initative);
        if(InitativeOrder.Count > 1)
        {
            TacticsMovement SavedPostion = InitativeOrder.Dequeue();
            InitativeOrder.Clear();
            //moved to a stack to reverse order 
            foreach (KeyValuePair<float, TacticsMovement> kvp in Sorter) {
                TempStack.Push(kvp.Value);
            }

            //finally added to Queue in correct order
            while(TempStack.Count != 0){
                InitativeOrder.Enqueue(TempStack.Pop());
            }

            while(InitativeOrder.Peek() != SavedPostion)
            {
                InitativeOrder.Enqueue(InitativeOrder.Dequeue());
            }
        }
        else
        {
            Player.GetComponent<TacticsMovement>().RemoveSelectableTiles();
            ActivePlayerStats = null;
            ActivePlayer = null;
            InitativeOrder.Clear();
        }
        PhotonNetwork.Destroy(Player);
        PrintInitiative();
    }
}