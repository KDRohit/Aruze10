using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/* 
 * Server action class for handling all the network actions
 */

public class WindowsPaymentAction : ServerAction 
{
	// Event that is called
	public const string GRANT_GOODS_WINDOWS = "grant_goods_windows";

	//property names
	private const string RECEIPT_XML = "receipt_xml";
	private const string TRANSACTION_ID = "transaction_id";
	private const string RECEIPT_ID = "receipt_id";
	private const string ITEM_CODE = "item_code";
	private const string USD_AMOUNT = "usd_amount";
	private const string CLIENT_ID = "client_id";
	private const string SN_ID = "sn_id";
	private const string TRANSACTION_TIME = "transaction_time";
	private const string REF = "ref";

	//property strings
	private string receiptXml = "";
	private string transactionId = "";
	private string receiptId = "";
	private string itemCode = "";
	private string usdAmount = "";
	private string clientId = "";
	private string snId = "";
	private string transactionTime = "";
	private Dictionary<string, string> reference = new Dictionary<string, string> ();

	/** Constructor */
	private WindowsPaymentAction(ActionPriority priority, string type) : base(priority, type) {}

	
	public static void testWindowPaymentAction ()
	{
#if UNITY_WSA_10_0 && NETFX_CORE
        WindowsPaymentAction windowsPaymentAction = new WindowsPaymentAction(ActionPriority.IMMEDIATE, GRANT_GOODS_WINDOWS);
		windowsPaymentAction.receiptXml = "<?xml version=\"1.0\"?><Receipt xmlns=\"http://schemas.microsoft.com/windows/2012/store/receipt\" Version=\"1.0\" ReceiptDate=\"2012-08-30T23:10:05Z\" CertificateId=\"b809e47cd0110a4db043b3f73e83acd917fe1336\" ReceiptDeviceId=\"4e362949-acc3-fe3a-e71b-89893eb4f528\"><AppReceipt Id=\"8ffa256d-eca8-712a-7cf8-cbf5522df24b\" AppId=\"55428GreenlakeApps.CurrentAppSimulatorEventTest_z7q3q7z11crfr\" PurchaseDate=\"2012-06-04T23:07:24Z\" LicenseType=\"Full\" /><ProductReceipt Id=\"6bbf4366-6fb2-8be8-7947-92fd5f683530\" ProductId=\"Product1\" PurchaseDate=\"2012-08-30T23:08:52Z\" ExpirationDate=\"2012-09-02T23:08:49Z\" ProductType=\"Durable\" AppId=\"55428GreenlakeApps.CurrentAppSimulatorEventTest_z7q3q7z11crfr\" /><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/2001/10/xml-exc-c14n#\" /><SignatureMethod Algorithm=\"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#sha256\" /><DigestValue>cdiU06eD8X/w1aGCHeaGCG9w/kWZ8I099rw4mmPpvdU=</DigestValue></Reference></SignedInfo><SignatureValue>SjRIxS/2r2P6ZdgaR9bwUSa6ZItYYFpKLJZrnAa3zkMylbiWjh9oZGGng2p6/gtBHC2dSTZlLbqnysJjl7mQp/A3wKaIkzjyRXv3kxoVaSV0pkqiPt04cIfFTP0JZkE5QD/vYxiWjeyGp1dThEM2RV811sRWvmEs/hHhVxb32e8xCLtpALYx3a9lW51zRJJN0eNdPAvNoiCJlnogAoTToUQLHs72I1dECnSbeNPXiG7klpy5boKKMCZfnVXXkneWvVFtAA1h2sB7ll40LEHO4oYN6VzD+uKd76QOgGmsu9iGVyRvvmMtahvtL1/pxoxsTRedhKq6zrzCfT8qfh3C1w==</SignatureValue></Signature></Receipt>";
		windowsPaymentAction.receiptXml = windowsPaymentAction.receiptXml.Trim();
		windowsPaymentAction.transactionId = "test" + DateTimeOffset.FromUnixTimeSeconds(1000).ToUnixTimeSeconds().ToString();
		windowsPaymentAction.receiptId = "test" + (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString();
		windowsPaymentAction.itemCode = "test:coin_package_1";
        windowsPaymentAction.usdAmount = "1.99";
        windowsPaymentAction.clientId = "266";
        windowsPaymentAction.snId = "1";
		windowsPaymentAction.transactionTime = (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds.ToString();
		windowsPaymentAction.reference.Add("g", "test");
        windowsPaymentAction.reference.Add("p", "test");
        ServerAction.processPendingActions(true);
#endif
    }

	public static void doWindowPaymentAction (Dictionary<string,string> paymentParams)
	{
		Debug.Log("In do window payment action");
		WindowsPaymentAction windowsPaymentAction = new WindowsPaymentAction(ActionPriority.IMMEDIATE, GRANT_GOODS_WINDOWS);
		windowsPaymentAction.receiptXml = paymentParams["receiptXml"];
		windowsPaymentAction.transactionId = paymentParams["transactionId"];
		windowsPaymentAction.receiptId = paymentParams["receiptId"];
		windowsPaymentAction.itemCode = "popcorn:"+paymentParams["itemCode"];
		windowsPaymentAction.usdAmount = paymentParams["usdAmount"];
		windowsPaymentAction.clientId = paymentParams["clientId"];
		windowsPaymentAction.snId = paymentParams["snId"];
		windowsPaymentAction.transactionTime = paymentParams["transactionTime"];
		windowsPaymentAction.reference.Add("g", paymentParams["g"]);
		windowsPaymentAction.reference.Add("p", paymentParams["p"]);
		Debug.LogFormat("Do window payment action {0} {1} {2} {3} {4} {5} {6} {7}", windowsPaymentAction.receiptXml, windowsPaymentAction.transactionId, windowsPaymentAction.receiptId, windowsPaymentAction.itemCode, windowsPaymentAction.usdAmount, windowsPaymentAction.clientId, windowsPaymentAction.snId, windowsPaymentAction.transactionTime);

		ServerAction.processPendingActions(true);
		Debug.Log("Do window payment action completed");
	}

	/// A dictionary of string properties associated with this
	public static Dictionary<string, string[]> propertiesLookup
	{
		get
		{
			if (_propertiesLookup == null)
			{
				_propertiesLookup = new Dictionary<string, string[]>();
				_propertiesLookup.Add(GRANT_GOODS_WINDOWS, new string[] {RECEIPT_XML, TRANSACTION_ID, RECEIPT_ID, ITEM_CODE, USD_AMOUNT, CLIENT_ID, SN_ID, TRANSACTION_TIME, REF});
				
			}
			return _propertiesLookup;
		}
	}

	/// Implements IResetGame hook, clears out properties as well as clearing based on superclass's view
	new public static void resetStaticClassData()
	{
		_propertiesLookup = null;
	}

	private static Dictionary<string, string[]> _propertiesLookup = null;

	/// Appends all the specific action properties to json
	public override void appendSpecificJSON(System.Text.StringBuilder builder)
	{
		if (!propertiesLookup.ContainsKey(type))
		{
			Debug.LogError("No properties defined for action: " + type);
			return;
		}
		
		foreach (string property in propertiesLookup[type])
		{
			switch (property)
			{
				case RECEIPT_XML:
					appendPropertyJSON(builder, property, receiptXml);
					break;
				case TRANSACTION_ID:
					appendPropertyJSON (builder, property, transactionId);
					break;
				case RECEIPT_ID:
					appendPropertyJSON (builder, property, receiptId);
					break;
				case ITEM_CODE:
					appendPropertyJSON (builder, property, itemCode);
					break;
				case USD_AMOUNT:
					appendPropertyJSON (builder, property, usdAmount);
					break;
				case CLIENT_ID:
					appendPropertyJSON (builder, property, clientId);
					break;
				case SN_ID:
					appendPropertyJSON (builder, property, snId);
					break;
				case TRANSACTION_TIME:
					appendPropertyJSON (builder, property, transactionTime);
					break;
				case REF:
					appendPropertyJSON (builder, property, reference);
					break;
			default:
				Debug.LogWarning("Unknown property for action: " + type + ", " + property);
				break;
			}
		}
	}
}