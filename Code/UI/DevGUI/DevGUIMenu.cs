using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Base class for all Dev GUI menus.
*/

public abstract class DevGUIMenu : IResetGame
{	
	private const string CLASS_NAME_FORMAT = "DevGUIMenu{0}";
	private const float TOOLTIP_DELAY_ON = 2.0f;

	public string name = "";
	public bool isInLobby = true;
	public bool isInGame = true;
	public string specificGameKey = "";		// Should this menu only be shown when in a specific game? If so, this is the game key to show it in.
	public DevGUIMenu parent = null;
	public List<DevGUIMenu> children = new List<DevGUIMenu>();

	public delegate void onEmailClickDelegate();

	private Vector2 scrollPosition;

	public static bool isHiRes = false;
	public static bool shouldIncludeScreenshot = false;
	
	private static DevGUIMenu mainMenu = null;
	private static DevGUIMenu currentMenu = null;
	private static DevGUIMenu switchToMenu = null;	// Temp variable so the currentMenu doesn't switch between
														// OnGUI's Layout and Repaint events, confusing the shit out of Unity.
	
	private static List<DevGUIMenu> stack = new List<DevGUIMenu>();
	
	private static Rect dragRect = new Rect(0, 0, 10000, 10000);

	private static Matrix4x4 svMat = Matrix4x4.identity;
	
	private static Color colorSelected = new Color(0.0f, 1.0f, 0.0f);
	private static GUIContent buttonContent = new GUIContent();
	private static GUIStyle buttonSelectedStyle;
	private static GUIContent tooltipContent = new GUIContent();
	private static GUIStyle tooltipStyle;
	private static float tooltipDelayOn;

	protected static string[] onOff = { "Off", "On" };
	
	private bool shouldBeShown
	{
		get
		{
			if (specificGameKey != "" &&
				!GameState.isMainLobby &&
				GameState.game.keyName != specificGameKey
				)
			{
				// This menu option is only available if a specific game is currently loaded.
				return false;
			}
			
			return
				(isInLobby && GameState.isMainLobby) ||
				(isInGame && !GameState.isMainLobby);
		}
	}
	
	public static void populateAll()
	{
		TextAsset textAsset = SkuResources.loadSkuSpecificEmbeddedResourceText("Data/DevGUI Menus") as TextAsset;
		
		if (textAsset == null)
		{
			Debug.Log("Could not find DevGUI Menus in Resources.");
			return;
		}
		
		JSON json = new JSON(textAsset.text);
		
		if (!json.isValid)
		{
			Debug.Log("DevGUI Menus JSON is invalid!");
			return;
		}
		
		mainMenu = DevGUIMenu.create(json, null);
		
		isHiRes = (MobileUIUtil.getDotsPerInch() > 200);
	}

	public static DevGUIMenu create(JSON data, DevGUIMenu parent)
	{
		string baseClassName = data.getString("class_name", "");
		string className = string.Format(CLASS_NAME_FORMAT, baseClassName);
		
		System.Type classType = System.Type.GetType(className);
		if (classType == null)
		{
			Debug.Log("Did not find DevGUI class: " + className);
			return null;
		}
		
		DevGUIMenu menu = System.Activator.CreateInstance(classType) as DevGUIMenu;

		if (menu == null)
		{
			Debug.Log("Could not create DevGUI object of type: " + className);
			return null;
		}
		
		menu.name = data.getString("name", baseClassName);
		menu.isInLobby = data.getBool("is_in_lobby", true);
		menu.isInGame = data.getBool("is_in_game", true);
		menu.specificGameKey = data.getString("specific_game_key", "");
		if (parent != null)
		{
			menu.parent = parent;
			parent.children.Add(menu);
		}
		
		foreach (JSON childData in data.getJsonArray("menus"))
		{
			DevGUIMenu.create(childData, menu);
		}
		
		return menu;
	}
	
	// Start at the previously used menu if available, otherwise start at the root.
	public static void setDefaultMenu()
	{
		if (currentMenu == null)
		{
			setMenu(mainMenu);
		}
	}
	
