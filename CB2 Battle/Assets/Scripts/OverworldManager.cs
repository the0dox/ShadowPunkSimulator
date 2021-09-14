using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldManager : MonoBehaviour
{
    public static Dictionary<string, PlayerStats> Party = new Dictionary<string, PlayerStats>();
    [SerializeField] private GameObject ButtonReference;
    private static GameObject ActionUIButton;
    private static bool FF = false;
    void Start()
    {
        ActionUIButton = ButtonReference;
    }

    public static void AddPlayer(GameObject newPlayer)
    {
        PlayerStats myStats = newPlayer.GetComponent<PlayerStats>();
        Party.Add(myStats.GetName(), myStats);
        //transform player object into a draggable overworld piece with no health bar
        newPlayer.AddComponent<OverworldMovement>();
        newPlayer.GetComponentInChildren<HealthBar>().ToggleBar();
        //update player options 
        ConstructActions();
    }

    public static void ConstructActions()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        foreach(string playerName in Party.Keys)
        {
            d.Add(playerName,playerName);
        }
        d.Add("Fast Forward","FastForward");
        ConstructActions(d);
    }

    //creates a set of interactable buttons can key = text value = method called
    public static void ConstructActions(Dictionary<string, string> d)
    {
        GameObject[] oldButtons = GameObject.FindGameObjectsWithTag("ActionInput");
        foreach(GameObject g in oldButtons)
        {
            g.GetComponent<ActionButtonScript>().DestroyMe();
        }
        if(d != null)
        {
            int displacement = 0;
            foreach (KeyValuePair<string, string> kvp in d)
            {
                GameObject newButton = Instantiate(ActionUIButton) as GameObject;
                newButton.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
                newButton.transform.position += new Vector3(displacement,0,0);
                newButton.GetComponent<ActionButtonScript>().SetAction(kvp.Value);
                newButton.GetComponent<ActionButtonScript>().SetText(kvp.Key);
                displacement += 150;
            }
        }
    }

    public void OnButtonPressed(string input)
    {
        Invoke(input,0);
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
