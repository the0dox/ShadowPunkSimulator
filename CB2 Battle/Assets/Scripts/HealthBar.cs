using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image Bar;
    public float currentHealth;
    private float maxHealth = 10;  
    public PlayerStats player;
    private bool visible = true; 

    // Update is called once per frame
    void Update()
    {
        if(visible)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation *- Vector3.back, Camera.main.transform.rotation *- Vector3.down);
            currentHealth = player.getWounds();
            maxHealth = player.Stats["MaxWounds"];
            Bar.fillAmount = currentHealth / maxHealth;  
        }
    }

    public void ToggleBar()
    {
        Bar.fillAmount = 0;
        visible = !visible;
    }
}
