using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Zap.Automation
{
#if UNITY_EDITOR && !ZYNGA_PRODUCTION
	public class ZyngaAutomatedPaytableImageGrabber
	{
		private const string ZAP_DEBUG_COLOR = "#42c5f4";
		public static bool shouldCheckForMissingBonusPaytableImages = false;
		#region Paytable Images
		public static IEnumerator checkForMissingBonusPaytableImages()
		{
			#if UNITY_EDITOR
			// See if paytable image exists.
			if (BonusGameManager.instance != null && !string.IsNullOrEmpty(BonusGameManager.instance.currentGameKey))
			{
				shouldCheckForMissingBonusPaytableImages = false;
				// Figure out current bonus game key name.
				string bonusGameKey = "";
				GameState.BonusGameNameData bonusGameNameData = GameState.bonusGameNameData;
				BonusGameType bonusGameType = BonusGameManager.instance.currentGameType;

				Debug.LogFormat("<color={0}>Checking for missing paytable images, type {1}</color>", ZAP_DEBUG_COLOR, bonusGameType);
				// Can skip "portal" type since we've already gotten past that.
				if (bonusGameType == BonusGameType.GIFTING)
				{
					for (int i = 0; i < bonusGameNameData.giftingBonusGameNames.Count; i++)
					{
						string name = bonusGameNameData.giftingBonusGameNames[i];
						SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
						if (outcome != null)
						{
							bonusGameKey = name;
							break;
						}
					}
				}
				else if (bonusGameType == BonusGameType.CHALLENGE)
				{
					for (int i = 0; i < bonusGameNameData.challengeBonusGameNames.Count; i++)
					{
						string name = bonusGameNameData.challengeBonusGameNames[i];
						SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
						if (outcome != null)
						{
							bonusGameKey = name;
							break;
						}
					}
				}
				else if (bonusGameType == BonusGameType.CREDIT)
				{
					for (int i = 0; i < bonusGameNameData.creditBonusGameNames.Count; i++)
					{
						string name = bonusGameNameData.creditBonusGameNames[i];
						SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
						if (outcome != null)
						{
							bonusGameKey = name;
							break;
						}
					}
				}
				else if (bonusGameType == BonusGameType.SCATTER)
				{
					for (int i = 0; i < bonusGameNameData.scatterPickGameBonusGameNames.Count; i++)
					{
						string name = bonusGameNameData.scatterPickGameBonusGameNames[i];
						SlotOutcome outcome = SlotOutcome.getBonusGameOutcome(BonusGameManager.currentBonusGameOutcome, name);
						if (outcome != null)
						{
							bonusGameKey = name;
							break;
						}
					}
				}
				else
				{
					Debug.LogFormat("<color={0}>Unknown bonus game type {1}</color>", ZAP_DEBUG_COLOR, bonusGameType);
				}

				if (string.IsNullOrEmpty(bonusGameKey))
				{
					Debug.LogFormat("<color={0}>Could not find bonus game key</color>", ZAP_DEBUG_COLOR);
					yield break;
				}

				// Wait for a bit so that intro animations get a chance to finish, we want the screenshot to be of actual
				// gameplay.
				yield return new WaitForSeconds(15.0f);

				Debug.LogFormat("<color={0}>Checking for missing paytable images for {1}</color>", ZAP_DEBUG_COLOR, bonusGameKey);
				BonusGame bonusGameData = BonusGame.find(bonusGameKey);
				if (bonusGameData != null)
				{
					if (string.IsNullOrEmpty(bonusGameData.paytableImage))
					{
						Debug.LogFormat("<color={0}>No paytable image defined, skipping file check.</color>", ZAP_DEBUG_COLOR);
						yield break;
					}

					// NB: "bonusGameData.paytableImage" data returns a URL designed for the web version of the game.  The code
					// below is adapted from PaytableBonus.init and SlotResourceMap.createPaytableImage to replicate the
					// search process used by the paytable dialog to find the paytable image asset paths.

					// Adapted from PaytableBonus.init to convert .jpg/.png URL to <name>_paytable
					string imageBaseName = PaytableBonus.getPaytableBonusImageBasename(bonusGameData);
					Debug.LogFormat("<color={0}>paytable image: {1}</color>", ZAP_DEBUG_COLOR, imageBaseName);

					SlotResourceData entry = SlotResourceMap.getData(ZyngaAutomatedPlayer.instance.currentAutomatable.key);
					if (entry != null)
					{
						// Adapted from SlotResourceMap.createPaytableImage to generate standard game-specific and
						// game-group-specific paths within Assets/Data, and then convert those to absolute paths for
						// checking file existence and screenshot save target filename.
						string dataPath = System.IO.Path.Combine(Application.dataPath, "Data"); // get full path to Assets/Data
																								// Get full game-specific path to file in Assets/Data/Games/<game group>/<gamekey>/Images
						string basicImagePath = System.IO.Path.Combine(dataPath, entry.getGameSpecificImagePath(imageBaseName) + ".png");
						// Get full game-group-specific path to file in Assets/Data/Games/<game group>/<game group>_common/Images
						string commonImagePath = System.IO.Path.Combine(dataPath, entry.getGroupSpecificImagePath(imageBaseName) + ".png");
						Debug.LogFormat("<color={0}>Check for {1} and {2}</color>", ZAP_DEBUG_COLOR, basicImagePath, commonImagePath);
						if (!(System.IO.File.Exists(basicImagePath) || System.IO.File.Exists(commonImagePath)))
						{
							string basicImagePathJPG = System.IO.Path.Combine(dataPath, entry.getGameSpecificImagePath(imageBaseName) + ".jpg");
							string commonImagePathJPG = System.IO.Path.Combine(dataPath, entry.getGroupSpecificImagePath(imageBaseName) + ".jpg");
							if (!(System.IO.File.Exists(basicImagePathJPG) || System.IO.File.Exists(commonImagePathJPG)))
							{
								Debug.LogWarning("Paytable image missing at: " + basicImagePath + " and " + commonImagePath);
#if UNITY_EDITOR
								yield return RoutineRunner.instance.StartCoroutine(capturePaytableScreenshot(basicImagePath));
#endif
							}
							else
							{
								Debug.LogWarning("Paytable image is using JPG at: " + basicImagePathJPG + " and " + commonImagePathJPG);
							}
						}
					}
				}
				else
				{
					Debug.LogFormat("<color={0}>No bonus game data found for {1}</color>", ZAP_DEBUG_COLOR, bonusGameKey);
				}
			}
			#else
			// If not in editor, do nothing.
			yield break;
			#endif
		}

		private static IEnumerator capturePaytableScreenshot(string saveScreenshotImagePath)
		{
			#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(saveScreenshotImagePath))
			{
				yield return null;  // Wait a frame
				Debug.LogFormat("<color={0}>CAPTURE screenshot to {1}</color>", ZAP_DEBUG_COLOR, saveScreenshotImagePath);

				Debug.LogFormat("<color={0}>Changing screen size for screen shot. {1}</color>", ZAP_DEBUG_COLOR, saveScreenshotImagePath);
				Vector2 oldSize = GetMainGameViewSize();
				changeGameViewSize(1024, 768, true);
				yield return null; // 2 frames for NGUI to catch up.
				yield return null;
				ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath, saveScreenshotImagePath));
				// Wait for screenshot save to finish, it is asynchronous with no completion hooks.
				while (!System.IO.File.Exists(saveScreenshotImagePath))
				{
					Debug.Log("Waiting for the screenshot to be saved to " + saveScreenshotImagePath);
					yield return null;
				}
				Debug.LogFormat("<color={0}>revert screen size from screen shot. {1}</color>", ZAP_DEBUG_COLOR, saveScreenshotImagePath);
				changeGameViewSize((int)oldSize.x, (int)oldSize.y, false);
				UnityEditor.TextureImporter textureImporter = null;
				string textureImporterPath = "Assets" + saveScreenshotImagePath.Replace(Application.dataPath, "");
				
				UnityEditor.AssetDatabase.ImportAsset(textureImporterPath, UnityEditor.ImportAssetOptions.ForceUpdate);
				while (textureImporter == null)
				{
					Debug.Log("Waiting for textureImporter to not be null from path " + textureImporterPath);
					textureImporter = UnityEditor.TextureImporter.GetAtPath(textureImporterPath) as UnityEditor.TextureImporter;
					yield return null;
				}
				setTextureImporterOverrides(
					textureImporter,
					"iPhone",
					512,
					UnityEditor.TextureImporterFormat.PVRTC_RGB4,
					100,
					false);
				setTextureImporterOverrides(
					textureImporter,
					"Android",
					512,
					UnityEditor.TextureImporterFormat.ETC_RGB4,
					50,
					false);
				setTextureImporterOverrides(
					textureImporter,
					"default",
					512,
					UnityEditor.TextureImporterFormat.Automatic,
					50,
					false);

				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
				#else
				// If not in editor, do nothing.
				yield break;
				#endif
			}
		}
