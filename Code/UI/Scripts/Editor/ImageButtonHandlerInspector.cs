using UnityEngine;
using UnityEditor;
using System.Collections;
using TMPro;

[CustomEditor(typeof(ImageButtonHandler))]
public class ImageButtonHandlerInspector : Editor
{
	private const float PRESSED_RGB_VALUE= 153f/ 255f;
	private const float PRESSED_DURATION = 0.05f;
	private const float PRESSED_SCALE = 0.95f;
	
	private ImageButtonHandler handler;
	
	public override void OnInspectorGUI()
	{
		handler = target as ImageButtonHandler;

		if (handler == null)
		{
			return;
		}
		// Display Errors
		GUI.contentColor = Color.red;
		if (handler.spriteCollider == null)
		{
			GUILayout.Label("No Box Collider found!");
		}
		
		if (handler.sprite == null)
		{
			GUILayout.Label("No UISprite found!");
		}

		if (handler.imageButton == null)
		{
			GUILayout.Label("No UIImageButton set!.");
		}
		else
		{
			if (string.IsNullOrEmpty(handler.imageButton.normalSprite))
			{
				GUILayout.Label("No Normal Sprite value set.");
			}
		
			if (string.IsNullOrEmpty(handler.imageButton.pressedSprite))
			{
				GUILayout.Label("No Pressed Sprite value set.");
			}		

			if (string.IsNullOrEmpty(handler.imageButton.hoverSprite))
			{
				GUILayout.Label("No Hover Sprite value set.");
			}

			if (string.IsNullOrEmpty(handler.imageButton.disabledSprite))
			{
				GUILayout.Label("No Disabled Sprite value set.");
			}
		}

		
		// Display Warnings
		GUI.contentColor = Color.yellow;
		if (handler.button == null)
		{
			GUILayout.Label("(Optional) No UIButton set.");
		}
				
		if (handler.buttonScale == null)
		{
			GUILayout.Label("(Optional) No UIButtonScale set.");
		}
		
		if (handler.label == null)
		{
			GUILayout.Label("(Optional) No Text Mesh Pro set. ");
		}
		GUI.contentColor = Color.white;
		
		DrawDefaultInspector();

		// Buttons
		if (GUILayout.Button("Reset"))
		{
			Reset();
		}
		if (GUILayout.Button("Relink"))
		{
			setupLinks();
		}
		if (Application.isPlaying && GUILayout.Button("Trigger!"))
		{ 
			handler.OnClick();
		}
	}


	// Reset is only called when in editor and the component is added for the first
	private void Reset()
	{
		setupLinks();
		setToDefaults();
	}	

	private void setupLinks()
	{
		if (handler == null)
		{
			return;
		}
		handler.imageButton = handler.GetComponent<UIImageButton>();
		handler.buttonScale = handler.GetComponent<UIButtonScale>();
		handler.spriteCollider = handler.GetComponent<BoxCollider>();
		handler.sprite = handler.GetComponent<UISprite>();
		handler.button = handler.GetComponent<UIButton>();
		if (handler.sprite == null)
		{
			// if this isnt all on one gameobject, then find a child sprite.
			handler.sprite = handler.GetComponentInChildren<UISprite>();
			if (handler.sprite == null)
			{
				Debug.LogError("ButtonHandler -- setupLinks -- You have no sprite in the hierarchy, please fix this.");
			}
		}
		
		handler.label = handler.GetComponent<TextMeshPro>();
		if (handler.label == null)
		{
			handler.label = handler.GetComponentInChildren<TextMeshPro>();
		}
		
	}

	private void setToDefaults()
	{
		if (handler == null)
		{
			return;
		}
		
		if (handler.buttonScale != null)
		{
			handler.buttonScale.duration = PRESSED_DURATION;
			handler.buttonScale.pressed = new Vector3(PRESSED_SCALE, PRESSED_SCALE, 1f);;
			handler.buttonScale.hover = Vector3.one;
		}

		if (handler.sprite != null)
		{
			// If we found a child, then use this to size the collider.
			Vector3 spriteSize = handler.sprite.transform.localScale;
			Vector3 colliderSize = new Vector3(spriteSize.x * 1.5f, spriteSize.y * 1.5f, 1);
			
			handler.spriteCollider.size = colliderSize;
			handler.spriteCollider.center = Vector3.zero;

			// If we have a child sprite then use it for the other elements as well.
			handler.imageButton.target = handler.sprite;
			if (handler.buttonScale != null)
			{
				handler.buttonScale.tweenTarget = handler.sprite.transform;
			}			
		}
		else
		{
			if (handler.imageButton != null)
			{
				handler.imageButton.target = null;
			}
			if (handler.buttonScale != null)
			{
				handler.buttonScale.tweenTarget = handler.transform;
			}

			handler.spriteCollider.size = Vector3.one;
			handler.spriteCollider.center = Vector3.zero;
		}
	}	
}
