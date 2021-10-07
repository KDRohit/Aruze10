using System.Collections.ObjectModel;
using UnityEngine;

	
public class DevGUIMenuPremiumSlice : DevGUIMenu
{
	private const string testDataPath = "Test Data/DailyBonusGames/DailyTripleWheel";
	private const string testPremiumDataPath = "Test Data/DailyBonusGames/PremiumSlice";
	public override void drawGuts()
	{
		GUILayout.BeginVertical();
		ReadOnlyCollection<string> winIds = PremiumSlice.instance != null ? PremiumSlice.instance.getSliceOrder() : null;
		if (winIds != null)
		{
			for (int i = 0; i < winIds.Count; i++)
			{
				GUILayout.Label(i + ".  " + winIds[i] + ": " + PremiumSlice.instance.getCreditsForSlice(i));
			}	
		}
		string sliceValue = "Slice Value: " + (PremiumSlice.instance != null
			? CreditsEconomy.convertCredits(PremiumSlice.instance.sliceValue)
			: "0");
		GUILayout.Label(sliceValue);
		string packageValue = "Package: " + (PremiumSlice.instance != null
			? PremiumSlice.instance.packageName
			: "");
		GUILayout.Label(packageValue);
		if (GUILayout.Button("Play normal spin and show offer"))
		{
			NewDailyBonusDialog.showFakeSpinAndOfferPremiumSpin(generateFakeDailySpin());
		}
		if (GUILayout.Button("Set to last round (requires manual restart)"))
		{
			PremiumSliceAction.devSetLastRound();
		}
		if (GUILayout.Button("Show Purchase Dialog"))
		{
			PremiumSlicePurchaseDialog.showDialog();
		}
		if (GUILayout.Button("Debug Purchase Premium Slice"))
		{
			string packageKey = !string.IsNullOrEmpty(packageValue)
				? packageValue
				: "premium_slice_package";
			PremiumSliceAction.devGrantPremiumSlice(packageKey);
		}
		GUILayout.EndVertical();
	}

	private JSON generateFakeDailySpin()
	{
		TextAsset textAsset = (TextAsset)Resources.Load(testDataPath,typeof(TextAsset));
		if (textAsset != null)
		{
			return new JSON(textAsset.text);
		}
		return null;
	}

	private JSON generateFakeOutcome()
	{
		TextAsset textAsset = (TextAsset)Resources.Load(testPremiumDataPath,typeof(TextAsset));
		if (textAsset != null)
		{
			return new JSON(textAsset.text);
		}
		return null;
	}

	
	public new static void resetStaticClassData()
	{
	}
}
