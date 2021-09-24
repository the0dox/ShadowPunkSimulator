using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelEditor : MonoBehaviour
{
    [SerializeField] private GameObject SelectedTile;
    [SerializeField] private GameObject indicator;
    [SerializeField] private GameObject UIDisplay;
    private Dictionary<Vector3,GameObject> TileLocations = new Dictionary<Vector3, GameObject>();
    private Vector3 viablePos;
    private bool MouseDown = false;

    void Start()
    {
        ChangeTileTexture(SelectedTile);
    }

    public void ChangeTileTexture(GameObject newTile)
    {
        SelectedTile = newTile;
        GameObject tempBlock = Instantiate(SelectedTile) as GameObject;
        Material newMat = tempBlock.GetComponent<MeshRenderer>().material;
        UIDisplay.GetComponent<MeshRenderer>().material = newMat;
        indicator.GetComponent<MeshRenderer>().material = newMat;
        indicator.GetComponent<MeshRenderer>().material.color = new Color(newMat.color.r,newMat.color.b,newMat.color.b,0.5f);
        Destroy(tempBlock);
    }


    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //if the mouse clicked on a thing
        if (Physics.Raycast(ray, out hit))
        {
            bool hitUi = false;
            GameObject[] ui = GameObject.FindGameObjectsWithTag("Input");
            foreach(GameObject g in ui)
            {
                if(EventSystem.current.IsPointerOverGameObject())
                {
                    hitUi = true;
                }
            }

            if(!hitUi)
            {
                if(hit.collider.tag.Equals("Tile") && (!MouseDown))
                {
                    viablePos = hit.collider.transform.position;
                    viablePos.y += 1;
                }
                else
                {
                    viablePos = hit.point;
                }
                viablePos.x = Mathf.RoundToInt(viablePos.x);
                viablePos.y = Mathf.RoundToInt(viablePos.y);
                viablePos.z = Mathf.RoundToInt(viablePos.z);
                indicator.transform.position = viablePos;    
                if(Input.GetMouseButton(1) && hit.collider.tag.Equals("Tile"))
                {
                    DeleteTile(hit.collider.gameObject);
                }
                else if(Input.GetMouseButton(0))
                {
                    CreateTile(viablePos);
                    MouseDown = true;
                }
                else if(Input.GetMouseButtonUp(0))
                {
                    MouseDown = false;
                }
            }
        }
    }

    private void CreateTile(Vector3 PlacementPos)
    {
        if(!TileLocations.ContainsKey(PlacementPos))
        {
            GameObject newTile = Instantiate(SelectedTile) as GameObject;
            newTile.transform.position = PlacementPos;
            TileLocations.Add(PlacementPos,newTile);
        }
    }

    private void DeleteTile(GameObject Tile)
    {
        if(TileLocations.ContainsKey(Tile.transform.position))
        {
            TileLocations.Remove(Tile.transform.position);
            Destroy(Tile);
        }
    }
}
