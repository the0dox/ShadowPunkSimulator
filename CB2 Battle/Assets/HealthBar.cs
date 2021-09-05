using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    private Image Bar;
    public float currentHealth;
    private float maxHealth = 10;  
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
        currentHealth = player.getWounds();
        maxHealth = player.Stats["MaxWounds"];
        Bar.fillAmount = currentHealth / maxHealth;  
    }
}
