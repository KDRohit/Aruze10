using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMProExtensions;

/*
Positions characters in a TextMeshPro object into the shape of an arc.
This is a standalone effect. It can't be combined with other TMPro effects due to needing to
force a mesh update before applying each effect.
*/

[ExecuteInEditMode]
public class TMProArc : MonoBehaviour
{
	public TextMeshPro tmPro;
	public int radius;

	// Angles in degrees, where angle 0 is the top of the arc. Positive is counter-clockwise. Negative is clockwise, to match Unity transform rotation.
	public int angleStart = 90;
	public int angleEnd = -90;
	
	private string lastText = "";
	private int lastRadius = 0;
	private int lastAngleStart = 0;
	private int lastAngleEnd = 0;
	
	private bool doesNeedUpdate
	{
		get
		{
			return
				lastText != tmPro.text ||
				lastRadius != radius ||
				lastAngleStart != angleStart ||
				lastAngleEnd != angleEnd;
		}
	}
	
	void Awake()
	{
		if (tmPro == null)
		{
			tmPro = gameObject.GetComponent<TextMeshPro>();
		}
	}
	
	void Update()
	{
		if (tmPro == null)
		{
			return;
		}
		
		if (doesNeedUpdate)
		{
			// Text changed, so rebuild the arc.
			refresh();
		}
	}
	
	public void refresh()
	{
		tmPro.enableWordWrapping = false;					// Word wrapping can't be enabled for arc text.
		tmPro.text = tmPro.text.Replace("\n", " ").Trim();	// The entire text must be on a single line for the arc.
		
		// Start with the default positioning of everything by forcing a mesh update first.
		tmPro.ForceMeshUpdate();
		
		// To maintain proper character spacing on the arc,
		// we find the original X position of the first and last characters,
		// and compare each character to those to determine the relative X position.
		float firstCharX = tmPro.textInfo.characterInfo[0].getCharacterCenter().x;
		float lastCharX = tmPro.textInfo.characterInfo[tmPro.textInfo.characterCount - 1].getCharacterCenter().x;
		
		for (int i = 0; i < tmPro.textInfo.characterCount; i++)
		{
			float charX = tmPro.textInfo.characterInfo[i].getCharacterCenter().x;
			float relativeCharPos = Mathf.InverseLerp(firstCharX, lastCharX, charX);
			
			float angle = Mathf.Lerp(angleStart, angleEnd, relativeCharPos);
			float radians = CommonMath.degreesToRadians(angle);
			Vector3 pos = new Vector3(-Mathf.Sin(radians), Mathf.Cos(radians), 0.0f) * radius;
			
			tmPro.transformCharacter(i,
				position:	pos,
				scale:		Vector2.one,	// No scaling
				rotation:	angle
			);
		}

		lastText = tmPro.text;
		lastRadius = radius;
	}
}
