using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;

/*
Editor tool to reduce repeating pixels and create stretchy sprites for NGUI atlases.
*/

public class NineSlicer : ScriptableWizard
{
	private Texture2D tex = null;
	private Texture2D sliced = null;
	private Texture2D sliceLine = null;
	private Texture2D[] previewSlices = new Texture2D[9];

	private string path = "";
	private string backupPath = "";

	private int left = 0;
	private int right = 0;
	private int top = 0;
	private int bottom = 0;
	private int middleWidthFinal = 20;
	private int middleHeightFinal = 20;
	private int maxTextureHeight = 750; //this is an arbitrary value set by me... if issues crop up we'll need to update this value, or figure out a better way to calculate it

	private int lastLeft = 0;
	private int lastRight = 0;
	private int lastTop = 0;
	private int lastBottom = 0;
	private int lastMiddleWidthFinal = 20;
	private int lastMiddleHeightFinal = 20;
	private int lastWidth = 0;
	private int lastHeight = 0;

	private bool isBlackLines = false;
	private bool validPath = false;
	private bool horizontalPreview = false;

	private const int MIN_WINDOW_WIDTH = 450;
	private const int IMAGE_TOP = 170; //this is the amount of pixels between the top of the dialog and the start of the texture image & preview image section; adjust this as more or less UI (text, etc) is added

	private const int TOP_LEFT = 0;
	private const int TOP = 1;
	private const int TOP_RIGHT = 2;
	private const int LEFT = 3;
	private const int MIDDLE = 4;
	private const int RIGHT = 5;
	private const int BOTTOM_LEFT = 6;
	private const int BOTTOM = 7;
	private const int BOTTOM_RIGHT = 8;

	private int middleWidth
	{
		get { return tex.width - left - right; }
	}

	private int middleHeight
	{
		get { return tex.height - top - bottom; }
	}

	[MenuItem("Zynga/Art Tools/Nine Slicer")]
	static void CreateWizard()
	{
		NineSlicer window = ScriptableWizard.DisplayWizard<NineSlicer>("Nine Slicer", "Close");

		window.setDefaultWindowSize();
	}

	private void setDefaultWindowSize()
	{
		setWindowSize(MIN_WINDOW_WIDTH, 200);
	}

	// Sets the window to the given size and centers it on the screen.
	private void setWindowSize(int windowWidth, int windowHeight)
	{
		this.position = new Rect((Screen.currentResolution.width - windowWidth) / 2, (Screen.currentResolution.height - windowHeight) / 2, windowWidth, windowHeight);
	}

	private void calcMaxTextureHeight()
	{
		//I'd like to iterate on this later, but either take the arbitrary maxTextureHeight value initially set, or
		int resolution = (Screen.currentResolution.height / 2);
		maxTextureHeight = Mathf.Max(resolution, maxTextureHeight);
	}