#endregion Paytable Images		
#region static_methods
		public static Vector2 GetMainGameViewSize()
		{
			#if UNITY_EDITOR
			System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
			System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
			return (Vector2)Res;
			#else
			return Vector2.one;
			#endif
		}
		#if UNITY_EDITOR
		public static UnityEditor.EditorWindow GetMainGameView()
		{
			System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
			System.Reflection.MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			System.Object Res = GetMainGameView.Invoke(null, null);
			return (UnityEditor.EditorWindow)Res;
		}
		#endif

		public static void changeGameViewSize(int width, int height, bool useExtraY)
		{
			#if UNITY_EDITOR
			int extraY = 17; // Extra gizmos on the top of game view;
			Rect R = GetMainGameView().position;
			R.width = width;
			R.height = height;
			if (useExtraY)
			{
				R.height += extraY;
			}
			GetMainGameView().position = R;
			#endif
		}

		public static void setTextureImporterOverrides(UnityEditor.TextureImporter importer, string platform, int maxTextureSize, UnityEditor.TextureImporterFormat textureFormat, int quality, bool allowsAlphaSplit)
		{
			#if UNITY_EDITOR
			// gets the existing platform settinga
			UnityEditor.TextureImporterPlatformSettings settings = null;
			if (platform != "default")
			{
				settings = importer.GetPlatformTextureSettings(platform);
			}
			else
			{
				settings = importer.GetDefaultPlatformTextureSettings();
			}

			// set our desired overrides
			settings.overridden = true;
			settings.maxTextureSize = maxTextureSize;
			settings.format = textureFormat;
			settings.compressionQuality = quality;
			settings.allowsAlphaSplitting = allowsAlphaSplit;

			// Set them back to the importer
			importer.SetPlatformTextureSettings(settings);
			importer.SaveAndReimport();
			#endif
		}
#endregion
	}
#endif
}
	
