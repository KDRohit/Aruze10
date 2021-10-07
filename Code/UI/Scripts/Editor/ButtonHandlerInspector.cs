using UnityEngine;
using UnityEditor;
using System.Collections;
using TMPro;

[CustomEditor(typeof(ButtonHandler))]
public class ButtonHandlerInspector : Editor
{
	private const float PRESSED_RGB_VALUE= 153f/ 255f;
	private const float PRESSED_DURATION = 0.05f;
	private const float PRESSED_SCALE = 0.95f;
	private const float DISABLED_RGB_VALUE= 153f/ 255f;

	private ButtonHandler handler;
	
	public override void OnInspectorGUI()
	{
		handler = target as ButtonHandler;

		if (handler == null)
		{
			return;
		}

		if (handler.button == null)
		{
			GUI.contentColor = Color.yellow;
			GUILayout.Label("No UIButton found!");
			GUI.contentColor = Color.white;
		}

		if (handler.spriteCollider == null)
		{
			GUI.contentColor = Color.red;
			GUILayout.Label("No Box Collider found!");
			GUI.contentColor = Color.white;
		}

		if (handler.sprite == null)
		{
			GUI.contentColor = Color.red;
			GUILayout.Label("No UISprite found!");
			GUI.contentColor = Color.white;
		}

		if (handler.buttonScale == null)
		{
			GUI.contentColor = Color.red;
			GUILayout.Label("No UIButtonScale found!");
			GUI.contentColor = Color.white;			
		}
		
		if (handler.label == null)
		{
			GUI.contentColor = Color.yellow;
			GUILayout.Label("No Text Mesh Pro set. ");
			GUI.contentColor = Color.white;
		}
		
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
		handler.buttonScale = handler.GetComponent<UIButtonScale>();
		handler.button = handler.GetComponent<UIButton>();
		handler.spriteCollider = handler.GetComponent<BoxCollider>();
		handler.sprite = handler.GetComponent<UISprite>();
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
		handler.colors = handler.GetComponentsInChildren<ButtonColorExtended>();
	}

	private void setToDefaults()
	{
		if (handler == null)
		{
			return;
		}
		handler.buttonScale.duration = PRESSED_DURATION;
		handler.buttonScale.pressed = new Vector3(PRESSED_SCALE, PRESSED_SCALE, 1f);
		handler.buttonScale.hover = Vector3.one;

		if (handler.button != null)
		{
			handler.button.hover = Color.white;
			handler.button.pressed = new Color(PRESSED_RGB_VALUE, PRESSED_RGB_VALUE, PRESSED_RGB_VALUE, 1f);;
			handler.button.duration = PRESSED_DURATION;
		}


		if (handler.sprite != null)
		{
			// If we found a child, then use this to size the collider.
			Vector3 spriteSize = handler.sprite.transform.localScale;
			Vector3 colliderSize = new Vector3(spriteSize.x * 1.5f, spriteSize.y * 1.5f, 1);
			
			handler.spriteCollider.size = colliderSize;
			handler.spriteCollider.center = Vector3.zero;

			// If we have a child sprite then use it for the other elements as well.
			if (handler.button != null)
			{
				handler.button.tweenTarget = handler.sprite.gameObject;
			}

			handler.buttonScale.tweenTarget = handler.sprite.transform;
		}
		else
		{
			if (handler.button != null)
			{
				handler.button.tweenTarget = handler.gameObject;
			}

			handler.buttonScale.tweenTarget = handler.transform;
			handler.spriteCollider.size = Vector3.one;
			handler.spriteCollider.center = Vector3.zero;
		}

		if (handler.colors != null)
		{
			for (int i = 0; i < handler.colors.Length; i++)
			{
				handler.colors[i].hover = Color.white;
				handler.colors[i].pressed = new Color(PRESSED_RGB_VALUE, PRESSED_RGB_VALUE, PRESSED_RGB_VALUE, 1f);
				handler.colors[i].disabled = new Color(DISABLED_RGB_VALUE, DISABLED_RGB_VALUE, DISABLED_RGB_VALUE, 1f);
				handler.colors[i].duration = PRESSED_DURATION;
			}
		}
	}	
}
