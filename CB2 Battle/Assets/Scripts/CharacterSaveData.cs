using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Save data for stored players, accessed through the DM menu, used to permanently save characters
[System.Serializable]
public class CharacterSaveData
{
    // Basic character stats
    private int BS = 20;
    private int WS = 20;
    private int S = 20;
    private int T = 20;
    private int A = 20;
    private int INT = 20;
    private int PER = 20;
    private int WP = 20;
    private int FEL = 20;
    private int Wounds = 5;
    private int MaxWounds = 5;
    private int MoveHalf = 0;
    private int MoveFull = 0;
    private int MoveCharge = 0;
    private int MoveRun = 0;
    private int Head = 0;
    private int Body = 0;
    private int RightArm = 0;
    private int LeftArm = 0;
    private int RightLeg = 0;
    private int LeftLeg = 0;
    private int Fatigue = 0;
    private int Critical = 0;
    private int Fate = 0;
    private int FateMax = 0;
    private int Gelt = 0;
    private int Income = 0;
    public int team = 0; 
    public string playername;
    // Name of mesh model
    public string Model;
    // Each entry is a unique skill name for the skillreference
    
    private string[] SkillNames = new string[50];
    // Each entry is the levels the player has in the skill of the same index
    private int[] SkillLevels = new int[50]; 
    // Each entry is a unique skill name for the itemReference
    public string[] equipment = new string[20];
    // Each entry is the number of each equipment piece a player has of the same index
    public int[] equipmentSize = new int[20];
    
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
        BasicSkills();
        StandardHitLocations();
    }

    // Saves a set of basic skills every player will have to SkillNames and SkillLevels
    private void BasicSkills()
    {
        Dictionary<string, SkillTemplate> templates = SkillReference.SkillsTemplates(); 
        foreach (string key in templates.Keys)
        {
            // Advanced skills are excluded
            if(templates[key].basic)
            {
                // By default all skills are level 0, untrained
                addSkill(new Skill(templates[key], 0));
            }
        }
    }

    // Determines what a body part is hit depending on the hit result, same for all humanoids
    // To implement: new hitlocations for monsters/vehicles
    public Dictionary<int,string> StandardHitLocations()
    {
        Dictionary<int,string> HitLocations = new Dictionary<int, string>();
        HitLocations.Add(10, "Head");
        HitLocations.Add(20, "RightArm");
        HitLocations.Add(30, "LeftArm");
        HitLocations.Add(70, "Body");
        HitLocations.Add(85, "RightLeg");
        HitLocations.Add(100, "LeftLeg");
        return HitLocations;
    }

    // Dictionaries can't be saved, so stats are saved as indvidual ints and are converted into 
    // a dictionary when the player is created 
    public Dictionary<string,int> GetStats()
    {
        Dictionary<string, int> output = new Dictionary<string, int>();
        output.Add("BS",BS);
        output.Add("WS",WS);
        output.Add("S",S);
        output.Add("T",T);
        output.Add("A",A);
        output.Add("INT",INT);
        output.Add("PER",PER);
        output.Add("WP",WP);
        output.Add("FEL",FEL);
        output.Add("Wounds",Wounds);
        output.Add("MaxWounds",MaxWounds);
        output.Add("MoveHalf",MoveHalf);
        output.Add("MoveFull",MoveFull);
        output.Add("MoveCharge",MoveCharge);
        output.Add("MoveRun",MoveRun);
        output.Add("Head",Head);
        output.Add("RightArm",RightArm);
        output.Add("LeftArm",LeftArm);
        output.Add("Body",Body);
        output.Add("RightLeg",RightLeg);
        output.Add("LeftLeg",LeftLeg);
        output.Add("Critical",Critical);
        output.Add("Fatigue", Fatigue);
        output.Add("Fate",Fate);
        output.Add("FateMax",FateMax);
        output.Add("Gelt",Gelt);
        output.Add("Income",Income);
        return output;
    }

    // Key: Specific stat being modified
    // Value: New value of modified stat
    // updates a stat's value 
    public void SetStat(string key, int value)
    {
        switch (key)
        {
            case "BS":
            BS = value;
            break;
            case "WS":
            WS = value;
            break;
            case "S":
            S = value;
            break;
            case "T":
            T = value;
            break;
            case "A":
            A = value;
            break;
            case "INT":
            INT = value;
            break;
            case "PER":
            PER = value;
            break;
            case "WP":
            WP = value;
            break;
            case "FEL":
            FEL = value;
            break;
            case "Wounds":
            Wounds = value;
            break;
            case "MaxWounds":
            MaxWounds = value;
            break;
            case "MoveHalf":
            MoveHalf = value;
            break;
            case "MoveFull":
            MoveFull = value;
            break;
            case "MoveCharge":
            MoveCharge = value;
            break;
            case "MoveRun":
            MoveRun = value;
            break;
            case "Head":
            Head = value;
            break;
            case "RightArm":
            RightArm = value;
            break;
            case "LeftArm":
            LeftArm = value;
            break;
            case "Body":
            Body = value;
            break;
            case "RightLeg":
            RightLeg = value;
            break;
            case "LeftLeg":
            LeftLeg = value;
            break;
            case "Fatigue":
            Fatigue = value;
            break;
            case "Critical":
            Critical = value;
            break;
            case "Fate":
            Fate = value;
            break;
            case "FateMax":
            FateMax = value;
            break;
            case "Gelt":
            Gelt = value;
            break;
            case "Income":
            Income = value;
            break;
        }
    }

    // Converts SkillNames and Skill Levels into a list of skill objects that playerstats can read
    public List<Skill> GetSkills()
    {
        List<Skill> output = new List<Skill>();
        for(int i = 0; i < 50; i++)
        {
            if(SkillNames[i] != null)
            {
                output.Add( new Skill(SkillReference.GetSkill(SkillNames[i]),SkillLevels[i]));
            }
        }
        return output;
    }

    // Resets all Skills, used when charactersheet is editing skills
    public void ClearSkills()
    {
        SkillNames = new string[50];
        SkillLevels = new int[50];
    }

    // Skill: an specific skill object
    // Converts object into back into basic structures 
    public void addSkill(Skill input)
    {
        int newindex = 1;
        while(newindex < 50 && SkillNames[newindex] != null)
        {
             newindex++;
        }
        //if there is space
        if(SkillNames[newindex] == null)
        {
            SkillNames[newindex] = input.name;
            SkillLevels[newindex] = input.levels;
        }
    }

    // Converts Equipment and EquipmentSize into a readable list of Item objects
    public List<Item> GetEquipment()
    {
        List<Item> output = new List<Item>();
        for(int i = 0; i < 8; i++)
        {
            if(equipment[i] != null)
            {
                // specific types of items have to be created seperately
                Item current = null;
                if(ItemReference.ItemTemplates()[equipment[i]].GetType() == typeof(WeaponTemplate))
                {
                    current = new Weapon((WeaponTemplate)ItemReference.ItemTemplates()[equipment[i]]);
                }
                else if(ItemReference.ItemTemplates()[equipment[i]].GetType() == typeof(ArmorTemplate))
                {
                    current = new Armor((ArmorTemplate)ItemReference.ItemTemplates()[equipment[i]]);
                }
                else
                {
                    current = new Item(ItemReference.GetItem(equipment[i]));
                }
                current.SetStack(equipmentSize[i]);
                output.Add(current);
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
}
