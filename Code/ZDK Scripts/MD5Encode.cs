using UnityEngine;
using System.Collections;

#if UNITY_FLASH
using FlowGroup.Crypto;
#else
using System.Security.Cryptography;
#endif

/// MD5-encodes stuff
public static class MD5Encode
{
	/// Encodes an MD5 hash of some string
	public static string encode(string strToEncrypt)
	{
		System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
		byte[] bytes = ue.GetBytes(strToEncrypt);

		return encode(bytes);
	}

	/// Encodes an MD5 hash of some string
	public static string encode(byte[] bytesToEncrypt)
	{
		// Encrypt bytes
		MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytesToEncrypt);

		// Convert the encrypted bytes back to a string (base 16)
		string hashString = "";

		for (int i = 0; i < hashBytes.Length; i++)
		{
			hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
			// We can replace this code below with the code above when flash fixes from Unity come.
			// #if UNITY_FLASH replacement. 
			// string convertedString = System.Convert.ToString(hashBytes[i], 16);
			// hashString += UnityReplacements.Utils.padLeft(convertedString, 2, '0');
		}

		return hashString.PadLeft(32, '0');

		// #if UNITY_FLASH replacement. We can remove this when Unity fixes their Flash Compiler.
		//return UnityReplacements.Utils.padLeft(hashString, 32, '0');
	}
}