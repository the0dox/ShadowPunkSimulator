using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// This is the class that defines a character, any data that a darkhersey charactersheet can hold is in this class
public class PlayerStats : MonoBehaviourPunCallbacks
{
    // for determine who can or cannot be targeted in combat, Players are team 0, NPCS are team 1
    public int team; 
    // display name, the only name the player should see from this character
    public string playername; 
    // a key used by turnmanager to lock a character into a continous action until its complete, 
    private string repeatingAction;
    // For strict action enforcement, a player can only attempt on action per sub-type per turn
    private List<string> CompletedActions = new List<string>(); 
    // A reference to who is currently grappling this player
    public PlayerStats grappler;
    // A reference to who this player is currently grappling
    public PlayerStats grappleTarget;
    // A reference to the highlighted color that flashes when highlighted
    public Material SelectedColor;
    // A reference to the regular material the player spawns in with
    private Material DefaultColor;
    // Reference to the savedata associated with this character
    public CharacterSaveData myData;
    // Characters can store the cover bonus of cover while advancing
    private int AdvanceBonus;
    // Keys are every condition this player has, values are the duration of the conditions
    public Dictionary<ConditionTemplate, int> Conditions = new Dictionary<ConditionTemplate, int>();
    // Used for overworld only, true if a player is occupied with a job
    private bool Occupied = false;
    // Every item, weapon and piece of armor this character holds
    public List<Item> equipment = new List<Item>();
    // Reference to the weapon in the player secondary hand
    public Weapon SecondaryWeapon;
    // Reference to the weapon in the player primary hand
    public Weapon PrimaryWeapon; 
    private RollResult currentRoll;
    // used for multiplayer quick referencing
    public int ID;
    private PhotonView pv;
    public int NPCHealth;
    private int moveMax;

    [SerializeField] private HealthBar HealthBar;
    [SerializeField] private FatigueBar FatigueBar;

    // given a charactersavedata copies all the values onto the playerstats
    public void DownloadSaveData(CharacterSaveData myData, int id)
    {   
        this.myData = myData;
        this.playername = myData.playername;
        this.team = myData.team;    
        if(team != 0)
        {
            NPCHealth = 0;
        }
        DefaultColor = GetComponentInChildren<MeshRenderer>().material;
        Init(id);
    }

    // Calculates bonuses from armor and stats
    public void Init(int id)
    {
        pv = GetComponent<PhotonView>();
        SetID(id);
        if(pv.IsMine)
        {
            pv.RPC("RPC_Init",RpcTarget.OthersBuffered, myData.team,myData.Model, id);
        }
        OnDownload();
    }

    [PunRPC]
    void RPC_Init(int team, string model, int ID)
    {
        SetID(ID);
        this.team = team;
        pv = GetComponent<PhotonView>();
        if(team == 0)
        {
            GetComponentInChildren<MeshRenderer>().material = PlayerSpawner.SPlayerMaterial;
        }
        else
        {
            GetComponentInChildren<MeshRenderer>().material = PlayerSpawner.SNPCMaterial;   
        }
        PlayerSpawner.ClientUpdateIDs(this);
        GetComponentInChildren<MeshFilter>().mesh = PlayerSpawner.GetPlayers()[model];
        DefaultColor = GetComponentInChildren<MeshRenderer>().material;
    }    

    public void OnDownload()
    {
        pv.RPC("RPC_SetHP", RpcTarget.All, getWounds(), myData.GetAttribute(AttributeKey.PhysicalHealth));  
        pv.RPC("RPC_SetSP", RpcTarget.All, myData.GetAttribute(AttributeKey.SDamage), myData.GetAttribute(AttributeKey.StunHealth));   
        pv.RPC("RPC_SetMove", RpcTarget.All, myData.GetAttribute(AttributeKey.Agility));
    }

    [PunRPC]
    void RPC_SetMove(int agility)
    {
        moveMax = agility * 2;
        Debug.Log(moveMax);
    }

    [PunRPC]
    void RPC_SetHP(int currentHP, int MaxHP)
    {
        HealthBar.UpdateHealth(currentHP, MaxHP);
    }

    [PunRPC]
    void RPC_SetSP(int currentFatigue, int maxFatigue)
    {
        FatigueBar.UpdateFatigue(currentFatigue, maxFatigue);
    }

