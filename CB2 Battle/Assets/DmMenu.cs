using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DmMenu : MonoBehaviour
{
    [SerializeField] private GameObject Display;
    [SerializeField] private GameObject CharacterSheet;
    [SerializeField] private List<ScriptableObject> SavedCharacters = new List<ScriptableObject>();
    [SerializeField] private GameObject PlayerReference;

    public void Toggle()
    {
        Display.SetActive(!Display.activeInHierarchy);
    }

    public void CreateCharacter()
    {
        Instantiate(CharacterSheet);
    }

    public void EditCharacter(int index)
    {
        ScriptableObject character = SavedCharacters[index];
    }

    public void one()
    {
        EditCharacter(1);
    }
}
