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
        string output = text + "\n-----------------------------------------------------\n";  
        g.GetComponent<Text>().text += (output);
        int indents = output.Split('\n').Length - 1;
        g.GetComponent<RectTransform>().transform.position += new Vector3(0, 7.25f * indents, 0);
    }

}
