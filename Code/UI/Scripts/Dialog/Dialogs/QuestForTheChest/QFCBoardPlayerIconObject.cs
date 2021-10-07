using UnityEngine;
using TMPro;

/**
Attached to the parent dialog object so the buttons can be linked and processed for clicks.
**/
namespace QuestForTheChest
{
	public class QFCBoardPlayerIconObject : MonoBehaviour
	{
		public TextMeshPro playerCount;
		public Animator animator;
		public string moveAnimation;
		public UISprite iconSprite;
		public string leaderSpriteName;
		public string defaultSpriteName;

		[SerializeField] private string idleAnimName;
		
		public void setToDefaultSprite()
		{
			iconSprite.spriteName = defaultSpriteName;
		}

		public void setToLeaderSprite()
		{
			iconSprite.spriteName = leaderSpriteName;
		}

		public void playIdleAnimation()
		{
			animator.Play(idleAnimName);
		}
	}
}
