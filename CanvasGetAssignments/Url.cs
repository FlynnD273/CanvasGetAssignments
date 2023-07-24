using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanvasGetAssignments
{
		public static class Url
		{
				public static string Combine (params string[] components)
				{
						return string.Join("/", components.Select(x => x.Trim('/')));
				}
		}
}
