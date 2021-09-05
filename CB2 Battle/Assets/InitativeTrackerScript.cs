using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InitativeTrackerScript : MonoBehaviour
{

    public GameObject TextEntry;
    // Start is called before the first frame update
    public void UpdateList(List<string> l)
    {
        ClearList();
        int verticalDisplacement = 0;
        foreach(string s in l)
        {
            verticalDisplacement -= 50;
            GameObject newEntry = Instantiate(TextEntry) as GameObject;
            newEntry.transform.SetParent(gameObject.transform, false);
            newEntry.transform.localPosition = new Vector3(0,verticalDisplacement,0);
            newEntry.GetComponent<Text>().text = s;
        }
    }

    public void ClearList()
    {
        GameObject[] g = GameObject.FindGameObjectsWithTag("Init");
        foreach(GameObject current in g)
        {
            Destroy(current);
        }
    }
}
