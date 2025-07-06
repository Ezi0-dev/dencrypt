using System;
using System.Windows.Forms;
using System.Drawing;

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
    }
}