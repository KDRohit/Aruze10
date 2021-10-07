using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class SpritePropertyCopier : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Editor Window Objects/Sprite Property Copier")]
	public static void openSpritePropertyCopier()
	{
		SpritePropertyCopier atlasViewer = (SpritePropertyCopier)EditorWindow.GetWindow(typeof(SpritePropertyCopier));
		atlasViewer.Show();
	}

	SpritePropertyCopierObject copierObject;
	public void OnGUI()
	{
		if (copierObject == null)
		{
			copierObject = new SpritePropertyCopierObject();
		}
		copierObject.drawGUI(position);
	}
}

public class SpritePropertyCopierObject : EditorWindowObject
{
	private bool shouldFoldout = false;
	private int numberOfAtlases = 1;

	private UIAtlas sourceAtlas;
	private List<UIAtlas> targetAtlases;

	protected override string getButtonLabel()
	{
		return "Sprite Property Copier";
	}

	protected override string getDescriptionLabel()
	{
		return "Takes in a source atlas and any number of target atlases. It finds any sprites that exist in the source atlas, and copy over the properties for that sprite. Currently only copies of the border attribute (for slicing), but can easily be modified to do more.";
	}

	public override void drawGuts(Rect position)
	{
		// Draw stuff
		GUILayout.BeginVertical();

		sourceAtlas = EditorGUILayout.ObjectField("Source Atlas", sourceAtlas, typeof(UIAtlas), allowSceneObjects:false) as UIAtlas;

		GUILayout.BeginHorizontal();
		numberOfAtlases = EditorGUILayout.IntField("Size", numberOfAtlases);
		if (GUILayout.Button("-"))
		{
			numberOfAtlases--;
			if (numberOfAtlases <= 0)
			{
				// No reason to allow an empty array here as the 
				numberOfAtlases = 1;
			}
		}
		if (GUILayout.Button("+"))
		{
			numberOfAtlases++;
		}

		GUILayout.EndHorizontal();

		if (numberOfAtlases > 0)
		{
			if (targetAtlases == null)
			{
				targetAtlases = new List<UIAtlas>();
			}
			if (targetAtlases != null && numberOfAtlases != targetAtlases.Count)
			{
				// If we have changed the number, then do behind the scenes array migration.
				List<UIAtlas> newAtlases = new List<UIAtlas>();
				if (targetAtlases != null)
				{
					for (int i = 0; i < numberOfAtlases; i++)
					{
						// Add all the old values (up to the new size);
						if (i < targetAtlases.Count)
						{
							newAtlases.Add(targetAtlases[i]);
						}
						else
						{
							newAtlases.Add(null);
						}
					}
				}
				// Now set the new one as the current, and update the size.
				targetAtlases = newAtlases;
			}

			if (targetAtlases != null)
			{
				for (int i = 0; i < targetAtlases.Count; i++)
				{
					targetAtlases[i] = EditorGUILayout.ObjectField(i.ToString(), targetAtlases[i], typeof(UIAtlas), false) as UIAtlas;
				}
			}
		}

		if (GUILayout.Button("Copy Properties"))
		{
			copyProperties();
		}
		GUILayout.EndVertical();
	}


	private void copyProperties()
	{
		for (int i = 0; i < targetAtlases.Count; i++)
		{
			UIAtlas currentAtlas = targetAtlases[i];
			if (currentAtlas == null)
			{
				continue;
			}
			List<UIAtlas.Sprite> sprites = currentAtlas.spriteList;
			for (int j = 0; j < sprites.Count; j++)
			{
				UIAtlas.Sprite currentSprite = sprites[j];
				string name = currentSprite.name;

				UIAtlas.Sprite sourceSprite = sourceAtlas.GetSprite(name);
				if (sourceSprite != null)
				{
					// If the source atlas has this sprite, then use its values for the new one.
					copySpriteBorder(sourceSprite, currentSprite);
					Debug.LogFormat("SpritePropertyCopier.cs -- copyProperties -- Copying the inner component for sprite: {0}", currentSprite.name);
				}
				currentAtlas.MarkAsDirty();
			}
			currentAtlas.MarkAsDirty();
			AssetDatabase.SaveAssets();
		}
	}


	// Copies over the sprite border values, a lot of this is ripped from UIAtlasInspector.cs to see how it accessed those attrributes.
	private void copySpriteBorder(UIAtlas.Sprite sourceSprite, UIAtlas.Sprite targetSprite)
	{
		Rect sourceInner = sourceSprite.inner;
		Rect sourceOuter = sourceSprite.outer;

		Rect targetInner = targetSprite.inner;
		Rect targetOuter = targetSprite.outer;

		Vector4 sourceBorder = new Vector4(
										   sourceSprite.inner.xMin - sourceSprite.outer.xMin,
										   sourceSprite.inner.yMin - sourceSprite.outer.yMin,
										   sourceSprite.outer.xMax - sourceSprite.inner.xMax,
										   sourceSprite.outer.yMax - sourceSprite.inner.yMax);

		Vector4 targetBorder = new Vector4(
										   targetSprite.inner.xMin - targetSprite.outer.xMin,
										   targetSprite.inner.yMin - targetSprite.outer.yMin,
										   targetSprite.outer.xMax - targetSprite.inner.xMax,
										   targetSprite.outer.yMax - targetSprite.inner.yMax);


		// Setting the new inner value
		targetInner.xMin = targetSprite.outer.xMin + sourceBorder.x;
		targetInner.yMin = targetSprite.outer.yMin + sourceBorder.y;
		targetInner.xMax = targetSprite.outer.xMax - sourceBorder.z;
		targetInner.yMax = targetSprite.outer.yMax - sourceBorder.w;

		if (targetOuter.xMax < targetOuter.xMin)
		{
			targetOuter.xMax = targetOuter.xMin;
		}
		if (targetOuter.yMax < targetOuter.yMin)
		{
			targetOuter.yMax = targetOuter.yMin;
		}

		if (targetInner != targetSprite.inner)
		{
			// If they don't match, add the difference.
			float x = targetOuter.xMin - targetSprite.outer.xMin;
			float y = targetOuter.yMin - targetSprite.outer.yMin;

			targetOuter.x += x;
			targetOuter.y += y;
		}

		// Sanity checks to ensure that the inner rect is always inside the outer
		targetInner.xMin = Mathf.Clamp(targetInner.xMin, targetOuter.xMin, targetOuter.xMax);
		targetInner.xMax = Mathf.Clamp(targetInner.xMax, targetOuter.xMin, targetOuter.xMax);
		targetInner.yMin = Mathf.Clamp(targetInner.yMin, targetOuter.yMin, targetOuter.yMax);
		targetInner.yMax = Mathf.Clamp(targetInner.yMax, targetOuter.yMin, targetOuter.yMax);


		if (targetSprite.inner != targetInner || targetSprite.outer != targetOuter)
		{
			targetSprite.inner = targetInner;
			targetSprite.outer = targetOuter;
		}
	}
}
