using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WithoutHaste.Windows.GUI;

namespace PerpetualPaint
{
	public class StatusPanel : Panel
	{
		private const int margin = 5;
		private Label label;
		private ProgressBar progressBar;
		private Button cancelButton;
		
		public string StatusText {
			get {
				return label.Text;
			}
			set {
				label.Text = value;
				UpdateLayout();
			}
		}

		public int StatusProgress {
			set {
				progressBar.Value = value;
				if(progressBar.Value == 100)
				{
					HideProgressBar();
				}
				else
				{
					ShowProgressBar();
				}
			}
		}

		public event EventHandler Cancel;
		
		public StatusPanel()
		{
			label = new Label();
			label.AutoSize = true;
			label.Location = new Point(margin, margin);
			label.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
			label.Text = "";
			this.Controls.Add(label);

			progressBar = new ProgressBar();
			progressBar.Size = new Size(100, label.Height);
			LayoutHelper.PlaceRightOf(progressBar, label, margin);
			this.Controls.Add(progressBar);

			cancelButton = new Button();
			cancelButton.Text = "Cancel";
			cancelButton.AutoSize = true;
			LayoutHelper.PlaceRightOf(cancelButton, progressBar, margin);
			cancelButton.Click += new EventHandler(OnCancel);
			this.Controls.Add(cancelButton);

			this.Height = cancelButton.Height + (2 * margin);
			LayoutHelper.CenterVertically(label, this);
			LayoutHelper.CenterVertically(progressBar, this);
			LayoutHelper.CenterVertically(cancelButton, this);

			HideProgressBar();
		}

		public void ShowProgressBar()
		{
			progressBar.Visible = true;
			cancelButton.Visible = true;
		}

		public void HideProgressBar()
		{
			progressBar.Visible = false;
			cancelButton.Visible = false;
		}

		private void UpdateLayout()
		{
			LayoutHelper.PlaceRightOf(progressBar, label, margin);
			LayoutHelper.PlaceRightOf(cancelButton, progressBar, margin);
			if(String.IsNullOrEmpty(label.Text))
			{
				HideProgressBar();
			}
		}

		public void ClearCancels()
		{
			Cancel = null;
		}

		private void OnCancel(object sender, EventArgs e)
		{
			Cancel?.Invoke(this, e);
		}
	}
}
