using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayerInfo : MonoBehaviour
{
    public GameObject PlayerName;
    public GameObject Health;
    public GameObject Actions;
    public GameObject WeaponOne;
    public GameObject WeaponTwo;
    
    // Start is called before the first frame update
    public void UpdateDisplay(PlayerStats ps, int actions)
    {
        PlayerName.GetComponent<Text>().text = ps.GetName();
        Health.GetComponent<Text>().text = "Wounds: " + ps.HealthToString();
        Actions.GetComponent<Text>().text = "Half Actions: " + actions +"/2";
        Text wpOne = WeaponOne.GetComponent<Text>();
        Text wpTwo = WeaponTwo.GetComponent<Text>();
        wpOne.text = null;
        wpTwo.text = null;
        if(ps.PrimaryWeapon != null)
        {
            wpOne.text = ps.PrimaryWeapon.ToString();
        }
        if(ps.SecondaryWeapon != null)
        {
            wpTwo.text = ps.SecondaryWeapon.ToString();
        }
    }
}
