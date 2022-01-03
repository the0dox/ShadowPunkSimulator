using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Save data for stored players, accessed through the DM menu, used to permanently save characters
[System.Serializable]
public class CharacterSaveData
{
    // Basic character stats Shadowrunner
    public Dictionary<string, int> attribues = new Dictionary<string, int>(); 

    // Skills Shadowrunner
    public Dictionary<string, int> skills = new Dictionary<string, int>();
    
    // Skills Shadowrunner
    public Dictionary<string, int> skillSpecialization = new Dictionary<string, int>();
    // non ttrpg stats used for game logic
    public int team = 0; 
    public string playername;
    // Name of mesh model
    public string Model;
    // Each entry is a unique skill name for the itemReference
    public string[] equipment = new string[20];
    // Each entry is the number of each equipment piece a player has of the same index
    public int[] equipmentSize = new int[20];
    // Condensed list of equipment and there properties, only modified on load and save
    public List<string> equipmentCode = new List<string>(); 
    public List<Item> equipmentObjects = new List<Item>();
    
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
        SetAttribute(AttributeKey.PDamage, 0,false);
        SetAttribute(AttributeKey.SDamage, 0,false);
        SetAttribute(AttributeKey.CurrentEdge, 0, false);
        SetAttribute(AttributeKey.Overflow, 0, false);
        CalculateCharacteristics();
    }

    public CharacterSaveData(string playername, Dictionary<string,int> attribues, Dictionary<string,int> skills, Dictionary<string,int> specalizations, List<string> newequipment)
    {
        this.playername = playername;
        this.attribues = attribues;
        this.skills = skills;
        this.skillSpecialization = specalizations;
        decompileEquipment(newequipment);
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

    public void decompileEquipment(List<string> newequipmentcode)
    {
        foreach(string code in newequipmentcode)
        {
            string[] codeDecompiled = code.Split('|');
            //decompile
            string name = codeDecompiled[0];
            int stacks = int.Parse(codeDecompiled[1]);
            string[] upgrades = codeDecompiled[2].Split(',');
            Item newItem = ItemReference.GetItem(name,stacks,upgrades);
            if(codeDecompiled.Length > 3)
            {
                int clip = int.Parse(codeDecompiled[3]);
                Weapon castItem = (Weapon) newItem;
                castItem.SetClip(clip);
            } 
            AddItem(newItem);
        }
    }  

    public List<string> compileEquipment()
    {
        List<string> output = new List<string>();
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
            Debug.Log(code);
            output.Add(code); 
        }
        return output;
    }

    // Dictionaries can't be saved, so stats are saved as indvidual ints and are converted into 
    // a dictionary when the player is created 
    public Dictionary<string,int> GetStats()
    {
        return attribues;
    }

    // Key: Specific stat being modified
    // Value: New value of modified stat
    // updates a stat's value 
    public void SetAttribute(string key, int value, bool calculate)
    {
        if(!attribues.ContainsKey(key))
        {
            attribues.Add(key,0);
        }
        attribues[key] = value;
        if(calculate)
        {
            CalculateCharacteristics();
        }
    }

    public void SetSkill(string key, int value)
    {
        if(!skills.ContainsKey(key))
        {
            skills.Add(key,0);
        }
        skills[key] = value;
        setSpecialization(key, 0);
    }

    public int GetAttribute(string key)
    {
        if(!attribues.ContainsKey(key))
        {
            attribues.Add(key,0);
        }
        return attribues[key];
    }

    public int GetSkill(string key, bool derrived)
    {
        if(!skills.ContainsKey(key))
        {
            skills.Add(key, 0);
        }
        int levels = skills[key];
        if(!derrived)
        {
            return levels;
        }
        int bonus = GetAttribute(SkillReference.GetDerrivedAttribute(key));
        if(levels > 0)
        {
            return levels + bonus;
        }
        else if(SkillReference.Defaultable(key))
        {
            return bonus - 1;
        }
        else
        {
            return 0;
        }
    }

    public RollResult AbilityCheck(string skillKey, string attributeKey = "", string LimitKey = "", int threshold = 0, int modifier = 0)
    {
        return new RollResult(this, skillKey, attributeKey, LimitKey, threshold, modifier);
    }

    // Converts SkillNames and Skill Levels into a list of skill objects that playerstats can read
    public Dictionary<string,int> GetSkills()
    {
        return skills;
    }

    // Resets all Skills, used when charactersheet is editing skills
    public void ClearSkills()
    {
        skills.Clear();
        skillSpecialization.Clear();
    }

    // whenever characteristics are set, updates all abilities/stats that are dependent on those conditions
    public void CalculateCharacteristics()
    {
        // Base Initative = reaction + Intuition
        SetAttribute(AttributeKey.InitativeStandard,GetAttribute(AttributeKey.Reaction) + GetAttribute(AttributeKey.Intuition),false);
        // Astral initative = Intuition * 2
        SetAttribute(AttributeKey.InitativeAstral, GetAttribute(AttributeKey.Intuition) * 2, false);
        // Matrix initative = Data processing + Intuition
        // SetAttribute(AttributeKey.InitativeMatrix, Dataprocessing + GetAttribute(AttributeKey.Intuition));
        // Mental Limit = [(Logic x 2) + Intuition + Willpower] / 3
        float MentalLimit = (float)(GetAttribute(AttributeKey.Logic) * 2 + GetAttribute(AttributeKey.Intuition) + GetAttribute(AttributeKey.Willpower));
        MentalLimit /= 3f;
        SetAttribute(AttributeKey.MentalLimit, Mathf.CeilToInt(MentalLimit),false);
        // Physical Limit = [(Strength x 2) + Body + Reaction] / 3 (round up)
        float PhysicalLimit = (float)(GetAttribute(AttributeKey.Strength) * 2 + GetAttribute(AttributeKey.Body) + GetAttribute(AttributeKey.Reaction));
        PhysicalLimit /= 3f;
        Debug.Log("phslim= " + PhysicalLimit);
        SetAttribute(AttributeKey.PhysicalLimit, Mathf.CeilToInt(PhysicalLimit),false);
        // Social Limit = [(Charisma x 2) + Willpower + Essence] / 3 (round up) 
        float SocialLimit = (float)(GetAttribute(AttributeKey.Charisma) * 2 + GetAttribute(AttributeKey.Willpower) + GetAttribute(AttributeKey.Essense));
        SocialLimit /= 3f;
        SetAttribute(AttributeKey.SocialLimit, Mathf.CeilToInt(SocialLimit), false);
        // Health max = [Physical / 2] + 8 
        float healthBonus = (float)(GetAttribute(AttributeKey.Body)) / 2f;
        SetAttribute(AttributeKey.PhysicalHealth, Mathf.CeilToInt(healthBonus) + 8, false);
        // Stun max = [Willpower / 2] + 8 
        float StunBonus = (float)(GetAttribute(AttributeKey.Willpower)) / 2f;
        SetAttribute(AttributeKey.StunHealth, Mathf.CeilToInt(StunBonus) + 8, false);
        SetAttribute(AttributeKey.MoveWalk, GetAttribute(AttributeKey.Agility) * 2,false);
        SetAttribute(AttributeKey.MoveRun, GetAttribute(AttributeKey.Agility) * 4, false);
        // COMPOSURE (CHA + WIL)
        SetAttribute(AttributeKey.Composure, GetAttribute(AttributeKey.Agility) + GetAttribute(AttributeKey.Willpower),false);
        // JUDGE INTENTIONS (CHA + INT)
        SetAttribute(AttributeKey.JudgeIntentions, GetAttribute(AttributeKey.Charisma) + GetAttribute(AttributeKey.Intuition),false);
        // LIFTING/CARRYING (BOD + STR)
        SetAttribute(AttributeKey.LiftCarry, GetAttribute(AttributeKey.Body) + GetAttribute(AttributeKey.Strength),false);
        // MEMORY (LOG + WIL)
        SetAttribute(AttributeKey.Memory, GetAttribute(AttributeKey.Logic) + GetAttribute(AttributeKey.Willpower),false);
    }

    public void setSpecialization(string skillKey, int SpecializationIndex)
    {
        if(!skillSpecialization.ContainsKey(skillKey))
        {
            skillSpecialization.Add(skillKey,0);
        }
        skillSpecialization[skillKey] = SpecializationIndex;
    }



    // Converts Equipment and EquipmentSize into a readable list of Item objects
    public Dictionary<string,int> GetEquipment()
    {
        Dictionary<string,int> output = new Dictionary<string, int>();
        for(int i = 0; i < 8; i++)
        {
            if(equipment[i] != null)
            {
                if(!output.ContainsKey(equipment[i]))
                {
                    output.Add(equipment[i],equipmentSize[i]);
                }
                else
                {
                    output[equipment[i]] += equipmentSize[i];
                }
            }
        }
        return output;
    }

    // Resets all Items, used when charactersheet is editing equipment
    public void ClearEquipment()
    {
        equipment = new string[20];
        equipmentSize = new int[20];
    }

    // Takes a series of Gameobjects and converts them back into basic datastructures
    public void AddEquipment(Dictionary<string,int> input)
    {
        ClearEquipment();
        int newIndex = 0;
        foreach(KeyValuePair<string,int> kvp in input)
        {
            equipment[newIndex] = kvp.Key;
            equipmentSize[newIndex] = kvp.Value;
            newIndex++;
        }
    }
    public void AddEquipment(List<Item> input)
    {
        ClearEquipment();
        int newIndex = 0;
        foreach(Item i in input)
        {
            equipment[newIndex] = i.GetName();
            equipmentSize[newIndex] = i.GetStacks();
            newIndex++;
        }
    }

    public void AddItem(Item item)
    {
        // if this object is already owned by the player
        if(item.Stackable())
        {
            if(equipmentObjects.Contains(item))
            {
                item.AddStack();
            }
            else
            {
                Item myItem = GetItem(item.Template.name);
                if(myItem != null)
                {
                    myItem.AddStack();
                }
                else
                {
                    equipmentObjects.Add(item);   
                }
            }
        }
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
            item.SubtractStack();
            if(item.IsConsumed())
            {
                equipmentObjects.Remove(item);
            }
        }
    }

    // Saves my data on to the computer for later play sessions
    public void Quit()
    {
        UploadSaveData();
        SaveSystem.SavePlayer(this);
    }

    // If this is a player character with a single map token, save the stats of the map token instead
    // Not done for NPCs because there can be multiple copies of them
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
            */
        }
    }
}
