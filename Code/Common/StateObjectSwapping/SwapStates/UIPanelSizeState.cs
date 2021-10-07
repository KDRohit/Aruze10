using System;
using UnityEngine;
using System.Collections;

[Serializable]
[System.Obsolete("Animation list should be used instead of object swapper")]
public sealed class UIPanelSizeState : SwapperState
{
	[Tooltip("X = Center X\nY = Center Y\nZ = Size X\nW = Size Y")]
	public Vector4 centerAndSize;
}