	// Draw the current menu. Duh.
	public static void drawCurrentMenu(int id)
	{
		scaleGUI();

		if (currentMenu != null)
		{
			// This should never be null, but checking anyway.
			if (currentMenu.drawTop())
			{
				currentMenu.drawGuts();
				currentMenu.drawBottom();
				currentMenu.drawTooltip();
			}
		}

		// If a new menu was chosen during the Layout event,
		// actually switch it now during the Repaint event after drawing everything.
		if (switchToMenu != null && Event.current.type == EventType.Repaint)
		{
			setMenu(switchToMenu);
			switchToMenu = null;
		}

		// restore the old matrix
		restoreGUIScaling();
	}
	
	// Each subclass must implement this to draw the guts of the page.
	public abstract void drawGuts();

	// use this to to add the ability to email debug info from any panel by calling from drawGuts
	public static void drawEmailButtonGuts(string buttonTitle, onEmailClickDelegate clickHandler)
	{
		GUILayout.BeginHorizontal();

		if (GUILayout.Button(buttonTitle, GUILayout.Width(200)))
		{
			clickHandler();
		}		

		shouldIncludeScreenshot = GUILayout.Toggle(shouldIncludeScreenshot, "Include Screenshot");

		GUILayout.EndHorizontal();				
	}

	public static bool drawButton(string text, string toolTip = "", bool highlight = false)
	{
		Color previousColor = GUI.color;

		if (highlight)
		{
			GUI.color = colorSelected;
		}

		buttonContent.text = text;
		buttonContent.tooltip = toolTip;

		if (buttonSelectedStyle == null)
		{
			buttonSelectedStyle = new GUIStyle(GUI.skin.button);
		}

		bool result = GUILayout.Button(buttonContent);

		GUI.color = previousColor;

		return result;
	}

	public static void sendDebugEmail(string subject, string body)
	{
		subject += " Hit it Rich ";	
		subject +=  Application.platform.ToString() + " Client Version " + Glb.clientVersion;

#if UNITY_EDITOR || UNITY_WEBGL
		Debug.LogWarning("Reached debug email call call with parms : " + "\n" +
			"Subject : " + subject + "\n" +
			"Body : " + body);
#else
		NativeBindings.ShareContent(subject, body, "", "");
#endif
	}	

	private static void scaleGUI()
	{
		//set up scaling
		//float rx = Mathf.Max(1, Screen.width / 2560f);
		//float ry = Mathf.Max(1, Screen.height / 1440f);
		//svMat = GUI.matrix;
		//GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(0, new Vector3(0, 1, 0)), new Vector3(rx, rx, 1));
	}

	private static void restoreGUIScaling()
	{
		GUI.matrix = svMat;
	}
	
	// Draws the "up" button as well as sub menu buttons, as necessary.
	private bool drawTop()
	{
		scaleGUI();
		
		if (!currentMenu.shouldBeShown)
		{
			// If a menu was left open in a different game state that is no longer value for the menu,
			// revert to the main menu.
			setMenu(mainMenu);
			return false;
		}

		// View the tab as a scrollable area
		Color resetColor = GUI.color;
		Color activePathColor = new Color(0.6f, 0.8f, 1.0f);

		// Put the main menu button at the top all the time.
		GUILayout.BeginHorizontal();
		GUI.color = activePathColor;
		if (GUILayout.Button(mainMenu.name))
		{
			// If touching this button, jump straight to the main menu from any menu.
			setMenu(mainMenu);
		}
		GUI.color = resetColor;
		if (GUILayout.Button("X", GUILayout.Width(isHiRes ? 50 : 25)))
		{
			DevGUI.isActive = false;
		}
		GUILayout.EndHorizontal();
		
		DevGUIMenu clickedMenu = null;
		
		foreach (DevGUIMenu menu in stack)
		{
			GUILayout.BeginHorizontal();
			int count = 0;
			foreach (DevGUIMenu child in menu.children)
			{
				if (!child.shouldBeShown)
				{
					continue;
				}
				
				if (stack.Contains(child))
				{
					GUI.color = activePathColor;
				}

				if (menu.parent != null && count > 4)
				{
					//If more than 5 buttons, move them to the next row
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
					count = 0;
				}
				
				if (GUILayout.Button(child.name))
				{
					clickedMenu = child;
					break;
				}

				GUI.color = resetColor;
				count++;
			}

			GUILayout.EndHorizontal();
		}
		
		GUILayout.Label("");
				
		if (clickedMenu != null)
		{
			// Must do this outside the stack foreach loop since it changes the stack contents.
			setMenu(clickedMenu);
			return false;
		}

		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.BeginVertical();

		// restore the old matrix
		restoreGUIScaling();

		return true;
	}
	
