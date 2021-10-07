using UnityEngine;
using System.Collections;

/**
Holds info about colliders for purposes of:
   selective rendering
   optimized selective raycasting
   optimized collisions and physics

The IGNOREBOUNDS layer is used as a flag to stop recursive bounds calculations from
including renderers on this layer.

The HIDDEN layer is used as a non-rendered space to stash things that you don't want
to activate/deactivate.
*/



public static class Layers
{
	public enum LayerID
	{
		// Builtin Layers
		ID_DEFAULT = 0,
		ID_WATER = 4,
		// NGUI Layers
		ID_NGUI = 8,
		ID_NGUI_IGNORE_BOUNDS = 9,
		ID_NGUI_OVERLAY = 10,
		ID_NGUI_LIST_OVERLAY = 11,
		ID_NGUI_PERSPECTIVE = 17,
		// Non-NGUI layers
		ID_3D_NO_NGUI = 12,
		// Big Win layers
		ID_BIG_WIN_2D = 13,
		ID_BIG_WIN_3D = 14,
		// For isolated bounds rendering
		ID_BOUNDS_RENDERING = 18,
		// Virtual rendering layer
		ID_VIRTUAL_RENDERING = 19,
		// Slot game layers
		ID_SLOT_BACKGROUND = 20,
		ID_SLOT_REELS = 21,
		ID_SLOT_FRAME = 22,
		ID_SLOT_OVERLAY = 23,
		ID_SLOT_PAYLINES = 24,
		ID_SLOT_REELS_OVERLAY = 25,
		ID_SLOT_TRANSITION = 29,
		// Foreground Layers
		ID_SLOT_FOREGROUND = 26,
		ID_SLOT_FOREGROUND_REELS = 27,
		ID_SLOT_FOREGROUND_FRAME = 28,
		// Hidden layer
		ID_HIDDEN = 31
}


	// Builtin Layers
	public const int ID_DEFAULT = (int)LayerID.ID_DEFAULT;
	public const int ID_WATER = (int)LayerID.ID_WATER;

	// NGUI Layers
	public const int ID_NGUI = (int)LayerID.ID_NGUI;
	public const int ID_NGUI_IGNORE_BOUNDS = (int)LayerID.ID_NGUI_IGNORE_BOUNDS;
	public const int ID_NGUI_OVERLAY = (int)LayerID.ID_NGUI_OVERLAY;
	public const int ID_NGUI_LIST_OVERLAY = (int)LayerID.ID_NGUI_LIST_OVERLAY;
	public const int ID_NGUI_PERSPECTIVE = (int)LayerID.ID_NGUI_PERSPECTIVE;
	
	// Non-NGUI layers
	public const int ID_3D_NO_NGUI = (int)LayerID.ID_3D_NO_NGUI;
	
	// Big Win layers
	public const int ID_BIG_WIN_2D = (int)LayerID.ID_BIG_WIN_2D;
	public const int ID_BIG_WIN_3D = (int)LayerID.ID_BIG_WIN_3D;

	// For Bounds Rendering
	public const int ID_BOUNDS_RENDERING = (int)LayerID.ID_BOUNDS_RENDERING;
	
	// For virtual rendering (i.e. game renders to a render texture that is displayed in this layer)
	public const int ID_VIRTUAL_RENDERING = (int)LayerID.ID_VIRTUAL_RENDERING;
	
	// Slot game layers
	public const int ID_SLOT_BACKGROUND = (int)LayerID.ID_SLOT_BACKGROUND;
	public const int ID_SLOT_REELS = (int)LayerID.ID_SLOT_REELS;
	public const int ID_SLOT_FRAME = (int)LayerID.ID_SLOT_FRAME;
	public const int ID_SLOT_OVERLAY = (int)LayerID.ID_SLOT_OVERLAY;
	public const int ID_SLOT_PAYLINES = (int)LayerID.ID_SLOT_PAYLINES;
	public const int ID_SLOT_REELS_OVERLAY = (int)LayerID.ID_SLOT_REELS_OVERLAY;
	public const int ID_SLOT_TRANSITION = (int)LayerID.ID_SLOT_TRANSITION;

	// Foreground Layers
	public const int ID_SLOT_FOREGROUND = (int)LayerID.ID_SLOT_FOREGROUND;
	public const int ID_SLOT_FOREGROUND_REELS = (int)LayerID.ID_SLOT_FOREGROUND_REELS;
	public const int ID_SLOT_FOREGROUND_FRAME = (int)LayerID.ID_SLOT_FOREGROUND_FRAME;
	
	// Hidden layer
	public const int ID_HIDDEN = (int)LayerID.ID_HIDDEN;

	// Builtin Layers
	public const int FLAG_DEFAULT = 0;

	// NGUI Layers
	public const int FLAG_NGUI = 1 << ID_NGUI;
	public const int FLAG_NGUI_IGNORE_BOUNDS = 1 << ID_NGUI_IGNORE_BOUNDS;
	public const int FLAG_NGUI_OVERLAY = 1 << ID_NGUI_OVERLAY;
	public const int FLAG_NGUI_LIST_OVERLAY = 1 << ID_NGUI_LIST_OVERLAY;

	// Non-NGUI layers
	public const int FLAG_3D_NO_NGUI = 1 << ID_3D_NO_NGUI;
	
	// Big Win layers
	public const int FLAG_BIG_WIN_2D = 1 << ID_BIG_WIN_2D;
	public const int FLAG_BIG_WIN_3D = 1 << ID_BIG_WIN_3D;

	// For Bounds Rendering
	public const int FLAG_BOUNDS_RENDERING  = 1 << ID_BOUNDS_RENDERING;
	
	// For virtual screen rendering (for rendering the whole game into a render texture)
	public const int FLAG_VIRTUAL_RENDERING = 1 << ID_VIRTUAL_RENDERING;

	// Slot game layers
	public const int FLAG_SLOT_BACKGROUND = 1 << ID_SLOT_BACKGROUND;
	public const int FLAG_SLOT_REELS = 1 << ID_SLOT_REELS;
	public const int FLAG_SLOT_FRAME = 1 << ID_SLOT_FRAME;
	public const int FLAG_SLOT_OVERLAY = 1 << ID_SLOT_OVERLAY;
	public const int FLAG_SLOT_PAYLINES = 1 << ID_SLOT_PAYLINES;
	
	// Hidden layer
	public const int FLAG_HIDDEN = 1 << ID_HIDDEN;
}
