using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CharacterSaveData
{
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
    private string[] SkillNames = new string[50];
    private int[] SkillLevels = new int[50];
    private string[] SkillChars = new string[50];
    private bool[] SkillBasic = new bool[50];
    public int team = 0; 
    public string playername; 
    public string[] weapons = new string[8];
    public CharacterSaveData(bool playable)
    {
        if(playable)
        {
            playername = "New Player";
            team = 0;
        }
        else
        {
            playername = "New NPC";
            team = 1;
        }
        BasicSkills();
        StandardHitLocations();
    }
    
    private void BasicSkills()
    {
        SkillNames[0] = "Parry";
        SkillLevels[0] = 0;
        SkillChars[0] = "WS";
        SkillBasic[0] = true;

        SkillNames[1] = "Awareness";
        SkillLevels[1] = 0;
        SkillChars[1] = "PER";
        SkillBasic[1] = true;
        
        SkillNames[3] = "Barter";
        SkillLevels[3] = 0;
        SkillChars[3] = "FEL";
        SkillBasic[3] = true;

        SkillNames[4] = "Carouse";
        SkillLevels[4] = 0;
        SkillChars[4] = "T";
        SkillBasic[4] = true;
        
        SkillNames[5] = "Charm";
        SkillLevels[5] = 0;
        SkillChars[5] = "FEL";
        SkillBasic[5] = true;
        
        SkillNames[6] = "Concealment";
        SkillLevels[6] = 0;
        SkillChars[6] = "A";
        SkillBasic[6] = true;
        
        SkillNames[7] = "Contortionist";
        SkillLevels[7] = 0;
        SkillChars[7] = "A";
        SkillBasic[7] = true;

        SkillNames[8] = "Deceive";
        SkillLevels[8] = 0;
        SkillChars[8] = "FEL";
        SkillBasic[8] = true;

        SkillNames[9] = "Disguise";
        SkillLevels[9] = 0;
        SkillChars[9] = "FEL";
        SkillBasic[9] = true;
        
        SkillNames[10] = "Dodge";
        SkillLevels[10] = 0;
        SkillChars[10] = "A";
        SkillBasic[10] = true;
        
        SkillNames[11] = "Evaluate";
        SkillLevels[11] = 0;
        SkillChars[11] = "INT";
        SkillBasic[11] = true;

        SkillNames[12] = "Gamble";
        SkillLevels[12] = 0;
        SkillChars[12] = "INT";
        SkillBasic[12] = true;
        
        SkillNames[13] = "Inquiry";
        SkillLevels[13] = 0;
        SkillChars[13] = "FEL";
        SkillBasic[13] = true;

        SkillNames[14] = "Intimidate";
        SkillLevels[14] = 0;
        SkillChars[14] = "S";
        SkillBasic[14] = true;

        SkillNames[15] = "Logic";
        SkillLevels[15] = 0;
        SkillChars[15] = "INT";
        SkillBasic[15] = true;
        
        SkillNames[16] = "Climb";
        SkillLevels[16] = 0;
        SkillChars[16] = "S";
        SkillBasic[16] = true;
        
        SkillNames[17] = "Scrutiny";
        SkillLevels[17] = 0;
        SkillChars[17] = "PER";
        SkillBasic[17] = true;
        
        SkillNames[18] = "Search";
        SkillLevels[18] = 0;
        SkillChars[18] = "PER";
        SkillBasic[18] = true;
        
        SkillNames[19] = "SilentMove";
        SkillLevels[19] = 0;
        SkillChars[19] = "A";
        SkillBasic[19] = true;

        SkillNames[20] = "Swim";
        SkillLevels[20] = 0;
        SkillChars[20] = "S";
        SkillBasic[20] = true;
        
        /*
        Skills = new List<Skill>();
        Skills.Add(new Skill("Parry",0,"WS",true));
        Skills.Add(new Skill("Awareness",0,"PER",true));
        Skills.Add(new Skill("Barter",0,"FEL",true));
        Skills.Add(new Skill("Carouse",0,"T",true));
        Skills.Add(new Skill("Charm",0,"FEL",true));
        Skills.Add(new Skill("Concealment",0,"A",true));
        Skills.Add(new Skill("Contortionist",0,"A",true));
        Skills.Add(new Skill("Deceive",0,"FEL",true));
        Skills.Add(new Skill("Disguise",0,"FEL",true));
        Skills.Add(new Skill("Dodge",0,"A",true));
        Skills.Add(new Skill("Evaluate",0,"INT",true));
        Skills.Add(new Skill("Gamble",0,"INT",true));
        Skills.Add(new Skill("Inquiry",0,"FEL",true));
        Skills.Add(new Skill("Intimidate",0,"S",true));
        Skills.Add(new Skill("Logic",0,"INT",true));
        Skills.Add(new Skill("Climb",0,"S",true));
        Skills.Add(new Skill("Scrutiny",0,"PER",true));
        Skills.Add(new Skill("Search",0,"PER",true));
        Skills.Add(new Skill("SilentMove",0,"A",true));
        Skills.Add(new Skill("Swim",0,"S",true));
        */
    }
    public Dictionary<int,string> StandardHitLocations()
    {
        Dictionary<int,string> HitLocations = new Dictionary<int, string>();
        HitLocations.Add(10, "Head");
        HitLocations.Add(20, "Right Arm");
        HitLocations.Add(30, "Left Arm");
        HitLocations.Add(70, "Body");
        HitLocations.Add(85, "Right Leg");
        HitLocations.Add(100, "Left Leg");
        return HitLocations;
    }

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
        return output;
    }

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
        }
    }

    public List<Skill> GetSkills()
    {
        List<Skill> output = new List<Skill>();
        for(int i = 0; i < 50; i++)
        {
            if(SkillNames[i] != null)
            {
                output.Add(new Skill(SkillNames[i],SkillLevels[i],SkillChars[i],SkillBasic[i]));
            }
        }
        return output;
    }

    public void ClearSkills()
    {
        SkillNames = new string[50];
        SkillLevels = new int[50];
        SkillChars = new string[50];
        SkillBasic = new bool[50];
    }

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
            SkillChars[newindex] = input.characterisitc;
            SkillBasic[newindex] = input.basic;
        }
    }

    public List<Weapon> GetWeapons()
    {
        List<Weapon> output = new List<Weapon>();
        for(int i = 0; i < 8; i++)
        {
            if(weapons[i] != null)
            {
                output.Add(new Weapon(WeaponsReference.GetWeapon(weapons[i])));
            }
        }
        return output;
    }

    public void ClearWeapons()
    {
        weapons = new string[8];
    }

    public void AddWeapons(List<Weapon> input)
    {
        ClearWeapons();
        int newIndex = 0;
        foreach(Weapon w in input)
        {
            weapons[newIndex] = w.GetName();
            newIndex++;
        }
    }

    public void Quit()
    {
        UploadSaveData();
        SaveSystem.SavePlayer(this);
    }

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
                ClearWeapons();
                AddWeapons(myPlayer.equipment);
            }
        }
    }
}