    public int getWounds()
    {
        int currentHP; 
        if(team != 0)
        {
            currentHP = NPCHealth;
        }
        else
        {
            currentHP = myData.GetAttribute(AttributeKey.PDamage);
        }
        return currentHP;
    }

    public int GetTeam()
    {
        return team; 
    }

    public string GetName()
    {
        return playername;
    }

    /*  damage: how much incoming damage already altered 
        location: reveresed die roll for hit location
        takes unaltered damage and applies any damage reduction and subtracts from health
    */
    public void takeDamage(int damage, string location)
    {
        takeDamage(damage,location, "I");
    }

    public void takeDamage(int damage, string location,string damageType)
    {
        int currentHealth = getWounds();
        myData.SetAttribute(AttributeKey.SDamage, currentHealth + damage,false);
        pv.RPC("RPC_SetHP", RpcTarget.AllBuffered, getWounds(), myData.GetAttribute(AttributeKey.PhysicalHealth));  
    }

    public void takeFatigue(int levels)
    {
        int currentStun = myData.GetAttribute(AttributeKey.SDamage);
        myData.SetAttribute(AttributeKey.SDamage, currentStun + levels,false);
        pv.RPC("RPC_SetHP", RpcTarget.AllBuffered, getWounds(), myData.GetAttribute(AttributeKey.PhysicalHealth));  
    }

    public int GetStat(string key)
    {
        return myData.GetAttribute(key);
    }

    public int GetMovement(string Key)
    {
        return moveMax;
        /*
        if(IsHelpless() || Grappling() || hasCondition("Stunned"))
        {
            return 0;
        }
        if(hasCondition("Prone"))
        {
            return moveMax/2;
        }
        */
    }

    public void Unequip(Weapon w)
    {
        if(SecondaryWeapon == w)
        {
            SecondaryWeapon = null;
        }
        if(PrimaryWeapon == w)
        {
            PrimaryWeapon = null;
        }
    }

    public void EquipPrimary(Weapon w)
    {
        PrimaryWeapon = w;
    }
    public void EquipSecondary(Weapon w)
    {
        SecondaryWeapon = w;
    }

    public bool IsDualWielding()
    {
        return(PrimaryWeapon != null && SecondaryWeapon != null);
    }

    public bool OffHandPenalty(Weapon w)
    {
        return (w.Equals(SecondaryWeapon)); //temporary fix as the game can't track duplicates;
    }
    public RollResult AbilityCheck(string input,int modifiers)
    {
        return AbilityCheck(input,modifiers, null);
    }
    public RollResult AbilityCheck(string input, int modifiers,string command)
    {
        return AbilityCheck(input,modifiers,command,null);
    }
    //attempts a skill OR Characteristic! from the skill dictionary, applying any modifiers if necessary, returns degrees of successes, not true/false
    public RollResult AbilityCheck(string input, int modifiers,string command, PlayerStats other)
    {
        int SkillTarget = 10;
        int ConditionalModifiers = modifiers;
        //Debug.Log("attempting " + input);
        //Skill convertedSkill = GetSkillReference(input);
        /*for skills not characteristics
        if (convertedSkill != null)
        {
            //modifier for skills
            ConditionalModifiers += CalculateStatModifiers(input);
            type = convertedSkill.characterisitc;
            int LevelsTrained = convertedSkill.levels;
            SkillTarget = GetStat(type);
            if (LevelsTrained < 1)
                {
                    //to implement, custom basic skills    
                    SkillTarget = SkillTarget/2;
                }
                else
                {
                    SkillTarget += 10 * (LevelsTrained-1);
                } 
        }
        //for characteristic tests only
        else
        {
            type = input; 
            SkillTarget = GetStat(type);
        }
        //modifier that always applies 
        ConditionalModifiers += CalculateStatModifiers(type);
        
        //untrained makes the skill half as likely to work

        SkillTarget += ConditionalModifiers;  
        
        */
        RollResult output;
        output = new RollResult(this,SkillTarget,input,command);
        if(other != null)
        {
            output.OpposedRoll(other.AbilityCheck(input,modifiers,command));
        }
        return output;
    }
    
    public float RollInitaitve()
    {
        return GetStat("A")/10f + Random.Range(1,11);
    }

    public string HealthToString()
    {
        return GetStat("Wounds") + "/" + GetStat("MaxWounds");
    }
    public bool ValidAction(string actionName)
    {
      return (!CompletedActions.Contains(actionName));
    }
    public void SpendAction(string actionName)
    {
      CompletedActions.Add(actionName);
    }

