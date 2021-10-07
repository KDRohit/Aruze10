using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
Allows us to log messages that are visible from within a console in the game,
rather than needing to find and track a log file from various devices or whatnot.
This is automatically disabled in Data.cs when not in debug mode.
*/
// We need to have a namespace here so we can call it explicitly in Debug.cs
namespace CustomLog
{
	public class Log : MonoBehaviour 
	{
		public static bool FORCE_LOG = false;	// Don't use const because it causes compiler warnings when set to false.
		
		private const KeyCode HOT_KEY = KeyCode.BackQuote;	// The traditional key for opening a console in a game.
		private const int MAX_LINES = 100;
		private const int WINDOW_ID = 999;

		public GUISkin skinLow;
		public GUISkin skinHi;
			
		private Rect windowPos;
		private bool isHiRes = false;
		private Vector2 scrollPos;
		
		public static Log instance = null;
			
		private static List<LogLine> lines = new List<LogLine>();
		private static List<string> tags = new List<string>();		// Contains all the tags specified by the log lines.
		private static List<string> filter = new List<string>();	// The tags to filter on.
		
		public static bool isActive
		{
			get { return _isActive; }
			
			set
			{
				_isActive = value;
				if (instance != null)
				{
					instance.GetComponent<Collider>().enabled = value;
				}
			}
		}
		private static bool _isActive = false;
		
		void Awake()
		{
			instance = this;
			
			if (MobileUIUtil.isSmallMobile)
			{
				// Use the full screen space on small devices.
				windowPos = new Rect(0, 0, Screen.width, Screen.height);
			}
			else
			{
				windowPos = new Rect(0, 0, Screen.width * .8f, Screen.height * .8f);
			}
			
			isHiRes = (MobileUIUtil.getDotsPerInch() > 200);
		}
		
		void Update()
		{
			if (Input.GetKeyDown(HOT_KEY) || (FORCE_LOG && !isActive))
			{
				isActive = !isActive;
			}
			
			if (lines.Count > MAX_LINES)
			{
				lines.RemoveRange(0, lines.Count - MAX_LINES);
				refreshTags();
			}
		}
		
		void OnGUI()
		{
			if ((Data.debugMode || FORCE_LOG) && isActive)
			{
				GUI.skin = isHiRes ? skinHi : skinLow;
				windowPos = GUILayout.Window(WINDOW_ID, windowPos, drawWindow, "");
			}
		}
		
		// Log a message to this log using the default text color.
		public static void log(string text)
		{
			log(text, Color.white);
		}

		// Log a message to this log using a predefined tag object.
		public static void log(string text, Tag tag)
		{
			log(text, tag.color, tag.tag);
		}

		// Log a message to this log using a specified text color.
		public static void log(string text, Color color, string tag = "")
		{
			if (!Data.debugMode && !FORCE_LOG)
			{
				return;
			}

            string rawStackTrace = "";

#if UNITY_WSA_10_0 && NETFX_CORE
#else
            rawStackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
#endif
			
			text = string.Format("<color=#{0}>{1}</color>", CommonColor.colorToHex(color), text);
			tag = tag.Trim();
			
			if (lines.Count > 0 &&
				lines[lines.Count - 1].text == text &&
				lines[lines.Count - 1].rawStackTrace == rawStackTrace &&
				lines[lines.Count - 1].tag == tag
				)
			{
				// The last logged text is the same as what's being logged now,
				// so instead of creating a new log entry, increment the counter on the previous one.
				// This prevents spamming the log with a bunch of repeated entries.
				lines[lines.Count - 1].count++;
			}
			else
			{
				LogLine line = new LogLine(text, rawStackTrace, tag);
				lines.Add(line);
				if (tag != "" && !tags.Contains(tag))
				{
					tags.Add(tag);
				}
			}

			if (instance != null)
			{
				instance.scrollToBottom();
			}
		}
		
		public void scrollToBottom()
		{
			// Whenever a new log entry is made, scroll to the bottom to show it.
			scrollPos.y = 5000;
		}
		
