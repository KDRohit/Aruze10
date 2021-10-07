public class BoardGameMiniSlotBoostPowerup : PowerupBase
{
    protected override void init(JSON data = null)
    {
        rarity = Rarity.UNCOMMON;
        name = BOARDGAME_MINISLOT_BOOST;
        isDisplayablePowerup = false;
    }
}