using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "Weapons", menuName = "ScriptableObjects/Weapons")]
public class Weapon : Item
{ 
    //attributes are keywords a weapon would possess, should really be called "Special"
    [SerializeField] private List<string> Attributes = new List<string>();
    //stats concern all weapon stats for easy modification in the player sheet 
    private Dictionary<string, int> Stats; 
    //number of dice rolled to determine damage Xd10 
    [SerializeField] private int numDice; 
    //random range of damage per die roll 1dX
    [SerializeField] private int sizeDice;
    //Flat damage bonus for ranged weapons, multiplier for SB for melee weapons
    [SerializeField] private int damageBonus;
    //effective range for weapon
    [SerializeField] private int range;
    //Negates AP of target
    [SerializeField] private int pen;
    //number of half actions must exceed this value to reload 
    [SerializeField] private int blast;
    //number of half actions must exceed this value to reload 
    [SerializeField] private int reloadMax;  
    //number of current reloads
    [SerializeField] private int reloads;  
    //number of shots fired in single firemode
    [SerializeField] private Dictionary<string, int> ROF;
    //clip capacity
    [SerializeField] private int clipMax;
    //current ammo
    [SerializeField] private int clip;
    //damage type for determining critical table
    [SerializeField] private string damageType;
    // Ranged weapons can become jammed, preventing them from being fired until repaired
    [SerializeField] private bool jammed = false;
    // Some weapons consume items in order to be reloaded
    [SerializeField] private ItemTemplate AmmoSource;
    // Class determines how the weapon functions, wether it is ranged, or melee
    private string Class;
    // Special weapons with the recharge abilitiy use this to determine if they can fire this turn or not
    private int Recharging;
    private int ID;

    public Weapon(WeaponTemplate template)
    {
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.unique = template.unique;
        this.stacks = 1;
        this.availablity = template.availablity;
        this.description = template.description;
        this.Attributes = template.Attributes;
        this.numDice = template.numDice;
        this.sizeDice = template.sizeDice;
        this.damageBonus = template.damageBonus;
        this.range = template.range;
        this.pen = template.pen;
        this.reloadMax = template.reloadMax;
        this.reloads = 0;
        this.blast = template.blast;
        this.AmmoSource = template.AmmoSource;
        ROF = new Dictionary<string, int>();
        ROF.Add("S", template.single);
        ROF.Add("Semi", template.semi);
        ROF.Add("Auto", template.auto);
        this.clipMax = template.clipMax;
        this.clip = this.clipMax;
        this.damageType = template.damageType;
        this.Class = template.Class;
        Recharging = 0;
        ID = Random.Range(0,100000);
    }

    public int GetStat(string key)
    {
        if(Stats.ContainsKey(key))
        {
            return Stats[key];
        }
        else
        {
            Debug.Log("Error!:" + key + " Not Defined!");
            return 0;
        }
    }
    
    public int GetReloads()
    {
        return reloadMax;
    }

     public int rollDamage(PlayerStats player, PlayerStats target)
    {
        int value = 0;
        int i = 0;
        if(HasWeaponAttribute("Tearing"))
        {
            CombatLog.Log(GetName() + " rolls two dice and takes the highest result because of its tearing quality");
        }
        string output = "";

        while(i < numDice){
            int roll;
            if(HasWeaponAttribute("Tearing") || target.IsHelpless())
            {
                int roll1 = Random.Range(1,sizeDice);
                int roll2 = Random.Range(1,sizeDice);
                if(roll1 >= roll2)
                {
                    roll = roll1;
                }
                else
                {
                    roll = roll2;
                }
            }
            else
            {
                roll = Random.Range(1,sizeDice);
            } 
            if(roll == sizeDice)
            {
                roll += TacticsAttack.Critical(player, this, false);
            }
            output += "[" + roll + "] +";
            value += roll; 
            i++;
        }

        int DB = damageBonus;
        //damageBonus is calculated differently for melee weapons SB is added on top of everything else 
        if (IsWeaponClass("Melee"))
        {
            DB += player.GetStatScore("S");
        }
        output += " " + DB;
        value += DB;
        if(HasWeaponAttribute("Unstable"))
        {
            int unstableResult = Random.Range(1,10);
            if(unstableResult == 10)
            {
                CombatLog.Log(GetName() + "'s unstable attribute doubles its damage!");
                value *= 2;
                output += " x 2";
            }
            else if(unstableResult == 1)
            {
                CombatLog.Log(GetName() + "'s unstable attribute halves its damage!");
                value /= 2;
                output += " % 2";
            }
        }
        CombatLog.Log(name + ": " + output + " = " + value);
        return value;
    }