	private void drawBottom()
	{
		scaleGUI();
		GUILayout.EndVertical();
		GUILayout.EndScrollView();

		GUI.DragWindow(dragRect);
		restoreGUIScaling();
	}

	private void drawTooltip()
	{
		// so only evals GUI.tooltip after all draw requests complete
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}

		if (GUI.tooltip == "")
		{
			tooltipDelayOn = TOOLTIP_DELAY_ON;
			return;
		}
		
		if (tooltipDelayOn > 0)
		{
			tooltipDelayOn -= Time.unscaledDeltaTime;
			return;
		}

		if (tooltipStyle == null)
		{
			tooltipStyle = new GUIStyle(GUI.skin.textArea);
			tooltipStyle.wordWrap = true;
			tooltipStyle.alignment = TextAnchor.UpperLeft;
			tooltipStyle.clipping = TextClipping.Overflow;
		}

		tooltipContent.text = GUI.tooltip;

		// adjust size of textArea based on tooltip text size
		Vector2 size = new Vector2(400f, tooltipStyle.CalcHeight(tooltipContent, 400f));

		GUI.TextArea(new Rect(Event.current.mousePosition.x - size.x * .5f, Event.current.mousePosition.y - size.y, size.x, size.y), GUI.tooltip, tooltipStyle);
	}
	
	private static void setMenu(DevGUIMenu menu)
	{
		if (Event.current != null && Event.current.type == EventType.Layout)
		{
			// If setting the menu during OnGUI's Layout event,
			// delay the switch until after the Repaint event.
			switchToMenu = menu;
			return;
		}
		
		currentMenu = menu;
		
		// Rebuild the stack whenever the current menu changes.
		stack.Clear();
		
		if (currentMenu == null)
		{
			return;
		}
		
		stack.Add(currentMenu);
		
		while (menu.parent != null)
		{
			stack.Insert(0, menu.parent);
			menu = menu.parent;
		}
	}
	
	// Draw an interactive integer field.
	protected int intInputField(string label, string stringValue, int increment, int min = 0, int max = int.MaxValue)
	{
		int val = 0;

		try
		{
			val = int.Parse(stringValue);
		}
		catch {}
		
		int width = isHiRes ? 32 : 16;

		GUILayout.Label(label + ":");
		if (GUILayout.Button("-", GUILayout.Width(width)))
		{
			val = Mathf.Clamp(val - increment, min, max);
		}
		if (GUILayout.Button("+", GUILayout.Width(width)))
		{
			val = Mathf.Clamp(val + increment, min, max);
		}
		
		stringValue = GUILayout.TextField(val.ToString(), GUILayout.Width(isHiRes ? 120 : 60)).Trim();

		try
		{
			val = int.Parse(stringValue);
		}
		catch {}

		return val;
	}

	protected float floatInputField(string label, string stringValue, float increment, float min = 0.0f, float max = float.MaxValue)
	{
		float val = 0f;
		
		try
		{
			val = float.Parse(stringValue);
		}
		catch {}
		
		int width = isHiRes ? 32 : 16;
		
		GUILayout.Label(label + ":");
		if (GUILayout.Button("-", GUILayout.Width(width)))
		{
			val = Mathf.Clamp(val - increment, min, max);
		}
		if (GUILayout.Button("+", GUILayout.Width(width)))
		{
			val = Mathf.Clamp(val + increment, min, max);
		}
		
		stringValue = GUILayout.TextField(val.ToString(), GUILayout.Width(isHiRes ? 120 : 60)).Trim();
		
		try
		{
			val = float.Parse(stringValue);
		}
		catch {}
		
		return val;
	}
	
	// Draw an interactive date field.
	protected System.DateTime dateInputField(string label, System.DateTime dateValue, int dayIncrement)
	{
		int width = isHiRes ? 32 : 16;

		GUILayout.Label(label);
		GUILayout.Space(10);
		if (GUILayout.Button("-", GUILayout.Width(width)))
		{
			dateValue = dateValue.AddDays(-dayIncrement);
		}
		if (GUILayout.Button("+", GUILayout.Width(width)))
		{
			dateValue = dateValue.AddDays(dayIncrement);
		}
		GUILayout.Space(10);
        GUILayout.TextField(dateValue.ToShortDateString());
        return dateValue;
	}
	
	// Implements IResetGame
	public static void resetStaticClassData()
	{
		setMenu(null);
	}
}
