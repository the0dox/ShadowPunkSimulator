using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileButton : MonoBehaviour
{
    [SerializeField] private MeshRenderer myRenderer;
    private string data;
    public void SetData(Tile newTile)
    {
        data = newTile.name;
        myRenderer.material = newTile.GetComponent<MeshRenderer>().sharedMaterial;
    }

    public string GetData()
    {
        return data;
    }
}
