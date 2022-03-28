using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A static library that contains all conditions TEMPLATES so that scripts can easily create add/edit conditions
public class TileReference : MonoBehaviour
{
    // IMPORTANT: you must add all scriptable objects to this list in order for them to be intialized in the game scene
    [SerializeField] public List<GameObject> TileInitializer = new List<GameObject>();
    // static reference of conditionsinitializer for other scripts
    private static Dictionary<string, GameObject> Library = new Dictionary<string, GameObject>();
    // creates Library so that it can be referenced statically
    public void Init()
    {
        foreach(GameObject tile in TileInitializer)
        {
            Library.Add(tile.name, tile);
        }
    }

    // name: the name of the scriptable object that needs to be copied
    // creates and returns a regular condition object out of the template that shares a name with input
    public static GameObject Tile(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }

    // returns the library
    public static Dictionary<string,GameObject> Tiles()
    {
        return Library;
    }
}
