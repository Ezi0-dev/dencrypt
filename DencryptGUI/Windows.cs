using System;
using System.Windows.Forms;
using System.Drawing;
using DencryptCore;

namespace DencryptGUI
{
    public static class Windows
    {
        public static void secretPopup(object sender, EventArgs e)
        {
            Form customPopup = new Form()
            {
                Text = "pls star on github :)))",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(300, 300),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Consolas", 16),
                Padding = new Padding(10),
                ShowInTaskbar = false,
                MaximizeBox = false
            };


            PictureBox pfp = new PictureBox()
            {
                Dock = DockStyle.Top,
                Image = Image.FromFile("Assets/ezio.png"),
                Size = new Size(200, 200),
                SizeMode = PictureBoxSizeMode.Zoom,
                Padding = new Padding(5)
            };


            LinkLabel message = new LinkLabel()
            {
                Text = "Made by Ezi0",
                Dock = DockStyle.Top,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                LinkColor = Color.White,
            };
            message.Links.Add(8, 4, "https://github.com/Ezi0-dev");

            message.LinkClicked += (sender, e) =>
            {
                string url = e.Link.LinkData as string;
                if (!string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true // required for .NET Core and modern Windows
                    });
                }
            };

            Label thanks = new Label()
            {
                Text = "Thanks for downloading!",
                Font = new Font("Consolas", 10),
                Dock = DockStyle.Top,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            customPopup.Controls.Add(thanks);
            customPopup.Controls.Add(message);
            customPopup.Controls.Add(pfp);

            customPopup.ShowDialog();
        }
        public static void settingsPopup(object sender, EventArgs e)
        {
            Form settingsPopup = new Form()
            {
                Text = "Settings",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(550, 400),
                Padding = new Padding(10),
                ShowInTaskbar = false,
                MaximizeBox = false
            };

            Button btnSave = new Button()
            {
                Text = "Save",
                Anchor = AnchorStyles.Bottom,
                Dock = DockStyle.Bottom,
            };

            CheckBox chkRemoveOriginal = new CheckBox()
            {
                Text = "Delete original file(s) and folder(s) when creating vault",
                Anchor = AnchorStyles.Bottom,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 13),
                AutoSize = true,
            };

            settingsPopup.Controls.Add(chkRemoveOriginal);
            settingsPopup.Controls.Add(btnSave);

            bool originalValue = SettingsManager.Current.RemoveOriginalFiles;

            chkRemoveOriginal.Checked = originalValue;

            btnSave.Click += (s, args) =>
            {
                SettingsManager.Current.RemoveOriginalFiles = chkRemoveOriginal.Checked;
                SettingsManager.Save();
                MessageBox.Show("Settings saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                originalValue = chkRemoveOriginal.Checked; 
                settingsPopup.Tag = "Saved";
                settingsPopup.Close();
            };

            settingsPopup.FormClosing += (s, e) =>
            {
                if (chkRemoveOriginal.Checked != originalValue && settingsPopup.Tag?.ToString() != "saved")
                {
                    DialogResult result = MessageBox.Show(
                        "You have unsaved settings. Do you want to save them before exiting?",
                        "Unsaved Settings",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true;
                    }
                    else if (result == DialogResult.Yes)
                    {
                        SettingsManager.Current.RemoveOriginalFiles = chkRemoveOriginal.Checked;
                        SettingsManager.Save();
                        MessageBox.Show("Settings saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            Style.ApplyDarkTheme(settingsPopup);

            settingsPopup.ShowDialog();
        }
    }
}