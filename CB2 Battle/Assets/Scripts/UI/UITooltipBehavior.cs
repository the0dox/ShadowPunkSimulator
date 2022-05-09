using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Controls the behavior of the tooltip box, requires tooltipsystem to know when/what to say
public class UITooltipBehavior : MonoBehaviour
{
    // Bold text displayed above the main body, used for item and ability names
    [SerializeField] private TextMeshProUGUI headerField;
    // main body of the text, where the meat of the text is
    [SerializeField] private TextMeshProUGUI contentField;
    // Reference to the layout element that controls text wrapping set on or off depending on the content
    [SerializeField] private LayoutElement layoutElement;
    // An editable limit to where text begins wrapping 
    [SerializeField] private int characterwraplimit;
    // A reference to my parent canvas
    [SerializeField] private Canvas myCanvas;
    // A reference to my own RectTransform, used to modifiy scale and position
    [SerializeField] private RectTransform rectTransform;
    // Used to properly scale UI objects to screen bounds
    private float canvasScale;

    // defined offsets of the tooltip syste 
    [SerializeField] private float xoffSet;
    [SerializeField] private float yoffSet;

    void Start()
    {
        canvasScale = myCanvas.GetComponent<RectTransform>().localScale.y;
    }

    public void Update()
    {
        // Sets pivot position to prevent UI going off screen
        Vector2 mousepos = Input.mousePosition;
        mousepos += new Vector2(xoffSet, yoffSet);
        float pivotX = 0;
        float pivotY = 1;
        float RightX = (rectTransform.sizeDelta.x * canvasScale) + mousepos.x;
        float BottomY = rectTransform.sizeDelta.y * canvasScale;
        float percentageOver = (BottomY - mousepos.y)/BottomY;
        //Debug.Log("mouse pos " + mousepos.y + "| y bound: " + BottomY + " | diff" + (BottomY - mousepos.y) +"| % over" + percentageOver + "| displace?:" + (percentageOver > 0));
        if(RightX > Screen.width)
        {
            pivotX = 1;
        }
        if(percentageOver > 0)
        {
            pivotY -= percentageOver;
        }
        rectTransform.pivot = new Vector2(pivotX,pivotY);
        transform.position = mousepos;
        // check input 
        if(Input.anyKeyDown)
        {
            CheckInput();
        }
    }

    public void CheckInput()
    {
        if(!EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("didnt hit ui");
            TooltipSystem.hide();
        }
    }

    // Given content and header, sets the given text and determines if text needs to be wrapped
    public void SetText(string content, string header = "")
    {
        if(string.IsNullOrEmpty(header))
        {
            headerField.gameObject.SetActive(false);
        }
        else
        {
            headerField.gameObject.SetActive(true);
            headerField.text = header;
        }
        contentField.text = content;
        
        int headerlength = headerField.text.Length;
        int contentlength = contentField.text.Length;
        bool aligned = contentField.text.Contains("\n");
        layoutElement.enabled = (headerlength > characterwraplimit || contentlength > characterwraplimit || aligned) ? true : false;
    }
}
