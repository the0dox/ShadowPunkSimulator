using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneSaveData
{
    private Dictionary<string,string> TileLocations = new Dictionary<string, string>();
    private Dictionary<string,string> PlayerLocations = new Dictionary<string, string>();
    public string[] LeadName = new string[5];
    public float[] LeadProgress = new float[5];
    public float[] LeadMax = new float[5];
    private string name;
    private bool Overworld = false;

    public SceneSaveData (string name, Dictionary<Vector3,GameObject> input)
    {
        this.name = name;
        foreach(Vector3 pos in input.Keys)
        {
            GameObject entity = input[pos];
            string PosString = pos.x + ", " + pos.y + ", " + pos.z;
            if(entity.tag.Equals("Tile"))
            {
                TileLocations.Add(PosString,entity.name.Split('(')[0]);
                //Debug.Log(entity.name.Split('(')[0] + ": " + entity.name + "x: " + PosString.Split(',')[0] + "y: " + PosString.Split(',')[1] + "z: " + PosString.Split(',')[2]);
            }
            else if(entity.tag.Equals("Player"))
            {
                PlayerLocations.Add(PosString,entity.GetComponent<PlayerStats>().playername);
                //Debug.Log(PosString + ": " + entity.GetComponent<PlayerStats>().playername);
            }
        }
    }

    public void AddLeads(List<LeadScript> leads)
    {
        int index = 0;
        Overworld = true;
        foreach(LeadScript ls in leads)
        {
            if(index < LeadName.Length)
            {
                LeadName[index] = ls.getName();
                LeadMax[index] = ls.GetMaxHours();
                LeadProgress[index] = ls.GetCompletedHours();
            }
            index++;
        }
    }

    public Dictionary<string,string> GetTileLocations()
    {
        return TileLocations;
    }

    /*
    public Dictionary<Vector3,GameObject> GetTileLocations()
    {
        Dictionary<Vector3,GameObject> output = new Dictionary<Vector3, GameObject>();
        foreach(string posKey in TileLocations.Keys)
        {
            string[] posSplit = posKey.Split(',');
            Vector3 pos = new Vector3(float.Parse(posSplit[0]),float.Parse(posSplit[1]),float.Parse(posSplit[2]));
            GameObject Tile = TileReference.Tile(TileLocations[posKey]);
            output.Add(pos,Tile);
        }
        return output;
    }
    */

    public Dictionary<Vector3,string> GetPlayerLocations()
    {
        Dictionary<Vector3,string> output = new Dictionary<Vector3, string>();
        foreach(string posKey in PlayerLocations.Keys)
        {
            string[] posSplit = posKey.Split(',');
            Vector3 pos = new Vector3(float.Parse(posSplit[0]),float.Parse(posSplit[1]),float.Parse(posSplit[2]));
            output.Add(pos,PlayerLocations[posKey]);
        }
        return output;
    }

    public string GetName()
    {
        return name;
    }

    public bool isOverworld()
    {
        return Overworld;
    }
}
