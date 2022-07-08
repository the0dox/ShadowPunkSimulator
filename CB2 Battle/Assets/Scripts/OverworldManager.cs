using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class OverworldManager : UIButtonManager
{
    public static Dictionary<string, PlayerStats> Party = new Dictionary<string, PlayerStats>();
    [SerializeField] private GameObject ButtonReference;
    private static GameObject ActionUIButton;
    private static bool FF = false;
    void Start()
    {
        ActionUIButton = ButtonReference;
        ConstructActions();
    }

    public static void AddPlayer(GameObject newPlayer)
    {
        PlayerStats myStats = newPlayer.GetComponent<PlayerStats>();
        Party.Add(myStats.GetName(), myStats);
        //transform player object into a draggable overworld piece with no health bar
        newPlayer.GetComponent<PlayerStats>().OverworldInit();
    }

    public void ConstructActions()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        foreach(string playerName in Party.Keys)
        {
            d.Add(playerName,playerName);
        }
        //d.Add("Fast Forward","FastForward");
        ConstructActions(d);
    }

    public void FastForward()
    {
        if(!FF)
        {
            FF = true;
            GameObject[] Leads = GameObject.FindGameObjectsWithTag("Lead");
            List<LeadScript> input = new List<LeadScript>();
            foreach(GameObject g in Leads)
            {
                input.Add(g.GetComponent<LeadScript>());
            }
            StartCoroutine(advancingTime(input));
        }
        else
        {
            FF = false;
        }
    }

    IEnumerator advancingTime(List<LeadScript> leads)
    {
        while(ContinueIenumerator(leads))
        {
            yield return new WaitForSeconds (1f);
        }
    }

    public static void SaveOverworld()
    {
        Party.Clear();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] leads = GameObject.FindGameObjectsWithTag("Lead");
        Dictionary<Vector3, GameObject> OverworldTokens = new Dictionary<Vector3, GameObject>();
        List<LeadScript> leadScripts = new List<LeadScript>();
        foreach(GameObject p in players)
        {
            OverworldTokens.Add(p.transform.position, p);
        }
        foreach(GameObject g in leads)
        {
            leadScripts.Add(g.GetComponent<LeadScript>());
        }
        Material groundMat = TileGround.GetMaterial();
        SceneSaveData overworldScene = new SceneSaveData("Overworld",OverworldTokens,groundMat);
        overworldScene.AddLeads(leadScripts);
        SaveSystem.SaveScene(overworldScene);
        Debug.Log("scene saved with " + players.Length + " tokens and " + leads.Length + " leads ");
    }

    private bool ContinueIenumerator(List<LeadScript> leads)
    {
        if(!FF)
        {
            return false;
        }
        bool ouput = true;
        foreach(LeadScript ls in leads)
        {
            ls.IncrementTime();
            if(ls.Completed())
            {
                ouput = false;
                ls.Delete();
                FF = false;
            }
        }
        return ouput;
    }
}
