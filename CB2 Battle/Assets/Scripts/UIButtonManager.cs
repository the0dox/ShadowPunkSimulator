using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButtonManager : MonoBehaviour
{
    [SerializeField] private GameObject ActionUIButton;

    //creates a set of interactable buttons can key = text value = method called
    public void ConstructActions(Dictionary<string, string> d)
    {
        GameObject[] oldButtons = GameObject.FindGameObjectsWithTag("ActionInput");
        foreach(GameObject g in oldButtons)
        {
            g.GetComponent<ActionButtonScript>().DestroyMe();
        }
        if(d != null)
        {
            int displacement = 0;
            foreach (KeyValuePair<string, string> kvp in d)
            {
                GameObject newButton = Instantiate(ActionUIButton) as GameObject;
                newButton.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
                newButton.transform.position += new Vector3(displacement,0,0);
                newButton.GetComponent<ActionButtonScript>().SetAction(kvp.Value);
                newButton.GetComponent<ActionButtonScript>().SetText(kvp.Key);
                displacement += 150;
            }
        }
    }

    //is passed the action value of a button as an input
    virtual public void OnButtonPressed(string input)
    {   
        Invoke(input,0);
    }
}