    public void ResetActions()
    {
        CompletedActions.Clear();
    }

    public bool CanParry()
    {
        return (SecondaryWeapon != null && SecondaryWeapon.CanParry() || (PrimaryWeapon != null && PrimaryWeapon.CanParry())); 
    }

    public int ParryBonus()
    {
        int primaryBonus = -99;
        if(PrimaryWeapon != null && PrimaryWeapon.CanParry())
        {
            primaryBonus = 0;
            if (PrimaryWeapon.HasWeaponAttribute("Defensive"))
            {
                primaryBonus += 15;
            }
            if(PrimaryWeapon.HasWeaponAttribute("Balanced"))
            {
                primaryBonus += 10;
            }
            if(PrimaryWeapon.HasWeaponAttribute("Unbalanced"))
            {
                primaryBonus -= 10;
            }
        }
        
        int secondaryBonus = -99;
        if(SecondaryWeapon != null && SecondaryWeapon.CanParry())
        {
            secondaryBonus = 0;
            if (SecondaryWeapon != null && SecondaryWeapon.HasWeaponAttribute("Defensive"))
            {
                secondaryBonus += 15;
            }
            if(SecondaryWeapon != null && SecondaryWeapon.HasWeaponAttribute("Balanced"))
            {
                secondaryBonus += 10;
            }
            if(SecondaryWeapon.HasWeaponAttribute("Unbalanced"))
            {
                secondaryBonus -= 10;
            }
        }
        if((primaryBonus > secondaryBonus))
        {
            return primaryBonus;
        }
        else
        {
            return secondaryBonus;
        }
    }

    public bool PowerFieldAbility()
    {
        if((PrimaryWeapon == null || !PrimaryWeapon.HasWeaponAttribute("Power Field")) && (SecondaryWeapon == null || !SecondaryWeapon.HasWeaponAttribute("Power Field")))
        {
            return false;
        }
        else
        {
            if (Random.Range(1,101) >= 25)
            {
                CombatLog.Log(GetName() + " 's power field shatters the attackers weapon!");
                return true;
            }
            return false;  
        }
    }
    
    //sets string as a reference for tactics action to repeat untill ClearRepeatingAction() is called
    public void SetRepeatingAction(string input)
    {
        repeatingAction = input;
    }

    public void CompleteRepeatingAction()
    {
        repeatingAction = null;
    }

    public string GetRepeatingAction()
    {
        return repeatingAction;
    }

    public void SetCondition(string name, int duration, bool visible)
    {
        ConditionTemplate c = ConditionsReference.Condition(name);
        if (visible)
        {
            PopUpText.CreateText(c.name + "!", Color.yellow, gameObject);
        }
        //if the condition already exists, set its duration to the new duration
        if(hasCondition(c.name))
        {
            Conditions[c] = duration;
        }
        //else add it like normal
        else
        {
            Debug.Log("Added" + c.name + "Condition!");
            Conditions.Add(c,duration);
        }
    }
    public bool hasCondition(string Name)
    {
        ConditionTemplate c = ConditionsReference.Condition(Name);
        return Conditions.ContainsKey(c);
    }
    public void RemoveCondition(string Name)
    {
        ConditionTemplate c = ConditionsReference.Condition(Name);
        Conditions.Remove(c);
    }

    public int CalculateStatModifiers(string ability)
    {
        int total = 0;
        foreach(ConditionTemplate key in Conditions.Keys)
        {
            total += key.GetModifier(ability);
        }
        return total;
    }

    public Stack<string> DisplayStatModifiers(string ability)
    {
        Stack<string> outputStack = new Stack<string>();
        foreach(ConditionTemplate key in Conditions.Keys)
        {
            int value = key.GetModifier(ability);
            if(value != 0)
            {
                string header = " ";
                if(value > 0)
                {
                    header = " +";
                }
                outputStack.Push(header + value + "%: " + key.name);
            }
        }
        return outputStack;
    }

    //given a hitlocation and if the damage source is primitive, returns if the player has any armor covering that point 
    public int GetAP(string HitLocation, Weapon w)
    {
        int bestAP = 0;
        foreach(Item i in equipment)
        {
            //only check armor
            if(i.GetType() == typeof(Armor))
            {
                Armor a = (Armor)i;
                int currentAP = a.GetAP(HitLocation, w);
                if(currentAP > bestAP)
                {
                    bestAP = currentAP;
                }
            }
        }
        return bestAP;
    }