		private void drawWindow(int id)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Email", GUILayout.Width(isHiRes ? 100 : 50)))
			{
				emailLog();
			}			
			if (GUILayout.Button("Clear", GUILayout.Width(isHiRes ? 100 : 50)))
			{
				lines.Clear();
				refreshTags();
			}
			GUILayout.FlexibleSpace();
			GUILayout.Label("<b>Log</b>", GUILayout.Width(isHiRes ? 100 : 50));
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("X", GUILayout.Width(isHiRes ? 100 : 50)))
			{
				isActive = false;
			}
			GUILayout.EndHorizontal();

			if (tags.Count > 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("<b>Tags:</b>");
				
				Color resetColor = GUI.color;

				foreach (string tag in tags)
				{
					bool isEnabled = filter.Contains(tag);
					if (isEnabled)
					{
						GUI.color = Color.green;
					}
					if (GUILayout.Button(tag))
					{
						if (isEnabled)
						{
							filter.Remove(tag);
						}
						else
						{
							filter.Add(tag);
						}
					}
					GUI.color = resetColor;
				}
				GUILayout.EndHorizontal();
			}
			
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			foreach (LogLine line in lines)
			{
				if (filter.Count > 0 && !filter.Contains(line.tag))
				{
					// Doesn't match the current filter, so skip it.
					continue;
				}
				if (GUILayout.Button(line.displayedText, "Label"))
				{
					line.isExpanded = !line.isExpanded;
				}
			}
			GUILayout.EndScrollView();
			GUI.DragWindow();
		}

		private void emailLog()
		{
			string logText = "";
			foreach (LogLine line in lines)
			{
				if (filter.Count > 0 && !filter.Contains(line.tag))
				{
					// Doesn't match the current filter, so skip it.
					continue;
				}
				logText += line.text + "\n";
			}

			DevGUIMenu.sendDebugEmail("Debug Log", logText);
		}
		
		// Called whenever log lines are removed, to make remove tags that no longer exist if necessary.
		private void refreshTags()
		{
			tags.Clear();
			foreach (LogLine line in lines)
			{
				if (line.tag != "" && !tags.Contains(line.tag))
				{
					tags.Add(tag);
				}
			}
		}

		public class LogLine
		{
			public string text { get; private set; }
			public string rawStackTrace { get; private set; }
			public int count = 1;
			public bool isExpanded = false;
			public string tag = "";

			private string stackTrace = "";	// Includes both the text and the stack trace.
			
			public LogLine(string text, string rawStackTrace, string tag)
			{
				this.text = text;
				this.rawStackTrace = rawStackTrace;
				this.tag = tag;
				
				// Remove any stack lines that start with "Log.log(" because we don't need to see
				// the part of the stack that did the actual logging.
				System.Text.StringBuilder builder = new System.Text.StringBuilder();
				string[] stackLines = rawStackTrace.Split('\n');

				foreach (string line in stackLines)
				{
					if (line.Length >= 8 && line.Substring(0, 8) != "Log:log(")
					{
						if (builder.Length > 0)
						{
							builder.Append("\n");
						}
						builder.Append(line);
					}
				}
				
				// Pre-format the stack trace with the text and the stack trace together.
				// Make the stack trace gray so the text stands out more.
				this.stackTrace = string.Format("{0}\n<color=#888888>{1}</color>", text, builder);
			}
			
			// Returns the text that should be displayed based on a couple of factors.
			public string displayedText
			{
				get
				{
					string outText = isExpanded ? stackTrace : text;
					if (count > 1)
					{
						outText = string.Format("<color=#888888>(x{0})</color> {1}", count, outText);
					}
					return outText;
				}
			}
		}
	}
	
	// Simple data structure to define a reusable tag with a specific color.
	// For example, if you have a feature that you'd like to log messages for,
	// and would like to filter on just those messages, create a tag object
	// and pass that object to the log function.
	// 		CustomLog.Tag myCoolTag = new CustomLog.Tag("cool", Color.cyan);
	//		CustomLog.Log.log("logging a message", myCoolTag);
	// Then the log will show the tag as a filter button at the top.
	public class Tag
	{
		public string tag;
		public Color color;
		
		public Tag(string tag, Color color)
		{
			this.tag = tag;
			this.color = color;
		}
	}
}
