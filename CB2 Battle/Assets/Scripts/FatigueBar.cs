using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FatigueBar : MonoBehaviour
{
    private Image Bar;
    public float currentFatigue;
    private float maxFatigue = 10;  
    public PlayerStats player; 
    // Start is called before the first frame update
    void Start()
    {
        Bar = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation *- Vector3.back, Camera.main.transform.rotation *- Vector3.down);
        currentFatigue = player.GetStat("Fatigue");
        maxFatigue = player.GetStatScore("T");
        float difference = maxFatigue - currentFatigue;
        if(difference == maxFatigue)
        {
            Bar.enabled = false;
        }
        else
        {
            Bar.enabled = true;
        }
        Bar.fillAmount = difference / maxFatigue;  
    }
}
