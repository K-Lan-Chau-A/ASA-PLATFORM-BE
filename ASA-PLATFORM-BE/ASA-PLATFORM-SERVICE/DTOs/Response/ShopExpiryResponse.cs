using System;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
	public class ShopExpiryResponse
	{
		public string ShopName { get; set; }
		public string OwnerName { get; set; }
		public string CurrentPackageName { get; set; }
		public decimal? CurrentPackagePrice { get; set; }
		public string ExpiredAt { get; set; }
	}
}


