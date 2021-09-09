using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "Weapons", menuName = "ScriptableObjects/Weapons")]
public class Weapon
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
    //number of hands required to wield this weapon
    [SerializeField] private string name;
    //number of hands required to wield this weapon
    [SerializeField] private bool jammed = false;
    //number of hands required to wield this weapon
    private string Class;

    public Weapon(WeaponTemplate template)
    {
        this.name = template.name;
        this.Attributes = template.Attributes;
        this.numDice = template.numDice;
        this.sizeDice = template.sizeDice;
        this.damageBonus = template.damageBonus;
        this.range = template.range;
        this.pen = template.pen;
        this.reloadMax = template.reloadMax;
        this.reloads = 0;
        this.blast = template.blast;
        ROF = new Dictionary<string, int>();
        ROF.Add("S", template.single);
        ROF.Add("Semi", template.semi);
        ROF.Add("Auto", template.auto);
        this.clipMax = template.clipMax;
        this.clip = this.clipMax;
        this.damageType = template.damageType;
        this.Class = template.Class;
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


    public string GetName()
    {
        return name;
    }

     public int rollDamage(PlayerStats player)
    {
        int value = 0;
        int i = 0;
        while(i < numDice){
            int roll;
            if(HasWeaponAttribute("Tearing"))
            {
                CombatLog.Log(GetName() + " rolls two dice and takes the highest result because of its tearing quality");
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
            value += roll; 
            i++;
        }
        return rollDamage(value, player);
    }

    public int rollDamage( int value, PlayerStats player){
        int DB = damageBonus;
        //damageBonus is calculated differently for melee weapons SB is added on top of everything else 
        if (IsWeaponClass("Melee"))
        {
            DB += player.GetStatScore("S");
        }
        value += DB;
        CombatLog.Log(name + ": " + numDice + "d" + sizeDice + " + " + DB + " = " + value);
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
            return cost;
        }
    }

    //increments reloads by one half action, returns true when reload is complete
    public bool Reload()
    {
        reloads++;
        if (reloads == reloadMax)
        {
            clip = clipMax;
            reloads = 0;
            return true;
        }
        return false;
    }
    //whether of not to display this weapon on the reload screen
    public bool CanReload()
    {
        if(IsWeaponClass("Melee"))
        {
            return false;
        }
        if(jammed)
        {
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
        if (IsWeaponClass("Melee") || IsWeaponClass("Thrown"))
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

    public string DisplayDamageRange(PlayerStats user)
    {
        int DB = damageBonus;
        if(IsWeaponClass("Melee"))
        {
            DB += user.GetStatScore("S");
        }
        return "Damage: " + numDice + "d" + sizeDice + " + " + DB + " " + damageType;  
    }
    public string DisplayDamageRange()
    {
        return numDice + "d" + sizeDice + " + " + damageType;  
    }


    public string AttributesToString()
    {
        string output = "";
        foreach(string s in Attributes)
        {
            if(!s.Equals("Melee")||!s.Equals("TwoHanded"))
            {
                output += s + " ";
            }
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
}
