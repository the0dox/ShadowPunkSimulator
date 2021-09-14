using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeadScript : MonoBehaviour
{
    [SerializeField] private float MaxHours;
    [SerializeField] private float CompletedHours = 0;
    private Vector3 dividerPos = new Vector3(0,-3.5f,0);
    [SerializeField] private GameObject DividerImage;
    [SerializeField] private Text TextTime;
    [SerializeField] private Image pieChart;
    private PlayerStats[] players;
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject player3;
    private Dictionary<string, int> timeReference = new Dictionary<string, int>
    {
        {"Simple",1},
        {"Basic",6},
        {"Drudging",24},
        {"Taxing",72},
        {"Arduous",264},
        {"Involved",744},
        {"Labyrinthine",8640}
    };
    private Dictionary<string, int> modifierReference = new Dictionary<string, int>
    {
        {"Simple",30},
        {"Basic",20},
        {"Drudging",10},
        {"Taxing",0},
        {"Arduous",-10},
        {"Involved",20},
        {"Labyrinthine",30}
    };

    void Update()
    {
        float difference = MaxHours - CompletedHours;
        if(difference < 0)
        {
            CompletedHours = MaxHours;
        } 
        TextTime.text = (MaxHours - CompletedHours) + " hours remaining";
        pieChart.fillAmount = (CompletedHours/MaxHours);
    }
    public void UpdateLead(string Difficultly, PlayerStats[] players, string skill)
    {
        this.players = players;
        MaxHours = timeReference[Difficultly];
        PlayerStats mainPlayer = players[0];
        player1.SetActive(true);
        player1.GetComponentInChildren<Text>().text = mainPlayer.GetName();
        mainPlayer.StartJob();
        int modifier = modifierReference[Difficultly];
        for(int i = 1; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                Debug.Log(players[i].GetName() + " assists and gives a + 10 Bonus!");
                modifier += 10;
                players[i].StartJob();
                if(!player2.activeInHierarchy)
                {
                    player2.SetActive(true);
                    player2.GetComponentInChildren<Text>().text = players[i].GetName();
                }
                else
                {
                    player3.SetActive(true);
                    player3.GetComponentInChildren<Text>().text = players[i].GetName();
                }
            }
        }
        RollResult LeadResult = mainPlayer.AbilityCheck(skill,modifier);
        if(LeadResult.Passed())
        {
            int extrahours = Random.Range(1,10);
            string usedStat = SkillReference.GetSkill(skill).characterisitc;
            extrahours += mainPlayer.GetStatScore(usedStat);
            CombatLog.Log(mainPlayer.GetName() + " succedes on their check and makes 1d10 + " + usedStat + " score (" + extrahours + ") hours of progress!");
            CompletedHours += extrahours;
        }
    }
    public void Init()
    {
        float angleIncrementer = -360f/(float)MaxHours;
        for(int i = 0; i < MaxHours; i++)
        {
            GameObject newLine = Instantiate(DividerImage) as GameObject;
            newLine.transform.SetParent(gameObject.transform);
            newLine.transform.localPosition = dividerPos;
            newLine.transform.Rotate(0,0, i * angleIncrementer, Space.Self);
        }
    }

    public bool Completed()
    {
        return CompletedHours >= MaxHours;
    }

    public void IncrementTime(float hours)
    {
        CompletedHours += hours;
    }

    public void Delete()
    {
        for(int i = 0;i<players.Length;i++)
        {
            if(players[i] != null)
            {
                players[i].EndJob();
            }
        }
        ActionQueueDisplay.RemoveActivity(this.gameObject);
    }
}
