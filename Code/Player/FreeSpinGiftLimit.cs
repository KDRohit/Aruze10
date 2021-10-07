using System;
using UnityEngine;
using System.Collections;

public class FreeSpinGiftLimit : InboxLimitValue<int>
{
	public FreeSpinGiftLimit(int limit, int valueRemaining) : base(limit, valueRemaining)
	{
		valueCollected = limit - valueRemaining;
		PowerupsManager.addEventHandler(onPowerupActivated);
		SlotsPlayer.instance.onVipLevelUpdated += onVipLevelUpdate;
	}

	public void onVipLevelUpdate()
	{
		VIPLevel vipLevel = VIPLevel.find(SlotsPlayer.instance.adjustedVipLevel);
		setLimit(vipLevel.freeSpinLimit);
	}

	/// <inheritdoc/>
	public override void setLimit(int newValue)
	{
		limit = newValue;
		valueRemaining = Mathf.Max(0, currentLimit - valueCollected);
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

	private void onPowerupActivated(PowerupBase powerup)
	{
		if (powerup.name == PowerupBase.POWER_UP_FREE_SPINS_KEY || powerup.name == PowerupBase.POWER_UP_VIP_BOOSTS_KEY)
		{
			powerup.runningTimer.registerFunction(onPowerupExpired);
			valueRemaining = currentLimit - valueCollected;
		}
	}

	private void onPowerupExpired(Dict  args = null, GameTimerRange originalTimer = null)
	{
		valueRemaining = Mathf.Max(0, currentLimit - valueCollected);
	}

	/// <inheritdoc/>
	public override int currentLimit
	{
		get
		{
			int newLimit = limit;

			if (VIPStatusBoostEvent.isEnabled())
			{
				newLimit = VIPLevel.find(VIPLevel.getEventAdjustedLevel()).freeSpinLimit;
			}

			// powerups doubles any current gift limits, including users vip boosted limit
			if (PowerupsManager.hasActivePowerupByName(PowerupBase.POWER_UP_FREE_SPINS_KEY))
			{
				return newLimit * 2;
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