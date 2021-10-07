using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashSaleExperiment : EosExperiment
{
	public int startingPackageCount { get; private set; }
	public bool enabled { get; private set; }
	public int duration { get; private set; }
	public float speedParameter { get; private set; }
	public int cooldown { get; private set; }
	public int bonusPercentage { get; private set; }
	public int minWaitTime { get; private set; }
	public int maxWaitTime { get; private set; }
	public string package { get; private set; }
	public bool filterNineAmToTenPm { get; private set; }

	public FlashSaleExperiment(string name) : base(name)
	{

	}

	protected override void init(JSON data)
	{
		startingPackageCount = getEosVarWithDefault(data, "startingPackageCount", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_STARTING_PACKAGE_COUNT);
		enabled = getEosVarWithDefault(data, "enabled", false);
		duration = getEosVarWithDefault(data, "duration", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_DURATION);
		speedParameter = getEosVarWithDefault(data, "speedParameter", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_SPEED_PARAMETER);
		cooldown = getEosVarWithDefault(data, "cooldown", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_COOLDOWN);
		bonusPercentage = getEosVarWithDefault(data, "bonusPercentage", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_BONUES_PERCENTAGE);
		minWaitTime = getEosVarWithDefault(data, "minWaitTime", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_MIN_WAIT_TIME);
		maxWaitTime = getEosVarWithDefault(data, "maxWaitTime", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_MAX_WAIT_TIME);
		package = getEosVarWithDefault(data, "package", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_COIN_PACKAGE);
		filterNineAmToTenPm = getEosVarWithDefault(data, "filterNineAmToTenPm", ExperimentWrapper.FlashSale.FLASH_SALE_DEFAULT_9_TO_10_FILTER);
	}

	public override void reset()
	{
		base.reset();
		startingPackageCount = 1000;
	}
}