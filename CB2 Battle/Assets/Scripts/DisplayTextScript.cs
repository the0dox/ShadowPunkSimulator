using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayTextScript : MonoBehaviour
{
    public GameObject TextObject;
    public float timer = 100.0f;
    public Vector3 location;

    void OnEnable()
    {
        if(location != null)
        {
            transform.position = location + new Vector3(0, 1.75f, 0);
        }
    }
    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation *- Vector3.back, Camera.main.transform.rotation *- Vector3.down);
        if (timer > 0)
        {
            if (timer < 50)
            {
                Color c = TextObject.GetComponent<TMPro.TextMeshPro>().color;
                c.a -= 0.02f;
                TextObject.GetComponent<TMPro.TextMeshPro>().color = c;
            }
            transform.position += new Vector3(0,0.0015f,0);
            timer--; 
        }
        else
        {
            PopUpText.Dequeue(location);
            Destroy(gameObject); 
        }
    }

    public void SetInfo(Color color, string text, Vector3 g)
    {
        location = g;
        TextObject.GetComponent<TMPro.TextMeshPro>().color = color;
        TextObject.GetComponent<TMPro.TextMeshPro>().text = text;
    }
}