    //calls at the beginning And end of turn each interger value refers to beginning and ending of turns 
    public void UpdateConditions(bool startTurn)
    {
        List<ConditionTemplate> removedKeys = new List<ConditionTemplate>();
        List<ConditionTemplate> IncrementKeys = new List<ConditionTemplate>();
        foreach (ConditionTemplate Key in Conditions.Keys)
        {
            //decay conditions at the end of turns, conditions with 0 as their length do not decay
            if(Conditions[Key] > 1 && !startTurn)
            {
                IncrementKeys.Add(Key); 
            }
            else if(Conditions[Key] == 1 && !startTurn)
            {
                removedKeys.Add(Key);
            }
            else if(startTurn && Key.clearOnStart)
            {
                removedKeys.Add(Key);
            }
            //Conditional update on turn starts here
            else if(Key.isCondition("Pinned") && !startTurn)
            {
                if(GetComponent<TacticsMovement>().GetAdjacentEnemies(GetTeam()) > 0)
                {
                    CombatLog.Log(GetName() + " is in melee, and thus loses the pinned condition");
                    PopUpText.CreateText("Unpinned!",Color.green, gameObject);
                    //removes at the end
                    removedKeys.Add(Key);
                }
                else
                {
                    int modifiers = 0;
                    if(!hasCondition("Under Fire"))
                    {  
                        modifiers += 30;
                    }
                    else
                    {
                        removedKeys.Add(ConditionsReference.Condition("Under Fire"));
                    }
                    AbilityCheck("WP",modifiers,"Suppression");
                }   
            }
            
        }
        foreach(ConditionTemplate Key in IncrementKeys)
        {
            Conditions[Key]--;
        }
        foreach(ConditionTemplate Key in removedKeys)
        {
            Debug.Log(Key.name + " condition removed");
            Conditions.Remove(Key);
        }
    }

    public void PaintTarget(bool painted)
    {
        if(painted)
        {
            GetComponentInChildren<MeshRenderer>().material = SelectedColor;
        }
        else
        {   
            GetComponentInChildren<MeshRenderer>().material = DefaultColor;
        }
    }

    public void SetGrappler(PlayerStats attacker)
    {
        grappleTarget = null;
        grappler = attacker;
        attacker.grappleTarget = this;
        SetCondition("Grappled",0,true);
        CombatLog.Log(GetName() + " is grappled by " + grappler.GetName());
        SpendAction("reaction");
        attacker.SpendAction("reaction");
    }

    public void ControlGrapple()
    {
        RemoveCondition("Grappled");
        grappler.SetGrappler(this);
        grappler = null;
    }

    public void ReleaseGrapple()
    {
        CombatLog.Log(GetName() + ": is no longer grappling" + grappleTarget.GetName());
        PopUpText.CreateText("Released!", Color.yellow, grappleTarget.gameObject);
        grappleTarget.grappler = null;
        grappleTarget.RemoveCondition("Grappled");
        grappleTarget = null;
    }

    public bool Grappling()
    {
        return (grappleTarget != null || grappler != null);
    }

    public void ApplyAdvanceBonus(int bonus)
    {
        AdvanceBonus = bonus;
    }

    public int GetAdvanceBonus(string location)
    {
        if (location != "Body" && location != "Left Leg" && location != "Right Leg")
        {
            return 0;
        }
        return AdvanceBonus;
    }

    public List<Weapon> GetWeaponsForEquipment()
    {
        List<Weapon> output = new List<Weapon>();
        foreach(Item i in equipment)
        {
            if(i.GetType() == typeof(Weapon))
            {
                output.Add((Weapon)i);
            }
        }
        return output;
    }

