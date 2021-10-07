using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace QuestForTheChest
{
	public class QFCRewardOverlay : QFCMapDialogOverlay
	{
		[SerializeField] private GameObject sizer;
		[SerializeField] private ButtonHandler closeButton;
		[SerializeField] private ButtonHandler collectButton;
		[SerializeField] private TextMeshPro buttonLabel;
		[SerializeField] private float coinFlyDuration;
		[SerializeField] private Transform coinEndPos;
		[SerializeField] private GameObject coinBurstPrefab;
		[SerializeField] private float coinBurstDuration;

		private List<QFCReward> rewards;
		private DialogBase.AnswerDelegate clickCallback;
		private string eventId;
		private long coinAmount;
		private Transform coinStartPos;
		private bool canAutoClose = false;
		
		private string checkpointSound = "QfcCheckpoint";
		private string checkpointCollectSound = "QfcCheckpointCollect";
		private string phylum = "";

		public void initIntro(DialogBase.AnswerDelegate callback)
		{
			//set background a text assets
			setUpDynamicNodeAssets(0);
			clickCallback = callback;

			//scale based on resolution
			adjustScale();

			//add key instruction
			attachStartInfo();

			buttonLabel.text = Localize.text("OK");

			phylum = "dialog_beginning_node";
			logView();

			//register button handlers
			collectButton.registerEventDelegate(onClick, null);
			closeButton.registerEventDelegate(onClick, null);
		}

		private void logView()
		{
			StatsManager.Instance.LogCount("dialog", "ptr_v2", phylum, "", "", "view");
		}

		private void logClick(bool isAutoClosing)
		{
			StatsManager.Instance.LogCount("dialog", "ptr_v2", phylum, "", "cta", isAutoClosing? "auto_close" : "click");
		}

		private void logClose()
		{
			StatsManager.Instance.LogCount("dialog", "ptr_v2", phylum, "", "close", "click");
		}


		public void init(string id, int nodeIndex, List<QFCReward> rewardList, DialogBase.AnswerDelegate callback)
		{
			initSounds();
			Audio.play(checkpointSound);
			//set background a text assets
			setUpDynamicNodeAssets(nodeIndex);

			//set variables
			rewards = rewardList;
			clickCallback = callback;
			eventId = id;

			//scale based on resolution
			adjustScale();

			buttonLabel.text = Localize.text("Collect");

			phylum = "dialog_story_node_" + ((nodeIndex + 1) / 4);
			logView();
			
			coinAmount = 0;

			int keysToAdd = 0;
			
			//attach rewards
			if (null != rewards)
			{
				for (int i = 0; i < rewards.Count; ++i)
				{
					if (rewards[i] == null)
					{
						Bugsnag.LeaveBreadcrumb("null QFC reward");
						continue;
					}

					QFCContainerItem item = attachReward(rewards[i]);

					if (rewards[i].type == "coin" && item != null)
					{
						coinAmount += rewards[i].value;
						coinStartPos = item.anchor == null ? gameObject.transform : item.anchor;
					}
					else if (rewards[i].type == "token")
					{
						keysToAdd += (int)rewards[i].value;
					}

				}
			}
			
			//register button handlers
			collectButton.registerEventDelegate(onClick, Dict.create(D.EVENT_ID, eventId, D.KEY, keysToAdd));
			closeButton.registerEventDelegate(onClick, Dict.create(D.EVENT_ID, eventId, D.KEY, keysToAdd));

			if (SlotBaseGame.instance != null && SlotBaseGame.instance.hasAutoSpinsRemaining)
			{
				canAutoClose = true;
				GameTimerRange.createWithTimeRemaining(15).registerFunction(autoCloseTimeout, Dict.create(D.EVENT_ID, eventId, D.KEY, keysToAdd, D.OPTION, true));
			}
		}
		
		private void autoCloseTimeout(Dict args = null, GameTimerRange sender = null)
		{
			if (this != null && canAutoClose)
			{
				onClick(args);
			}
		}

		protected IEnumerator coinBurst()
		{
			GameObject obj = NGUITools.AddChild(coinStartPos, coinBurstPrefab);
			if (obj != null)
			{
				QFCCoinBurst burst = obj.GetComponent<QFCCoinBurst>();
				if (burst != null)
				{
					burst.setTarget(coinEndPos);
				}
				Animator anim = obj.GetComponent<Animator>();
				if (anim != null)
				{
					yield return StartCoroutine(CommonAnimation.playAnimAndWait(anim, "Anim"));
				}
			}

			Destroy(obj);
		}

		protected IEnumerator coinFly()
		{
			// Create the coin as a child of "sizer", at the position of "coinIconSmall",
			// with a local offset of (0, 0, -100) so it's in front of everything else with room to spin in 3D.
			CoinScriptUpdated  coin = CoinScriptUpdated.create(
				sizer.transform,
				coinStartPos.position,
				new Vector3(0, 0, -100)
			);

			Audio.play("initialbet0");
			Vector2 destination = NGUIExt.localPositionOfPosition(sizer.transform, coinEndPos.position);
			yield return StartCoroutine(coin.flyTo(destination, coinFlyDuration));
			coin.destroy();
		}

		private void initSounds()
		{
			string suffix = ExperimentWrapper.QuestForTheChest.theme;
			checkpointSound = "QfcCheckpoint" + suffix;
			checkpointCollectSound = "QfcCheckpointCollect" + suffix;
		}

		private void adjustScale()
		{
			if (sizer != null && sizer.GetComponent<AspectRatioScaler>() != null)
			{
				// clearly we are scaling to a different aspect ratio, and dialog base should not be
				// trying to scale anything!!

				AspectRatioScaler scaler = sizer.GetComponent<AspectRatioScaler>();
				if (scaler.goalAspectRatio <= AspectRatioScaler.IPAD_ASPECT)
				{
					sizer.transform.localScale = Dialog.getAnimScale(Dialog.AnimScale.FULL);
					iTween.EaseType easeType = Dialog.getAnimEaseType(Dialog.AnimEase.SMOOTH, false);
					iTween.ScaleTo(sizer.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.25f, "islocal", true, "delay", .1f, "easetype", easeType));
				}
			}
			else
			{
				sizer.transform.localScale = Dialog.getAnimScale(Dialog.AnimScale.FULL);
				iTween.EaseType easeType = Dialog.getAnimEaseType(Dialog.AnimEase.SMOOTH, false);
				iTween.ScaleTo(sizer.gameObject, iTween.Hash("scale", Vector3.one, "time", 0.25f, "islocal", true, "delay", .1f, "easetype", easeType));
			}
		}

		private void onClick(Dict args)
		{
			canAutoClose = false;
			Audio.play(checkpointCollectSound);

			collectButton.enabled = false;
			closeButton.enabled = false;

			logClick((bool)args.getWithDefault(D.OPTION, false));
			
			StartCoroutine(finishPresentationAndClose(args));

		}

		private IEnumerator finishPresentationAndClose(Dict args)
		{
			if (coinAmount > 0)
			{
				//run the coin fly
				StartCoroutine(coinBurst());

				yield return new WaitForSeconds(coinBurstDuration);
			}

			if (clickCallback != null)
			{
				clickCallback(args);
			}

			logClose();
			Destroy(gameObject);
		}
	}

}