using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionButtonScript : MonoBehaviour
{
    [SerializeField] private string action;
    [SerializeField] private GameObject Text;
    public void SetAction(string action)
    {
        Text.GetComponent<Text>().text = action;
        this.action = action;
    }
    public void SetText(string text)
    {
        Text.GetComponent<Text>().text = text;
    }
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
                GameObject.FindGameObjectWithTag("GameController").GetComponent<OverworldManager>().OnButtonPressed(action);
            }
        }
    }

    public void DestroyMe()
    {
        Destroy(gameObject);
    }
}
