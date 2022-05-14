using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Save data for stored players, accessed through the DM menu, used to permanently save characters
[System.Serializable]
public class CharacterSaveData
{
    // Basic character stats Shadowrunner
    public int[] attribues = new int[100]; 
    public bool[] talents = new bool[100];
    
    // Skills Shadowrunner
    public Dictionary<string, int> skillSpecialization = new Dictionary<string, int>();
    // non ttrpg stats used for game logic
    public int team = 0; 
    public string playername;
    // Name of mesh model
    public string Model;
    // Condensed list of equipment and there properties, only modified on load and save
    public string[] equipmentCode; 
    public List<Item> equipmentObjects = new List<Item>();
    public bool isMinion = false;
    public int ownerID;
    
    // Playable: whether to create an NPC or Player character
    // Creates a new Save Data with default skills and hit locations
    public CharacterSaveData(bool playable)
    {
        if(playable)
        {
            playername = "New Player";
            Model = "Guard";
            team = 0;
        }
        else
        {
            playername = "New NPC";
            Model = "Renegade";
            team = 1;
        }
        foreach(AttributeKey key in AttribueReference.keys)
        {
            SetAttribute(key, 0, false);
        }
        foreach(string skillKey in SkillReference.SkillsTemplates().Keys)
        {
            setSpecialization(skillKey,-1);
        }
        CalculateCharacteristics();
    }

    public CharacterSaveData(string playername, string[] attribues, Dictionary<string,int> specalizations, string[] newequipment, Dictionary<int, bool> newTalents)
    {
        this.playername = playername;
        this.skillSpecialization = specalizations;
        DecompileStats(attribues);
        decompileEquipment(newequipment);
        DecompileTalents(newTalents);
    }

    // used for creating drones
    public CharacterSaveData(PlayerStats owner, Drone minionTemplate)
    {
        isMinion = true;
        Model = minionTemplate.Template.model;
        this.ownerID = owner.ID;
        this.playername = owner.GetName() + "'s " + minionTemplate.GetName();
        this.team = owner.GetTeam();
        SetAttribute(AttributeKey.DroneHandling, minionTemplate.Template.Handling, false);
        SetAttribute(AttributeKey.DroneStructure, minionTemplate.Template.Structure, false);
        SetAttribute(AttributeKey.DroneArmor, minionTemplate.Template.Armor, false);
        SetAttribute(AttributeKey.DroneSensor, minionTemplate.Template.Sensor, false);
        SetAttribute(AttributeKey.DronePiloting, minionTemplate.Template.Piloting, false);
        SetAttribute(AttributeKey.DroneRating, minionTemplate.Template.rating, false);
        SetAttribute(AttributeKey.Body, minionTemplate.Template.Structure,false);
        SetAttribute(AttributeKey.Agility, minionTemplate.Template.Speed, false);
        CalculateCharacteristics();
        equipmentObjects.Add(ItemReference.GetItem(minionTemplate.Template.Weapon.name));
    }

    public void OnLoad()
    {
        if(equipmentObjects == null)
        {
            equipmentObjects = new List<Item>();
        }   
        decompileEquipment(equipmentCode);
    }

    public void OnSave()
    {
        equipmentCode = compileEquipment();
        equipmentObjects = null;
    }
    
    // Dictionaries can't be saved, so stats are saved as indvidual ints and are converted into 
    // a dictionary when the player is created 
    public string[] CompileStats()
    {
        string[] convertedAttributes = new string[100];
        for(int i = 0; i < 100; i++)
        {
            string convertedAttribute = attribues[i].ToString();
            convertedAttributes[i] = convertedAttribute;
        }
        return convertedAttributes;
    }

    public void DecompileStats(string[]newAttributes)
    {
        int[] convertedAttributes = new int[100];
        for(int i = 0; i < 100; i++)
        {
            int convertedInt = 0;
            int.TryParse(newAttributes[i], out convertedInt);
            convertedAttributes[i] = convertedInt;
        }
        this.attribues = convertedAttributes;
    }


    public void decompileEquipment(string[] newequipmentcode)
    {
        equipmentObjects.Clear();
        for( int i = 0; i < newequipmentcode.Length; i++)
        {
            string code = newequipmentcode[i];
            if(!code.Equals("empty"))
            {
                string[] codeDecompiled = code.Split('|');
                //decompile
                string name = codeDecompiled[0];
                int stacks = int.Parse(codeDecompiled[1]);
                string[] upgrades = codeDecompiled[2].Split(',');
                Item newItem = ItemReference.GetItem(name,stacks,upgrades);
                if(newItem != null)
                {
                    if(codeDecompiled.Length > 3)
                    {
                        int clip = int.Parse(codeDecompiled[3]);
                        Weapon castItem = (Weapon) newItem;
                        castItem.SetClip(clip);
                        AddItem(castItem);
                    }
                    else
                    {
                        AddItem(newItem);
                    }
                }
            }
        }
    }  