	private string getFilePath(string the_path)
	{
		string getPath = EditorUtility.OpenFilePanel("Choose a png", the_path, "png");

		//If the getPath dialog is closed out of, it returns "". In those cases, flag the file as an invalid file.
		//If a user selects a file, flag it as a valid file and copy its data to the backupPath to be referenced later.
		if (getPath != "")
		{
			backupPath = getPath;
			validPath = true;
			return getPath;
		}
		else
		{
			validPath = false;
			return "";
		}
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Select Image", GUILayout.Height(30), GUILayout.Width(100)))
		{
			calcMaxTextureHeight();
			path = getFilePath(path);

			if (path == "")
			{
				path = backupPath;
			}

			if (validPath)
			{
				WWW www = new WWW("file:///" + path);

				if (www != null)
				{
					tex = www.texture;
				}

				if (tex != null)
				{
					if (tex.width % 2 == 1 || tex.height % 2 == 1)
					{
						EditorUtility.DisplayDialog("Oddly Sized Source Image", "Source images must be evenly sized in both x and y directions.", "Doh!");
						DestroyImmediate(tex);
						tex = null;
					}
					else
					{
						//Checks to see if the texture width and height match the last loaded texture's width and height
						//to speed up slicing on same-sized assets (since they'll probably be different states of buttons or something similar)
						if (tex.width != lastWidth || tex.height != lastHeight)
						{
							// Set the slice amounts to equal slices by default.
							left = right = Mathf.FloorToInt(0.33f * tex.width);
							top = bottom = Mathf.FloorToInt(0.33f * tex.height);
						}

						//stores the current texture width and height values to be compared in the conditional above on subsequent image loads
						lastWidth = tex.width;
						lastHeight = tex.height;

						if (sliceLine == null)
						{
							sliceLine = Resources.Load("Slice Line") as Texture2D;
						}

						// Adjust the window size to best fit the texture and a preview.
						int windowWidth = Mathf.Clamp(tex.width + 5, MIN_WINDOW_WIDTH, Screen.currentResolution.width);
						int windowHeight = Mathf.Min(IMAGE_TOP + tex.height * 2 + 20, Screen.currentResolution.height - 50);
						horizontalPreview = false;

						if (tex.height > maxTextureHeight || tex.height > tex.width )
						{
							//If the texture is taller than wide, or the texture is taller than the maxTextureHeight value, show the preview to the right instead of below.
							windowWidth = Mathf.Clamp(tex.width * 2 + 20, MIN_WINDOW_WIDTH, Screen.currentResolution.width);
							windowHeight = Mathf.Min(IMAGE_TOP + tex.height + 5, Screen.currentResolution.height - 50);

							//sets variable to set the preview to display horizontally instead of vertically...
							//to remove the need to keep if/then'ing later on in the script to check whether that's true or not like in previous implementation
							horizontalPreview = true;
						}

						setWindowSize(windowWidth, windowHeight);
						refreshPreview();
					}
				}
			}

			if (tex == null)
			{
				setDefaultWindowSize();
			}
		}

		if (tex != null)
		{
			// If the preview is NOT outdated, disable the preview button.
			GUI.enabled = isPreviewOutdated;

			if (GUILayout.Button("Refresh Preview", GUILayout.Height(30), GUILayout.Width(150)))
			{
				refreshPreview();
			}

			// If the preview is outdated, disable the export button.
			GUI.enabled = !isPreviewOutdated;

			if (GUILayout.Button("Export Sliced Image", GUILayout.Height(30), GUILayout.Width(150)))
			{
				string info = string.Format("Left: {0}\nTop: {1}\nRight: {2}\nBottom: {3}\nHorizontal Middle Final: {4}\nVertical Middle Final: {5}\nSource Dimensions: {6}\nOutput Dimensions: {7}",
					left,
					top,
					right,
					bottom,
					middleWidthFinal,
					middleHeightFinal,
					tex.width + "w x " + tex.height + "h",
					(left + right + middleWidthFinal) + "w x " + (top + bottom + middleHeightFinal) + "h"
				);

				string exportPath = path.Substring(0, path.Length - 4) + " Stretchy.png";
				string infoExportPath = path.Substring(0, path.Length - 4) + " Stretchy.txt";

				byte[] png = sliced.EncodeToPNG();
				File.WriteAllBytes(exportPath, png);
				File.WriteAllBytes(infoExportPath, System.Text.Encoding.ASCII.GetBytes(info));

				EditorUtility.DisplayDialog("Sliced image exported to", exportPath, "Kick Ass!");
			}

			GUI.enabled = true;
		}

		GUILayout.EndHorizontal();

		if (tex != null)
		{
			GUI.color = Color.yellow;
			EditorGUILayout.LabelField("Output Texture Size: " + (left + middleWidthFinal + right) + " x " + (top + middleHeightFinal + bottom));
			GUI.color = Color.white;
		}
	
		GUILayout.BeginHorizontal();
		isBlackLines = GUILayout.Toggle(isBlackLines, "Black slicing lines");
		GUILayout.EndHorizontal();

		if (tex != null)
		{
			EditorGUILayout.BeginHorizontal();
			left = borderField("Left", left);
			right = borderField("Right", right);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			top = borderField("Top", top);
			bottom = borderField("Bottom", bottom);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("", "Reducing the size of the middle 1/3 below is where we get image size reduction.");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			middleWidthFinal = borderField("Horizontal Middle Final", middleWidthFinal);
			EditorGUILayout.LabelField("", "(of " + middleWidth + ")");
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			middleHeightFinal = borderField("Vertical Middle Final", middleHeightFinal);
			EditorGUILayout.LabelField("", "(of " + middleHeight + ")");
			EditorGUILayout.EndHorizontal();

			// Make sure borders are within valid range and not overlapping.
			left = Mathf.Clamp(left, 0, tex.width - 2);
			right = Mathf.Clamp(right, 0, tex.width - left - 2);
			top = Mathf.Clamp(top, 0, tex.height - 2);
			bottom = Mathf.Clamp(bottom, 0, tex.height - top - 2);

			// Make sure the final Middle size isn't any larger than the original
			// Middle size, because that would defeat the purpose of this tool.
			middleWidthFinal = Mathf.Clamp(middleWidthFinal, 2, middleWidth);
			middleHeightFinal = Mathf.Clamp(middleHeightFinal, 2, middleHeight);

			if (Event.current.type == EventType.Repaint)
			{
				GUI.DrawTexture(new Rect(0, IMAGE_TOP, tex.width, tex.height), tex, ScaleMode.ScaleToFit);

				if (isBlackLines)
				{
					GUI.color = Color.black;
				}

				// Left line
				GUI.DrawTexture(new Rect(left, IMAGE_TOP, 1, tex.height), sliceLine, ScaleMode.StretchToFill);

				// Right line
				GUI.DrawTexture(new Rect(tex.width - right, IMAGE_TOP, 1, tex.height), sliceLine, ScaleMode.StretchToFill);

				// Top line
				GUI.DrawTexture(new Rect(0, IMAGE_TOP + top, tex.width, 1), sliceLine, ScaleMode.StretchToFill);

				// Bottom line
				GUI.DrawTexture(new Rect(0, IMAGE_TOP + tex.height - bottom, tex.width, 1), sliceLine, ScaleMode.StretchToFill);

				GUI.color = Color.white;

				if (sliced != null)
				{
					// Draw the preview images.
					// Use the "last" variables for drawing the preview,
					// so the preview doesn't shift strangely in realtime as values are changed.
					int x = 0;
					int y = IMAGE_TOP + tex.height + 10;

					if (horizontalPreview)
					{
						// If the texture is taller than wide, show the preview to the right instead of below.
						x = tex.width + 10;
						y = IMAGE_TOP;
					}

					for (int i = 0; i < previewSlices.Length; i++)
					{
						Texture2D slice = previewSlices[i];
						int width = 0;
						int height = 0;

						if (slice != null)
						{
							// Determine width.
							if (i == LEFT || i == TOP_LEFT || i == BOTTOM_LEFT)
							{
								width = lastLeft;
							}
							if (i == TOP || i == MIDDLE || i == BOTTOM)
							{
								width = tex.width - lastLeft - lastRight;
							}
							if (i == TOP_RIGHT || i == RIGHT || i == BOTTOM_RIGHT)
							{
								width = lastRight;
							}
							// Determine height.
							if (i == TOP_LEFT || i == TOP || i == TOP_RIGHT)
							{
								height = lastTop;
							}
							if (i == LEFT || i == MIDDLE || i == RIGHT)
							{
								height = tex.height - lastTop - lastBottom;
							}
							if (i == BOTTOM_LEFT || i == BOTTOM || i == BOTTOM_RIGHT)
							{
								height = lastBottom;
							}

							GUI.DrawTexture(new Rect(x, y, width, height), slice, ScaleMode.StretchToFill);
						}

						x += width;
						if ((i + 1) % 3 == 0)
						{
							if (horizontalPreview)
							{
								// If the texture is taller than wide, show the preview to the right instead of below.
								x = tex.width + 10;
							}
							else
							{
								x = 0;
							}
							y += height;
						}
					}
				}
			}
		}
	}

