using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionButtonScript : MonoBehaviour
{
    [SerializeField] private string action;
    [SerializeField] private GameObject Text;
    GameObject TurnOrder;
    public void SetAction(string action)
    {
        Text.GetComponent<Text>().text = action;
        this.action = action;
        TurnOrder = GameObject.FindGameObjectWithTag("GameController");
    }
    public void SetText(string text)
    {
        Text.GetComponent<Text>().text = text;
        TurnOrder = GameObject.FindGameObjectWithTag("GameController");
    }
    public void GetAction()
    {
        TurnOrder.GetComponent<TurnManager>().OnButtonPressed(action);
    }

    public void DestroyMe()
    {
        Destroy(gameObject);
    }
}
