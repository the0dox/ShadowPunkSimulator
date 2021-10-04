using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI element that displays the events of the game, any script can access and make this display print a message to the player
public class CombatLog : MonoBehaviour
{
    [SerializeField] private int MaxEntries = 10;
    [SerializeField] private Text textRef;
    // Static prefernce tp the game object
    static Text displayText;
    static Vector3 DefaultPos;
    static private Vector3 originalPos;
    // lines are entered into queue so that when the text box overflows, the first entry is deleted.
    private static Queue<string> ContentsQueue = new Queue<string>();

    // text: string input to be read to the player
    // types out text and formats it for the player

    void Start()
    {
        displayText = textRef;
        DefaultPos = displayText.transform.localPosition;
    }
    public static void Log(string text)
    {
        ContentsQueue.Enqueue(text);
        TrimEntries();
        int Reps = ContentsQueue.Count;
        Stack<string> tempStack = new Stack<string>();
        displayText.text = "";
        for(int i = 0; i < Reps; i++)
        {
            string currentLine = ContentsQueue.Dequeue();
            displayText.text += "----------------------------------------------------\n" + currentLine +"\n";
            ContentsQueue.Enqueue(currentLine);
        }
        displayText.transform.localPosition = DefaultPos;
    }

    private static void TrimEntries()
    {
        while(ContentsQueue.Count > 10)
        {
            ContentsQueue.Dequeue();
        }
    }
}
