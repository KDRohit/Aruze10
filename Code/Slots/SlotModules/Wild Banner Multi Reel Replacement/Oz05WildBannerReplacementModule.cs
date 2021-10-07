using UnityEngine;
using System.Collections;

public class Oz05WildBannerReplacementModule : DeprecatedWildBannerMultiReelReplacementModule
{

	// create effects and symbol banner, in that order
	protected override IEnumerator playWildBannerAt(int reelID, GameObjectCacher bannerPrefab)
	{
		int bannerIndex = numWildBanners % WILD_BANNER_ANIMATION_NAMES.Length;
		GameObject preBannerEffect = null;
		GameObject bannerEffect = null;

		//This is for any game that need to play an effect before placing the expanding the wild banners
		if (prePlaceWildBannerEffectPrefab != null)
		{
			preBannerEffect = prePlaceBannerEffectCacher.getInstance();
			preBannerEffect.SetActive(true);
			preBannerEffect.transform.parent = wildBannerParents[reelID].transform;
			preBannerEffect.transform.localPosition = PRE_PLACE_WILD_BANNER_EFFECT_OFFSET;
			preBannerEffect.transform.localScale = Vector3.one;
			if (wildBannerScalesOnReels != null && wildBannerScalesOnReels.Length > reelID)
			{
				preBannerEffect.transform.localScale = wildBannerScalesOnReels[reelID];
			}

			//We've played both sounds we still have more reveals, reset index
			if (revealIndex > 1)
			{
				revealIndex = 0;
			}
			Audio.play(Audio.soundMap(VERTICAL_WILD_PRE_PLACE_KEYS[gameType, revealIndex]), 1.0f, 0.0f, VERTICAL_WILD_PRE_PLACE_SOUND_DELAY);
			yield return new TIWaitForSeconds(prePlaceAnimationLength);
			//make sure we play the next sound on the next pre place
			++revealIndex;
		}

		if (wildBannerEffectPrefab != null)
		{
			bannerEffect = bannerEffectCacher.getInstance();
			bannerEffect.SetActive(true);
			bannerEffect.transform.parent = wildBannerParents[reelID].transform;
			bannerEffect.transform.localPosition = WILD_BANNER_EFFECT_OFFSET;
			bannerEffect.transform.localScale = Vector3.one;
			if (wildBannerScalesOnReels != null && wildBannerScalesOnReels.Length > reelID)
			{
				bannerEffect.transform.localScale = wildBannerScalesOnReels[reelID];
			}

			bannerEffect.GetComponent<Animator>().Play(WILD_BANNER_ANIMATION_NAMES[bannerIndex]);
			bannerEffect.GetComponent<Animator>().speed = WILD_BANNER_ANIMATION_SPEED;
		}

		if (!shouldOnlyPlayFirstBannerSound || (bannerIndex == 0))
		{
			Audio.play(Audio.soundMap(VERTICAL_WILD_INIT_KEYS[gameType]), 1.0f, 0.0f, VERTICAL_WILD_INIT_SOUND_DELAY);
		}

		yield return new TIWaitForSeconds(WAIT_BEFORE_CREATING_SYMBOL_BANNERS[bannerIndex]); // 1st wait

		foreach (SlotSymbol symbol in reelGame.engine.getVisibleSymbolsAt(reelID))
		{
			if (symbol.name != featureMutation.symbol)
			{
				if (featureMutation.symbol != "")
				{
					symbol.mutateTo(featureMutation.symbol);
				}
			}
		}

		// Determine if there is a specific numWildBanners reveal sound or just generic
		string revealSoundKey = string.Format("{0}{1}", VERTICAL_WILD_REVEAL_KEYS[gameType], numWildBanners + 1);
		bool specificKeyExists = Audio.canSoundBeMapped(revealSoundKey);

		if (specificKeyExists)
		{
			Audio.play(Audio.soundMap(revealSoundKey), 1.0f, 0.0f, VERTICAL_WILD_REVEAL_SOUND_DELAY);
		}
		else
		{
			Audio.play(Audio.soundMap(VERTICAL_WILD_REVEAL_KEYS[gameType]), 1.0f, 0.0f, VERTICAL_WILD_REVEAL_SOUND_DELAY);
		}

		GameObject bannerSymbol = bannerPrefab.getInstance();
		bannerSymbol.SetActive(true);
		// This forces the default animation to play from the beginning instead of playing a frame where it was left off on the last spin
		bannerSymbol.GetComponent<Animator>().Update(0.0f);
		bannerSymbol.transform.parent = wildBannerParents[reelID].transform;
		bannerSymbol.transform.localPosition = WILD_BANNER_SYMBOL_OFFSET;
		bannerSymbol.transform.localScale = Vector3.one;
		if (wildBannerScalesOnReels != null && wildBannerScalesOnReels.Length > reelID)
		{
			bannerSymbol.transform.localScale = wildBannerScalesOnReels[reelID];
		}

		instantiatedBanners.Add(bannerSymbol, bannerPrefab);
		wildBanners[reelID] = bannerSymbol;

		yield return new TIWaitForSeconds(WAIT_BEFORE_DESTROYING_EFFECTS[bannerIndex]); // 2nd wait

		if (wildBannerEffectPrefab != null)
		{
			bannerEffect.transform.localScale = Vector3.one;
			bannerEffectCacher.releaseInstance(bannerEffect);
		}

		if (prePlaceBannerEffectCacher != null && preBannerEffect != null)
		{
			prePlaceBannerEffectCacher.releaseInstance(preBannerEffect);
		}
	}
}
