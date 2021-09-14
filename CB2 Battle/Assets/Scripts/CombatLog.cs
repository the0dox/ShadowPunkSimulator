using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatLog : MonoBehaviour
{
    // Start is called before the first frame update 
    static GameObject g;
    static Queue<string> log;
    public static void Log(string text)
    {
        g = GameObject.FindGameObjectWithTag("Log");  
        g.GetComponent<Text>().text += ("[" + Time.deltaTime + "]: " + text + "\n\n");
        g.GetComponent<RectTransform>().transform.position += new Vector3(0, 24, 0);
    }

}
