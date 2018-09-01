using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaint
{
	public class RequestRegionResult
	{
		public List<Region> Regions;

		public RequestRegionResult(List<Region> regions)
		{
			Regions = regions;
		}
	}
}
