//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using TMPro;			// Zynga / Todd - added for TextMeshPro support on input boxes.
using TMProExtensions;	// Zynga / Todd - added for TextMeshPro support on input boxes.

/// <summary>
/// Editable text input field.
/// </summary>OnInputChanged

[AddComponentMenu("NGUI/UI/Input (Basic)")]
public class UIInput : TICoroutineMonoBehaviour
{
	public delegate char Validator (string currentText, char nextChar);

	public enum KeyboardType
	{
		Default = 0,
		ASCIICapable = 1,
		NumbersAndPunctuation = 2,
		URL = 3,
		NumberPad = 4,
		PhonePad = 5,
		NamePhonePad = 6,
		EmailAddress = 7,
	}

	public delegate void OnSubmit (string inputString);

	/// <summary>
	/// Current input, available inside OnSubmit callbacks.
	/// </summary>

	static public UIInput current;

	/// <summary>
	/// Text label modified by this input.
	/// </summary>

	public UILabel label;
	public TextMeshPro tmPro;		// Zynga / Todd - added for TextMeshPro support on input boxes.

	/// <summary>
	/// Maximum number of characters allowed before input no longer works.
	/// </summary>

	public int maxChars = 0;

	/// <summary>
	/// Visual carat character appended to the end of the text when typing.
	/// </summary>

	public string caratChar = "|";

	/// <summary>
	/// Delegate used for validation.
	/// </summary>

	public Validator validator;

	/// <summary>
	/// Type of the touch screen keyboard used on iOS and Android devices.
	/// </summary>

	public KeyboardType type = KeyboardType.Default;

	/// <summary>
	/// Whether this input field should hide its text.
	/// </summary>

	public bool isPassword = false;

	/// <summary>
	/// Whether to use auto-correction on mobile devices.
	/// </summary>

	public bool autoCorrect = false;

	/// <summary>
	/// Whether the label's text value will be used as the input's text value on start.
	/// By default the label is just a tooltip of sorts, letting you choose helpful
	/// half-transparent text such as "Press Enter to start typing", while the actual
	/// value of the input field will remain empty.
	/// </summary>

	public bool useLabelTextAtStart = false;

	/// <summary>
	/// Color of the label when the input field has focus.
	/// </summary>

	public Color activeColor = Color.white;

	/// <summary>
	/// Object to select when Tab key gets pressed.
	/// </summary>
	/// 

	public bool hideInput = false;

	public GameObject selectOnTab;

	/// <summary>
	/// Event receiver that will be notified when the input field submits its data (enter gets pressed).
	/// </summary>

	public GameObject eventReceiver;

	/// <summary>
	/// Function that will be called on the event receiver when the input field submits its data.
	/// </summary>

	public string functionName = "OnSubmit";

	/// <summary>
	/// Delegate that will be notified when the input field submits its data (by default that's when Enter gets pressed).
	/// </summary>

	public OnSubmit onSubmit;

	// Zynga / Todd - added convenience property getter.
	private bool isMultiLine
	{
		get
		{
			if (tmPro != null)
			{
				return tmPro.enableWordWrapping;
			}
			else if (label != null)
			{
				return label.multiLine;
			}
			return false;
		}
	}		

	string mText = "";
	string mDefaultText = "";
	Color mDefaultColor = Color.white;
	UIWidget.Pivot mPivot = UIWidget.Pivot.Left;
	float mPosition = 0f;

	TouchScreenKeyboard mKeyboard;
	string mLastIME = "";
	
	/// <summary>
	/// Input field's current text value.
	/// </summary>

	public virtual string text
	{
		get
		{
			if (mDoInit) Init();
			return mText;
		}
		set
		{
			if (mDoInit) Init();
			mText = value;

			if (TouchScreenKeyboard.isSupported && mKeyboard != null) mKeyboard.text = text;

			if (label != null)
			{
				if (string.IsNullOrEmpty(value)) value = mDefaultText;

				label.supportEncoding = false;
				label.text = selected ? value + caratChar : value;
				label.showLastPasswordChar = selected;
				label.color = (selected || value != mDefaultText) ? activeColor : mDefaultColor;
			}
			
			if (tmPro != null)
			{
				tmPro.richText = false;
				tmPro.parseCtrlCharacters = false;
				tmPro.text = selected ? value + caratChar : value;
				tmPro.color = (selected || value != mDefaultText) ? activeColor : mDefaultColor;
			}
		}
	}

