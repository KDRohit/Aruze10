using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/*
Controls UI behavior of a menu option button that has an action instead of a game.
*/

public class LobbyOptionButtonAction : LobbyOptionButtonActive
{
	// Even though this class needs no special behavior, we can't use LobbyOptionButtonActive directly
	// because it's an abstract class. Maybe some day this class will have special behavior for actions.
}