    public int rollDamage( int value, PlayerStats player){
        int DB = damageBonus;
        //damageBonus is calculated differently for melee weapons SB is added on top of everything else 
        if (IsWeaponClass("Melee"))
        {
            DB += player.GetStatScore("S");
        }
        value += DB;
        if(HasWeaponAttribute("Unstable"))
        {
            int unstableResult = Random.Range(1,10);
            if(unstableResult == 10)
            {
                CombatLog.Log(GetName() + "'s unstable attribute doubles its damage!");
                value *= 2;
            }
            else if(unstableResult == 1)
            {
                CombatLog.Log(GetName() + "'s unstable attribute halves its damage!");
                value /= 2;
            }
        }
        CombatLog.Log(name + ": " + DisplayDamageRange() + " = " + value);
        return value;
    }

    public bool IsWeaponClass(string DesiredClass)
    {
        return DesiredClass.Equals(Class);
    }

    public string GetClass()
    {
        return Class;
    }

    public int GetAP()
    {
        return pen;
    }

    public bool CanFire(string FireMode)
    {
        
        if(HasWeaponAttribute("Recharge") && Recharging > 0)
        {
            return false;
        }
        if(jammed)
        {
            return false;
        }
        //if this weapon can't fire this weapon mode regardless of clip size
        if (ROF[FireMode] == 0)
        {
            return false;
        }
        int subtractedClip = clip - ROF[FireMode];
        //else return false and don't change ammo
        return subtractedClip >= 0; 
    }

    public int ExpendAmmo(string FireMode)
    {
        if(FireMode.Equals("Jam"))
        {
            clip = 0;
            return 0;
        }
        else
        {
            int cost = ROF[FireMode];
            clip -= cost;
            if ( clip < 0)
            {
                clip = 0;
            }
            Recharging = 2;
            return cost;
        }
    }

