using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSheetButton : MonoBehaviour
{
    
    public GameObject CharacterSheet;
    // Start is called before the first frame update
    void start()
    {
        CharacterSheet = GameObject.FindGameObjectWithTag("Sheet");
    }
    public void DisplayCharacterSheet()
    {
        
        if (CharacterSheet.activeInHierarchy)
        {
            CharacterSheet.SetActive(false);
        }
        else
        {
            CharacterSheet.SetActive(true);
        }
        
    }
}
