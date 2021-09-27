using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITileSelector : MonoBehaviour
{
    [SerializeField] private GameObject ButtonPrefab;
    private Vector3 CurrentPos;
    private Vector3 StartingPos = new Vector3(-140,60,-60);
    private Vector3 DisplacementPos = new Vector3(50,0,0);
    private Vector3 StandardEulers = new Vector3(45,0,45);
    private Vector3 StandardScale = new Vector3(30,30,30);
    Dictionary<string,GameObject> DisplayTiles = new Dictionary<string, GameObject>();
    void Display()
    {

        CurrentPos = StartingPos;
        DisplayTiles = TileReference.Tiles();
        foreach(string s in DisplayTiles.Keys)
        {
            GameObject button = Instantiate(ButtonPrefab) as GameObject;
            button.GetComponent<ActionButtonScript>().SetAction(s);
            button.transform.SetParent(gameObject.transform);
            button.transform.localScale = new Vector3(0.5f,0.5f,0.5f);
            button.transform.localPosition = new Vector3(CurrentPos.x, CurrentPos.y, CurrentPos.z + 59);
            button.transform.localEulerAngles = new Vector3(0,0,0);

            GameObject current = Instantiate(DisplayTiles[s]) as GameObject;
            current.layer = 5;
            current.transform.SetParent(gameObject.transform);
            current.transform.localPosition = CurrentPos;
            current.transform.localEulerAngles = StandardEulers;
            current.transform.localScale = StandardScale;
            current.tag = "UITile";
            
            CurrentPos += DisplacementPos;
        }
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //if the mouse clicked on a thing
        if (Physics.Raycast(ray, out hit))
        {
            if(hit.collider.tag.Equals("UITile"))
            {
                string tilename = hit.collider.name.Split(')')[1];
                Debug.Log(tilename);
            }
        }
    }

    public void Toggle()
    {
        if(gameObject.activeInHierarchy)
        {
            GameObject[] UITiles = GameObject.FindGameObjectsWithTag("UITile");
            GameObject[] buttons = GameObject.FindGameObjectsWithTag("ActionInput");
            foreach(GameObject g in UITiles)
            {
                Destroy(g);
            }
            foreach(GameObject g in buttons)
            {
                Destroy(g);
            }
        }
        else
        {
            Display();
        }
        gameObject.SetActive(!gameObject.activeInHierarchy);
    }
}