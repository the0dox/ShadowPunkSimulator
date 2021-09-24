using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI element that displays the events of the game, any script can access and make this display print a message to the player
public class CombatLog : MonoBehaviour
{
    // Static prefernce tp the game object
    static GameObject g;
    
    // text: string input to be read to the player
    // types out text and formats it for the player
    public static void Log(string text)
    {
        g = GameObject.FindGameObjectWithTag("Log");
        string output = text + "\n-----------------------------------------------------\n";  
        g.GetComponent<Text>().text += (output);
        int indents = output.Split('\n').Length - 1;
        g.GetComponent<RectTransform>().transform.position += new Vector3(0, 7.25f * indents, 0);
    }

}