	private bool isPreviewOutdated
	{
		get
		{
			return (
				left != lastLeft ||
				right != lastRight ||
				top != lastTop ||
				bottom != lastBottom ||
				middleWidthFinal != lastMiddleWidthFinal ||
				middleHeightFinal != lastMiddleHeightFinal
			);
		}
	}

	private void refreshPreview()
	{
		// Create the sliced image based on these border values.
		int width = left + middleWidthFinal + right;
		int height = top + middleHeightFinal + bottom;

		if (sliced != null)
		{
			DestroyImmediate(sliced);
			for (int i = 0; i < previewSlices.Length; i++)
			{
				if (previewSlices[i] != null)
				{
					DestroyImmediate(previewSlices[i]);
				}
			}
		}

		sliced = new Texture2D(width, height, TextureFormat.ARGB32, false);

		// It's possible to have 0 middle, in which case we don't need to do anything with the middle.
		if (middleWidthFinal > 0 && middleHeightFinal > 0)
		{
			Texture2D middle = createPreviewSlice(middleWidth, middleHeight);
			copyPixelRect(tex, middle, new Rect(left, bottom, middleWidth, middleHeight), Vector2.zero);

			// Scale the middle to the final size.
			TextureScale.Point(middle, middleWidthFinal, middleHeightFinal);

			// Now put the middle into the final sliced image.
			copyPixelRect(middle, sliced, new Rect(0, 0, middle.width, middle.height), new Vector2(left, bottom));

			previewSlices[MIDDLE] = middle;
		}
		else
		{
			DestroyImmediate(previewSlices[MIDDLE]);
		}


		if (middleWidthFinal > 0)
		{
			if (top > 0)
			{
				// Do the top middle.
				Texture2D middle = createPreviewSlice(middleWidth, top);
				copyPixelRect(tex, middle, new Rect(left, bottom + middleHeight, middleWidth, top), Vector2.zero);

				// Scale the middle to the final size.
				TextureScale.Point(middle, middleWidthFinal, top);

				// Now put the middle into the final sliced image.
				copyPixelRect(middle, sliced, new Rect(0, 0, middle.width, middle.height), new Vector2(left, bottom + middleHeightFinal));

				previewSlices[TOP] = middle;
			}
			else
			{
				DestroyImmediate(previewSlices[TOP]);
			}

			if (bottom > 0)
			{
				// Do the bottom middle.
				Texture2D middle = createPreviewSlice(middleWidth, bottom);
				copyPixelRect(tex, middle, new Rect(left, 0, middleWidth, bottom), Vector2.zero);

				// Scale the middle to the final size.
				TextureScale.Point(middle, middleWidthFinal, bottom);

				// Now put the middle into the final sliced image.
				copyPixelRect(middle, sliced, new Rect(0, 0, middle.width, middle.height), new Vector2(left, 0));

				previewSlices[BOTTOM] = middle;
			}
			else
			{
				DestroyImmediate(previewSlices[BOTTOM]);
			}
		}

		if (middleHeightFinal > 0)
		{
			if (left > 0)
			{
				// Do the left middle.
				Texture2D middle = createPreviewSlice(left, middleHeight);
				copyPixelRect(tex, middle, new Rect(0, bottom, left, middleHeight), Vector2.zero);

				// Scale the middle to the final size.
				TextureScale.Point(middle, left, middleHeightFinal);

				// Now put the middle into the final sliced image.
				copyPixelRect(middle, sliced, new Rect(0, 0, middle.width, middle.height), new Vector2(0, bottom));

				previewSlices[LEFT] = middle;
			}
			else
			{
				DestroyImmediate(previewSlices[LEFT]);
			}

			if (right > 0)
			{
				// Do the right middle.
				Texture2D middle = createPreviewSlice(right, middleHeight);
				copyPixelRect(tex, middle, new Rect(left + middleWidth, bottom, right, middleHeight), Vector2.zero);

				// Scale the middle to the final size.
				TextureScale.Point(middle, right, middleHeightFinal);

				// Now put the middle into the final sliced image.
				copyPixelRect(middle, sliced, new Rect(0, 0, middle.width, middle.height), new Vector2(left + middleWidthFinal, bottom));

				previewSlices[RIGHT] = middle;
			}
			else
			{
				DestroyImmediate(previewSlices[RIGHT]);
			}
		}

		if (left > 0 && top > 0)
		{
			// Do the top left corner.
			Texture2D corner = createPreviewSlice(left, top);
			copyPixelRect(tex, corner, new Rect(0, bottom + middleHeight, left, top), Vector2.zero);

			copyPixelRect(tex, sliced, new Rect(0, bottom + middleHeight, left, top), new Vector2(0, bottom + middleHeightFinal));

			previewSlices[TOP_LEFT] = corner;
		}
		else
		{
			DestroyImmediate(previewSlices[TOP_LEFT]);
		}

		if (right > 0 && top > 0)
		{
			// Do the top right corner.
			Texture2D corner = createPreviewSlice(right, top);
			copyPixelRect(tex, corner, new Rect(left + middleWidth, bottom + middleHeight, right, top), Vector2.zero);

			copyPixelRect(tex, sliced, new Rect(left + middleWidth, bottom + middleHeight, right, top), new Vector2(left + middleWidthFinal, bottom + middleHeightFinal));

			previewSlices[TOP_RIGHT] = corner;
		}
		else
		{
			DestroyImmediate(previewSlices[TOP_RIGHT]);
		}

		if (left > 0 && bottom > 0)
		{
			// Do the bottom left corner.
			Texture2D corner = createPreviewSlice(left, bottom);
			copyPixelRect(tex, corner, new Rect(0, 0, left, bottom), Vector2.zero);

			copyPixelRect(tex, sliced, new Rect(0, 0, left, bottom), Vector2.zero);

			previewSlices[BOTTOM_LEFT] = corner;
		}
		else
		{
			DestroyImmediate(previewSlices[BOTTOM_LEFT]);
		}

		if (right > 0 && bottom > 0)
		{
			// Do the bottom right corner.
			Texture2D corner = createPreviewSlice(right, bottom);
			copyPixelRect(tex, corner, new Rect(left + middleWidth, 0, right, bottom), Vector2.zero);

			copyPixelRect(tex, sliced, new Rect(left + middleWidth, 0, right, bottom), new Vector2(left + middleWidthFinal, 0));

			previewSlices[BOTTOM_RIGHT] = corner;
		}
		else
		{
			DestroyImmediate(previewSlices[BOTTOM_RIGHT]);
		}

		lastLeft = left;
		lastRight = right;
		lastTop = top;
		lastBottom = bottom;
		lastMiddleWidthFinal = middleWidthFinal;
		lastMiddleHeightFinal = middleHeightFinal;
	}

