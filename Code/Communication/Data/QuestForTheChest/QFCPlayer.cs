namespace QuestForTheChest
{
	public class QFCPlayer
	{
		public int keys;
		public int lastKeyTimestamp;
		public int position;
		public string url;
		public string name;
		public int round; //number of times a player has gone around the board
		public SocialMember member;

		/// <summary>
		/// Get a shallow copy
		/// </summary>
		/// <returns></returns>
		public QFCPlayer getClone()
		{
			return (QFCPlayer)this.MemberwiseClone();
		}
	}
}

