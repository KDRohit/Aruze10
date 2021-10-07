using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * SpinReel.cs
 * Subclass of SlotReel. Handles visual outcome for Reel games.
 * author: Nick Reynolds
 */
public class SpinReel : SlotReel
{
	private const float JP_BONUS_SYMBOL_SOUND_AND_ANIM_DELAY = 0.33f;
	private const string REEL_STOP_SOUND_PREFIX = "reel_stop";
	private const string REEL_STOP_SOUND_RESPIN_SUFFIX = "_respin";
	private const string REEL_STOP_SOUND_ZINDEX_ONE_PREFIX = "zindex1_reel_stop";
	private const string REEL_STOP_SOUND_FREESPIN_SUFFIX = "_freespin_";
	private bool waitingForRollbackStartCoroutinesToFinish = false;

	public SpinReel(ReelGame reelGame) : base(reelGame)
	{
		
	}

	public SpinReel(ReelGame reelGame, GameObject reelRoot) : base(reelGame, reelRoot)
	{
		
	}

	// frameUpdate - manages the state progession and the passage of time to the movement of the symbols.
	public override void frameUpdate()
	{
		base.frameUpdate();

		switch (_spinState)
		{
			case ESpinState.BeginRollback:
			{
				// reset the anticipation animation tracking stuff
				startedAnticipationAnims = 0;
				finishedAnticipationAnims = 0;

				float rollbackPct = Mathf.Min(1.0f, (Time.time - _rollbackStartTime) / gameData.beginRollbackSpeed);
				
				_reelOffset = -1.0f * rollbackPct * gameData.rollbackAmount;
				slideSymbols(_reelOffset);
				RoutineRunner.instance.StartCoroutine(checkForOnRollbackStartCoroutines());
				if (rollbackPct == 1.0f && !waitingForRollbackStartCoroutinesToFinish)
				{
					_spinState = ESpinState.Spinning;
				}
				break;
			}
				
			case ESpinState.Spinning:
			{
				_reelOffset += gameData.getSpinMovement(_reelGame.hasAutoSpinsRemaining, _reelID - 1);
				
				while (_reelOffset > 1f)
				{
					advanceSymbols();
					_reelOffset -= 1f;
				}
				
				slideSymbols(_reelOffset);
				break;
			}
				
			case ESpinState.SpinEnding:
			{
				// This state should continue behaving like Spinning, but switches reel position to start inserting the symbols needed to stop on the correct one.
				// State ends when the _reelStopIndex symbol is gameData.reelStopAmount past the correct stop position.
				_reelOffset += gameData.getSpinMovement(_reelGame.hasAutoSpinsRemaining, _reelID - 1);
				
				bool didAdvance = false;
				while (_reelOffset > 1f && (isForcingAdvanceBeforeStopIndexCheck || _reelPosition != _reelStopIndex))
				{
					advanceSymbols();
					_reelOffset -= 1f;

					if (isForcingAdvanceBeforeStopIndexCheck)
					{
						isForcingAdvanceBeforeStopIndexCheck = false;
					}

					didAdvance = true;
				}
				
				if (!isForcingAdvanceBeforeStopIndexCheck && (!didAdvance || _reelPosition == _reelStopIndex))
				{
					float targetOffset = gameData.reelStopAmount;
					
					bool bonusHit = false;
					bool scatterHit = false;
					bool TWHit = false;
					bool isSCHit = false;
					
					if (_reelPosition == _reelStopIndex && _reelOffset >= targetOffset)
					{
						_reelOffset = targetOffset;
						_spinState = ESpinState.EndRollback;
						foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
						{
							if (module.needsToExecuteOnReelEndRollback(this))
							{
								RoutineRunner.instance.StartCoroutine(module.executeOnReelEndRollback(this));
							}
						}

						_rollbackStartTime = Time.time;
						//Debug.Log("Time we started on reel Index" + _reelID + ":" + Time.time);
						
						// Grab Bonus Data
						for (int i = 0; i < visibleSymbols.Length; i++)
						{
							// make sure if this is a mega BN symbol we only trigger a bonusHit on the leftmost piece of the mega
							if (visibleSymbols[i].isBonusSymbol && visibleSymbols[i].getColumn() == 1)
							{
								// Bonus Hit.
								incrementBonusHits();

								// only play if this is also an anticipation reel 
								// so it only celebrates bonus if one is possible
								// i.e. if bonus needs reels [1,3,5] and you only
								// get BN on 1 and 5 then 1 will play but 5 will not 
								if (_isAnticipation)
								{
									bonusHit = true;
								}
								break;
							}

							// skip parts of tall bonus symbols, since only one part will trigger a bonus hit
							float symHeight = visibleSymbols[i].getWidthAndHeightOfSymbol().y;
							for (int k = visibleSymbols[i].getRow(); k < symHeight; k++)
							{
								i++;
							}
						}
						
						// Grab scatter Data and play appropriate reel stop sounds.
						for (int i = 0; i < visibleSymbols.Length; i++)
						{
							if (visibleSymbols[i].name.Contains("SC"))
							{
								// Scatter hit.
								_reelGame.engine.scatterHits++;
								scatterHit = true;
								break;
							}
						}

						// Check if this SpinReel has an actual 'SC' Symbol. This is separate so
						// we don't clobber the scatterHit functionality above. This allows
						// us to only play anticipation sounds for 'SC' symbols
						// and not for SC1..10 symbols. Used in marilyn02.
						if(_reelGame.playSoundsOnlyOnSCScatterSymbols)
						{
							for (int i = 0; i < visibleSymbols.Length; i++)
							{
								if (visibleSymbols[i].serverName == "SC")
								{
									isSCHit = true;
									break;
								}
							}
						}

						for (int i = 0; i < visibleSymbols.Length; i++)
						{
							if (visibleSymbols[i].name.FastStartsWith("TW"))
							{
								// We hit a TW, so lets play the TW sound when the time comes.
								TWHit = true;
								break;
							}
						}
						
						// Sound for All wow progressive freespins bonus hits.
						for (int i = 0; i < visibleSymbols.Length; i++)
						{
							// In wow Free Spins, its possible to get progressives in the reels.
							if (visibleSymbols[i].name.Contains("JP"))
							{
								// Grab the hits and increment it because we have a progressive symbol land
								int hits = _reelGame.engine.getAudioProgressiveBonusHits();
								_reelGame.engine.setAudioProgressiveBonusHits(hits+1);
								
								// Get the hincrement hits above (1 based) and generate the audio key
								hits = _reelGame.engine.getAudioProgressiveBonusHits();
								string bonusSoundKey = "";
								if (GameState.game.keyName.Contains("wow"))
								{
									bonusSoundKey = "WFS_prog_jackpot_hit_0" + hits;
								}
								else
								{
									// Handled in ScatterJackpotModule now.
									//bonusSoundKey = Audio.soundMap("jackpot_symbol_fanfare" + hits);
								}

								if (bonusSoundKey != "")
								{
									// Calculate the delay, if we have 1 hit no delay, 2 or 3 hits do 0.33 delay offset
									float delay = JP_BONUS_SYMBOL_SOUND_AND_ANIM_DELAY * i;
									
									// Play the Sound.
									Audio.play(bonusSoundKey, 1f, 0f, delay);
								}
							}

							// in SATC we want to play a special sound when the TW symbol lands
							if (GameState.game.keyName.Contains("satc02") && visibleSymbols[i].name.FastStartsWith("TW"))
							{
								Audio.play("TWDiamondSymbolLands");
							}
							else if (GameState.game.keyName.Contains("grandma01") && visibleSymbols[i].name.FastStartsWith("TW"))
							{
								Audio.play("TWFruitcakeLands");
							}
							else if (GameState.game.keyName.Contains("ani02") && visibleSymbols[i].name.FastStartsWith("SC"))
							{
								Audio.play("SymbolScatterNecklaceSparkle");
							}
							else if (GameState.game.keyName.Contains("sundy01") && visibleSymbols[i].name.FastStartsWith("TW"))
							{
								Audio.play(Audio.soundMap("trigger_symbol"));
							}
						}
						bool wasAnticipationEffectHiddenByModule = false;
						foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
						{
							if (module.needsToHideReelAnticipationEffectFromModule(this))
							{
								wasAnticipationEffectHiddenByModule = true;
								RoutineRunner.instance.StartCoroutine(module.hideReelAnticipationEffectFromModule(this));
							}
						}
						if (!wasAnticipationEffectHiddenByModule)
						{
							_reelGame.engine.hideAnticipationEffect(getRawReelID() + 1);
						}
						int rawReelID = getRawReelID(true);

						// Play appropriate reel stop sounds.
						if (!_reelGame.engine.isSlamStopPressed)
						{
							playReelStopSounds(rawReelID);
							//Debug.Log("stopReelSpin on " + reelID);

							// anticipation info doesn't include layer as a concept, so always assume we are talking about layer 0
							if (shouldPlayAnticipateEffect)
							{
								_reelGame.engine.checkAnticipationEffect(this); //We are looking up the reel number not the position in the array.
							}
						}
						else if (_reelID == _reelGame.engine.getReelArray().Length)
						{
							// if slam stop was pressed, and we don't have a bonus, play the normal slamstop sound.
							Audio.play(Audio.soundMap("slam_reels"));
						}
						
						// Play our anticipation sounds.
						int soundReelIndex = rawReelID;
						SlotReel reelFromEngine = _reelGame.engine.getSlotReelAt(rawReelID, layer);
						if (reelFromEngine != this)
						{
							// Sliding games keep their own information.
							soundReelIndex = reelID - 1;
						}

						if(_reelGame.playSoundsOnlyOnSCScatterSymbols)
						{
							if(isSCHit)
							{
								// In marilyn02 it is possible to have BN and SC symbols land on the same anticipation reel.
								// In this case we only want the SC landing sound to play. 
								_reelGame.engine.playAnticipationSound(soundReelIndex, false, scatterHit, false, layer);
							}
							else
							{
								// This also stops other SC1..10 symbols and twHit from playing an anticipation sound if they happen
								// to land on a reel with BN anticipation happenning. If future games need TW sounds, they will have
								// to add to this.
								_reelGame.engine.playAnticipationSound(soundReelIndex, bonusHit, false, false, layer);
							}
						}
						else
						{
							_reelGame.engine.playAnticipationSound(soundReelIndex, bonusHit, scatterHit, TWHit, layer);
						}

						foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
						{
							if (module.needsToExecuteOnSpinEnding(this))
							{
								module.executeOnSpinEnding(this);
							}
						}

						// check if we should animate anticipations yet, will only play those animations on synced reels once all synced reels are in the same spot
						// otherwise animating could cause issues with mega symbols
						bool shouldAnimateAnticipations = false;

						int reelIndex = _reelID - 1;
						int currentStopTimingIndex = _reelGame.getReelStopTimingIndex(reelIndex, position, layer);

						// make sure this reel has a reelTiming entry, otherwise just ignore it
						if (currentStopTimingIndex != -1)
						{
							if (reelIndex + 1 < _reelGame.engine.getReelRootsLength(layer))
							{
								int nextReelStopTimingIndex = currentStopTimingIndex + 1;
								int nextReelTiming = _reelGame.engine.reelTiming[nextReelStopTimingIndex];
								if (nextReelTiming != 0)
								{
									// next reel isn't synced to this one, so we should animate this reel and any reels synced with it that came before
									shouldAnimateAnticipations = true;
								}
								else
								{
									// the next reel is synced with this one, so we should wait until we reach the last synced reel in this set of synced reels
									shouldAnimateAnticipations = false;
								}
							}
							else
							{
								// this is the last reel, so we should animate it now, since none will follow it
								shouldAnimateAnticipations = true;
							}
						}

						// make sure this reel is in the final position before doing the anticipation animations
						slideSymbols(_reelOffset);
						
						if (shouldAnimateAnticipations)
						{
							int currentStopInfoIndex = currentStopTimingIndex;
							bool isFinalLinkedReelAnimated = false;

							// we need to make sure that everything in this stop order is currently stopped, 
							// otherwise we will be doing stuff while for instance a layer isn't stopped yet
							// if they aren't all stopped we will just let the final one to stop handle 
							// anticipation stuff
							bool isEveryReelInCurrentStopOrderIndexStopped = true;
							foreach (ReelGame.StopInfo stopInfo in _reelGame.stopOrder[currentStopInfoIndex])
							{
								SlotReel currentReel = _reelGame.engine.getSlotReelAt(stopInfo.reelID, stopInfo.row, stopInfo.layer);

								if (currentReel != null && !currentReel.isEndingRollbackOrStopped)
								{
									isEveryReelInCurrentStopOrderIndexStopped = false;
									break;
								}
							}

							if (isEveryReelInCurrentStopOrderIndexStopped)
							{
								// check currentStopInfoIndex to make sure this isn't a reel that doesn't have a stop index assigned
								while (!isFinalLinkedReelAnimated && currentStopInfoIndex != -1)
								{
									foreach (ReelGame.StopInfo stopInfo in _reelGame.stopOrder[currentStopInfoIndex])
									{
										SlotReel currentReel = _reelGame.engine.getSlotReelAt(stopInfo.reelID, stopInfo.row, stopInfo.layer);
										
										if (currentReel != null)
										{
											foreach (SlotSymbol symbol in currentReel.visibleSymbols)
											{
												if (!_reelGame.engine.isSlamStopPressed && currentReel.isAnticipationReel() && symbol.hasAnimator && !symbol.isAnimatorDoingSomething)
												{
													bool isModuleHandlingAnticipation = false;

													//Debug.Log("SpinReel.frameUpdate() - Animating aniticipation for symbol: " + symbol.name + " at stopInfo.reelID = " + stopInfo.reelID + "; stopInfo.row = " + stopInfo.row + "; stopInfo.layer = " + stopInfo.layer + "; currentReel = " + currentReel);

													foreach(SlotModule module in _reelGame.cachedAttachedSlotModules)
													{
														if (module.needsToExecuteForSymbolAnticipation(symbol))
														{
															module.executeForSymbolAnticipation(symbol);
															isModuleHandlingAnticipation = true;
														}
													}

													if (!isModuleHandlingAnticipation)
													{
														// Play any setup anticipation animation if stopping naturally
														incrementStartedAnticipationAnims();
														symbol.animateAnticipation(onAnticipationAnimationDone);
													}
												}
											}
										}
									}
									
									int currentReelTiming = _reelGame.engine.reelTiming[currentStopInfoIndex];
									if (currentStopInfoIndex == 0)
									{
										// we've reached the starting reels, so their aren't any reels before it that could be synced
										isFinalLinkedReelAnimated = true;
									}
									else
									{
										if (currentReelTiming != 0)
										{
											// we've reached a reel whose timing wasn't 0, which means this is the end of this series of synced reels
											// (Note: that could mean this was just a single reel with nobody synced to it)
											isFinalLinkedReelAnimated = true;
										}
										else
										{
											isFinalLinkedReelAnimated = false;
										}
									}
									currentStopInfoIndex -= 1;
								}
							}
						}
					}
				}

				slideSymbols(_reelOffset);

				break;
			}
				
			case ESpinState.EndRollback:
			{
				float rollbackPct = Mathf.Min( 1.0f, (Time.time - _rollbackStartTime) / (gameData.endRollbackSpeed * KNOCKBACK_MULTIPLIER));
				_reelOffset = (1.0f - rollbackPct) * gameData.reelStopAmount;
				
				slideSymbols(_reelOffset);
				
				if (rollbackPct == 1.0f)
				{
					//Debug.Log("Time we ended on reel Index: " + _reelID + "; Time: " + Time.time);
					_spinState = ESpinState.Stopped;

					if (_replacementStrip != null)
					{
						// check if the size of the base reel strip is shorter than the replacement, 
						// in which case we need to adjust _reelPosition so it isn't out of bounds
						if (_reelData.reelStrip.symbols.Length < _replacementStrip.symbols.Length)
						{
							_reelPosition = _reelData.reelStrip.symbols.Length - numberOfBottomBufferSymbols - 1;
						}
						
						//_replacementStrip = null;
					}
					
					//					Debug.Log("Reel " + _reelPosition + ", " + _reelID + " has stopped, showing symbols: " + shownSymbols);
					//					if (foundMutations != "")
					//					{
					//						Debug.Log("Found mutations: " + foundMutations);
					//					}
				}
				break;
			}
				
			case ESpinState.Stopped:
			{
				if (!anticipationAnimsFinished)
				{
					// check if the user has forced the reels to stop, in which case we need to cancel waiting for anticipation animations if the user had bonuses showing
					if (_reelGame.engine.isSlamStopPressed)
					{
						// Look for symbols that are already doing anticipation animations and stop them
						for (int i = 0; i < visibleSymbols.Length; i++)
						{
							SlotSymbol symbol = visibleSymbols[i];

							if (_isAnticipation && symbol.hasAnimator && symbol.isAnimatorDoingSomething)
							{
								// force animations to stop
								// Dec. 7 2015 NOTE: this doesn't call the animation callback!
								symbol.haltAnimation();
							}
						}

						// mark all animation as finished, which will allow us to continue to the next steps of starting a bonus
						finishedAnticipationAnims = startedAnticipationAnims;
					}
				}

				break;
			}
		}
	}

