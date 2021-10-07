using UnityEngine;
using TMPro;

public class GDPRBlockedPanel : MonoBehaviour 
{
    public ButtonHandler urlButton;
    public GameObject userInfo;
    public TextMeshPro titleText;
    public TextMeshPro buttonText;
    public TextMeshPro textBlock;
    public TextMeshPro zid;
    

    private void Update()
    {
        //when this is running, the loading screen is no longer updating touch input and the game/lobby scenes are not loaded so we must call it to make hyperlinks work
        TouchInput.update();
    }

    public void init(string title, string descriptionKey, string instructionsKey, string button, bool showZID, ClickHandler.onClickDelegate onUrl)
    {
        
        string text = "";
        if (!string.IsNullOrEmpty(descriptionKey))
        {
            text = Localize.text(descriptionKey);
        }
        
        if (!string.IsNullOrEmpty(instructionsKey))
        {
            if (!string.IsNullOrEmpty(text))
            {
                text += System.Environment.NewLine;
                text += System.Environment.NewLine;
            }
            text += Localize.text(instructionsKey);
        }

        textBlock.SetText(text);
        

        if (!string.IsNullOrEmpty(title))
        {
            titleText.text = Localize.text(title);
        }

        if (!string.IsNullOrEmpty(button))
        {
            buttonText.text = Localize.text(button);
        }

        if (onUrl != null)
        {
            urlButton.registerEventDelegate(onUrl);
        }
        else
        {
            //Hide the button if theres no callback passed in
            urlButton.gameObject.SetActive(false);
            buttonText.gameObject.SetActive(false);
        }

        SafeSet.gameObjectActive(userInfo, showZID);

        if (showZID)
        {
            zid.text = Localize.text(GDPRDialog.ZID_LOCALIZE_KEY,  SlotsPlayer.instance.socialMember.zId);
        }
    }
}
