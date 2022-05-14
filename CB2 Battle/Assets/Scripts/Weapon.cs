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
    //Flat damage bonus for ranged weapons, multiplier for SB for melee weapons
    //effective range for weapon
    [SerializeField] private int range;
    //number of half actions must exceed this value to reload 
    [SerializeField] private int blast;
    //current choice of firerate
    private string FireRate;
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
    [SerializeField] private int damage;
    [SerializeField] private int AP;
    int sizeDice;
    // Special weapons with the recharge abilitiy use this to determine if they can fire this turn or not
    private bool clipEjected = false;
    // Reference to the scriptable object, a weapon object represents a modifiable copy of a source weapon template that never changes.
    new public WeaponTemplate Template;

    // standard constructor takes a weapon template and creates a copy of it
    public Weapon(WeaponTemplate template)
    {
        Template = template;
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.availablity = template.availablity;
        this.description = template.description;
        this.rating = template.rating;
        this.stacks = 1;
        this.unique = template.unique;
        this.damage = template.damageBonus;
        this.AP = template.pen;
        this.sizeDice = template.sizeDice;
        if(Template.SingleShot)
        {
            FireRate = "SS";
        }
        else if(Template.SemiAuto)
        {
            FireRate = "SA";
        }
        else if(Template.BurstFire)
        {
            FireRate = "BF";
        }
        else if(Template.FullAuto)
        {
            FireRate = "FA";
        }

        this.Attributes = template.Attributes;
        this.blast = template.blast;
        this.AmmoSource = template.AmmoSource;
        this.clipMax = template.clipMax;
        this.clip = this.clipMax;
        this.damageType = template.damageType;
        
        
        upgrades = new Dictionary<ItemTemplate, bool>();
        foreach(ItemTemplate ug in template.upgrades)
        {
            this.upgrades.Add(ug, template.debugAddAllUpgrades);
        }
        UpdateTooltip();
    }

    // returns true if the weapon template has the same class as desired class
    public bool IsWeaponClass(WeaponClass DesiredClass)
    {
        // shields are considered melee for most purposes 
        if(DesiredClass == WeaponClass.melee && Template.weaponClass == WeaponClass.shield)
        {
            return true;
        }
        return DesiredClass.Equals(Template.weaponClass);
    }

    // getter for weaponclass of template
    public WeaponClass GetClass()
    {
        return Template.weaponClass;
    }

    // Getter for armor penatration 
    public int GetAP()
    {
        return AP;
    }

    // Returns a string code of each firerate this weapon is capable of shooting 
    public Dictionary<string, string> GetSelectableFireRates()
    {
        Dictionary<string, string> output = new Dictionary<string, string>();
        if(Template.SingleShot && !(FireRate.Equals("SS")))
        {
            output.Add("Single Shot", "setSS");
        }
        if(Template.SemiAuto && !(FireRate.Equals("SA")))
        {
            output.Add("Semi-Auto", "setSA");
        }
        if(Template.BurstFire && !(FireRate.Equals("BF")))
        {
            output.Add("Burst Fire", "setBF");
        }
        if(Template.FullAuto && !(FireRate.Equals("FA")))
        {
            output.Add("Full Auto", "setFA");
        }
        return output;
    }

    // Returns a compatable list of actions for turn manager that this weapon can preform with a given number of half actions
    public Dictionary<string, string> GetWeaponActions(bool complex)
    {
        Dictionary<string, string> output = new Dictionary<string, string>();
        if(IsWeaponClass(WeaponClass.melee) || IsWeaponClass(WeaponClass.shield))
        {
            if(complex)
            {
                output.Add("Melee Attack", "Melee");
            }
            return output;
        }
        if(IsWeaponClass(WeaponClass.thrown))
        {
            if(Template.blastClass.Equals(BlastClass.sphere))
            {
                output.Add("Throw Attack", "BlastAttack");
            }
            else
            {
                output.Add("Throw Attack", "fireThrown");
            }
            return output;
        }
        if(FireRate.Equals("SS") && clip > 0)
        {
            output.Add("Single Shot", "fireSS");
        }
        else if(FireRate.Equals("SA") && clip > 0)
        {
            output.Add("Semi Auto", "fireSA");
            if(complex)
            {
                output.Add("Semi Auto Burst", "fireSAB");
            }
        }
        else if(FireRate.Equals("BF") && clip > 0)
        {
            output.Add("Burst Fire", "fireBF");
            if(complex)
            {
                output.Add("Long Burst", "fireLB");
            }
        }
        else if(FireRate.Equals("FA") && clip > 0)
        {
            output.Add("Full Auto", "fireFA");
            if(complex)
            {
                output.Add("Full Auto Burst", "fireFAB");
            }
        }
        if((Template.SingleShot ? 1:0) + (Template.SemiAuto ? 1:0) + (Template.BurstFire ? 1:0) + (Template.FullAuto ? 1:0) > 1)
        {
            output.Add("Switch Fire Rate", "ChangeFireRate");
        }
        return output;
    }

    // changes current fire rate to a new string code, should never accept anything out side of standard firerates
    public void setFireRate(string newFirerate)
    {
        FireRate = newFirerate;
    }

    // returns true if current firerate equals given firemode
    public bool CanFire(string FireMode)
    {
        return FireRate.Equals(FireMode);
    }

    // Removes given ammount of bullets from current clip
    public void ExpendAmmo(int bullets)
    {
        clip -= bullets;
        if(clip < 0)
        {
            clip = 0;
        }
    }

    //increments reloads by one half action, returns true when reload is complete
    public bool Reload(PlayerStats owner, int roundsNeeded)
    {
        if(AmmoSource != null)
        {
            //if ammo is from another source: i.e. shells for a shotgun
            foreach(Item i in owner.myData.equipmentObjects)
            {
                //when i is equal to my ammo source
                if(i.GetName() == AmmoSource.name)
                {
                    while(clipMax > clip && roundsNeeded > 0 && i.GetStacks() > 0)
                    {
                        clip++;
                        roundsNeeded--;
                        i.SubtractStack();
                    }
                    if(i.IsConsumed())
                    {
                        owner.myData.RemoveItem(i);
                        return true;
                    }
                }
            }
        }
        UpdateTooltip();
        return true;
    }
    
    //whether of not to display this weapon on the reload screen
    public bool CanReload(PlayerStats owner)
    {
        if(!IsWeaponClass(WeaponClass.ranged))
        {
            return false;
        }
        if(jammed)
        {
            return false;
        }
        if(AmmoSource != null)
        {
            foreach(Item i in owner.myData.equipmentObjects)
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

    // returns a string indicating if this weapon is lethal or not
    public string GetDamageType()
    {
        if(Template.Lethal)
        {
            return "P";
        }
        else
        {
            return "S";
        }
    }

    // returns a readout of quick information for the player ui
    public override string ToString()
    {
        if(!IsWeaponClass(WeaponClass.ranged))
        {
            return GetName();
        }
        if(jammed)
        {
            return GetName() + ": Jammed!";
        }
        return GetName() + ": " + clip + "/" + clipMax;
    }

    // return range if specified on template. Thrown weapons have a range defined by the user
    public int getRange(PlayerStats owner)
    {
        if(IsWeaponClass(WeaponClass.thrown))
        {
            return owner.GetStat(AttributeKey.Strength) * 3;
        }
        return range;
    }

    // in certain instances owner isn't specified in which case we assume the weapon cannot be thrown
    public int getRange()
    {
        return range;
    }

    // returns true if weapon has given attribute
    public bool HasWeaponAttribute(string attribute)
    {
        return Attributes.Contains(attribute);
    }

    // information sent to the player to inform them how to reload this weapon
    public string ReloadString()
    {
        if(hasUpgrade("Speed Loader"))
        {
            return "(Speed Reloader: 2 half actions)";
        }
        return "(" + Template.clipType.ToString() + ": " + ReloadWeapon(null, false) + " half actions)";
    }

    // certain weapons have a defined blast radius, calling for a blastattack from turnmanager instead of roll to hit
    public int GetBlast()
    {
        return blast;
    }

    // depreciated
    public void SetJamStatus(bool input)
    {
        jammed = input;
    }

    // depreciated
    public bool isJammed()
    {
        return jammed;
    }

    public bool CanParry()
    {
        if(!IsWeaponClass(WeaponClass.melee) || HasWeaponAttribute("Unwieldy"))
        {
            return false;
        }
        return true;
    }

    public string DisplayDamageRange()
    {
        if(Template != null)
        {
            string DB = "1d" + Template.sizeDice;
            string addition = "";
            if(IsWeaponClass(WeaponClass.melee))
            {
                addition = " + S";
            }
            else if(damage > 0)
            {
                addition = " + " + damage;
            }
            else if(damage != 0)
            {
                addition = " - " + Mathf.Abs(damage);
            }
            return "Damage: " + DB + addition;  
        }
        return "Damage: " + damage;
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
        if(IsWeaponClass(WeaponClass.ranged))
        {
            string output = "";
            if(Template.SingleShot)
            {
                output += "SS/";
            }
            if(Template.SemiAuto)
            {
                output += "SA/";
            }
            if(Template.BurstFire)
            {
                output += "BF/";
            }
            if(Template.FullAuto)
            {
                output += "FA/";
            }
            output.TrimEnd(output[output.Length - 2]);
            return output;
        }
        else
        {
            return "";
        }        
    }

    public int getClip()
    {
        return clip;
    }

    public void SetClip(int newClip)
    {
        clip = newClip;
        UpdateTooltip();
    }

    public void OnTurnStart()
    {
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
            owner.myData.equipmentObjects.Remove(this);
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

    // returns an array of d6 rolls of length equal to number of dice in the weapon template
    public int RollDamage()
    {
        /*
        int[] rolls = new int[numDice];
        for(int i = 0; i < rolls.Length; i++)
        {
            rolls[i] = Random.Range(1,7);
        }
        */
        return Random.Range(1,sizeDice + 1);
    }

    /* returns a sum total of Rolldamage() for cleaner code when the indvidual rolls don't need to be known
    public int RollDamageTotal()
    {
        int[] rolls = RollDamage();
        int total = 0;
        for(int i = 0; i < rolls.Length; i++)
        {
            total += rolls[i];
        }
        return total;
    }
    */


    public int GetDamageBonus()
    {
        return damage;
    }

    public override Sprite GetSprite()
    {
        if(Template.icon != null)
        {
            return Template.icon;
        }
        return Resources.Load<Sprite>("Assets/Resources/Materials/Icons/Items/Pistol.png");
    
    }

    public override void UpdateTooltip()
    {
        tooltip = "Rating " + rating + " " + Template.weaponClass.ToString() + " weapon";
        tooltip += "\n\n" + DisplayDamageRange() + " " + damageType;
        
        tooltip += "\nRequired Skill: " + Template.WeaponSkill.name;
        tooltip += "\nSpecialization: " + Template.WeaponSkill.Specializations.ToArray()[Template.WeaponSpecialization];
        tooltip += "\nAccuracy" + Template.accuracy;
        tooltip += "\nArmor Penetration: " + Template.pen;

        if(IsWeaponClass(WeaponClass.melee))
        {
            if(Template.rangeClass.getReach() > 1)
            {
                tooltip += "\nReach: " + Template.rangeClass.getReach();
            }
        }
        else
        {
            tooltip += "\nReload: " + ReloadString();
            tooltip += "\nRate of Fire: " + ROFtoString(); 
            tooltip += "\nClip: " + clip + "/" + clipMax;
            if(AmmoSource != null)
            {
                tooltip += "\nAmmo type: " + AmmoSource.name;
            }
        }
        tooltip += "\nupgrades:";
        string upgradedesc = " ";
        foreach(ItemTemplate ug in upgrades.Keys)
        {
            if(upgrades[ug])
            {
                upgradedesc += ug.name + ",";
            } 
            upgradedesc = upgradedesc.TrimEnd(upgradedesc[upgradedesc.Length - 2]);
        }
        tooltip += upgradedesc;
        tooltip += "\nweight: " + weight;
        tooltip += "\nvalue: " + cost;
        tooltip += "\n\n\"" + description + "\"";
    }

    public int ReloadWeapon(PlayerStats owner, bool apply)
    {
        if(hasUpgrade("Speed Loader"))
        {
            if(apply)
            {            
                CombatLog.Log(GetName() + "'s speed loader fully reloads the clip");
                Reload(owner, clipMax);
            }
            return 2;
        }
        // weapons that don't have a speed loader; 
        else
        {
            switch(Template.clipType)
            {
                case reloadingMethod.clip:
                {
                    if(apply)
                    {
                        // reload ammo if clip is already ejected
                        if(clipEjected)
                        {
                            CombatLog.Log(owner.GetName() + " loads in another clip to their " + GetName());
                            Reload(owner, clipMax);
                        }
                        else
                        {
                            CombatLog.Log(owner.GetName() + " ejects the clip in their " + GetName());
                        }
                        clipEjected = !clipEjected;
                    }
                    return 1;
                }

                case reloadingMethod.breakaction:
                {
                    if(apply)
                    {
                        CombatLog.Log(owner.GetName() + " loads 2 bullets into their break action " + GetName());
                        Reload(owner, 2);
                    }
                    return 2;
                }

                case reloadingMethod.beltfed:
                {
                    if(apply)
                    {
                        // reload ammo if clip is already ejected
                        if(clipEjected)
                        {
                            CombatLog.Log(owner.GetName() + " attaches a new belt feed on their " + GetName());
                            Reload(owner, clipMax);
                        }
                        else
                        {
                            CombatLog.Log(owner.GetName() + " detaches the belt feed on their " + GetName());
                        }
                    }
                    return 2;
                }
                
                case reloadingMethod.internalmagazine:
                {
                    if(apply)
                    {
                        CombatLog.Log(owner.GetName() + " inserts bullets equal to their agility into their " + GetName());
                        Reload(owner, owner.myData.GetAttribute(AttributeKey.Agility));
                    }
                    return 2;
                }

                case reloadingMethod.muzzleloader:
                {
                    if(apply)
                    {
                        CombatLog.Log(owner.GetName() + " reloads the muzzle of their " + GetName());
                        Reload(owner, 1);
                    }
                    return 2;
                }

                case reloadingMethod.cylinder:
                {
                    if(apply)
                    {
                        CombatLog.Log(owner.GetName() + " inserts bullets equal to their agility into their " + GetName());
                        Reload(owner, owner.myData.GetAttribute(AttributeKey.Agility));
                    }
                    return 2;
                }

                case reloadingMethod.drum:
                {
                    if(apply)
                    {
                        // reload ammo if clip is already ejected
                        if(clipEjected)
                        {
                            CombatLog.Log(owner.GetName() + " attaches a drum on their " + GetName());
                            Reload(owner, clipMax);
                        }
                        else
                        {
                            CombatLog.Log(owner.GetName() + " detaches the drum on their " + GetName());
                        }
                    }
                    return 2;
                }

                case reloadingMethod.bow:
                {
                    if(apply)
                    {
                        CombatLog.Log(owner.GetName() + " knocks another arrow into their " + GetName());
                        Reload(owner, 1);
                    }
                    return 1;
                }
            }
        }
        return 0;
    }

    public void SetAP(int newAP)
    {
        AP = newAP;
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public override string GetTemplateName()
    {
        return (Template.name);
    }
}
