using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITileSelector : MonoBehaviour
{
    private Vector3 CurrentPos;
    private Vector3 StartingPos = new Vector3(-170,50,-500);
    private Vector3 DisplacementPos = new Vector3(50,0,0);
    private Vector3 StandardEulers = new Vector3(45,0,45);
    private Vector3 StandardScale = new Vector3(30,30,30);
    Dictionary<string,GameObject> DisplayTiles = new Dictionary<string, GameObject>();
    void Start()
    {
        CurrentPos = StartingPos;
        DisplayTiles = TileReference.Tiles();
        foreach(string s in DisplayTiles.Keys)
        {
            GameObject current = Instantiate(DisplayTiles[s]) as GameObject;
            current.layer = 5;
            current.transform.SetParent(gameObject.transform);
            current.transform.localPosition = CurrentPos;
            current.transform.localEulerAngles = StandardEulers;
            current.transform.localScale = StandardScale;
            current.AddComponent<UISelectableTiles>().SetOwner(gameObject);
            CurrentPos += DisplacementPos;
        }
    }

    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}
