using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Chat : MonoBehaviourInstance<Chat>
{
    public TextMeshProUGUI chatText;
    public int maxLines = 6;

    private List<string> textLines = new List<string>();

    public void AddText(string t)
    {
        textLines.Add(t);
        UpdateText();
    }

    private void UpdateText()
    {
        while (textLines.Count > maxLines)
        {
            textLines.RemoveAt(0);
        }
        string auxText = "";
        for(int i = 0; i < textLines.Count; i++)
        {
            auxText += textLines[i];
            if(i < textLines.Count - 1)
            {
                auxText += '\n';
            }
        }
        chatText.text = auxText;
    }
}
