using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class ImageButtonSpriteChanger : EditorWindow
{
	[MenuItem("Zynga/Editor Tools/Editor Window Objects/ImageButtonSpriteChanger")]
	public static void openImageButtonSpriteChanger()
	{
		ImageButtonSpriteChanger imageButtonSpriteChanger = (ImageButtonSpriteChanger)EditorWindow.GetWindow(typeof(ImageButtonSpriteChanger));
		imageButtonSpriteChanger.Show();
	}

	private ImageButtonSpriteChangerObject spriteChanger;	
	public void OnGUI()
	{
		if (spriteChanger == null)
		{
			spriteChanger = new ImageButtonSpriteChangerObject();
		}
		spriteChanger.drawGUI(position);
	}
}

public class ImageButtonSpriteChangerObject : EditorWindowObject
{
	public UIAtlas sourceAtlas;
	public UIAtlas populatedSourceAtlas;
	public string[] sourceSpriteNameArray;
	public int selectedSourceSpriteUpIndex = 0;
	public int selectedSourceSpriteDownIndex = 0;

	public UIAtlas targetAtlas;
	public UIAtlas populatedTargetAtlas;
	public string[] targetSpriteNameArray;
	public int selectedTargetSpriteUpIndex = 0;
	public int selectedTargetSpriteDownIndex = 0;


	public GameObject targetObject;

	protected override string getButtonLabel()
	{
		return "Image Button Sprite Swapper";
	}

	protected override string getDescriptionLabel()
	{
		return "Finds any UIImageButton objects that match that criteria provided (sourceAtlas, and up/down sprite) and swaps them to use the new atlas and sprites provided.";
	}

	public override void drawGuts(Rect position)
	{
		GUILayout.BeginVertical();
		targetObject = EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), allowSceneObjects:true) as GameObject;
		sourceAtlas = EditorGUILayout.ObjectField("Source Atlas", sourceAtlas, typeof(UIAtlas), allowSceneObjects:false) as UIAtlas;
		if (sourceAtlas != null )
		{
			if (sourceAtlas != populatedSourceAtlas)
			{
				// Populate the string enum dropdown.
				populatedSourceAtlas = sourceAtlas;
				BetterList<string> spriteNames = sourceAtlas.GetListOfSprites();
				spriteNames.Sort(alphabetSort);
				sourceSpriteNameArray = spriteNames.ToArray();
			}
			selectedSourceSpriteUpIndex = EditorGUILayout.Popup("Up Sprite", selectedSourceSpriteUpIndex, sourceSpriteNameArray);
			selectedSourceSpriteDownIndex = EditorGUILayout.Popup("Down Sprite", selectedSourceSpriteDownIndex, sourceSpriteNameArray);
		}

		targetAtlas = EditorGUILayout.ObjectField("Target Atlas", targetAtlas, typeof(UIAtlas), allowSceneObjects:false) as UIAtlas;
		if (targetAtlas != null )
		{
			if (targetAtlas != populatedTargetAtlas)
			{
				// Populate the string enum dropdown.
				populatedTargetAtlas = targetAtlas;
				BetterList<string> spriteNames = targetAtlas.GetListOfSprites();
				spriteNames.Sort(alphabetSort);
				targetSpriteNameArray = spriteNames.ToArray();
			}
			selectedTargetSpriteUpIndex = EditorGUILayout.Popup("Up Sprite", selectedTargetSpriteUpIndex, targetSpriteNameArray);
			selectedTargetSpriteDownIndex = EditorGUILayout.Popup("Down Sprite: ", selectedTargetSpriteDownIndex, targetSpriteNameArray);
		}

		if (GUILayout.Button("Swap Sprites!"))
		{
			swapImageButtonSprites(sourceAtlas, sourceSpriteNameArray[selectedSourceSpriteUpIndex], sourceSpriteNameArray[selectedSourceSpriteDownIndex], targetAtlas, targetSpriteNameArray[selectedTargetSpriteUpIndex], targetSpriteNameArray[selectedTargetSpriteDownIndex], targetObject);
		}
		GUILayout.EndVertical();
	}

	// This is needed because the NGUI functions use BetterList, which doesnt have a default sort.
	private int alphabetSort(string a, string b)
	{
		return a.CompareTo(b);
	}
		
	private void swapImageButtonSprites(UIAtlas sourceAtlas, string sourceUpSprite, string sourceDownSprite, UIAtlas targetAtlas, string targetUpSprite, string targetDownSprite, GameObject target)
	{
		List<UIImageButton> imageButtons = new List<UIImageButton>(target.GetComponentsInChildren<UIImageButton>());
		for (int i = 0; i < imageButtons.Count; i++)
		{
			UIImageButton button = imageButtons[i];

			if (button.target.atlas == sourceAtlas &&
				button.normalSprite == sourceUpSprite &&
				button.pressedSprite == sourceDownSprite)
			{
				button.normalSprite = targetUpSprite;
				button.hoverSprite = targetUpSprite;
				button.pressedSprite = targetDownSprite;
				button.disabledSprite = targetUpSprite;

				// Set the target its pointing to as well.
				button.target.spriteName = targetUpSprite;
				button.target.atlas = targetAtlas;
				Debug.LogFormat("ImageButtonSpriteChanger.cs -- swapImageButtonSprites -- converting {0} to use the new sprites", button.gameObject.name);
			}
			else
			{
				Debug.LogFormat("ImageButtonSpriteChanger.cs -- swapImageButtonSprites -- not converting {0}, did not match criteria", button.gameObject.name);
			}
		}
	}
}