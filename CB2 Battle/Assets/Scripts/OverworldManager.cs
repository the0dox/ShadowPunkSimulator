using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        newPlayer.AddComponent<OverworldMovement>();
        newPlayer.GetComponentInChildren<HealthBar>().ToggleBar();
    }

    public void ConstructActions()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        foreach(string playerName in Party.Keys)
        {
            d.Add(playerName,playerName);
        }
        d.Add("Fast Forward","FastForward");
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

    private bool ContinueIenumerator(List<LeadScript> leads)
    {
        if(!FF)
        {
            return false;
        }
        bool ouput = true;
        foreach(LeadScript ls in leads)
        {
            ls.IncrementTime(1);
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
