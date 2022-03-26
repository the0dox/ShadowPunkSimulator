using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISelectableTiles : MonoBehaviour
{

    private GameObject owner;

    public void SetOwner(GameObject input)
    {
        owner = input;
    }

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        //if the mouse clicked on a thing
        if (Physics.Raycast(ray, out hit))
        {
            if(Input.GetMouseButtonDown(0) && hit.collider == gameObject)
            {
                GameObject.FindGameObjectWithTag("GameController").GetComponent<LevelEditor>().ChangeTileTexture(gameObject);
                owner.GetComponent<UITileSelector>().Toggle();
            }
        }
    }
}
