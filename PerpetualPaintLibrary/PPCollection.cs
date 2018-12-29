using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WithoutHaste.Drawing.Colors;

namespace PerpetualPaintLibrary
{
	/// <summary>
	/// Perpetual Paint Collection: a collection of Perpetual Paint Projects.
	/// Ordered.
	/// Possibly with global palette.
	/// </summary>
	public class PPCollection
	{
		/// <summary>
		/// Triggered when collection is saved or is edited.
		/// </summary>
		public event EventHandler StatusChanged;

		public static readonly string COLLECTION_EXTENSION = ".ppc";
		//public static readonly string COLLECTION_EXTENSION_UPPERCASE = COLLECTION_EXTENSION.ToUpper();

		/// <summary>
		/// Ordered list of projects in the collection.
		/// </summary>
		public PPProject[] Projects { get { return projects.ToArray(); } }
		private List<PPProject> projects = new List<PPProject>();

		/// <summary>
		/// Optional global palette for all projects in collection.
		/// </summary>
		public ColorPalette ColorPalette = null;

		public PPConfig Config = new PPConfig();

		public string SaveToFileName { get; protected set; }

		public bool EditedSinceLastSave {
			get {
				return editedSinceLastSave;
			}
			protected set {
				bool previousValue = editedSinceLastSave;
				editedSinceLastSave = value;
				if(previousValue != value) TriggerStatusChanged();
			}
		}
		private bool editedSinceLastSave = false;

		/// <summary>
		/// Create empty collection.
		/// </summary>
		public PPCollection()
		{
		}

		/// <summary>
		/// Create new collection from existing data.
		/// </summary>
		public PPCollection(string[] projectFileNames, PPConfig config, ColorPalette colorPalette)
		{
			Config = config;
			foreach(string projectFileName in projectFileNames)
			{
				LoadProject_Internal(projectFileName);
			}
			ColorPalette = colorPalette;
		}

		private void TriggerStatusChanged()
		{
			if(StatusChanged == null) return;
			StatusChanged(this, new EventArgs());
		}

		/// <summary>
		/// Loads collection from file.
		/// </summary>
		public static PPCollection Load(string fullFileName)
		{
			PPCollection collection = IO.LoadCollection(fullFileName);
			collection.EditedSinceLastSave = false;
			return collection;
		}

		private PPProject LoadProject_Internal(string fullFileName)
		{
			if(String.IsNullOrEmpty(fullFileName))
				throw new FileNotFoundException("Cannot load from empty file name.");
			PPProject project = null;
			if(Path.GetExtension(fullFileName) == PPProject.PROJECT_EXTENSION)
			{
				project = PPProject.FromProject(fullFileName);
			}
			else
			{
				project = PPProject.FromImage(fullFileName);
			}
			AddProject(project);
			return project;
		}

		/// <summary>
		/// Load and add project from file.
		/// </summary>
		/// <returns>Returns the new project.</returns>
		/// <exception cref='DuplicateException'>Tried to add duplicate file to collection.</exception>
		public PPProject LoadProject(string fullFileName)
		{
			PPProject project = LoadProject_Internal(fullFileName);
			EditedSinceLastSave = true;
			return project;
		}

		/// <summary>
		/// Add project to collection.
		/// </summary>
		/// <exception cref='DuplicateException'>Tried to add duplicate file to collection.</exception>
		private void AddProject(PPProject project)
		{
			if(projects.Any(p => p.FromSameFile(project)))
				throw new DuplicateException("File is already in the collection.");

			projects.Add(project);
		}

		/// <summary>
		/// Remove project from collection by 0-based index.
		/// </summary>
		public void RemoveProjectAt(int index)
		{
			projects.RemoveAt(index);
			EditedSinceLastSave = true;
		}

		/// <summary>
		/// Save project file as <paramref name='fullFileName'/>.
		/// Saves all project files first.
		/// </summary>
		public void SaveAs(string fullFileName)
		{
			SaveToFileName = fullFileName;
			Save();
		}

		/// <summary>
		/// Save collection file.
		/// Saves all project files first.
		/// </summary>
		public void Save()
		{
			if(String.IsNullOrEmpty(SaveToFileName))
				throw new FileNotFoundException("Cannot save to an empty file name.");

			foreach(PPProject project in projects)
			{
				if(project.EditedSinceLastSave)
					project.Save();
			}

			if(Path.GetExtension(SaveToFileName) != COLLECTION_EXTENSION)
				SaveToFileName = Path.ChangeExtension(SaveToFileName, COLLECTION_EXTENSION);
			IO.ZipCollection(SaveToFileName, this);
			EditedSinceLastSave = false;
		}

		/// <summary>
		/// Returns list of project file names included in the collection.
		/// </summary>
		public string[] ToTextFormat()
		{
			return projects.Select(p => p.SaveToFileName).ToArray();
		}

		/// <summary>
		/// Returns true when options or values have changed.
		/// </summary>
		private bool OptionsHaveChanged(PPConfig.PaletteOptions paletteOption, WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			if(Config.PaletteOption == paletteOption)
			{
				switch(Config.PaletteOption)
				{
					case PPConfig.PaletteOptions.SaveNothing:
						return false;
					case PPConfig.PaletteOptions.SaveFile:
						if(ColorPalette == colorPalette)
							return false;
						break;
					case PPConfig.PaletteOptions.SaveFileName:
						if(Config.PaletteFileName == paletteFileName)
							return false;
						break;
				}
			}
			return true;
		}

		public void SetPaletteOption(PPConfig.PaletteOptions paletteOption, WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			if(!OptionsHaveChanged(paletteOption, colorPalette, paletteFileName))
				return;

			Config.PaletteOption = paletteOption;
			switch(Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveNothing:
					Config.PaletteFileName = null;
					ColorPalette = null;
					break;
				case PPConfig.PaletteOptions.SaveFile:
					Config.PaletteFileName = null;
					ColorPalette = colorPalette;
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					Config.PaletteFileName = paletteFileName;
					ColorPalette = null;
					break;
			}
			EditedSinceLastSave = true;
		}
		
		public void UpdatePaletteOption(WithoutHaste.Drawing.Colors.ColorPalette colorPalette = null, string paletteFileName = null)
		{
			if(!OptionsHaveChanged(Config.PaletteOption, colorPalette, paletteFileName))
				return;

			switch(Config.PaletteOption)
			{
				case PPConfig.PaletteOptions.SaveNothing:
					return;
				case PPConfig.PaletteOptions.SaveFile:
					ColorPalette = colorPalette;
					break;
				case PPConfig.PaletteOptions.SaveFileName:
					Config.PaletteFileName = paletteFileName;
					break;
			}
			EditedSinceLastSave = true;
		}
	}
}
