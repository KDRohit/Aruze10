using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class UIAlertView
{
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")] public static extern void _DisplayUIAlert(string title,string message, string yesMessage, string noMessage,string keyAsked, string keyAnswer);
#else
	public static void _DisplayUIAlert(string title,string message, string yesMessage, string noMessage,string keyAsked, string keyAnswer){}
#endif
	
	public UIAlertView ()
	{
		
	}
	
	static public void ShowUIAlert(string title,string message, string yesMessage, string noMessage,string keyAsked, string keyAnswer)
	{
		_DisplayUIAlert(title,message, yesMessage, noMessage, keyAsked, keyAnswer);
	}
}


