using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualPaintLibrary
{
	public delegate void ProjectEventHandler(object sender, ProjectEventArgs e);

	public class ProjectEventArgs : EventArgs
	{
		public PPProject Project { get; protected set; }

		public ProjectEventArgs(PPProject project)
		{
			Project = project;
		}
	}
}
