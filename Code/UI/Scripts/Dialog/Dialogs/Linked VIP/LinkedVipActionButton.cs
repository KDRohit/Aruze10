using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
This button has gotten to be quite elaborate, so this script helps sort that out
without the need to have the code in every dialog that uses the button.
*/

public class LinkedVipActionButton : MonoBehaviour
{
	private const float THROB_DELAY = 1.25f;
	private const float THROB_SCALE = 1.1f;
	private const float THROB_DURATION = 0.5f;
	
	public UIButton button;
	public TextMeshPro label;
	public GameObject throbSizer;       // leave null to have no throb
	public GameObject sparkleParticles; // leave null to have no sparkle
	
	private Throbamatic throbber = null;
	
	public bool isEnabled
	{
		get { return button.isEnabled; }
		
		set
		{
			button.isEnabled = value;
			// We have to tint the label separately since UIButton
			// doesn't support tinting multiple objects when disabled.
			label.color = (value ? Color.white : button.disabledColor);

			if (sparkleParticles != null)
			{
				sparkleParticles.SetActive(value);
			}
		}
	}
	
	void Awake()
	{
		if (throbSizer != null)
		{
			throbber = new Throbamatic(this, throbSizer, THROB_DELAY, THROB_SCALE, THROB_DURATION);
		}
	}
	
	void Update()
	{
		if (throbber != null)
		{
			if(button.isEnabled)
				throbber.update();
		}
	}
}