    public string[] compileEquipment()
    {
        string[] output;
        // an array of 1 should be artifically extended to avoid photon jank
        if(equipmentObjects.Count == 1)
        {
            output = new string[2];
            output[1] = "empty";
        }
        else
        {
            output = new string[equipmentObjects.Count];
        }
        int index = 0;
        foreach(Item item in equipmentObjects)
        {
            string name = item.GetName();
            string stack = "" + item.GetStacks();
            
            //compile
            string code = name + "|" + stack + "|" + item.CompileUpgrades();
            if(item.GetType().Equals(typeof(Weapon)))
            {
                Weapon weaponcast = (Weapon)item;
                int clip = weaponcast.getClip();
                code+="|" + clip;
            }
            //Debug.Log(code);
            output[index] = code;
            index++; 
        }
        return output;
    }

    // Dictionaries can't be saved, so stats are saved as indvidual ints and are converted into 
    // a dictionary when the player is created 
    public int[] GetStats()
    {
        return attribues;
    }

    // Key: Specific stat being modified
    // Value: New value of modified stat
    // updates a stat's value 
    public void SetAttribute(AttributeKey key, int value, bool calculate)
    {
        attribues[(int)key] = value;
        if(calculate)
        {
            CalculateCharacteristics();
        }
    }

    public void SetTalent(TalentKey key, bool value)
    {
        talents[(int)key] = value;
        //enforce TalentRules
        foreach(KeyValuePair<TalentKey, Talent> kvp in TalentReference.getLibraries())
        {
            if(!kvp.Value.CanSelect(this))
            {
                talents[(int)kvp.Key] = false;
            }
        }
    }

    public int GetAttribute(AttributeKey key)
    {
        return attribues[(int)key];
    }

    public int GetOwnerAttribute(AttributeKey key)
    {
        if(isMinion)
        {
            return getOwner().myData.GetAttribute(key);
        }
        return 0;
    }

    public PlayerStats getOwner()
    {
        return PlayerSpawner.IDtoPlayer(ownerID);
    }

    public bool hasTalent(TalentKey key)
    {
        return talents[(int)key];
    }

    public RollResult AbilityCheck(AttributeKey skillKey, int threshold = 0, int modifier = 0)
    {
        return new RollResult(this, skillKey, threshold, modifier);
    }

    // whenever characteristics are set, updates all abilities/stats that are dependent on those conditions
    public void CalculateCharacteristics()
    {
        // Base Initative = reaction + Intuition
        SetAttribute(AttributeKey.InitativeStandard,GetAttribute(AttributeKey.Reaction) + GetAttribute(AttributeKey.Intuition),false);
        // Astral initative = Intuition * 2
        SetAttribute(AttributeKey.InitativeAstral, GetAttribute(AttributeKey.Intuition) * 2, false);
        // Matrix initative = Data processing + Intuition
        SetAttribute(AttributeKey.InitativeMatrix, GetAttribute(AttributeKey.Logic) + GetAttribute(AttributeKey.Intuition),false);
        // Mental Limit = [(Logic x 2) + Intuition + Willpower] / 3
        float MentalLimit = (float)(GetAttribute(AttributeKey.Logic) * 2 + GetAttribute(AttributeKey.Intuition) + GetAttribute(AttributeKey.Willpower));
        MentalLimit /= 3f;
        SetAttribute(AttributeKey.MentalLimit, Mathf.CeilToInt(MentalLimit),false);
        // Physical Limit = [(Strength x 2) + Body + Reaction] / 3 (round up)
        float PhysicalLimit = (float)(GetAttribute(AttributeKey.Strength) * 2 + GetAttribute(AttributeKey.Body) + GetAttribute(AttributeKey.Reaction));
        PhysicalLimit /= 3f;
        SetAttribute(AttributeKey.PhysicalLimit, Mathf.CeilToInt(PhysicalLimit),false);
        // Social Limit = [(Charisma x 2) + Willpower + Essence] / 3 (round up) 
        float SocialLimit = (float)(GetAttribute(AttributeKey.Charisma) * 2 + GetAttribute(AttributeKey.Willpower) + GetAttribute(AttributeKey.Essense));
        SocialLimit /= 3f;
        SetAttribute(AttributeKey.SocialLimit, Mathf.CeilToInt(SocialLimit), false);
        // Health max = [Physical / 2] + 8 
        float healthBonus = (float)(GetAttribute(AttributeKey.Body));
        SetAttribute(AttributeKey.PhysicalHealth, Mathf.CeilToInt(healthBonus) + 8, false);
        // Stun max = [Willpower / 2] + 8 
        float StunBonus = (float)(GetAttribute(AttributeKey.Willpower));
        SetAttribute(AttributeKey.StunHealth, Mathf.CeilToInt(StunBonus) + 8, false);
        SetAttribute(AttributeKey.MoveWalk, GetAttribute(AttributeKey.Agility) + 6,false);
        SetAttribute(AttributeKey.MoveRun, GetAttribute(AttributeKey.MoveWalk) * 2, false);
        // COMPOSURE (CHA + WIL)
        SetAttribute(AttributeKey.Composure, GetAttribute(AttributeKey.Charisma) + GetAttribute(AttributeKey.Willpower),false);
        // JUDGE INTENTIONS (CHA + INT)
        SetAttribute(AttributeKey.JudgeIntentions, GetAttribute(AttributeKey.Charisma) + GetAttribute(AttributeKey.Intuition),false);
        // LIFTING/CARRYING (BOD + STR)
        SetAttribute(AttributeKey.LiftCarry, GetAttribute(AttributeKey.Body) + GetAttribute(AttributeKey.Strength),false);
        // MEMORY (LOG + WIL)
        SetAttribute(AttributeKey.Memory, GetAttribute(AttributeKey.Logic) + GetAttribute(AttributeKey.Willpower),false);
        // DEFENSE (REF + INT)
        SetAttribute(AttributeKey.Defense, GetAttribute(AttributeKey.Reaction) + GetAttribute(AttributeKey.Intuition),false);
        // RECOILCOMP = 1 + S/3
        SetAttribute(AttributeKey.RecoilComp, 1 + Mathf.CeilToInt(GetAttribute(AttributeKey.Strength)/2),false);
        // ESSENSE = 6 - penalties
        SetAttribute(AttributeKey.Essense, 6, false);
    }

