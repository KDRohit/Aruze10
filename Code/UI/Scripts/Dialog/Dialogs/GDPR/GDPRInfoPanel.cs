
using UnityEngine;
using TMPro;

public class GDPRInfoPanel : MonoBehaviour 
{
	private const string contentTextKey = "gdpr_data_request_text";
	public ButtonHandler urlButton;
	public TextMeshPro zidLabel;
	public TextMeshPro pinLabel;
	public TextMeshPro contentLabel;


	public void SetZID(string zid)
	{
		zidLabel.text = Localize.text(GDPRDialog.ZID_LOCALIZE_KEY, zid);
		
	}

	public void SetPin(string pin)
	{   
		pinLabel.text = Localize.text(GDPRDialog.PIN_LOCALIZE_KEY, pin);
	}

	public void SetUrl(string url)
	{
		string contentText = Localize.text(contentTextKey, "cs_url:" + url);
		contentLabel.text = contentText;

	}

	private void supportSiteClick(Dict args)
	{
		Common.openSupportUrl(Glb.HELP_LINK_SUPPORT);
	}	

	public void init()
	{
		if (null != urlButton)
		{
			urlButton.registerEventDelegate(supportSiteClick);
		}
	}
}
