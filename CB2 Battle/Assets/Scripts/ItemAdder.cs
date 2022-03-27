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
        ItemSlots = new ItemInputField[17];
        foreach(ItemInputField iif in itemInputFieldInitalizer)
        {
            ItemSlots[index] = iif;
            index++;
        }
        ItemSlots[16] = new ItemInputField();
    }

    // Given newowner savedata, creates Skillinputs for each skill the player already knows
    public static void DownloadOwner(CharacterSaveData newonwer)
    { 
        lastIndex = 0;
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
            ItemSlots[lastIndex].DownloadCharacter(owner,newItem);
            lastIndex++;
        }
    }

    // Called from the edited player when their item is reduced to 0, removes said item from the box
    public static void RemoveItem(Item removedItem)
    {
        ClearPopup();
        lastIndex--;
        bool incrementPosition = false;
        for(int i = 0; i < ItemSlots.Length-1; i++)
        {
            if(removedItem == ItemSlots[i].GetItem())
            {
                incrementPosition = true;
            }
            if(incrementPosition && i < ItemSlots.Length)
            {
                ItemInputField nextInputfield = ItemSlots[i+1];
                if(nextInputfield != null)
                {
                    Item nextItem = ItemSlots[i+1].GetItem();
                    ItemSlots[i].DownloadCharacter(owner, nextItem);
                }
            }
        }
    }

    public static void OnItemClicked(ItemInputField clickedItemField)
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

    public static void RemoveItem()
    {
        if(activePopupfield != null)
        {
            activePopupfield.Reduce();
        }
    }

    public static void ClearPopup()
    {
        activePopupfield = null;
        SPopupObject.SetActive(false);
    }

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
