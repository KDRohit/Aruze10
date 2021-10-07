using UnityEngine;
using System.Collections;
using TMPro;

public class PPULightObject : MonoBehaviour
{
	public UISprite[] lights;
	private const string LIGHT_ON = "lights on";
	private const string LIGHT_OFF = "lights off";

	public void setLights(bool areLightsOn = false)
	{
		string lightState = areLightsOn ? LIGHT_ON : LIGHT_OFF;

		for (int i = 0; i < lights.Length; i++)
		{
			lights[i].spriteName = lightState;
		}
	}

}