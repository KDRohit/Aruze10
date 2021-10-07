using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;

/**
This is a purely static class of generic useful functions that relate to math.
*/
public static class CommonDeviceInfo
{
	
	//	public static string deviceAdvertisingID moved to ZyngaConstantsGame in the ZDK
	/// Returns the current device name as a string.
	#if UNITY_IPHONE
	public static string deviceName
	{
		get
		{
			if (_iPhoneDeviceDictionary == null)
			{				
				_iPhoneDeviceDictionary = new Dictionary<UnityEngine.iOS.DeviceGeneration, string>();
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone,			"iPhone 1");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone3G,			"iPhone 3G");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone3GS,			"iPhone 3GS");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone4,			"iPhone 4");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone4S,			"iPhone 4S");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone5,			"iPhone 5");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone5C,			"iPhone 5C");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhone5S,			"iPhone 5S");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPodTouch1Gen,		"iPod Touch 1");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPodTouch2Gen,		"iPod Touch 2");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPodTouch3Gen,		"iPod Touch 3");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPodTouch4Gen,		"iPod Touch 4");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPodTouch5Gen,		"iPod Touch 5");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPad1Gen,			"iPad 1");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPad2Gen,			"iPad 2");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPad3Gen,			"iPad 3");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPad4Gen,			"iPad 4");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPadMini1Gen,		"iPad Mini");
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPhoneUnknown,		Localize.text("your_device"));
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPadUnknown,		Localize.text("your_device"));
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.iPodTouchUnknown,	Localize.text("your_device"));
				_iPhoneDeviceDictionary.Add(UnityEngine.iOS.DeviceGeneration.Unknown,			Localize.text("your_device"));
			}
			
			if (_iPhoneDeviceDictionary.ContainsKey(UnityEngine.iOS.Device.generation))
			{
				return _iPhoneDeviceDictionary[UnityEngine.iOS.Device.generation];
			}
			else
			{
				return Localize.text("your_device");
			}
		}
	}
	private static Dictionary<UnityEngine.iOS.DeviceGeneration, string> _iPhoneDeviceDictionary = null;
	#else	
	public static string deviceName
	{
		get
		{
			return Localize.text("your_device");
		}
	}
#endif

#if !UNITY_WSA_10_0 && NETFX_CORE
    public static string getMacAddress()
	{

		string macAddress = "";
		NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
		
		foreach (NetworkInterface nic in nics)
		{
			PhysicalAddress pa = nic.GetPhysicalAddress();
			if (pa.ToString() != "")
			{
				string temp = pa.ToString().ToUpper();
				macAddress = string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
				                           temp.Substring(0,2),
				                           temp.Substring(2,2),
				                           temp.Substring(4,2),
				                           temp.Substring(6,2),
				                           temp.Substring(8,2),
				                           temp.Substring(10,2));
				break;
			}
		}
		
		return macAddress;
	}
#endif //UNITY_WSA_10_0 && NETFX_CORE
}


