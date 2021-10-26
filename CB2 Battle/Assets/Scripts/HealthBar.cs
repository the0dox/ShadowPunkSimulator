using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image Bar;
    private bool visible = true; 

    // Update is called once per frame
    void Update()
    {
        if(visible)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation *- Vector3.back, Camera.main.transform.rotation *- Vector3.down);
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        Bar.fillAmount = (float)currentHealth / maxHealth;  
    }

    public void ToggleBar()
    {
        Bar.fillAmount = 0;
        visible = !visible;
    }
}
