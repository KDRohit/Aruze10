using System;
using UnityEngine;
using System.Collections;

public class CoinGiftLimit : InboxLimitValue<int>
{
	public CoinGiftLimit(int limit, int valueRemaining) : base(limit, valueRemaining)
	{
		valueCollected = limit - valueRemaining;
		SlotsPlayer.instance.onVipLevelUpdated += onVipLevelUpdate;
	}

	public void onVipLevelUpdate()
	{
		VIPLevel vipLevel = VIPLevel.find(SlotsPlayer.instance.adjustedVipLevel);
		setLimit(vipLevel.creditsGiftLimit);
	}

	/// <inheritdoc/>
	public override void setLimit(int newValue)
	{
		limit = newValue;
		valueRemaining = Mathf.Max(0, limit - valueCollected);
	}

	/// <inheritdoc/>
	public override void add(int amount)
	{
		valueRemaining += amount;
		valueCollected = Mathf.Max(0, valueCollected - amount);
	}

	/// <inheritdoc/>
	public override void subtract(int amount)
	{
		valueRemaining = Mathf.Max(0, valueRemaining - amount);
		valueCollected += amount;
	}

	/// <inheritdoc/>
	public override int currentLimit
	{
		get
		{
			int newLimit = limit;

			if (VIPStatusBoostEvent.isEnabled())
			{
				newLimit = VIPLevel.find(VIPLevel.getEventAdjustedLevel()).creditsGiftLimit;
			}

			return newLimit;
		}
	}

	/// <inheritdoc/>
	public override int amountRemaining
	{
		get { return Mathf.Max(0,valueRemaining); }
	}

	/// <inheritdoc/>
	public override int amountCollected
	{
		get { return valueCollected; }
	}
}