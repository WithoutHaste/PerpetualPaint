using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	public class RequestRegionResult
	{
		public List<ImageRegion> Regions;

		public RequestRegionResult(List<ImageRegion> regions)
		{
			Regions = regions;
		}
	}
}
