using System;
using UnityEngine;
using System.Collections;
using NUnit.Framework;

public class UnitTestInbox
{
	private static JSON testData = new JSON("{\"type\":\"inbox_items_event\",\"event\":\"i7ta9Nv6ksmS1QoUanTklOuvwCOgyCNfNA01t904k9d3c\",\"creation_time\":\"1589485743\",\"player_id\":\"39411032375\",\"inbox_items\":[{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"coins\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"\",\"args\":\"1000\"}},\"event_id\":\"7xA4OLECd8p1p39iQGDnFLt5kRPsBnkNW8SbgewNB8S2e\",\"message\":\"Claim your coins!\",\"message_params\":[],\"message_key\":\"Gifted coins\",\"sort_order\":\"2\",\"cta_text\":\"Claim!\",\"expiration\":\"1590157056\",\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"default_gifted_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"coins\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"\",\"args\":\"1000\"}},\"event_id\":\"x52wflV97F8gUSG1WVpACU2Jj5xtDf3Sj65zlp1t0w0qm\",\"message\":\"Claim your coins!\",\"message_params\":[],\"message_key\":\"Gifted coins\",\"sort_order\":\"2\",\"cta_text\":\"Claim!\",\"expiration\":\"1590157057\",\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"default_gifted_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"coins\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"\",\"args\":\"1000\"}},\"event_id\":\"8AESWhcW4PWzq63BTteQ96lsH7SUMFGUkNBRnTiRQo7ZV\",\"message\":\"Claim your coins!\",\"message_params\":[],\"message_key\":\"Gifted coins\",\"sort_order\":\"2\",\"cta_text\":\"Claim!\",\"expiration\":\"1590157057\",\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"default_gifted_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"123456\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"\",\"args\":null}},\"event_id\":\"BqwjyUZMqj42ujDJibCnXyTnrtGyf7LGLtn4USwGe4Eeh\",\"message\":\"Check out this special offer!\",\"message_params\":[],\"message_key\":\"Special Offer_copy\",\"sort_order\":\"1\",\"cta_text\":\"Buy!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"default_special_offer_00.png\",\"timer\":false,\"coin_package\":\"coin_package_5\",\"bonus_percent\":\"18\"},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1000000\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"open_elite_pass\",\"args\":\"Check out Elite! [Teaser for non members]\"}},\"event_id\":\"4nMmkhR6MhFcwkgIaiFGphA6FV3z77yFXRk3ycOM4Z47v\",\"message\":\"Check out Elite! [Teaser for non members]\",\"message_params\":[],\"message_key\":\"Elite Teaser non members \",\"sort_order\":\"2\",\"cta_text\":\"Click!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"elite_nonmember_teaser.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"collect_credits\",\"args\":\"2000000\"}},\"event_id\":\"YJJFXoq6oaGZSShhAM9ILfA7REvHiIR6H4FKez8yQu7k4\",\"message\":\"Here are {0} coins from your remaining Elite pass points!\",\"message_params\":[\"credits\"],\"message_key\":\"Excess coins - elite pass\",\"sort_order\":\"3\",\"cta_text\":\"Claim!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"elite_member_bonus_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"collect_credits\",\"args\":\"2000000\"}},\"event_id\":\"tgPfJ7V0p5PkqNcKzsPliVJN0LVJoNwiEj8XKM8KZFcnF\",\"message\":\"Here are {0} coins from your remaining Elite pass points!\",\"message_params\":[\"credits\"],\"message_key\":\"Excess coins - elite pass\",\"sort_order\":\"3\",\"cta_text\":\"Claim!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"elite_member_bonus_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"collect_credits\",\"args\":\"2000000\"}},\"event_id\":\"2f0SM851JNL8vJSP5je5FKrEyoVdB2bO3k7DSNn2r8iA5\",\"message\":\"Here are {0} coins from your remaining Elite pass points!\",\"message_params\":[\"credits\"],\"message_key\":\"Excess coins - elite pass\",\"sort_order\":\"3\",\"cta_text\":\"Claim!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"elite_member_bonus_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"collect_credits\",\"args\":\"2000000\"}},\"event_id\":\"VgyWGZER3f6Ur3Azv2yVf1LMFfW96GXxeP99tkn1Cg0lI\",\"message\":\"Here are {0} coins from your remaining Elite pass points!\",\"message_params\":[\"credits\"],\"message_key\":\"Excess coins - elite pass\",\"sort_order\":\"3\",\"cta_text\":\"Claim!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"elite_member_bonus_coins.png\",\"timer\":false},{\"sender_zid\":\"39411032375\",\"cooldown\":\"0\",\"max_claims\":\"1\",\"inbox_tab\":\"messages\",\"actions\":{\"close\":{\"action\":null,\"args\":null},\"primary\":{\"action\":\"collect_credits\",\"args\":\"2000000\"}},\"event_id\":\"xRllx6SSHeF2ObcGFxfLMe9OtGrkMGoBmCDbq4wjAqtEo\",\"message\":\"Here are {0} coins from your remaining Elite pass points!\",\"message_params\":[\"credits\"],\"message_key\":\"Excess coins - elite pass\",\"sort_order\":\"3\",\"cta_text\":\"Claim!\",\"expiration\":null,\"claims\":\"0\",\"last_viewed_time\":\"0\",\"background\":\"elite_member_bonus_coins.png\",\"timer\":false},{\"sender_zid\":\"77613685285\",\"inbox_tab\":\"coins\",\"actions\":{\"close\":{\"action\":\"cancel\",\"args\":null},\"primary\":{\"action\":\"send_credits\",\"args\":null}},\"event_id\":\"C29aEHPCOsS1AoyEZAc7Moy8ATIsEjFgx5vQdelnHx3ha\",\"sort_order\":\"3\"}]}");

	[Test]
	public static void testInit()
	{
		try
		{
			InboxInventory.init();
		}
		catch (Exception e)
		{
			Assert.Fail("Failed inbox initialization: " + e.Message);
		}
	}

	[Test]
	public static void testContainsData()
	{
		InboxInventory.resetStaticClassData();
		InboxInventory.onInboxUpdate(testData);
		InboxItem item = InboxInventory.items[0];
		Assert.AreEqual(true, InboxInventory.containsItem(item.itemData));
	}

	[Test]
	public static void testFindItemBy()
	{
		InboxInventory.resetStaticClassData();
		InboxInventory.onInboxUpdate(testData);

		InboxItem item = InboxInventory.items[0];
		Assert.AreEqual(item, InboxInventory.findItemBy(item.senderZid, item.itemType));
	}

	[Test]
	public static void testActionItems()
	{
		InboxInventory.resetStaticClassData();
		InboxInventory.onInboxUpdate(testData);

		Assert.AreEqual(0, InboxInventory.totalActionItems(InboxItem.InboxType.FREE_CREDITS));
		Assert.AreEqual(5, InboxInventory.totalActionItems(InboxItem.InboxType.MESSAGE));
	}

	[Test]
	public static void testTotalActionItems()
	{
		InboxInventory.resetStaticClassData();
		InboxInventory.onInboxUpdate(testData);

		Assert.Greater(InboxInventory.totalActionItems(true), 0);
	}
}