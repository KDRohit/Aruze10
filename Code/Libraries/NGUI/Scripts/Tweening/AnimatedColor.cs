//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Makes it possible to animate a color of the widget.
/// </summary>

[ExecuteInEditMode]
[RequireComponent(typeof(UIWidget))]
public class AnimatedColor : TICoroutineMonoBehaviour
{
	public Color color = Color.white;
	
	UIWidget mWidget;

	void Awake () { mWidget = GetComponent<UIWidget>(); }
	void Update () { mWidget.color = color; }
}
