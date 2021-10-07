using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Base abstract class for all payline drawer classes.
The only reason for this so far is so we can get every kind of PaylineDrawer object with a GetComponents call, for cleanup.
*/

public abstract class PaylineDrawer
{
	private const float OUTLINE_THICKNESS = 0.1f;
	protected float outlineThickness
	{
		get { return OUTLINE_THICKNESS * paylineScaler; }
	}

	private const float INLINE_THICKNESS = 0.06f;
	protected float inlineThickness
	{
		get { return INLINE_THICKNESS * paylineScaler; }
	}

	private const float HIGHLIGHT_THICKNESS = 0.002f;
	protected float highlightThickness
	{
		get { return HIGHLIGHT_THICKNESS * paylineScaler; }
	}

	private const float SOFTNESS_THICKNESS = 0.01f;
	protected float softnessThickness
	{
		get { return SOFTNESS_THICKNESS * paylineScaler; }
	}
	
	protected PaylineMesh outlineMesh = null;
	protected PaylineMesh inlineMesh = null;
	protected PaylineMesh highlightMesh = null;
	
	protected float paylineScaler = 1.0f;
}