    public bool IsHelpless()
    {
        if(hasCondition("Immobilised") || hasCondition("Unconscious"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsOccupied()
    {
        return Occupied;
    }
    public void EndJob()
    {
        Occupied = false;
    }
    public override string ToString()
    {
        string output = GetName();
        return output;
    }

    public void RollComplete(RollResult myRoll)
    {
        string methodName = myRoll.GetCommand();
        if(methodName != null)
        {
            currentRoll = myRoll;
            Invoke(methodName,0);
        }
    }

    public void ApplySpecialEffects(string hitBodyPart, Weapon w, int result)
    {
        if(result > 0 && w.HasWeaponAttribute("Shocking"))
        {
            int shockModifier = GetStat(hitBodyPart) * 10;
            StartCoroutine(ShockEffect(shockModifier,result));            
        }
        if(result > 0 && w.HasWeaponAttribute("Toxic"))
        {
            int toxicModifier = result * -5;
            StartCoroutine(ToxicEffect(toxicModifier, result));
        }
        if(w.HasWeaponAttribute("Snare"))
        {
            AbilityCheck("A",0,"Snare");
        }
        if(w.HasWeaponAttribute("Smoke"))
        {
            CombatLog.Log("Target is covered by smoke!");
            SetCondition("Obscured",3,true);
        }
    }

    IEnumerator ShockEffect(int modifier, int damage)
    {
        RollResult ShockResistRoll = AbilityCheck("T",modifier);
        while(!ShockResistRoll.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        if(!ShockResistRoll.Passed())
        {
            int rounds = damage;
            if(rounds / 2 > 0)
            {
                rounds = rounds/2;
            }
            CombatLog.Log("The shocking attribute of the weapon" + GetName() + " for " + rounds + " rounds");
            SetCondition("Stunned",rounds,true);
        }
        else
        {
            CombatLog.Log(GetName() + " resists the shocking attribute of the weapon");
            PopUpText.CreateText("Resisted!", Color.yellow, gameObject);
        }
    }

    IEnumerator ToxicEffect(int modifier, int damage)
    {
        RollResult ToxicResistRoll = AbilityCheck("T",modifier);
        while(!ToxicResistRoll.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        if(!ToxicResistRoll.Passed())
        {
            int toxicdamage = Random.Range(1,11);
            takeDamage(toxicdamage,"Body");
            CombatLog.Log("The toxic attribute of the weapon deals an additional 1d10 = <" + damage + "> ignoring soak");
            PopUpText.CreateText("Posioned!", Color.red, gameObject);
        }
        else
        {
            CombatLog.Log(GetName() + " resists the toxic attribute of the weapon");
            PopUpText.CreateText("Resisted!", Color.yellow, gameObject);
        }
    }

    public void Snare()
    {
        if(!currentRoll.Passed())
        {
            CombatLog.Log("The snare attribute of the weapon immobilises " + GetName());
            SetCondition("Immobilised",0,true);
            SetCondition("Prone",0,true);
            SpendAction("Reaction");
        }
        else
        {
            CombatLog.Log(GetName() + " avoids the snare attribute of the weapon");
        }
        currentRoll = null;
    }

    public void Suppression()
    {
        bool suppressed = hasCondition("Pinned");
        if(currentRoll.Passed())
        {
            if(suppressed)
            {
                CombatLog.Log(GetName() + " passes their check and is no longer pinned!");
                RemoveCondition("Pinned");
            }
            else
            {
                CombatLog.Log(GetName() + " passes their check and can act freely!");
            }
        }
        else if(!suppressed)
        {
            SetCondition("Pinned",0,true);
            CombatLog.Log(GetName() + " fails their check and is pinned!");
        }
        else
        {
            CombatLog.Log(GetName() + " is unable to steel their nerves!");
        }
        currentRoll = null;
    }

    public void Fire()
    {
        bool ablaze = hasCondition("On Fire");
        if(currentRoll.Passed())
        {
            if(ablaze)
            {
                PopUpText.CreateText("Extinguished!",Color.green,gameObject);
                CombatLog.Log(GetName() + " passes their check puts out the fire!");
                RemoveCondition("On Fire");
            }
        }
        else if(!ablaze)
        {
            SetCondition("On Fire",0,true);
            CombatLog.Log(GetName() + " fails their check and is set ablaze!");
        }
        else
        {
            CombatLog.Log(GetName() + " is unable to put the fire out!");
        }
        currentRoll = null;
    }

    public void EscapeBonds()
    {
        if(currentRoll.Passed())
        {
            CombatLog.Log(GetName() + " escapes the grapple!");
            grappler.ReleaseGrapple();
        }
        else
        {
            CombatLog.Log(GetName() + " is unable to escape the grapple.");
        }
        currentRoll = null;
    }

    public void SetID(int id)
    {
        ID = id;
    }

    public int GetID()
    {
        return ID;
    }

    public void OverworldInit()
    {
        pv.RPC("RPC_Overworld_Init",RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_Overworld_Init()
    {
        gameObject.AddComponent<OverworldMovement>();
        //HealthBar.gameObject.SetActive(false);
        //FatigueBar.gameObject.SetActive(false);
    }
}
