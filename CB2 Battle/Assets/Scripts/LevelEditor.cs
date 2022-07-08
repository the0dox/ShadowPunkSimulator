using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelEditor: MonoBehaviour
{
    [SerializeField] private GameObject SelectedTile;
    [SerializeField] private GameObject indicator;
    [SerializeField] private GameObject UIDisplay;
    [SerializeField] private UITileSelector UITileSelectorRef;
    // adjusts time it takes to register a mouse hold
    [SerializeField] private int HoldDelayMax = 500;
    [SerializeField] private InputField NameIF;
    private int HoldDelay;
    private Dictionary<Vector3,GameObject> TileLocations = new Dictionary<Vector3, GameObject>();
    private Vector3 viablePos;
    private bool MouseHold = false;
    private float HeldY;
    private GameObject Player;
    

    void Start()
    {
        HoldDelay = HoldDelayMax;
        ChangeTileTexture(SelectedTile);
        TileLocations = TileGround.LoadTiles(20,20);
    }

    public void ChangeTileTexture(GameObject newTile)
    {
        UIDisplay.SetActive(true);
        SelectedTile = newTile;
        Material newMat = newTile.GetComponent<MeshRenderer>().sharedMaterial;
        UIDisplay.GetComponent<MeshRenderer>().material = newMat;
        indicator.GetComponent<MeshRenderer>().material = newMat;
        indicator.GetComponent<MeshRenderer>().material.color = new Color(newMat.color.r,newMat.color.b,newMat.color.b,0.5f);
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
            if(EventSystem.current.IsPointerOverGameObject())
            {
                hitUi = true;
            }

            if(!hitUi)
            {
                if(hit.collider.tag.Equals("Ground"))
                {
                    viablePos = hit.point;
                    viablePos.y = 1;
                }
                else if(hit.collider.tag.Equals("Tile") || !MouseHold)
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

                if(Player != null)
                {
                    viablePos.y += 0.5f;
                    Player.transform.position = viablePos;
                }
                else
                {
                    indicator.transform.position = viablePos;   
                }

                if(Input.GetMouseButtonUp(0))
                {
                    HoldDelay = HoldDelayMax;
                    MouseHold = false;
                    HeldY = 0;
                } 
                else if(Input.GetMouseButtonDown(1) && hit.collider != null)
                {
                    DeleteTile(hit.collider.gameObject);
                }
                else if(MouseHold)
                {
                    CreateTile(new Vector3(viablePos.x, HeldY,viablePos.z));
                }
                else if(Input.GetMouseButtonDown(0))
                {
                    if(Player != null)
                    {
                        TileLocations.Add(viablePos,Player);
                        Player.GetComponent<Collider>().enabled = true;
                        Player = null;
                        indicator.SetActive(true);
                    }
                    else
                    {
                        CreateTile(viablePos);
                        HeldY = viablePos.y;
                    }
                }
                // if player continues to hold, start subtracting delay
                else if(Input.GetMouseButton(0) && HoldDelay > 0 && !MouseHold)
                {
                    HoldDelay--;
                }
                else if(Input.GetMouseButton(0) && HoldDelay <= 0 && !MouseHold)
                {
                    MouseHold = true;
                }
            }
        }
    }

    public void OnButtonPressed(string input)
    {
        ChangeTileTexture(TileReference.Tile(input));
    }

    public void HideIndicator()
    {
        UIDisplay.SetActive(UIDisplay.activeInHierarchy);
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

    public void AddPlayer(GameObject input)
    {
        input.GetComponent<Collider>().enabled = false;
        input.GetComponent<PlayerStats>().DisableHP();
        Player = input;
        indicator.SetActive(false);
    }

    public void SaveLevel()
    {
        string name = NameIF.text;
        NameIF.text = null;
        Material groundMat = TileGround.GetMaterial();
        SceneSaveData newScene = new SceneSaveData(name, TileLocations,groundMat);
        SaveSystem.SaveScene(newScene);
        CameraButtons.UIFreeze(false);
    }

    public void LoadLevel()
    {
        GameObject[] newTiles = GameObject.FindGameObjectsWithTag("Tile");
        GameObject[] newTokens = GameObject.FindGameObjectsWithTag("Player");
        TileLocations.Clear();
        foreach(GameObject g in newTiles)
        {
            if(!TileLocations.ContainsKey(g.transform.position))
            {
                TileLocations.Add(g.transform.position,g);
            }
        }
        foreach(GameObject g in newTokens)
        {
            if(!TileLocations.ContainsKey(g.transform.position))
            {
                TileLocations.Add(g.transform.position,g);
            }
        }
    }
}