    public void setSpecialization(string skillKey, int SpecializationIndex)
    {
        if(!skillSpecialization.ContainsKey(skillKey))
        {
            skillSpecialization.Add(skillKey,0);
        }
        skillSpecialization[skillKey] = SpecializationIndex;
    }

    public bool hasSpecialization(string skillKey, int SpecializationIndex)
    {
        if(!skillSpecialization.ContainsKey(skillKey))
        {
            skillSpecialization.Add(skillKey,0);
        }
        //Debug.Log("my index: " + skillSpecialization[skillKey] + " desired index " + SpecializationIndex);
        return skillSpecialization[skillKey] == SpecializationIndex+1;
    }

    public int GetSpecializationIndex(string skillKey)
    {
        if(!skillSpecialization.ContainsKey(skillKey))
        {
            skillSpecialization.Add(skillKey,0);
        }
        return skillSpecialization[skillKey];
    }

    // Converts talents into readable code for PUN
    public Dictionary<int, bool> CompileTalents()
    {
        Dictionary<int,bool> output = new Dictionary<int, bool>();
        for(int i = 0; i < talents.Length; i++) 
        {
            output.Add(i, talents[i]);
        }
        return output;
    }

    public void DecompileTalents(Dictionary<int, bool> input)
    {
        foreach(KeyValuePair<int,bool> kvp in input)
        {
            talents[kvp.Key] = kvp.Value;
        }
    }

    public void AddItem(Item item)
    {
        // if an item needs to be stacked
        if(item.Stackable())
        {
            // if a player already has one of these items, stack exisiting item and discard the new one
            if(equipmentObjects.Contains(item))
            {
                foreach(Item i in equipmentObjects)
                {
                    if(i.Equals(item))
                    {
                        i.AddStack();
                    }
                }
            }
            else
            {
                equipmentObjects.Add(item);   
            }
        }
        // unstackable items can be directly added to the inventory
        else 
        {
            equipmentObjects.Add(item);
        }
    }

    public Item GetItem(string key)
    {
        ItemTemplate myTemplate = ItemReference.GetTemplate(key);
        foreach(Item item in equipmentObjects)
        {
            if(item.Template == myTemplate)
            {
                return item;
            }
        }
        return null;
    }

    public void RemoveItem(Item item)
    {
        if(!equipmentObjects.Contains(item))
        {
            Debug.Log("Error: Item not found");
        }
        else
        {
            // often item will not be apart of my inventory, in which case we need to actually see our stacks of this item
            Item modifiedItem = item;
            Item[] equipment = equipmentObjects.ToArray();
            for(int i = 0; i < equipmentObjects.Count; i++)
            {
                if(equipment[i].Equals(item))
                {
                    modifiedItem = equipment[i];
                }
            }
            modifiedItem.SubtractStack();
            if(modifiedItem.IsConsumed())
            {
                equipmentObjects.Remove(modifiedItem);
            }
        }
    }

    // Saves my data on to the computer for later play sessions
    public void Quit()
    {
        //UploadSaveData();
        SaveSystem.SavePlayer(this);
    }

    // Depreciated Save method
    /* Not done for NPCs because there can be multiple copies of them
    public void UploadSaveData()
    {
        if(team == 0)
        {
            GameObject[] activePlayers = GameObject.FindGameObjectsWithTag("Player");
            PlayerStats myPlayer = null;
            foreach(GameObject g in activePlayers)
            {
                if(myPlayer == null && g.GetComponent<PlayerStats>().myData == this)
                {
                    myPlayer = g.GetComponent<PlayerStats>();
                }
            }
            /*
            if(myPlayer != null)
            {
                Debug.Log("Found My player");
                this.playername = myPlayer.playername;
                Dictionary<string,int> myStats = myPlayer.Stats;
                foreach(string key in myStats.Keys)
                {
                    SetStat(key, myStats[key]);
                }
                ClearSkills();
                List<Skill> mySkills = myPlayer.Skills;
                foreach(Skill s in mySkills)
                {
                    addSkill(s);
                }
                ClearEquipment();
                AddEquipment(myPlayer.equipment);
            }
            
        }
    }
    */
}
