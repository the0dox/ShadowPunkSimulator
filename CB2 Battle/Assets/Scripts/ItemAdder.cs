using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Used by charactersheet to add more skills to a player
public class ItemAdder : MonoBehaviour
{
    // Non static references to UI objects
    [SerializeField] private Transform DisplayRef;
    [SerializeField] private GameObject ItemFieldRef;
    [SerializeField] private Transform AdderContentRef;
    [SerializeField] private GameObject ButtonRef;
    // Static references to objects
    // Display for skill adder
    private static Transform SAdderContent;
    // Display for skills the player already has
    private static Transform SDisplay;
    // Object used to modify skill values
    private static GameObject ItemInput;
    // Object used to select which skills to add/remove
    private static GameObject SButton;
    // Reference to the player being edited
    private static CharacterSaveData owner;
    
    // Saves the static references
    void Awake()
    {
        SDisplay = DisplayRef;
        ItemInput = ItemFieldRef;
        SAdderContent = AdderContentRef;
        SButton = ButtonRef;
    }

    // Given newowner savedata, creates Skillinputs for each skill the player already knows
    public static void DownloadOwner(CharacterSaveData newonwer)
    { 
        owner = newonwer;
        foreach(Item item in newonwer.equipmentObjects)
        {
            AddItem(item,false);
        } 
        UpdateAdderContent();
    }
    
    // Adds a skill from skillreferences that matches a given name
    // if add, add the skill to the owner
    public static void AddItem(Item newItem, bool add)
    {
        int oldCount = owner.equipmentObjects.Count;
        if(add)
        {
            owner.AddItem(newItem);
        }
        if(!add || oldCount != owner.equipmentObjects.Count)
        {
            ItemInputField indicator = Instantiate(ItemInput as GameObject).GetComponent<ItemInputField>();
            indicator.DownloadCharacter(owner, newItem);
            indicator.transform.SetParent(SDisplay);
            indicator.transform.localScale = Vector3.one;
        }
    }

    // Destroys the corresponding Skillinput and removes the skill from the player
    public static void RemoveSkill(string skillName)
    {
        owner.skills.Remove(skillName);
        owner.skillSpecialization.Remove(skillName);
        SkillScript removedSkill = null;
        Destroy(removedSkill.gameObject);
    }

    // If one of my buttons was clicked, check if I already own that skill
    // If I don't own it, create it
    // If I already own it, remove it
    public void OnClicked()
    {
        string name = EventSystem.current.currentSelectedGameObject.name;
        Item newItem = ItemReference.GetItem(name);
        AddItem(newItem,true);
    }

    // Adds button prefabs to SContent to represent all the skills the player can have 
    private static void UpdateAdderContent()
    {
        foreach(string itemKey in ItemReference.ItemTemplates().Keys)
        {
            GameObject newButton = Instantiate(SButton as GameObject);
            newButton.name = itemKey; 
            newButton.GetComponentInChildren<Text>().text = itemKey;
            newButton.transform.SetParent(SAdderContent);
            newButton.transform.localScale = Vector3.one;
        }
    }
}
