using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FatigueBar : MonoBehaviour
{
    [SerializeField] private Image Bar;

    void Start()
    {
        Bar.fillAmount = 1;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation *- Vector3.back, Camera.main.transform.rotation *- Vector3.down);
    }
    public void UpdateFatigue(int currentFatigue, int maxFatigue)
    {
        float fill = (float)(maxFatigue - currentFatigue) / maxFatigue;
        if(fill < 0)
        {
            fill = 0;
        }
        Bar.fillAmount = fill;
    }
}