    //increments reloads by one half action, returns true when reload is complete
    public bool Reload(PlayerStats owner)
    {
        reloads++;
        if (reloads >= reloadMax)
        {
            reloads = 0;
            if(AmmoSource != null)
            {
                //if ammo is from another source: i.e. shells for a shotgun
                foreach(Item i in owner.equipment)
                {
                    //when i is equal to my ammo source
                    if(i.GetName() == AmmoSource.name)
                    {
                        //las weaponry use charge packs instead of individual bullets
                        if(HasWeaponAttribute("Full Reload"))
                        {
                            i.SubtractStack();
                            clip = clipMax;
                        }
                        else
                        {
                            int AmmoNeeded = clipMax - clip;
                            int Reserves = i.GetStacks();
                            //if a full reload can't be completed, try to fill as much as you can
                            if(AmmoNeeded > Reserves)
                            {
                                clip += Reserves;
                                i.SetStack(0);
                            }
                            else
                            {
                                clip = clipMax;
                                i.SubtractStack(AmmoNeeded);
                            }
                        }
                        if(i.IsConsumed())
                        {
                            owner.equipment.Remove(i);
                            return true;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
    //whether of not to display this weapon on the reload screen
    public bool CanReload(PlayerStats owner)
    {
        if(IsWeaponClass("Melee"))
        {
            return false;
        }
        if(jammed)
        {
            return false;
        }
        if(AmmoSource != null)
        {
            foreach(Item i in owner.equipment)
            {
                if(i.GetName().Equals(AmmoSource.name))
                {
                    return clip != clipMax;
                }
            }
            return false;
        }
        return clip != clipMax;
    }

    public string GetDamageType()
    {
        return damageType;
    }

    public int RangeBonus(Transform target, PlayerStats myStats)
    {
        if (IsWeaponClass("Melee"))
        {
            return 0;
        }
        float distance = Mathf.RoundToInt(Vector3.Distance(myStats.transform.position, target.position)); 
        float myRange = (float)getRange(myStats);
        float percentage = distance/myRange;
        //easy at extreme range
        if (distance <= 3 && distance >= 2)
        {
            //CombatLog.Log("Extreme range improves BS by 30!");
            return 30;
        } 
        //ordinary at half range
        else if (percentage <= 0.5 && distance >= 2)
        {
            //CombatLog.Log("half range improves BS by 10!");
            return 10;
        }
        //regular until double range
        else if (percentage < 2)
        {
            return 0;
        }
        //dificult at double range
        else if (percentage < 3)
        {
            //CombatLog.Log("double range reduces BS by 10!");
            return -10;
        }
        //very hard past triple range
        else {
            //CombatLog.Log("extreme range reduces BS by 30!");
            return -30;
        }
    }

    public override string ToString()
    {
        if(IsWeaponClass("Melee"))
        {
            return GetName();
        }
        if(jammed)
        {
            return GetName() + ": Jammed!";
        }
        return GetName() + ": " + clip + "/" + clipMax;
    }

    public int getRange(PlayerStats owner)
    {
        if(IsWeaponClass("Thrown"))
        {
            return owner.GetStatScore("S") * 3;
        }
        return range;
    }
    public int getRange()
    {
        return range;
    }
    public bool HasWeaponAttribute(string attribute)
    {
        return Attributes.Contains(attribute);
    }

    public string ReloadString()
    {
        return "Reloading! " + (reloadMax - reloads) + " half actions left.";
    }
    public int GetBlast()
    {
        return blast;
    }

    public void SetJamStatus(bool input)
    {
        jammed = input;
    }

    public bool isJammed()
    {
        return jammed;
    }

    public bool CanParry()
    {
        if(!IsWeaponClass("Melee") || HasWeaponAttribute("Unwieldy"))
        {
            return false;
        }
        return true;
    }

    public string DisplayDamageRange()
    {
        string DB = " ";
        if (damageBonus > 0)
        {
            DB += " + " + damageBonus;
        }
        if(IsWeaponClass("Melee"))
        {
            DB += " + SB";
        }
        return "Damage: " + numDice + "d" + sizeDice + DB;  
    }

    public string AttributesToString()
    {
        string output = "";
        foreach(string s in Attributes)
        {
            output += s + " ";
        }
        return output;
    }
    public string ROFtoString()
    {
        string output = "";
        foreach(string key in ROF.Keys)
        {
            if(ROF[key] > 0)
            {
                output += ROF[key] + "";
            }
            else
            {
                output += "-";
            }
            output += "/"; 
        }
        output.TrimEnd(output[output.Length - 1]);
        return output;
    }

    public int getClip()
    {
        return clipMax;
    }

    public void OnTurnStart()
    {
        if(HasWeaponAttribute("Recharge") && Recharging > 0)
        {
            Recharging--;
        }
    }

    public string reloadToString()
    {
        if(reloadMax % 2 == 0)
        {
            return (reloadMax / 2) + " Full"; 
        }
        else
        {
            return reloadMax + "Half";
        }
    }

    public int GetAdditionalHits(string type, int DOF, int FireRate, bool scatterDistance)
    {
        bool scatterExtraAttacks = scatterDistance && HasWeaponAttribute("Scatter");
        int scatterAttacks = 0;
        if(scatterExtraAttacks)
        {
            Debug.Log("extra attack");
            scatterAttacks = DOF / 2;
        }
        if(type.Equals("S"))
        {
            return 1 + scatterAttacks;
        }
        int extraAttacks = DOF;
        if(type.Equals("Semi"))
        {
            extraAttacks /= 2;
        }
        int numAttacks = 1 + extraAttacks;
        //cannot get a number of extra attacks equal to 
        if((numAttacks > FireRate))
        {
            numAttacks = FireRate;
        }
        return numAttacks + scatterAttacks;
    }


    public void ThrowWeapon(PlayerStats owner)
    {
        clip = clipMax;
        owner.Unequip(this);
        this.SubtractStack();
        if(this.IsConsumed())
        {
            owner.equipment.Remove(this);
        }
    }

    public int AmmoConsumed()
    {
        if(HasWeaponAttribute("Las"))
        {
            return 1;
        }
        else
        {
            return clipMax - clip;
        }
    }
}
