using UnityEngine;
using System.Collections;
using System;
using UnityEngine;

/// <summary>
/// Assigned texture to use for state driven tech leveraging ObjectSwap classes
/// </summary>
[Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class TextureState : SwapperState
{
	public Texture textureToUse;
	public Material materialToUse;
}