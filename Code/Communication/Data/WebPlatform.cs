// Defines WebGL WebPlatform type (dotcom, facebook).

public struct WebPlatform
{
	public const string PLATFORM_NAME_FACEBOOK = "Facebook";
	public const string PLATFORM_NAME_DOTCOM = "DotCom";

	private readonly string platformName;

	public WebPlatform(string platformName)
	{
		this.platformName = platformName;
	}

	// The dotcom platform is a zynga owned webpage hosting the webgl build- hititrich.com 
	public bool IsDotCom =>  PLATFORM_NAME_DOTCOM.Equals(platformName, System.StringComparison.OrdinalIgnoreCase);

	public bool IsFacebook => PLATFORM_NAME_FACEBOOK.Equals(platformName, System.StringComparison.OrdinalIgnoreCase);
}
