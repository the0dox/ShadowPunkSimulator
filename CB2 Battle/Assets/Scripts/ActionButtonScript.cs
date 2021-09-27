using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// simple UI button that holds a single string that can be invoked
public class ActionButtonScript : MonoBehaviour
{
    // Invoke string, the name of the method thats called
    [SerializeField] private string action;
    // Display name of the button shown to the player, can be different than action
    [SerializeField] private GameObject Text;

    // Called by whatevered instantiated the button to set its saved string and display name
    public void SetAction(string action)
    {
        Text.GetComponent<Text>().text = action;
        this.action = action;
    }
    // Same as SetAction, but only changes display name 
    public void SetText(string text)
    {
        Text.GetComponent<Text>().text = text;
    }
    // On button press send my action string to the game controller
    public void GetAction()
    {
        if(!CameraButtons.UIActive())
        {
            if(GameObject.FindGameObjectWithTag("GameController").TryGetComponent<TurnManager>(out TurnManager tm))
            {
                tm.OnButtonPressed(action);
            }
            else
            {
                GameObject.FindGameObjectWithTag("GameController").GetComponent<UIButtonManager>().OnButtonPressed(action);
            }
        }
    }
    // Called by the game controller to clear button options
    public void DestroyMe()
    {
        Destroy(gameObject);
    }
}
