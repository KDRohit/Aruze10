using UnityEngine;
using System.Collections;
using System.Text;
using TMPro;

public class PackagedToasterHIR : ProgressiveJackpotToaster
{
	// =============================
	// PUBLIC
	// =============================
	public GameObject giantIcon;
	public GameObject jackpotIcon;
	public GameObject megaJackpotIcon;
	public GameObject jackpotDaysIcon;
	public GameObject maxVoltageIcon;
	public GameObject vipIcon;

	public UISprite background;
	public TextMeshPro levelText;
	public UIStretchTextMeshPro uiStretch;
	
	// =============================
	// PRIVATE
	// =============================
	private GameObject currentIcon;

	// =============================
	// CONST
	// =============================	
	protected const string GIANT_JACKPOT_SPRITE = "Giant Jackpot Toast Panel Stretchy";
	protected const string JACKPOT_DAYS_SPRITE = "Jackpot Days Toast Panel Stretchy";
	protected const string PROGRESSIVE_JACKPOT_SPRITE = "Progressive Jackpots Toast Panel Stretchy";
	protected const string VIP_JACKPOT_SPRITE = "VIP Jackpot Toast Panel Stretchy";
	protected const string MAX_VOLTAGE_JACKPOT_SPRITE = "VIP Jackpot Toast Panel Stretchy";

	protected const float MAX_TEXT_WIDTH = 300f;
	protected const float MAX_ICON_SIZE = 316f;
	protected const float RELATIVE_MAX_CHARS = 16f;
	protected const float DEFAULT_BACKGROUND_SIZE = 1024f;
	protected const float PADDING = 40f;

	private const string LEVEL_TEXT = "LVL \n";
	
	public override string introAnimationName { get { return "Anim"; } }
	
	public override string outroAnimationName { get { return "Off"; } }
	
	public override void init(ProtoToaster proto)
	{
		disableAllIcons();
		int unlockLevel = 0;
		Color newColor = Color.white;
		if (proto.args.ContainsKey(D.UNLOCK_LEVEL))
		{
			JSON data = proto.args.getWithDefault(D.CUSTOM_INPUT, null) as JSON;
			long creditAmount = 0L;
			
			if (data != null)
			{
				creditAmount = data.getLong("credits", 0L);
			}
 
			unlockLevel = (int)proto.args[D.UNLOCK_LEVEL];
			
			if (creditAmount <= 0)
			{
				creditAmount = (long)proto.args.getWithDefault(D.TOTAL_CREDITS, 0L);
			}
			
			StatsManager.Instance.LogCount("toaster", "jackpot_info", "" ,proto.type.ToString(),unlockLevel.ToString(), "view", creditAmount);
			if (levelText != null)
			{
				levelText.text = string.Format("{0}{1}", LEVEL_TEXT, CommonText.formatNumber(unlockLevel));
			}
		}
		else
		{
			SafeSet.gameObjectActive(levelText.gameObject, false);
		}

		switch(proto.type)
		{
			case ToasterType.VIP_REVAMP_PROGRESSIVE:
				currentIcon = vipIcon;
				background.spriteName = VIP_JACKPOT_SPRITE;
				
				if (levelText != null)
				{
					ColorUtility.TryParseHtmlString("#9957b4", out newColor);
					levelText.color = newColor;
				}
				break;

			case ToasterType.GIANT_PROGRESSIVE:
				currentIcon = giantIcon;
				background.spriteName = GIANT_JACKPOT_SPRITE;
				
				if (levelText != null)
				{
					ColorUtility.TryParseHtmlString("#c8614e", out newColor);
					levelText.color = newColor;
				}
				break;

			case ToasterType.JACKPOT_DAYS:
			case ToasterType.BUY_COIN_PROGRESSIVE:
				currentIcon = jackpotDaysIcon;
				background.spriteName = JACKPOT_DAYS_SPRITE;
				levelText.gameObject.SetActive(false);
				break;

			case ToasterType.MAX_VOLTAGE:
				StatsManager.Instance.LogCount(
					counterName: "dialog",
					kingdom: "max_voltage",
					phylum: "toaster_notif",
					klass: "",
					family: "",
					genus: "view");
				currentIcon = maxVoltageIcon;
				background.spriteName = VIP_JACKPOT_SPRITE;
				
				if (levelText != null)
				{
					ColorUtility.TryParseHtmlString("#9957b4", out newColor);
					levelText.color = newColor;
				}
				break;
			
			case ToasterType.MEGA_JACKPOT:
				currentIcon = megaJackpotIcon;
				if (levelText != null)
				{
					ColorUtility.TryParseHtmlString("#c9893e", out newColor);
					levelText.color = newColor;
				}
				background.spriteName = PROGRESSIVE_JACKPOT_SPRITE;
				break;

			default:
				currentIcon = jackpotIcon;
				if (levelText != null)
				{
					ColorUtility.TryParseHtmlString("#c9893e", out newColor);
					levelText.color = newColor;
				}
				background.spriteName = PROGRESSIVE_JACKPOT_SPRITE;
				break;
		}

		SafeSet.gameObjectActive(currentIcon, true);
		
		base.init(proto);

		addTransforms();
	}

	private void addTransforms()
	{
		// calculate the width the background needs to be
		GameObject icon = currentIcon;

		// use the jackpot icon for determining size since the giant jackpot is actually two separate assets
		if ( currentIcon == giantIcon )
		{
			icon = jackpotIcon;
		}

		float charCount = Mathf.Max(poolAmountLabel.text.Length, playerNameLabel.text.Length);
		float iconSize = Mathf.Min(MAX_ICON_SIZE - (float)icon.transform.localScale.x, MAX_ICON_SIZE);
		float backgroundSize = DEFAULT_BACKGROUND_SIZE - Mathf.Min(MAX_TEXT_WIDTH, (MAX_TEXT_WIDTH - (charCount/RELATIVE_MAX_CHARS * MAX_TEXT_WIDTH))) - iconSize;

		background.transform.localScale = new Vector3(backgroundSize, background.transform.localScale.y, 1);
	}

	protected override void introAnimation()
	{
		base.introAnimation();
		Audio.play("MVSomebodyWonJackpot");
	}
	
	protected override void onImageSet(bool didSucceed)
	{
		runImageSetLogic(didSucceed, false);
		StartCoroutine(startAnim());
	}

	private IEnumerator startAnim()
	{
		if (this == null || this.gameObject == null)
		{
			yield break;
		}
		
		//activate game object
		gameObject.SetActive(true);
			
		//stop playing the default animation and set to end of off state to ensure all tmpro objects are enabled so bounds will
		//calculate correctly.
		if (animator != null)
		{
			animator.Play("Off", -1, 1.0f);
		}

		//wait at least one full frame to ensure animation stops
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		
		//safety check if object has been destroyed
		if (this == null || this.gameObject == null)
		{
			yield break;
		}
		
		//run the refresh on the UI stretch
		uiStretch.refresh();
			
		// Now trigger the intro Animation
		introAnimation();
	}

	private void disableAllIcons()
	{
		SafeSet.gameObjectActive(giantIcon, false);
		SafeSet.gameObjectActive(jackpotIcon, false);
		SafeSet.gameObjectActive(megaJackpotIcon, false);
		SafeSet.gameObjectActive(jackpotDaysIcon, false);
		SafeSet.gameObjectActive(maxVoltageIcon, false);
		SafeSet.gameObjectActive(vipIcon, false);
	}
}