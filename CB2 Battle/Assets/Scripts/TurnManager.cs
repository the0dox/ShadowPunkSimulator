using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnManager : TurnActions
{
    //static means easily accessable reference for all moveable characters
    static Dictionary<TacticsMovement, float> units = new Dictionary<TacticsMovement, float>();
    static Queue<string> turnKey = new Queue<string>();
    private static Queue<TacticsMovement> InitativeOrder = new Queue<TacticsMovement>();
    public GameObject CharacterSheet;
    public GameObject SkillModifierScript;
    public InitativeTrackerScript It;
    float incrementer = 0;
    SortedList<float, TacticsMovement> Sorter = new SortedList<float, TacticsMovement>();  
    // Start is called before the first frame update
    void Start()
    {   
        SortQueue();
        StartTurn();
    }

    void Update ()
    {
        if(CurrentAttack == null && AttackQueue.Count > 0)
        {
            CurrentAttack = AttackQueue.Dequeue();
            TryReaction();
        }
        //Where all possible actions take place
        else if (CurrentAttack == null && !CameraButtons.UIActive()) 
        {
            if (halfActions < 0)
            {
                halfActions = 0;
            }
            CheckMouse();
            switch(currentAction)
            {
                case "ThreatRange":
                    ActivePlayer.GetValidAttackTargets(ActiveWeapon);
                break;
                case "Move":
                    if(!ActivePlayerStats.ValidAction("Charge"))
                    {
                        ActivePlayer.moving = true;
                        currentAction = "Charge";
                    }
                    if(ActivePlayerStats.grappling())
                    {
                        currentAction = "Grapple";
                    }
                    if (!ActivePlayer.moving)
                    {
                        if(!ActivePlayerStats.hasCondition("Prone"))
                        {  
                            if(halfActions > 1)
                            {
                                ActivePlayer.FindSelectableTiles(ActivePlayerStats.GetStat("MoveHalf"),ActivePlayerStats.GetStat("MoveFull"),ActivePlayerStats.GetTeam());     
                            }
                            else if( halfActions > 0)
                            {
                                ActivePlayer.FindSelectableTiles(0,ActivePlayerStats.GetStat("MoveHalf"),ActivePlayerStats.GetTeam());     
                            } 
                            else 
                            {
                                ActivePlayer.FindSelectableTiles(0,0,ActivePlayerStats.GetTeam());     
                            }
                        }
                        else
                        {
                            ActivePlayer.FindSelectableTiles(0,0,ActivePlayerStats.GetTeam());
                        }
                    }
                    else 
                    {
                        ActivePlayer.RemoveSelectableTiles();
                        ActivePlayer.Move();
                    }
                break;
                case "Attack":
                    ActivePlayer.GetValidAttackTargets(ActiveWeapon);
                break;
                case "CM":
                    ActivePlayer.GetValidAttackTargets(ActiveWeapon);
                break;
                case "Grapple":
                    ActivePlayer.GetGrapplePartner(ActivePlayerStats);
                break;
                case "Reacting":
                    ActivePlayer.RemoveSelectableTiles();
                    if(target != null)
                    {
                        target.GetComponent<TacticsMovement>().FindSelectableTiles(0,0,ActivePlayerStats.GetTeam());
                    }
                break;
                case "Charge":
                    if (!ActivePlayer.moving)
                    {
                        //reached destination 
                        if(!ActivePlayerStats.ValidAction("Charge"))
                        {
                            ClearActions();
                            ActivePlayer.FindSelectableTiles(0,0,ActivePlayerStats.GetTeam());
                            Combat();
                        }
                        else
                        {
                            ActivePlayer.FindChargableTiles(ActivePlayerStats.GetStat("MoveCharge"),ActivePlayerStats.GetTeam());     
                        }
                    }
                    else 
                    {
                        ActivePlayer.RemoveSelectableTiles();
                        ActivePlayer.Move();
                    }
                break;
                case "Run":
                    if (!ActivePlayer.moving)
                    {
                        if(halfActions > 1)
                        {
                            ActivePlayer.FindSelectableTiles(ActivePlayerStats.GetStat("MoveRun"), ActivePlayerStats.GetTeam());     
                        }
                        else
                        {
                            Cancel();
                        }
                    }
                    else 
                    {
                        ActivePlayer.RemoveSelectableTiles();
                        ActivePlayer.Move();
                    }
                break;
                case "Disengage":
                    if (!ActivePlayer.moving)
                    {
                            ActivePlayer.FindSelectableTiles(ActivePlayerStats.GetStat("MoveHalf"), ActivePlayerStats.GetTeam());         
                    }
                    else 
                    {
                        ActivePlayer.Move();
                    }
                break;
                case "Advance":
                    if (!ActivePlayer.moving)
                    {
                        //to implmenent, enforced advance zones
                            ActivePlayer.FindSelectableTiles(ActivePlayerStats.GetStat("MoveFull"), ActivePlayerStats.GetTeam());         
                    }
                    else 
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
                            StartTurn(hit.collider.GetComponent<TacticsMovement>());
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

                switch(currentAction)
                {
                case "Move":
                    if (hit.collider.tag == "Tile" && !ActivePlayer.moving && currentAction != null)
                    {
                        Tile t = hit.collider.GetComponent<Tile>();

                        //player can reach 
                        if (t.selectable)
                        {
                            //make tile green
                            ActivePlayer.moveToTile(t);
                            halfActions--;
                            ActivePlayerStats.SetCondition("KnockDownBonus",1,false);
                            AttackOfOppertunity();

                        } 
                        else if (t.selectableRunning)
                        {
                            //make tile green
                            ActivePlayer.moveToTile(t);
                            halfActions = 0;
                            AttackOfOppertunity();
                        }
                        Cancel();
                    }
                    break;
                case "Run":
                    if (hit.collider.tag == "Tile" && !ActivePlayer.moving)
                    {
                        Tile t = hit.collider.GetComponent<Tile>();
                        //player can reach 
                        ActivePlayerStats.SetCondition("Running",0,true);
                        if (t.selectableRunning)
                        {
                            //make tile green
                            ActivePlayer.moveToTile(t);
                            AttackOfOppertunity();
                            halfActions-=2;
                        }
                    }
                    break;
                case "Disengage":
                    if (hit.collider.tag == "Tile" && !ActivePlayer.moving)
                    {
                        Tile t = hit.collider.GetComponent<Tile>();
                        //player can reach 
                        if (t.selectableRunning)
                        {
                            //make tile green
                            ActivePlayer.moveToTile(t);
                            halfActions-=2;
                        }
                    }
                    break;
                case "Advance":
                    if (hit.collider.tag == "Tile" && !ActivePlayer.moving)
                    {
                        Tile t = hit.collider.GetComponent<Tile>();
                        //player can reach 
                        if (t.selectableRunning)
                        {
                            //make tile green
                            ActivePlayer.moveToTile(t);
                            ActivePlayerStats.ApplyAdvanceBonus(TacticsAttack.SaveCoverBonus(ActivePlayerStats)); 
                            halfActions-=2;
                        }
                }
                break;
                case "Charge":
                    if (hit.collider.tag == "Tile" && !ActivePlayer.moving)
                    {
                        Tile t = hit.collider.GetComponent<Tile>();
                        //player can reach 
                        if (t.selectableRunning)
                        {
                            //make tile green
                            ActivePlayer.moveToTile(t);
                            AttackOfOppertunity();
                            ActivePlayerStats.SetCondition("Charging",1,true);
                            ActivePlayerStats.SpendAction("Charge");
                        }
                    }
                    break;
                case "Attack":
                    if (hit.collider.tag == "Player")
                    {
                        RollToHit(hit.collider.GetComponent<PlayerStats>(), FireRate, ActiveWeapon, ActivePlayerStats);
                    }
                    break;
                case "CM":
                    if (hit.collider.tag == "Player")
                    {
                        if(FireRate.Equals("Stun"))
                        {
                            RollToStun(hit.collider.GetComponent<PlayerStats>(), ActivePlayerStats);
                        }
                        else if(FireRate.Equals("Grapple"))
                        {
                            RollToGrapple(hit.collider.GetComponent<PlayerStats>(), ActivePlayerStats);
                        }
                        else if(FireRate.Equals("Knock"))
                        {
                            RollToKnock(hit.collider.GetComponent<PlayerStats>(), ActivePlayerStats);
                        }
                        else if(FireRate.Equals("Feint"))
                        {
                            RollToFeint(hit.collider.GetComponent<PlayerStats>(), ActivePlayerStats);
                        }
                    }
                break;
                default:
                    break;
                }
            }
        }
    }

    public void StartTurn()
    {
        if (InitativeOrder.Count != 0) 
        {
            ActivePlayer = InitativeOrder.Peek();
            ActivePlayerStats = ActivePlayer.GetComponent<PlayerStats>();
            CombatLog.Log("player " + ActivePlayerStats.GetName() + " is starting their turn");
            halfActions = 2;
            ActivePlayerStats.ResetActions();
            ActivePlayer.ResetMove();
            ApplyConditions();
            Cancel();
            GrappleStart();
            PrintInitiative();
            RemoveRange(ActivePlayerStats);
            ActivePlayerStats.UpdateConditions(true);
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
            CombatLog.Log("player " + ActivePlayerStats.GetName() + " is starting their turn");
            halfActions = 2;
            ActivePlayerStats.ResetActions();
            ActivePlayer.ResetMove();
            ApplyConditions();
            Cancel();
            GrappleStart();
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
            Sorter.Add(initiative,tm);
            units.Add(tm, initiative);
        }

        //moved to a stack to reverse order 
        foreach (KeyValuePair<float, TacticsMovement> kvp in Sorter) {
            TempStack.Push(kvp.Value);
        }

        //finally added to Queue in correct order
        while(TempStack.Count != 0){
            InitativeOrder.Enqueue(TempStack.Pop());
        }
    }

    public GameObject GetActivePlayer()
    {
        return ActivePlayer.gameObject; 
    }

    //Button to bring up player sheet
    public void DisplayCharacterSheet()
    {
        if(ActivePlayerStats != null && !CameraButtons.UIActive())
        {
            GameObject newSheet = Instantiate(CharacterSheet) as GameObject;
            newSheet.GetComponent<CharacterSheet>().UpdateStatsIn(ActivePlayerStats);
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
        int iterations = InitativeOrder.Count;
        List<string> entries = new List<string>();
        while (iterations > 0)
        {
            TacticsMovement tm = InitativeOrder.Dequeue();
            InitativeOrder.Enqueue(tm);
            string name = tm.GetComponent<PlayerStats>().GetName();
            string initiative = "" + units[tm];
            entries.Add(name + ": " + initiative);
            iterations--;
        }
        It.UpdateList(entries);
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
            halfActions -= 2;
            ActivePlayerStats.SpendAction("reaction");
        }
        if(ActivePlayerStats.hasCondition("Unconscious"))
        {
            PopUpText.CreateText("Unconscious!", Color.yellow, ActivePlayerStats.gameObject);
            halfActions -= 2;
            ActivePlayerStats.SpendAction("reaction");
        }
        if(ActivePlayerStats.hasCondition("On Fire"))
        {
            if(!ActivePlayerStats.AbilityCheck("WP",0).Passed())
            {
                CombatLog.Log(ActivePlayerStats.GetName() + " is too distracted by the fire to act!");
                halfActions -= 2;
            }
            int damage = Random.Range(1,10);
            ActivePlayerStats.takeDamage(damage,"Body","E");
            ActivePlayerStats.takeFatigue(1);
            CombatLog.Log(ActivePlayerStats.GetName() + " takes 1d10 = " + damage + " damage from the fire!");
            PopUpText.CreateText("Burning! (-" + damage +")",Color.red,ActivePlayerStats.gameObject);
        }
    }
    
    public void RollToHit(PlayerStats target, string ROF, Weapon w, PlayerStats attacker)
    {
        FireRate = ROF;
        this.target = target; 
        ActiveWeapon = w;
        target.SetCondition("Under Fire", 1,false);
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            AttackSequence newAttack = TacticsAttack.Attack(target, attacker, ActiveWeapon, ROF);
            if(HitLocation != null)
            {
                newAttack.HitLocation = HitLocation;
            }
            AttackQueue.Enqueue(newAttack);
            currentAction = null;
            if(!ROF.Equals("Free"))
            {
                halfActions--;
            
                if(!ROF.Equals("S"))
                {
                    halfActions--;
                }
            }
        }
    }

    public void RollToStun(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            //to implement check talents to avoid penalty
            int modifiers = -20;
            RollResult StunResult = attacker.AbilityCheck("WS",modifiers);
            if (StunResult.Passed())
            {
                AttackQueue.Enqueue(new AttackSequence(target,ActivePlayerStats,ActiveWeapon,"Stun",1));
            }
            halfActions -= 2;
        }
    }

    public void RollToKnock(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            int modifier = 0;
            if(attacker.hasCondition("KnockDownBonus"))
            {
                modifier += 10;
            }
            //to implement check to see if you moved
            int result = attacker.OpposedAbilityCheck("S",target,modifier,0);
            if (result > 1)
            {
                CombatLog.Log(attacker.GetName() + " wins by 2 DOF and takes the wind out of " + target.GetName());
                target.takeDamage(attacker.GetStatScore("S")-4,"Body");
                target.takeFatigue(1);
            }
            if (result > 0)
            {
                CombatLog.Log(attacker.GetName() + "Knocks " + target.GetName() + "Prone");
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
    }

    public void RollToGrapple(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            if(!target.grappling()){
                RollResult GrappleResult = attacker.AbilityCheck("WS",0);
                if (GrappleResult.Passed())
                {
                    AttackQueue.Enqueue(new AttackSequence(target,ActivePlayerStats,ActiveWeapon,"Grapple",1));
                }
                halfActions -= 2;
            }
            else
            {
                CombatLog.Log("Target is already grappling!");
            }
        }
    }

    public void RollToFeint(PlayerStats target, PlayerStats attacker)
    {
        this.target = target; 
        //only a valid target if on diferent teams
        if (TacticsAttack.HasValidTarget(target, attacker, ActiveWeapon))
        {
            //to implement check to see if you moved
            int result = attacker.OpposedAbilityCheck("WS",target,0,0);
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
    }

    public void GrappleStart()
    {
        if(ActivePlayerStats.grappling())
        {
            PopUpText.CreateText("Grappling!", Color.red, ActivePlayerStats.gameObject);
            ActivePlayerStats.SpendAction("reaction");
            currentAction = "Grapple";
        }
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

    public List<string> GetTooltip(RaycastHit hit)
    {
        List<string> output = new List<string>();
        if(hit.collider == null || hit.collider.tag != "Player")
        {
            return output;
        }
        PlayerStats selectedPlayer = hit.collider.GetComponent<PlayerStats>();
        if(currentAction != null && currentAction.Equals("Attack"))
        {
            return TacticsAttack.GenerateTooltip(selectedPlayer,ActivePlayerStats,ActiveWeapon,FireRate);
        }
        else
        {
            output.Add(selectedPlayer.ToString());
        }
        return output;
    }
    public void AbilityCheck(string skill)
    {
        GameObject prompt = Instantiate(SkillModifierScript) as GameObject;
        prompt.GetComponent<SkillPromptBehavior>().SetValue(skill);
        prompt.transform.SetParent(Canvas.transform, false);
        currentAction = null;
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
        units.Add(tm, initiative);
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
}