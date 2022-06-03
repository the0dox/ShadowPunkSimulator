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
    [SerializeField] private Transform AdderContentRef;
    [SerializeField] private GameObject ButtonRef;
    [SerializeField] private List<ItemInputField> itemInputFieldInitalizer;
    [SerializeField] private GameObject PopupObject;
    [SerializeField] private static ItemInputField[] ItemSlots;  
    [SerializeField] CharacterSheet mySheet;
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
    // Last empty item box used to add new items at the end of the chain
    private static int lastIndex;
    // popup that is used to interact with a specific item
    private static GameObject SPopupObject;
    // last item box to be clicked that the popup box interacts with
    private static ItemInputField activePopupfield;
    
    // Saves the static references
    void Awake()
    {
        SDisplay = DisplayRef;
        SAdderContent = AdderContentRef;
        SButton = ButtonRef;
        SPopupObject = PopupObject;
        int index = 0;
        ItemSlots = new ItemInputField[16];
        foreach(ItemInputField iif in itemInputFieldInitalizer)
        {
            ItemSlots[index] = iif;
            index++;
        }
    }

    // Given newowner savedata, creates Skillinputs for each skill the player already knows
    public void DownloadOwner(CharacterSaveData newonwer)
    { 
        owner = newonwer;
        DisplayItems(); 
        UpdateAdderContent();
    }

    public void DisplayItems()
    {
        Item[] equipement = owner.equipmentObjects.ToArray();
        // for each item slot
        for(int i = 0; i < ItemSlots.Length; i++)
        {
            // add an item
            if(i < equipement.Length)
            {
                ItemSlots[i].SetItem(equipement[i]);
            }
            // clear slot
            else
            {
                ItemSlots[i].Clear();
            }
        }
    }

    public void OnItemClicked(ItemInputField clickedItemField)
    {
        // if this has already been clicked
        if(activePopupfield == clickedItemField || clickedItemField.GetItem() == null)
        {
            ClearPopup();
        }
        // if this is a new item that hasn't been picked yet
        else
        {
            activePopupfield = clickedItemField;
            SPopupObject.SetActive(true);
            SPopupObject.transform.position = activePopupfield.transform.position + new Vector3(60,0,0);
        }
    }

    public void OnSubtractButtonClicked()
    {
        Item activeItem = activePopupfield.GetItem();
        if(activeItem.GetStacks() < 2)
        {
            ClearPopup();
        }
        if(activeItem != null)
        {
            string name = activeItem.GetTemplateName();
            mySheet.ChangeItem(name, false);
        }
    }

    public void ClearPopup()
    {
        activePopupfield = null;
        SPopupObject.SetActive(false);
    }

    public void OnClicked()
    {
        string name = EventSystem.current.currentSelectedGameObject.name;
        mySheet.ChangeItem(name, true);
    }

    // Adds button prefabs to SContent to represent all the skills the player can have 
    private void UpdateAdderContent()
    {
        foreach(string itemKey in ItemReference.ItemTemplates().Keys)
        {
            GameObject newButton = Instantiate(SButton as GameObject);
            newButton.SetActive(true);
            newButton.name = itemKey; 
            newButton.GetComponentInChildren<Text>().text = itemKey;
            newButton.transform.SetParent(SAdderContent);
            newButton.transform.localScale = Vector3.one;
        }
    }
}
