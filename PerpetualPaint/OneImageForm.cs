using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerpetualPaint
{
	public class OneImageForm : Form
	{
		private string saveFullFilename;
		private Bitmap masterImage;
		private PictureBox pictureBox;

		public OneImageForm()
		{
			this.Text = "Perpetual Paint";
			this.Width = 800;
			this.Height = 600;

			InitMenus();
			InitImage();
		}

		#region Init

		private void InitMenus()
		{
			MenuItem fileMenu = new MenuItem("File");
			fileMenu.MenuItems.Add("Open", new EventHandler(OpenFile));

			this.Menu = new MainMenu();
			this.Menu.MenuItems.Add(fileMenu);
		}

		private void InitImage()
		{
			pictureBox = new PictureBox();
			pictureBox.Dock = DockStyle.Fill;

			this.Controls.Add(pictureBox);
		}

		#endregion

		#region Event Handlers

		private void OpenFile(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files|*.BMP;*.PNG;*.JPG;*.JPEG;*.GIF;*.TIFF";
			openFileDialog.Title = "Select an Image File";

			if(openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}
			try
			{
				UpdateImage(openFileDialog.FileName);
			}
			catch(FileNotFoundException exception)
			{
				HandleError("Failed to open file.", exception);
			}
		}

		#endregion

		private void UpdateImage(string fullFilename)
		{
			saveFullFilename = fullFilename;
			masterImage = (Bitmap)Image.FromFile(fullFilename);
			pictureBox.Image = masterImage;
		}

		private void HandleError(string userMessage, Exception e)
		{
			string formattedMessage = String.Format("{0}\n\n{1}\n\n{2}", userMessage, e.Message, e.StackTrace);
			MessageBox.Show(formattedMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}
}
