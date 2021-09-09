using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CriticalPopup : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject displayText;
    [SerializeField] private GameObject displayTitle;
    public void Toggle()
    {
        Destroy(gameObject);
    }

    public void DisplayText(string header, string output)
    {
        displayTitle.GetComponent<Text>().text = header;
        displayText.GetComponent<Text>().text = output;
        gameObject.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        gameObject.transform.localPosition = new Vector3(0,0,0);
    }
}
