using UnityEngine;
using System.Collections;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCPlayerPortrait : MonoBehaviour, IRecycle
	{
		public FacebookFriendInfo profileInfo;
		[SerializeField] private SerializableDictionaryOfQFCIconTypeToPortraitVersion teamTypeToPortraitInfo;
		[SerializeField] private UISprite frameSprite;
		[SerializeField] private Animator portraitAnimator;
		[SerializeField] private Animator portraitUpdateAnimator;
		[SerializeField] private GameObject crownParent;
		[SerializeField] private string updateEffectAnimationName;

		public int currentKeys { get; private set; }
		private QFCTeamPortraitVersion currentTeamVersion;

		//part of recycle interface
		public void init(Dict args)
		{
			QFCMapDialog.QFCBoardPlayerIconType playerType = (QFCMapDialog.QFCBoardPlayerIconType)args.getWithDefault(D.TYPE, QFCMapDialog.QFCBoardPlayerIconType.AWAY);
			SocialMember member = (SocialMember)args.getWithDefault(D.PLAYER, null);
			int numberOfKeys = (int) args.getWithDefault(D.KEYS_NEED, 0);
			bool isTeamLeader = (bool)args.getWithDefault(D.OPTION, false);
			init(playerType, member, numberOfKeys, isTeamLeader);
		}

		public void init(QFCMapDialog.QFCBoardPlayerIconType playerType, SocialMember player, int numberOfKeys, bool isTeamLeader, bool showPointsBubble = true)
		{
			if (player != null)
			{
				profileInfo.member = player;
			}
			else
			{
				Material material = profileInfo.image.sharedMaterial;
				if (material == null)
				{
					material = new Material(LobbyOptionButtonActive.getOptionShader());
				}
				material.mainTexture = Random.Range(0, 2) == 0 ? profileInfo.defaultMale : profileInfo.defaultFemale;
				profileInfo.image.sharedMaterial = material;
			}
			currentKeys = numberOfKeys;
			resetPointsBubbles();
			currentTeamVersion = teamTypeToPortraitInfo[playerType];
			currentTeamVersion.keysLabel.text = currentKeys.ToString();
			currentTeamVersion.pointsBubble.SetActive(showPointsBubble);
			frameSprite.spriteName = currentTeamVersion.frameSpriteName;
			currentTeamVersion.keysBubbleAnimator.Play(currentTeamVersion.keysBubbleIdleAnimationName);
			crownParent.SetActive(isTeamLeader);
		}

		public void reset()
		{
			if (profileInfo != null)
			{
				profileInfo.recycle();
			}
		}

		public void playClickedAnimation()
		{
			portraitAnimator.Play(currentTeamVersion.clickedAnimation);
		}

		public void playDismissedAnimation()
		{
			portraitAnimator.Play(currentTeamVersion.dismissedAnimation);
		}

		public void playPortraitUpdateAnimation()
		{
			portraitUpdateAnimator.Play(updateEffectAnimationName);
		}

		public void ToggleTeamLeader(bool isTeamLeader)
		{
			crownParent.SetActive(isTeamLeader);
		}

		public IEnumerator updateKeysBubble(int newKeys, bool playRollup = true)
		{
			currentTeamVersion.keysBubbleAnimator.Play(currentTeamVersion.keysBubbleUpdateAnimationName);
			if (newKeys < currentKeys || !playRollup)
			{
				//rollup fails for negative key value
				currentTeamVersion.keysLabel.text = CommonText.formatNumber(currentKeys);
			}
			else
			{
				StartCoroutine(SlotUtils.rollup(currentKeys, newKeys, currentTeamVersion.keysLabel, false, 0.5f, false, false, isCredit:false));
				yield return new WaitForSeconds(0.5f);
			}
			currentTeamVersion.keysBubbleAnimator.Play(currentTeamVersion.keysBubbleIdleAnimationName);
			currentKeys = newKeys;
		}

		public void instantUpdateKeysBubble(int newKeys)
		{
			StartCoroutine(SlotUtils.rollup(currentKeys, newKeys, currentTeamVersion.keysLabel, false, 0.1f, false, false, isCredit:false));
		}

		public void playIdleBubbleAnimation()
		{
			currentTeamVersion.keysBubbleAnimator.Play(currentTeamVersion.keysBubbleIdleAnimationName);
		}

		private void resetPointsBubbles()
		{
			for (int i = 0; i < teamTypeToPortraitInfo.Count; i++)
			{
				teamTypeToPortraitInfo[(QFCMapDialog.QFCBoardPlayerIconType)i].pointsBubble.SetActive(false);
			}
		}

		[System.Serializable]
		private class QFCTeamPortraitVersion
		{ 
			public TextMeshPro keysLabel;
			public GameObject pointsBubble;
			public string frameSpriteName;
			public string clickedAnimation;
			public string dismissedAnimation;
			public Animator keysBubbleAnimator;
			public string keysBubbleRollupAnimationName;
			public string keysBubbleUpdateAnimationName;
			public string keysBubbleIdleAnimationName;
		}
		
		[System.Serializable] private class KeyValuePairOfQFCIconTypeToPortraitVersion : CommonDataStructures.SerializableKeyValuePair<QFCMapDialog.QFCBoardPlayerIconType, QFCTeamPortraitVersion> {}
		[System.Serializable] private class SerializableDictionaryOfQFCIconTypeToPortraitVersion : CommonDataStructures.SerializableDictionary<KeyValuePairOfQFCIconTypeToPortraitVersion, QFCMapDialog.QFCBoardPlayerIconType, QFCTeamPortraitVersion> {}
	}
}
