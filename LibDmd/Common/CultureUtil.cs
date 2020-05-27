using System.Globalization;
using System.Threading;

namespace LibDmd.Common
{
	public static class CultureUtil
	{
		/// <summary>
		/// In order to not have to deal with the inconsistencies in Windows regional/culture settings whereby decimals use either periods or commas we default to the 
		/// InvariantCulture, which means that all formatting and parsing of strings (to/from datetime, float, double, decimal) should be parseable by a peice of software independent 
		/// of the user's local windows settings.
		/// This is especially important with features such as LibDmd's settings windows which read/write to configuration files.
		/// Reference(s): https://stackoverflow.com/questions/9760237/what-does-cultureinfo-invariantculture-mean
		/// https://stackoverflow.com/questions/13354211/how-to-set-default-culture-info-for-entire-c-sharp-application
		/// </summary>
		public static void NormalizeUICulture()
		{
			//Fix for persisting changes to config files so that decimals in non-US cultures do not get the decimal values replaced with comma's,
			//and always remain periods.
			CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
			CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
			//If upgrading DMDExt to .NET 4.6 or beyond, this might be required according to this: https://stackoverflow.com/questions/36312697/setting-default-currentculture-and-currentuiculture-differences-between-net-4
			//AppContext.SetSwitch("Switch.System.Globalization.NoAsyncCurrentCulture", true);
		}
	}
}
