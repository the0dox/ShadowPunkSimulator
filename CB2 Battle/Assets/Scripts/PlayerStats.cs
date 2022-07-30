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
    // Keys are every condition this player has, values are the duration of the conditions
    public Dictionary<ConditionTemplate, int> Conditions = new Dictionary<ConditionTemplate, int>();
    // Used for overworld only, true if a player is occupied with a job
    private bool Occupied = false;
    // Every item, weapon and piece of armor this character holds
    //public List<Item> equipment = new List<Item>();
    // Reference to the weapon in the player secondary hand
    public Weapon SecondaryWeapon;
    // Reference to the weapon in the player primary hand
    public Weapon PrimaryWeapon; 
    // used for multiplayer quick referencing
    public int ID;
    private PhotonView pv;
    public int NPCHealth;
    public int NPCStun;
    public int remainingMove;
    private int defensePenality = 0;
    private int TotalBulletsFired;
    private int actions;
    
    [SerializeField] private HealthBar HealthBar;
    [SerializeField] private FatigueBar FatigueBar;
    [SerializeField] private List<GameObject> specialEffects;

    // given a charactersavedata copies all the values onto the playerstats
    public void DownloadSaveData(CharacterSaveData myData, int ID, int ownerID)
    {   
        this.myData = myData;
        this.playername = myData.playername;
        this.team = myData.team;    
        DefaultColor = GetComponentInChildren<MeshRenderer>().material;
        pv = GetComponent<PhotonView>();
        SetID(ID);
        if(pv.IsMine)
        {
            pv.RPC("RPC_Init",RpcTarget.Others, myData.playername, myData.team, myData.Model, ID, ownerID);
        }
        GetComponent<TooltipTrigger>().header = myData.playername;
        OnDownload();
    }

    [PunRPC]
    void RPC_Init(string playername, int team, string model, int ID, int ownerID)
    {
        NPCHealth = 0;
        NPCStun = 0;
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
        if(PhotonNetwork.LocalPlayer != PhotonNetwork.CurrentRoom.GetPlayer(ownerID))
        {
            GetComponent<TacticsMovement>().draggable = false;
        }
        PlayerSpawner.ClientUpdateIDs(this);
        GetComponentInChildren<MeshFilter>().mesh = PlayerSpawner.GetPlayers()[model];
        DefaultColor = GetComponentInChildren<MeshRenderer>().material;
        GetComponent<TooltipTrigger>().header = playername;
    }    

    public void OnDownload()
    {
        pv.RPC("RPC_SetHP", RpcTarget.All, getWounds(), myData.GetAttribute(AttributeKey.PhysicalHealth));  
        pv.RPC("RPC_SetSP", RpcTarget.All, getStun(), myData.GetAttribute(AttributeKey.StunHealth));   
        ResetMovement();
    }

    public void ResetMovement()
    {
        pv.RPC("RPC_SetMove", RpcTarget.All, myData.GetAttribute(AttributeKey.MoveWalk));
    }

    public void DisableHP()
    {
        HealthBar.enabled = false;
        FatigueBar.enabled = false;
    }

    [PunRPC]
    void RPC_SetMove(int remainingMove)
    {
        this.remainingMove = remainingMove;
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
        if(team != 0)
        {
            return NPCHealth;
        }
        else
        {
            return myData.GetAttribute(AttributeKey.PDamage);
        }
    }

    public int getStun()
    {
        if(team != 0)
        {
            return NPCStun;
        }
        else
        {
            return myData.GetAttribute(AttributeKey.SDamage);
        }
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
    public void takeDamage(int damage)
    {
        if(team != 0)
        {
            NPCHealth += damage;
        }
        else
        {
            int newHealth = myData.GetAttribute(AttributeKey.PDamage) + damage;
            myData.SetAttribute(AttributeKey.PDamage, newHealth, true);
        }
        pv.RPC("RPC_SetHP", RpcTarget.AllBuffered, getWounds(), myData.GetAttribute(AttributeKey.PhysicalHealth));  
    }

    public void ResetHealth()
    {
        if(team != 0)
        {
            NPCHealth = 0;
        }
        else
        {
            myData.SetAttribute(AttributeKey.PDamage, 0, false);
        }
    }

    public void takeStun(int damage)
    {
        if(team != 0)
        {
            NPCStun += damage;
        }
        else
        {
            int newStun = myData.GetAttribute(AttributeKey.SDamage) + damage;
            myData.SetAttribute(AttributeKey.SDamage, newStun, true);
        }
        pv.RPC("RPC_SetSP", RpcTarget.All, getStun(), myData.GetAttribute(AttributeKey.StunHealth));  
    }
    
    public void healStress(int damage)
    {
        if(!hasCondition(Condition.Winded))
        {
            if(team != 0)
            {
                NPCStun -= damage;
                if(NPCStun < 0)
                {
                    NPCStun = 0;
                }
            }
            else
            {
                int newStun = myData.GetAttribute(AttributeKey.SDamage) - damage;
                if(newStun < 0)
                {
                    newStun = 0;
                }
                myData.SetAttribute(AttributeKey.SDamage, newStun, true);
            }
        }
        pv.RPC("RPC_SetSP", RpcTarget.All, getStun(), myData.GetAttribute(AttributeKey.StunHealth));  
    }

    public int GetStat(AttributeKey key)
    {
        return myData.GetAttribute(key);
    }

    public int GetMovement()
    {
        return remainingMove;
    }

    public void OnMovementEnd()
    {
        List<ConditionTemplate> removeKey = new List<ConditionTemplate>(); 
        foreach(ConditionTemplate ct in Conditions.Keys)
        {
            if(ct.clearOnMove)
            {
                removeKey.Add(ct);
            }
        }
        foreach(ConditionTemplate ct in removeKey)
        {
            CombatLog.Log("By moving: " + playername + " loses their " + ct.name + " condition!");
            RemoveCondition(ct.conditionKey);
        }
    }

    public void SpendMovement(float distance)
    {
        int roundedDistance = Mathf.CeilToInt(distance);
        int newMove = remainingMove;
        newMove -= roundedDistance;
        if(newMove < 0)
        {
            newMove = 0;
        }
        pv.RPC("RPC_SetMove", RpcTarget.All, newMove);
    }

    public void Run()
    {
        SetCondition(Condition.Running, 1, true);
        int newMove = remainingMove + myData.GetAttribute(AttributeKey.MoveWalk);
        pv.RPC("RPC_SetMove", RpcTarget.All, newMove);
    }

    public void Prone()
    {
        SetCondition(Condition.Prone, 0, true);
    }

    public void Stand()
    {
        RemoveCondition(Condition.Prone);
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
    public RollResult AbilityCheck(AttributeKey skillKey, AttributeKey attributeKey = AttributeKey.Empty, AttributeKey LimitKey = AttributeKey.Empty, string customName = "", int threshold = 0, int modifier = 0)
    {
        /*
        int ConditionalModifiers = 0;
        if(!customName.Equals("Armor"))
        {
            ConditionalModifiers += getStun()/3 + getWounds()/3;
        }
        */
        RollResult newRoll = new RollResult(myData, skillKey, attributeKey, LimitKey, threshold, modifier);
        newRoll.customName = customName;
        return newRoll;
    }

    public float RollInitaitve()
    {
        int baseInitiative = 0;
        if(hasCondition(Condition.AR))
        {
            baseInitiative = myData.GetAttribute(AttributeKey.InitativeMatrix);
        }
        else
        {
            baseInitiative = myData.GetAttribute(AttributeKey.InitativeStandard);
        }
        int roll = 0;
        int initativeDice = 1;
        // adept talent grants +1 initative dice
        if(myData.hasTalent(TalentKey.ImprovedReflexes))
        {
            initativeDice++;
        }
        // wiredReflexes grants +1 initative dice
        if(myData.equipmentObjects.Contains(ItemReference.GetItem("Wired Reflexes")))
        {
            initativeDice++;
        }
        // SimTechs can gain more initative in hotsim
        if(myData.hasTalent(TalentKey.HotSimAR) && hasCondition(Condition.AR))
        {
            initativeDice++;
        }
        for(int i = 0; i < initativeDice; i++)
        {
            roll += Random.Range(1,7);
        }
        return baseInitiative + roll;
    }

    public bool HoldingWeaponClass(WeaponClass desiredClass)
    {
        if(PrimaryWeapon != null && PrimaryWeapon.IsWeaponClass(desiredClass))
        {
            return true;
        }
        if(SecondaryWeapon != null && SecondaryWeapon.IsWeaponClass(desiredClass))
        {
            return true;
        }
        return false;
    }

    public string HealthToString()
    {
        int wounds;
        int maxWounds = myData.GetAttribute(AttributeKey.PhysicalHealth);
        if(team != 0)
        {
            wounds = NPCHealth;
        }
        else
        {
            wounds = myData.GetAttribute(AttributeKey.PDamage);
        }
        return "Physical CM:" + (maxWounds - wounds) + "/" + maxWounds;
    }

    public string MoveToString()
    {
        if(hasCondition(Condition.Running))
        {
            return "Remaining Moves: " + remainingMove + "/" + myData.GetAttribute(AttributeKey.MoveRun);
        }
        return "Remaining Moves: " + remainingMove + "/" + myData.GetAttribute(AttributeKey.MoveWalk);
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

    public void SetCondition(Condition key, int duration, bool visible)
    {
        ConditionTemplate currentTemplate = ConditionsReference.GetTemplate(key);
        if (visible)
        {
            PopUpText.CreateText(key.ToString() + "!", Color.yellow, gameObject);
        }
        //if the condition already exists, set its duration to the new duration
        if(hasCondition(key))
        {
            Conditions[currentTemplate] = duration;
        }
        //else add it like normal
        else
        {
            Debug.Log("Added" + key.ToString() + "Condition!");
            Conditions.Add(currentTemplate,duration);
        }
        // if the condition has an associated effect, activate it
        if(currentTemplate.effectKey != EffectKey.None)
        {
            SetSpecialEffect(currentTemplate.effectKey,true);
        }
    }
    public bool hasCondition(Condition key)
    {
        ConditionTemplate templateKey = ConditionsReference.GetTemplate(key);
        if(templateKey != null)
        {
            return Conditions.ContainsKey(templateKey);
        }
        else
        {
            Debug.LogWarning("asking for " + key.ToString() + " condition but this condition is not recognized, has " + key.ToString() + " been added to the libray?");
            return false;
        }
    }
    public void RemoveCondition(Condition key)
    {
        ConditionTemplate templateKey = ConditionsReference.GetTemplate(key);
        if(Conditions.ContainsKey(templateKey))
        {
            Conditions.Remove(templateKey);   
            // if the condition has an associated effect, remove it
            if(templateKey.effectKey != EffectKey.None)
            {
                SetSpecialEffect(templateKey.effectKey,false);
            }
        }
    }

    public int CalculateStatModifiers(string ignoreKey = "")
    {
        int total = 0;
        foreach(ConditionTemplate key in Conditions.Keys)
        {
            total += key.GetModifier(ignoreKey);
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
                outputStack.Push(header + value + " from " + key.name + " condition");
            }
        }
        return outputStack;
    }
    public int GetAP()
    {
        int bestAP = 0;
        if(myData.isMinion)
        {
            return myData.GetAttribute(AttributeKey.DroneArmor);
        }
        foreach(Item i in myData.equipmentObjects)
        {
            //only check armor
            if(i.GetType() == typeof(Armor))
            {
                Armor a = (Armor)i;
                int currentAP = a.GetAP();
                if(currentAP > bestAP)
                {
                    bestAP = currentAP;
                }
            }
        }
        if(hasCondition(Condition.TerrainArmor))
        {
            bestAP += Conditions[ConditionsReference.GetTemplate(Condition.TerrainArmor)];
        }
        return bestAP;
    }

    public int CalculateRecoilPenalty(int bulletsFired)
    {
        int newTotalBulletsFired = TotalBulletsFired + bulletsFired;
        int totalPenalty = myData.GetAttribute(AttributeKey.RecoilComp) - newTotalBulletsFired;
        if(totalPenalty < 0)
        {
            return totalPenalty;
        }
        return 0;
    }

    public void ResetRecoilPenalty()
    {
        TotalBulletsFired = 0;
    }

    public void IncreaseRecoilPenalty(int bulletsFired)
    {
        TotalBulletsFired += bulletsFired;
    }

    //calls at the beginning And end of turn each interger value refers to beginning and ending of turns 
    public void StartRound()
    {
        ResetRecoilPenalty();
        defensePenality = 0;
        pv.RPC("RPC_SetMove", RpcTarget.All, myData.GetAttribute(AttributeKey.MoveWalk));
        List<ConditionTemplate> removedKeys = new List<ConditionTemplate>();
        List<ConditionTemplate> IncrementKeys = new List<ConditionTemplate>();
        foreach (ConditionTemplate Key in Conditions.Keys)
        {
            //decay conditions at the end of turns, conditions with 0 as their length do not decay
            if(Conditions[Key] > 1)
            {
                IncrementKeys.Add(Key); 
            }
            else if(Conditions[Key] == 1)
            {
                removedKeys.Add(Key);
            }
        }
        foreach(ConditionTemplate Key in IncrementKeys)
        {
            Conditions[Key]--;
        }
        foreach(ConditionTemplate Key in removedKeys)
        {
            CombatLog.Log(GetName() + "'s " + Key.name + " condition has expired");
            RemoveCondition(Key.conditionKey);
        }
    }

    // called in the board scene when a player ends their turn
    public void OnTurnEnd(int remainingHalfActions)
    {
        if(remainingHalfActions > 1)
        {
            healStress(2);
        }
        else if (remainingHalfActions > 0)
        {
            healStress(1);
        }
        List<ConditionTemplate> removedKeys = new List<ConditionTemplate>();
        foreach(ConditionTemplate Key in Conditions.Keys)
        {
            if(Key.clearOnTurnEnd)
            {
                removedKeys.Add(Key);
            }
        }
        foreach(ConditionTemplate key in removedKeys)
        {
            Conditions.Remove(key);
        }
    }

    public void OnAttackFinished()
    {
        List<ConditionTemplate> removedKeys = new List<ConditionTemplate>();
        foreach(ConditionTemplate Key in Conditions.Keys)
        {
            if(Key.clearOnAttack)
            {
                Debug.Log(Key + " should be cleared");
                removedKeys.Add(Key);
            }
        }
        foreach(ConditionTemplate key in removedKeys)
        {
            Conditions.Remove(key);
        }
    }

    public int GetDefensePenality()
    {
        return -(defensePenality);
    }

    public void IncreaseDefensePenality()
    {
        defensePenality++;
    }

    public void RemoveItem(Item item)
    {
        myData.RemoveItem(item);
        if(!myData.equipmentObjects.Contains(item))
        {
            if(PrimaryWeapon == item)
            {
                PrimaryWeapon = null;
            }
            if(SecondaryWeapon == item)
            {
                SecondaryWeapon = null;
            }
        }
    }

    public bool ThreateningMelee()
    {
        if(PrimaryWeapon != null && PrimaryWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return true;
        }
        if(SecondaryWeapon != null && SecondaryWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return true;
        }
        return false;
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
        SetCondition(Condition.Grappled,0,true);
        CombatLog.Log(GetName() + " is grappled by " + grappler.GetName());
        SpendAction("reaction");
        attacker.SpendAction("reaction");
    }

    public void ControlGrapple()
    {
        RemoveCondition(Condition.Grappled);
        grappler.SetGrappler(this);
        grappler = null;
    }

    public void ReleaseGrapple()
    {
        CombatLog.Log(GetName() + ": is no longer grappling" + grappleTarget.GetName());
        PopUpText.CreateText("Released!", Color.yellow, grappleTarget.gameObject);
        grappleTarget.grappler = null;
        grappleTarget.RemoveCondition(Condition.Grappled);
        grappleTarget = null;
    }

    public bool Grappling()
    {
        return (grappleTarget != null || grappler != null);
    }

    public List<Weapon> GetWeaponsForEquipment()
    {
        List<Weapon> output = new List<Weapon>();
        foreach(Item i in myData.equipmentObjects)
        {
            if(i.GetType() == typeof(Weapon))
            {
                output.Add((Weapon)i);
            }
        }
        return output;
    }

    public bool IsOccupied()
    {
        return Occupied;
    }
    
    public override string ToString()
    {
        string output = GetName();
        return output;
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

    public void SetSpecialEffect(EffectKey effect, bool enabled)
    {
        pv.RPC("RPC_SetSpecialEffect",RpcTarget.All, (int)effect, enabled);
    }
    [PunRPC] 
    void RPC_SetSpecialEffect(int effectIndex, bool enabled)
    {
        specialEffects[effectIndex-1].SetActive(enabled);
    }
}
