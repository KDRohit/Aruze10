using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LottoBlastProgressNode : MonoBehaviour
{
	[SerializeField] private LabelWrapperComponent levelLabel;
	[SerializeField] private UIMeterNGUI meter;
	[SerializeField] private UISprite nodeSprite;
	[SerializeField] private ObjectSwapper swapper;
	
	public void setup(int level, long target, long prevValue, float width, bool showBuffActiveState)
	{
		long currentValue = SlotsPlayer.instance.xp.amount;
		
		levelLabel.text = CommonText.formatNumber(level);
		
		//The final Node is fixed and has no meter
		if (meter == null)
		{
			return;
		}

		if (swapper != null)
		{
			if (showBuffActiveState)
			{
				swapper.setState("powerup_active");	
			}
			else
			{
				swapper.setState("powerup_inactive");
			}
				
		}
		
		meter.setSpriteScale(width);

		if (currentValue > target)
		{
			meter.setState(1,1);
		}
		else
		{
			float fillAmount = Mathf.Max(currentValue - prevValue, 0);
			if (fillAmount <= 0)
			{
				nodeSprite.gameObject.SetActive(false);
			}
			float normalizedTarget = target - prevValue;
			meter.setState((long)fillAmount, (long)normalizedTarget);
		}
	}
}
