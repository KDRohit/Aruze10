using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/**
This overlay is to indicate Giant Jackpot games of all kinds for 1x2 squares
*/

public class MultiJackpotLobbyOptionDecorator1x2 : LobbyOptionDecorator
{
	public TextMeshPro[] jackpotLabels;

	public static GameObject overlayPrefab;

	public const string LOBBY_PREFAB_PATH = "Lobby Option Decorators/Multi Jackpot Lobby Option Decorator 1x2";

	public const float TEXTURE_RELATIVE_Y = 0.5f;
	public const float TEXTURE_ANCHOR_PIXEL_OFFSET_Y = -185.0f;

	public static void loadPrefab(GameObject parentObjectect, LobbyOptionButtonGeneric option)
	{
		prepPrefabForLoading(overlayPrefab, parentObjectect, option, LOBBY_PREFAB_PATH, typeof(MultiJackpotLobbyOptionDecorator1x2));
	}
	
	public static void cleanup()
	{
		overlayPrefab = null;
	}

	protected override void setup()
	{
		if (MainLobby.hirV3 != null)
		{
			MainLobby.hirV3.masker.addObjectArrayToList(jackpotLabels);
		}

		if (parentOption != null && parentOption.option != null && parentOption.option.game != null)
		{
			parentOption.option.game.registerMultiProgressiveLabels(jackpotLabels, false);

			// this overlay uses a 1x1 texture in the top half, so we need to resize the anchor/stretchy settings for the texture which defualted to 1x2
			UIStretch stretcher = parentOption.image.GetComponent<UIStretch>();
			if (stretcher != null)
			{
				stretcher.relativeSize.y = TEXTURE_RELATIVE_Y;
			}

			UIAnchor anchor = parentOption.image.GetComponent<UIAnchor>();
			if (anchor != null)
			{
				anchor.pixelOffset.y = TEXTURE_ANCHOR_PIXEL_OFFSET_Y;
			}

			parentOption.refresh();	
		}
	}

	protected override void setOverlayPrefab(GameObject prefabFromAssetBundle, string assetPath)
	{
		overlayPrefab = prefabFromAssetBundle;
	}
}
