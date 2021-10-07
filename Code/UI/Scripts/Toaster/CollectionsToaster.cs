using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CollectionsToaster : Toaster {

	public const string EXTRA_BONUS_REWARD = "Extras Bonus Awarded!";
	public const string SET_COMPLETE_REWARD = "Set Completed!";

	public TextMeshPro coinText;
	public TextMeshPro rewardText;
	public TextMeshPro cardNumText;
	public GameObject coinParent;
	public GameObject packParent;
	public GameObject checkMark;

	[SerializeField] private Renderer albumLogo;

	public override void init(ProtoToaster proto)
	{
		base.init(proto);
		CollectableAlbum currentAlbum = Collectables.Instance.getAlbumByKey(Collectables.currentAlbum);
		AssetBundleManager.load(currentAlbum.logoTexturePath, logoLoadedSuccess, logoLoadFail);
		List<CollectableCardData> collectableCards = (List<CollectableCardData>)proto.args.getWithDefault(D.COLLECTABLE_CARDS, null);
		List<string> completedSetsList = (List<string>)proto.args.getWithDefault(D.DATA, null);
		JSON starPackData = (JSON)proto.args.getWithDefault(D.BONUS_CREDITS, null);
		string packId = (string)proto.args.getWithDefault(D.PACKAGE_KEY, "");
		string packSource = (string)proto.args.getWithDefault(D.KEY, "");
		long starPackReward = (long)proto.args.getWithDefault(D.VALUE, 0L);
		if (collectableCards != null)
		{
			int cardNum = collectableCards.Count;

			StatsManager.Instance.LogCount(counterName:"toaster",
				kingdom: "hir_collection",
				phylum: "pack_award",
				klass: packSource,
				family: packId,
				genus: "view",
				val: cardNum);

			cardNumText.text = "+" + cardNum.ToString();

			if (starPackData != null)
			{
				string mileStoneString = string.Format("{0}_{1}", currentAlbum.numCompleted, currentAlbum.maxStars);
				RoutineRunner.instance.StartCoroutine(waitForToaster(starPackData));
			}

			if (starPackReward > 0)
			{
				coinText.text = CreditsEconomy.convertCredits(starPackReward);
				rewardText.text = EXTRA_BONUS_REWARD;
				SlotsPlayer.addFeatureCredits(starPackReward, "starPackToaster");
			}
			else
			{
				coinParent.SetActive(false);
				packParent.SetActive(true);
			}

			if (completedSetsList != null && completedSetsList.Count > 0)
			{
				RoutineRunner.instance.StartCoroutine(waitForMultipleSet(completedSetsList));
			}
		}
		else if (completedSetsList != null)
		{
			Queue<string> completedSets = new Queue<string>(completedSetsList);
			if (completedSetsList != null && completedSetsList.Count > 0)
			{
				CollectableSetData completedSet = null;
				string completedSetName = completedSets.Dequeue();
				completedSetsList.Remove(completedSetName);
				completedSet = Collectables.Instance.findSet(completedSetName);
				completedSet.isComplete = true;
				long completeSetReward = completedSet.rewardAmount;
				rewardText.text = SET_COMPLETE_REWARD;
				coinText.text = CreditsEconomy.convertCredits(completeSetReward);
				SlotsPlayer.addFeatureCredits(completeSetReward, "completedSetToaster");
				cardNumText.gameObject.SetActive(false);
				checkMark.SetActive(true);
				if (completedSetsList.Count > 0)
				{
					RoutineRunner.instance.StartCoroutine(waitForMultipleSet(completedSetsList));
				}
			}
		}
	}

	IEnumerator waitForToaster(JSON starPackData)
	{
		yield return new WaitForSeconds(lifetime);
		Collectables.claimPackDropNow(starPackData);
	}

	IEnumerator waitForMultipleSet(List<string> rewardSet)
	{
		yield return new WaitForSeconds(lifetime);
		Collectables.showCollectionToaster(rewardSet);
	}

	private void logoLoadFail(string assetPath, Dict data = null)
	{
		Debug.LogError("Failed to load set image at " + assetPath);
	}

	private void logoLoadedSuccess(string assetPath, Object obj, Dict data = null)
	{
		if (this != null && this.gameObject != null)
		{
			Material material = new Material(albumLogo.material.shader);
			material.mainTexture = obj as Texture2D;
			albumLogo.material = material;
			albumLogo.gameObject.SetActive(true);
		}
	}
}
