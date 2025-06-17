namespace DencryptGUI;
using DencryptCore;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System;

public partial class MainWindow : Form
{
    public MainWindow()
    {
        InitializeComponent();

        // UI setup
        this.Text = "Dencrypt";
        this.Size = new Size(600, 400);

        Label lblPassword = new Label() { Text = "Password:", Location = new Point(10, 10), AutoSize = true };
        TextBox txtPassword = new TextBox() { Location = new Point(80, 10), Width = 400, PasswordChar = '*' };

        Button btnSelectFiles = new Button() { Text = "Select Files", Location = new Point(10, 50) };
        Button btnEncrypt = new Button() { Text = "Encrypt", Location = new Point(120, 50) };
        Button btnDecrypt = new Button() { Text = "Decrypt", Location = new Point(220, 50) };

        ListBox listFiles = new ListBox() { Location = new Point(10, 90), Size = new Size(560, 200) };
        Label lblStatus = new Label() { Text = "Ready.", Location = new Point(10, 300), AutoSize = true };

        this.Controls.Add(lblPassword);
        this.Controls.Add(txtPassword);
        this.Controls.Add(btnSelectFiles);
        this.Controls.Add(btnEncrypt);
        this.Controls.Add(btnDecrypt);
        this.Controls.Add(listFiles);
        this.Controls.Add(lblStatus);

        List<string> selectedFiles = new();

        btnSelectFiles.Click += (s, e) =>
        {
            using OpenFileDialog ofd = new OpenFileDialog()
            {
                Multiselect = true,
                Title = "Select files to encrypt or decrypt"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                selectedFiles.Clear();
                listFiles.Items.Clear();
                foreach (var file in ofd.FileNames)
                {
                    selectedFiles.Add(file);
                    listFiles.Items.Add(file);
                }
            }
        };

        btnEncrypt.Click += (s, e) =>
        {
            if (selectedFiles.Count == 0)
            {
                lblStatus.Text = "❌ No files selected.";
                return;
            }

            if (!Encryption.IsPasswordStrong(txtPassword.Text))
            {
                lblStatus.Text = "❌ Password too weak. Use at least 12 characters, with upper/lowercase, a digit, and a symbol.";
                return;
            }

            foreach (var file in selectedFiles)
            {
                try
                {
                    Encryption.EncryptFileOverwrite(file, txtPassword.Text);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"❌ Error: {ex.Message}";
                    return;
                }
            }

            lblStatus.Text = "✅ Files encrypted.";
        };


        btnDecrypt.Click += (s, e) =>
        {
            if (selectedFiles.Count == 0)
            {
                lblStatus.Text = "❌ No files selected.";
                return;
            }

            foreach (var file in selectedFiles)
            {
                try
                {
                    Encryption.DecryptFileOverwrite(file, txtPassword.Text);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"❌ Error: {ex.Message}";
                    return;
                }
            }

            lblStatus.Text = "✅ Files decrypted.";
        };
    }
}