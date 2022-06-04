using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UI tool used to edit a ground tile object in real time. This class assumes there is a groundTile in the current scene
public class UIGroundEditor : MonoBehaviour
{
    // reference to a dummy button that is copied to create every texture option
    [SerializeField] GameObject buttonRef;
    // reference to the layout group that organizes new buttons
    [SerializeField] RectTransform content;
    // reference to the index each button represetns
    private List<GameObject> buttons;

    // foreach material in manager, create a set of buttons that correspond to each of their indexes
    void Awake()
    {
        buttons = new List<GameObject>();
        Material[] allMaterials = MaterialReference.Materials();
        for(int i = 0; i < allMaterials.Length; i++)
        {
            Texture2D newTexture = (Texture2D)allMaterials[i].mainTexture;
            Sprite displayImage = Sprite.Create(newTexture, new Rect(0,0,newTexture.width, newTexture.height) , new Vector2(0.5f, 0.5f));
            GameObject newButton = Instantiate(buttonRef) as GameObject;
            newButton.SetActive(true);
            newButton.GetComponent<Image>().sprite = displayImage;
            newButton.transform.SetParent(content);
            buttons.Add(newButton);
        }
        gameObject.SetActive(false);
    }
    
    // when a button is clicked find if it has an index and apply the material to the ground tile
    public void OnClicked()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        // if clicked button corresponds to a saved material index
        if(buttons.Contains(clickedButton))
        {
            int materialIndex = buttons.IndexOf(clickedButton);
            Material newMaterial = MaterialReference.GetMaterial(materialIndex);
            TileGround.SetMaterial(newMaterial);
            Toggle();
        }
    }

    // enable or disables the gameobject panel 
    public void Toggle()
    {
        CameraButtons.UIFreeze(!gameObject.activeInHierarchy);
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
