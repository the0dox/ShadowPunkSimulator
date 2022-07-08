using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Tile Selector allows a level creator to change the material type of their blocks
public class UITileSelector : MonoBehaviour
{
    // A dicitionary that ties buttons to individual tile names
    Dictionary<string,GameObject> DisplayTiles = new Dictionary<string, GameObject>();
    // reference to a simple button
    [SerializeField] GameObject buttonRef;
    // reference to a content organizer for the display
    [SerializeField] RectTransform content;
    // reference to the game logic of the leve editor
    [SerializeField] LevelEditor levelEditor;
    // used to reference tile selector statically, there should never be more than one tile selector
    public static UITileSelector instance;

    // On startup, generate buttons that correspond to each of the known tile types and hides itself
    void Awake()
    {
        DisplayTiles = TileReference.Tiles();
        foreach(string s in DisplayTiles.Keys)
        {
            if(!s.Equals("TileBlank"))
            {
                GameObject button = GameObject.Instantiate(buttonRef, Vector3.zero, Quaternion.identity) as GameObject;
                button.SetActive(true);
                button.transform.SetParent(content);
                button.transform.localScale = Vector3.one;
                button.transform.localEulerAngles = Vector3.zero;
                button.GetComponent<TileButton>().SetData(DisplayTiles[s].GetComponent<Tile>());
            }
        }
        instance = this;
        gameObject.SetActive(false);
    }

    // When a button is clicked, set level editors current tile texture based on the button clicked
    public void OnClicked()
    {
        TileButton button;
        if(EventSystem.current.currentSelectedGameObject.TryGetComponent<TileButton>(out button))
        {
            levelEditor.OnButtonPressed(button.GetData());
            Toggle();
        }
    }

    // Either enable or disable the UI display
    public void Toggle()
    {
        CameraButtons.UIFreeze(!gameObject.activeInHierarchy);
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