	/// <summary>
	/// Whether the input is currently selected.
	/// </summary>

	public bool selected
	{
		get
		{
			return UICamera.selectedObject == gameObject;
		}
		set
		{
			if (!value && UICamera.selectedObject == gameObject) UICamera.selectedObject = null;
			else if (value) UICamera.selectedObject = gameObject;
		}
	}

	/// <summary>
	/// Set the default text of an input.
	/// </summary>

	public string defaultText
	{
		get
		{
			return mDefaultText;
		}
		set
		{
			if (label != null && label.text == mDefaultText) label.text = value;
			
			// Zynga / Todd - added:
			if (tmPro != null && tmPro.text == mDefaultText)
			{
			    tmPro.text = value;
			}
			
			mDefaultText = value;
		}
	}

	// Zynga/MCC Adding a public getter/setter for this so its not based only on
	// what the prefab originally had setup.
	public Color defaultColor
	{
		get
		{
			return mDefaultColor;
		}
		set
		{
			mDefaultColor = value;
		}
	}

	/// <summary>
	/// Labels used for input shouldn't support color encoding.
	/// </summary>

	protected void Init ()
	{
		if (mDoInit)
		{
			mDoInit = false;
			// Zynga / Todd - added
			if (tmPro == null)
			{
				tmPro = GetComponentInChildren<TextMeshPro>();
			}
			
			if (tmPro == null && label == null)
			{
				// If TextMeshPro isn't found, and UILabel isn't specified, look for a UILabel.
				label = GetComponentInChildren<UILabel>();
			}

			if (tmPro != null)
			{
				// Zynga / Todd - added this condition block.
				if (useLabelTextAtStart)
				{
					mText = tmPro.text;
				}
				mDefaultText = tmPro.text;
				mDefaultColor = tmPro.color;
				tmPro.richText = false;
				tmPro.parseCtrlCharacters = false;
				tmPro.overflowMode = TextOverflowModes.Truncate;
				mPivot = NGUIExt.textMeshProAnchorToPivot(tmPro);
				mPosition = tmPro.transform.localPosition.x;
				label = null;	// If using TextMeshPro, clear the UILabel property.
			}
			else if (label != null)
			{
				if (useLabelTextAtStart) mText = label.text;
				mDefaultText = label.text;
				mDefaultColor = label.color;
				label.supportEncoding = false;
				label.password = isPassword;
				mPivot = label.pivot;
				mPosition = label.cachedTransform.localPosition.x;
			}
			else enabled = false;
		}
	}

	bool mDoInit = true;

	/// <summary>
	/// If the object is currently highlighted, it should also be selected.
	/// </summary>

	protected override void OnEnable ()
	{
		base.OnEnable();
		
		if (UICamera.IsHighlighted(gameObject)) OnSelect(true);
	}

	/// <summary>
	/// Remove the selection.
	/// </summary>

	protected override void OnDisable ()
	{
		base.OnDisable();
		
		if (UICamera.IsHighlighted(gameObject)) OnSelect(false);
	}

	/// <summary>
	/// Selection event, sent by UICamera.
	/// </summary>

