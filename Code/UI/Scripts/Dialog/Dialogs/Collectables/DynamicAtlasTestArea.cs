using System.Collections;
using System.Collections.Generic;
using Com.Scheduler;
using UnityEngine;

public class DynamicAtlasTestArea : DialogBase 
{
	public UISprite spriteToDuplicate;
	DynamicAtlas passedDynamicAtlas;

	/// Initialization
	public override void init()
	{
		// If this fails someone passed a null atlas.
		passedDynamicAtlas = dialogArgs[D.DATA] as DynamicAtlas;
		bool hasSetupAtlas = false;

		for (int i = 0; i < passedDynamicAtlas.atlasHandle.spriteList.Count; i++)
		{
			GameObject spriteObject = NGUITools.AddChild(sizer.gameObject, spriteToDuplicate.gameObject);

			if (spriteObject != null)
			{
				UISprite spriteHandle = spriteObject.GetComponent<UISprite>();

				if (!hasSetupAtlas)
				{
					hasSetupAtlas = true;
				}

				spriteHandle.spriteName = passedDynamicAtlas.atlasHandle.spriteList[i].name;
				spriteHandle.MakePixelPerfect();
			}
		}
	}

	// Cleanup
	public override void close()
	{
		
	}

	// required for Unity/Mono compiler
	private void checkBackButton()
	{
		Dialog.close();
	}

	void Update()
	{
		AndroidUtil.checkBackButton(checkBackButton);
	}

	public static void showDialog(DynamicAtlas dynamicAtlas)
	{
		Dict args = Dict.create(D.DATA, dynamicAtlas);
		Scheduler.addDialog("dynamic_atlas_test", args);
	}
}
