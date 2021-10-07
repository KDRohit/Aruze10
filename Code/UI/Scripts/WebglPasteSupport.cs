using UnityEngine;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class WebglPasteSupport
{
#if UNITY_WEBGL
	delegate void StringCallback( string content );

#if UNITY_EDITOR
	private static void initPasteCallback(StringCallback pasteCallback)
	{
		pasteCallback("fake");
	}
#else
	[DllImport("__Internal")]
	private static extern void initPasteCallback(StringCallback pasteCallback);
#endif
	public static void Init()
	{
		initPasteCallback(OnPaste);
	}

	[AOT.MonoPInvokeCallback(typeof(StringCallback))]
	private static void OnPaste(string str)
	{
		GUIUtility.systemCopyBuffer = str;
	}
#else
	public static void Init()
	{
	}
#endif
}
