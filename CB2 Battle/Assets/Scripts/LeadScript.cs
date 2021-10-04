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
    [SerializeField] private InputField Title;
    [SerializeField] private Text TextTime;
    [SerializeField] private Image pieChart;
    private PlayerStats[] players;
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject player3;

    void Update()
    {
        int difference = (int)(MaxHours - CompletedHours);
        if(difference / 24 > 0)
        {
            TextTime.text = (difference / 24) + " days remaining";
        }
        else
        {
            TextTime.text = (MaxHours - CompletedHours) + " hours remaining";
        }
        pieChart.fillAmount = (CompletedHours/MaxHours);
    }
    public void UpdateLead(int modifier, int time, PlayerStats[] players, string skill, string name)
    {
        Title.text = name;
        this.players = players;
        MaxHours = time;
        PlayerStats mainPlayer = players[0];
        player1.SetActive(true);
        player1.GetComponentInChildren<Text>().text = mainPlayer.GetName();
        mainPlayer.StartJob();
        for(int i = 1; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                CombatLog.Log(players[i].GetName() + " assists and gives a + 10 Bonus!");
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
            extrahours += LeadResult.GetDOF();
            CombatLog.Log("Each degree of success (" + LeadResult.GetDOF() + ") counts for one more hour of progress!");
            CompletedHours += extrahours;
        }
    }

    public void SetLead( string name, float progress, float time)
    {
        Debug.Log(name);
        Title.text = name;
        this.MaxHours = time;
        this.CompletedHours = progress;
    }

    //depreciated
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

    public float GetCompletedHours()
    {
        return CompletedHours;
    }

    public float GetMaxHours()
    {
        return MaxHours;
    }

    public string getName()
    {
        return Title.text;
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
