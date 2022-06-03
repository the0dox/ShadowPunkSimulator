using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITileSelector : MonoBehaviour
{
    private Vector3 CurrentPos;
    Dictionary<string,GameObject> DisplayTiles = new Dictionary<string, GameObject>();
    [SerializeField] GameObject buttonRef;
    [SerializeField] RectTransform content;
    [SerializeField] LevelEditor levelEditor;

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
        gameObject.SetActive(false);
    }

    public void OnClicked()
    {
        TileButton button;
        if(EventSystem.current.currentSelectedGameObject.TryGetComponent<TileButton>(out button))
        {
            levelEditor.OnButtonPressed(button.GetData());
        }
    }

    public void Toggle()
    {
        CameraButtons.UIFreeze(!gameObject.activeInHierarchy);
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