	public void OnSelect (bool isSelected)
	{
		if (mDoInit) Init();

		if ((tmPro != null || label != null) && enabled && NGUITools.GetActive(gameObject))
		{
			if (isSelected)
			{
				if (tmPro != null)
				{
					mText = (!useLabelTextAtStart && tmPro.text == mDefaultText) ? "" : tmPro.text;
					tmPro.color = activeColor;
				}
				else if (label != null)
				{
					mText = (!useLabelTextAtStart && label.text == mDefaultText) ? "" : label.text;
					label.color = activeColor;
					if (isPassword) label.password = true;
				}

				if (TouchScreenKeyboard.isSupported)
				{
					if (Application.platform == RuntimePlatform.IPhonePlayer)
					{
						TouchScreenKeyboard.hideInput = hideInput;
					}

					if (isPassword)
					{
						mKeyboard = TouchScreenKeyboard.Open(mText, TouchScreenKeyboardType.Default, false, false, true);
					}
					else
					{
						mKeyboard = TouchScreenKeyboard.Open(mText, (TouchScreenKeyboardType)((int)type), autoCorrect);
					}
				}
				else
				{
					Input.imeCompositionMode = IMECompositionMode.On;
					Transform t;
					Vector3 offset;

					if (tmPro != null)
					{
						t = tmPro.transform;
						offset = tmPro.getAnchorOffset();
					}
					else
					{
						t = label.cachedTransform;
						offset = label.pivotOffset;
						offset.y += label.relativeSize.y;
					}
					offset = t.TransformPoint(offset);
					Input.compositionCursorPos = UICamera.currentCamera.WorldToScreenPoint(offset);
				}
				UpdateLabel();
				eventReceiver.SendMessage("OnShowKeyboard", this, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				// Zynga: Added mKeyboard.active condition below, because setting it to false when it's
				// already false also sets mKeyboard.done to true, even if the user didn't touch the Done button.
				if (TouchScreenKeyboard.isSupported && mKeyboard != null && mKeyboard.active)
				{
					mKeyboard.active = false;
				}
				
				if (tmPro != null)
				{
					if (string.IsNullOrEmpty(mText))
					{
						tmPro.text = mDefaultText;
						tmPro.color = mDefaultColor;
					}
					else
					{
						tmPro.text = mText;
					}
				}
				else
				{
					if (string.IsNullOrEmpty(mText))
					{
						label.text = mDefaultText;
						label.color = mDefaultColor;
						if (isPassword) label.password = false;
					}
					else label.text = mText;

					label.showLastPasswordChar = false;
				}
				Input.imeCompositionMode = IMECompositionMode.Off;
				RestoreLabel();
				eventReceiver.SendMessage("OnHideKeyboard", this, SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	/// <summary>
	/// Update the text and the label by grabbing it from the iOS/Android keyboard.
	/// </summary>

	void Update()
	{
		if (TouchScreenKeyboard.isSupported)
		{
			if (mKeyboard != null)
			{
				string text = mKeyboard.text;

				if (mText != text)
				{
					mText = "";

					for (int i = 0; i < text.Length; ++i)
					{
						char ch = text[i];
						if (validator != null) ch = validator(mText, ch);
						if (ch != 0) mText += ch;
					}

					if (maxChars > 0 && mText.Length > maxChars) mText = mText.Substring(0, maxChars);
					UpdateLabel();
					if (mText != text) mKeyboard.text = mText;
					eventReceiver.SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);	// Zynga: Added eventReceiver.
				}

				// Zynga: If the keyboard isn't active, then unselect the input box
				// so it can be selected again when touched. If it's still selected
				// from the previous touch, then the OnSelect won't fire when touching it again.
				if (!mKeyboard.active && selected)
				{
					selected = false;
				}

				if (mKeyboard.done)
				{
					mKeyboard = null;
					current = this;
					if (onSubmit != null) onSubmit(mText);
					if (eventReceiver == null) eventReceiver = gameObject;
					eventReceiver.SendMessage(functionName, mText, SendMessageOptions.DontRequireReceiver);
					current = null;
					selected = false;
				}
			}
		}
		else
		{
			if (selected)
			{
				if (selectOnTab != null && Input.GetKeyDown(KeyCode.Tab))
				{
					UICamera.selectedObject = selectOnTab;
				}

				// Note: this won't work in the editor. Only in the actual published app. Unity blocks control-keys in the editor.
				if (
						Input.GetKeyDown(KeyCode.V) &&
						(
						 Input.GetKey(KeyCode.LeftApple) ||
						 Input.GetKey(KeyCode.RightApple) ||
						 Input.GetKey(KeyCode.LeftControl) ||
						 Input.GetKey(KeyCode.RightControl)
						))
				{
					Append(GUIUtility.systemCopyBuffer);
				}
			
				if (mLastIME != Input.compositionString)
				{
					mLastIME = Input.compositionString;
					UpdateLabel();
				}
			}
		}
	}

	/// <summary>
	/// Input event, sent by UICamera.
	/// </summary>

	void OnInput (string input)
	{
		if (mDoInit) Init();

		if (selected && enabled && NGUITools.GetActive(gameObject))
		{
			// Mobile devices handle input in Update()
			if (Application.platform == RuntimePlatform.Android) return;
			if (Application.platform == RuntimePlatform.IPhonePlayer) return;
			//if (Application.platform == RuntimePlatform.WSAPlayerARM) return;
			//if (Application.platform == RuntimePlatform.WSAPlayerX64) return;
			//if (Application.platform == RuntimePlatform.WSAPlayerX86) return;
			Append(input);
		}
	}

	/// <summary>
	/// Append the specified text to the end of the current.
	/// </summary>

	void Append (string input)
	{
		for (int i = 0, imax = input.Length; i < imax; ++i)
		{
			char c = input[i];

			if (c == '\b')
			{
				// Backspace
				if (mText.Length > 0)
				{
					mText = mText.Substring(0, mText.Length - 1);
					eventReceiver.SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);	// Zynga: Added eventReceiver.
				}
			}
			else if (c == '\r' || c == '\n')
			{
				if (UICamera.current.submitKey0 == KeyCode.Return || UICamera.current.submitKey1 == KeyCode.Return)
				{
					// Not multi-line input, or control isn't held
					if (!isMultiLine || (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)))
					{
						// Enter
						current = this;
						if (onSubmit != null) onSubmit(mText);
						if (eventReceiver == null) eventReceiver = gameObject;
						eventReceiver.SendMessage(functionName, mText, SendMessageOptions.DontRequireReceiver);
						current = null;
						selected = false;
						return;
					}
				}

				// If we have an input validator, validate the input first
				if (validator != null) c = validator(mText, c);

				// If the input is invalid, skip it
				if (c == 0) continue;

				// Append the character
				if (c == '\n' || c == '\r')
				{
					if (isMultiLine) mText += "\n";
				}
				else mText += c;

				// Notify the listeners
				eventReceiver.SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);	// Zynga: Added eventReceiver.
			}
			else if (c >= ' ')
			{
				// Fix for TextMeshPro upgrade to 1.4.1
				if (tmPro != null && !tmPro.font.HasCharacter(c))
				{
					// If using TextMeshPro, only allow characters that are in the font.
					continue;
				}

				// If we have an input validator, validate the input first
				if (validator != null) c = validator(mText, c);

				// If the input is invalid, skip it
				if (c == 0) continue;

				// Append the character and notify the "input changed" listeners.
				mText += c;
				eventReceiver.SendMessage("OnInputChanged", this, SendMessageOptions.DontRequireReceiver);	// Zynga: Added eventReceiver.
			}
		}

		// Ensure that we don't exceed the maximum length
		UpdateLabel();
	}

	/// <summary>
	/// Update the visual text label, capping it at maxChars correctly.
	/// </summary>

	void UpdateLabel ()
	{
		if (mDoInit) Init();
		if (maxChars > 0 && mText.Length > maxChars) mText = mText.Substring(0, maxChars);

		// Start with the text and append the IME composition and carat chars
		string processed;

		if (isPassword && selected)
		{
			processed = "";
			for (int i = 0, imax = mText.Length; i < imax; ++i) processed += "*";
			processed += Input.compositionString + caratChar;
		}
		else processed = selected ? (mText + Input.compositionString + caratChar) : mText;

		if (tmPro != null)
		{
			tmPro.text = processed;
		}
		else if (label != null && label.font != null)
		{
			// Now wrap this text using the specified line width
			label.supportEncoding = false;

			if (!label.shrinkToFit)
			{
				Vector3 scale = label.cachedTransform.localScale;

				if (label.multiLine)
				{
					label.font.WrapText(processed, out processed, label.lineWidth / scale.x, label.lineHeight / scale.y, 0, false, UIFont.SymbolStyle.None);
				}
				else
				{
					string fit = label.font.GetEndOfLineThatFits(processed, label.lineWidth / scale.x, false, UIFont.SymbolStyle.None);

					if (fit != processed)
					{
						processed = fit;
						Vector3 pos = label.cachedTransform.localPosition;
						pos.x = mPosition + label.lineWidth;

						if (mPivot == UIWidget.Pivot.Left) label.pivot = UIWidget.Pivot.Right;
						else if (mPivot == UIWidget.Pivot.TopLeft) label.pivot = UIWidget.Pivot.TopRight;
						else if (mPivot == UIWidget.Pivot.BottomLeft) label.pivot = UIWidget.Pivot.BottomRight;

						label.cachedTransform.localPosition = pos;
					}
					else RestoreLabel();
				}
			}

			// Update the label's visible text
			label.text = processed;
			label.showLastPasswordChar = selected;
		}
	}

	/// <summary>
	/// Restore the input label's pivot point and position.
	/// </summary>

	void RestoreLabel ()
	{
		// Don't do anything here for TextMeshPro since we don't change the anchor point.
		if (label != null)
		{
			label.pivot = mPivot;
			Vector3 pos = label.cachedTransform.localPosition;
			pos.x = mPosition;
			label.cachedTransform.localPosition = pos;
		}
	}
}
