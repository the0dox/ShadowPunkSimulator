using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TooltipBehavior : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject textInput; 
    [SerializeField] private TurnManager TurnMaster; 
    [SerializeField] Canvas myCanvas;
    
    // Update is called once per frame
    void Update()
    {
        ResetSize();
        textInput.SetActive(false);
        background.SetActive(false);
        RaycastHit hit;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            List<string> output = TurnMaster.GetTooltip(hit);
            if(output.Count > 0)
            {
                background.SetActive(true);
                SetText(output);
            }
        }  
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myCanvas.transform as RectTransform, Input.mousePosition, myCanvas.worldCamera, out pos);
        transform.position = myCanvas.transform.TransformPoint(pos);
    }

    public void SetText(List<string> input)
    {
        textInput.SetActive(true);
        Text ToolTipText = textInput.GetComponent<Text>();
        ToolTipText.text = "";
        foreach(string s in input)
        {
            ToolTipText.text += s;
            ToolTipText.text += "\n";
            IncreaseSize();
        }
    }

    public void ResetSize()
    {
        background.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(110,0); 
    }
    
    public void IncreaseSize()
    {
        textInput.GetComponent<Text>().rectTransform.sizeDelta += new Vector2(0,55f); 
        background.GetComponent<Image>().rectTransform.sizeDelta += new Vector2(0,55f); 
    }
}
