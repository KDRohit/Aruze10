using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for state driven materials, textures, and sprites. Can be extended to provide new fields
/// to be assign and manipulated per states or individual requirements. See MaterialState, SpriteState, and TextureState
/// for examples
/// </summary>
[Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public class SwapperState
{
	public string state;
}