	// Helper function.
	private Texture2D createPreviewSlice(int width, int height)
	{
		Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
		tex.wrapMode = TextureWrapMode.Clamp;
		return tex;
	}

	private int borderField(string label, int val)
	{
		val = EditorGUILayout.IntField(label, val, GUILayout.ExpandWidth(false));

		// Enforce even-sized slice borders so they divide evenly by 2 for low res atlases.
		if (val % 2 == 1)
		{
			val++;
		}
		return val;
	}

	// Copy a rectangle of pixels from one texture to another.
	private void copyPixelRect(Texture2D source, Texture2D dest, Rect sourceRect, Vector2 destPos)
	{
		if (sourceRect.x + sourceRect.width > source.width ||
			sourceRect.y + sourceRect.height > source.height ||
			destPos.x + sourceRect.width > dest.width ||
			destPos.y + sourceRect.height > dest.height
			)
		{
			Debug.LogError("copyPixelRect: Invalid Rect size for textures. " + sourceRect + ", " + destPos + ", source size: " + source.width + "/" + source.height + ", dest size: " + dest.width + "/" + dest.height);
			return;
		}

		Color32[] sourcePixels = source.GetPixels32();
		Color32[] destPixels = dest.GetPixels32();

		for (int y = 0; y < sourceRect.height; y++)
		{
			for (int x = 0; x < sourceRect.width; x++)
			{
				int sourceIndex = (int)(source.width * (sourceRect.y + y) + sourceRect.x + x);
				int destIndex = (int)(dest.width * (destPos.y + y) + destPos.x + x);

				destPixels[destIndex] = sourcePixels[sourceIndex];
			}
		}

		dest.SetPixels32(destPixels);
		dest.Apply();
	}
}
