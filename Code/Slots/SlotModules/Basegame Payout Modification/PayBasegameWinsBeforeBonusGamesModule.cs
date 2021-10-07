using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Module that will tell the basegame that it should payout any base game wins before
 * launching a bonus game.  These wins will be treated as fully paid out and rolled up directly
 * to the players wallet without a big win.
 *
 * Creaiton Date: 6/3/2020
 * Original Author: Scott Lepthien
 */
public class PayBasegameWinsBeforeBonusGamesModule : SlotModule
{
	// isPayingBasegameWinsBeforeBonusGames() section
	// By setting this you can control if basegame wins are fully paid out
	// before going into bonus games (including delaying their transitions until
	// after base game payout).
	//
	// Normally basegame wins are paid out after bonus games, but for gen97 Cash Tower and maybe
	// some future games we want to pay out the base game wins before the bonus game
	// due to the bonus functioning in a way where we want to get the base payouts done
	// before starting the complciated bonus game flow.
	//
	// NOTE: If any module sets this to true it will be treated as being used
	// NOTE: Big wins will not occur for the base winnings even if they go over the
	// threshold (since it would be a fairly big interruption for transitioning into
	// bonuses).  This also means that the final bonus payout will only big win
	// if it goes over the big win threshold on its own.
	public override bool isPayingBasegameWinsBeforeBonusGames()
	{
		return true;
	}
}
