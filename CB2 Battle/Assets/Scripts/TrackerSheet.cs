using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used to track some resource on the charactersheet
public class TrackerSheet : MonoBehaviour
{
    // used to assign interactable tokens in editor
    [SerializeField] private List<GameObject> TokenInitalizer;
    // reference to the indicator of max health, edge limit etc.
    [SerializeField]private GameObject indicator;
    // assigns indvidual tokens to a numerical value for resource
    private Dictionary<int, GameObject> Tokens;
    // generic resource tokens represents, could be health, stun, or edge for example
    private int MyResource; 
    // used to determine the location of the indicator
    private int ResourceMaximum;
    // if set to true then myresource can never pass the maximum value
    private bool EnforceMaximum = true;
    [SerializeField] private CharacterSheet myData;

    public void Init()
    {
        MyResource = 0;
        Tokens = new Dictionary<int, GameObject>();
        int value = 1;
        foreach(GameObject g in TokenInitalizer)
        {
            Tokens.Add(value, g);
            value++;
        }
        UpdateTokens();
    }
    // if resource is a non-zero value from memory
    public void SetResource(int resource)
    {
        MyResource = resource;
        UpdateTokens();
    }

    // takes a maximum to establish indicator position
    public void SetMaximum(int max)
    {
        if(max == 0)
        {
            max = 1;
        }
        if(!Tokens.ContainsKey(max))
        {
            max = Tokens.Count;
        }
        else
        {
            ResourceMaximum = max;
            Vector3 newPosition = Tokens[ResourceMaximum].transform.localPosition;
            indicator.transform.localPosition = newPosition;          
        }
    }

    // On button press, decrease resource by one, and update tokens to reflect the new value
    public void SubtractValue()
    {
        if(MyResource > 0)
        {
            MyResource--;
            UpdateTokens();
        }
    }

    public void IncrementValue()
    {
        MyResource++;
        if(MyResource > ResourceMaximum && EnforceMaximum)
        {
            MyResource = ResourceMaximum;
        }
        UpdateTokens();
    }

    // Udates tokens to match values
    private void UpdateTokens()
    {
        foreach(int key in Tokens.Keys)
        {
            // if the value this token represents is less than or equal to the represented value, set active
            if(key <= MyResource)
            {
                Tokens[key].SetActive(true);
            }
            // else the token should be disabled
            else
            {
                Tokens[key].SetActive(false);
            }
        }    
    }

}