	// handles playing a specific reel's stop (overridden, custom and default) and VO sounds
	private void playReelStopSounds(int rawReelID)
	{
		if (shouldPlayReelStopSound)
		{
			if (reelStopVOSound != "")
			{
				Audio.play(reelStopVOSound);
			}

			// If using an override sound, play that
			if (reelStopSoundOverride != "")
			{
				Audio.playSoundMapOrSoundKey(reelStopSoundOverride);
			}
			else if (rawReelID >= 0)
			{
				// rawReelID is zero based, but reel_stop sounds are one based, so increment it
				playMappedReelStopSound(rawReelID + 1);
			}
		}
	}

	// plays the reel's mapped stop sound for the base game/free spins, also checks for layered reels stop sound (takes precedence)
	private void playMappedReelStopSound(int reelStopSoundNumber)
	{
		string customReelStopMap;
		
		// check if there are mapped sounds for layer 1
		if (layer == 1)
		{
			customReelStopMap = REEL_STOP_SOUND_ZINDEX_ONE_PREFIX + reelStopSoundNumber;
			if (Audio.canSoundBeMapped(customReelStopMap))
			{
				Audio.play(Audio.soundMap(customReelStopMap));
				return;
			}
		}
		
		// Check for respin reelstop specific sounds
		if (_reelGame.currentReevaluationSpin != null)
		{
			if (_reelGame.isFreeSpinGame() || _reelGame.isDoingFreespinsInBasegame())
			{
				// Check for freespin respin reelstop sound
				customReelStopMap = REEL_STOP_SOUND_PREFIX + REEL_STOP_SOUND_RESPIN_SUFFIX + REEL_STOP_SOUND_FREESPIN_SUFFIX + reelStopSoundNumber;
				if (Audio.canSoundBeMapped(customReelStopMap))
				{
					Audio.play(Audio.soundMap(customReelStopMap));
					return;
				}
			}
			
			// Check for standard base respin reelstop sound
			customReelStopMap = REEL_STOP_SOUND_PREFIX + REEL_STOP_SOUND_RESPIN_SUFFIX + reelStopSoundNumber;
			if (Audio.canSoundBeMapped(customReelStopMap))
			{
				// play the standard base respin reelstop
				Audio.play(Audio.soundMap(customReelStopMap));
				return;
			}
		}
		
		
		// If we're in a freespins or freespins in base check for a freespin override
		if (_reelGame.isFreeSpinGame() || _reelGame.isDoingFreespinsInBasegame())
		{
			customReelStopMap = REEL_STOP_SOUND_PREFIX + REEL_STOP_SOUND_FREESPIN_SUFFIX + reelStopSoundNumber;
			// and a reelstop is mapped for it
			if (Audio.canSoundBeMapped(customReelStopMap))
			{
				// play the freespin reelstop
				Audio.play(Audio.soundMap(customReelStopMap));
				return;
			}
		}

		
		// No specific reelstop sounds for this game have been triggered, so play the standard base one
		string defaultBaseGameReelStopKey = Audio.soundMap(REEL_STOP_SOUND_PREFIX + reelStopSoundNumber);
		Audio.play(defaultBaseGameReelStopKey);
	}

	private void incrementBonusHits()
	{
		bool handledInModule = false;
		foreach(SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnBonusHitsIncrement())
			{
				module.executeOnBonusHitsIncrement(reelID);
				handledInModule = true;
			}
		}

		if (!handledInModule) 
		{
			_reelGame.engine.bonusHits++;
		}
	}

	private IEnumerator checkForOnRollbackStartCoroutines()
	{
		foreach (SlotModule module in _reelGame.cachedAttachedSlotModules)
		{
			if (module.needsToExecuteOnBeginRollback(this))
			{
				waitingForRollbackStartCoroutinesToFinish = true;
				yield return RoutineRunner.instance.StartCoroutine(module.executeOnBeginRollback(this));
			}
		}
		waitingForRollbackStartCoroutinesToFinish = false; //Now that the coroutines are finished, lets start spinning
	}
}
