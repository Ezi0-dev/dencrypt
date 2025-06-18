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
        this.Text = "Dencrypt";
        this.Size = new Size(700, 650);
        this.AutoSize = true;
        this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        FlowLayoutPanel mainPanel = new FlowLayoutPanel()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll = true,
            Padding = new Padding(10),
            WrapContents = false
        };
        this.Controls.Add(mainPanel);

        FlowLayoutPanel passwordRow = new FlowLayoutPanel()
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false
        };

        FlowLayoutPanel buttonRow = new FlowLayoutPanel()
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false
        };

        Label lblPassword = new Label()
        {
            Text = "Password:",
            AutoSize = true,
            Margin = new Padding(5)
        };

        TextBox txtPassword = new TextBox()
        {
            Width = 400,
            UseSystemPasswordChar = true,
            Margin = new Padding(5)
        };

        Button btnSelectFiles = new Button()
        {
            Text = "📄 Select Files",
            AutoSize = true,
        };

        Button btnSelectFolder = new Button()
        {
            Text = "📁 Select Folder",
            AutoSize = true,
        };

        Button btnEncrypt = new Button()
        {
            Text = "🔐 Encrypt",
            AutoSize = true,
        };

        Button btnDecrypt = new Button()
        {
            Text = "🔓 Decrypt",
            AutoSize = true,
        };

        ListBox listFiles = new ListBox()
        {
            Width = 640,
            Height = 200,
            SelectionMode = SelectionMode.None,
            Margin = new Padding(5)
        };

        ListBox statusFiles = new ListBox()
        {
            Width = 640,
            Height = 200,
            SelectionMode = SelectionMode.None,
            Margin = new Padding(5)
        };

        Label lblStatus = new Label()
        {
            Text = "Ready.",
            AutoSize = true,
        };

        mainPanel.Controls.Add(passwordRow);
        passwordRow.Controls.Add(lblPassword);
        passwordRow.Controls.Add(txtPassword);
        mainPanel.Controls.Add(buttonRow);
        buttonRow.Controls.Add(btnSelectFiles);
        buttonRow.Controls.Add(btnSelectFolder);
        buttonRow.Controls.Add(btnEncrypt);
        buttonRow.Controls.Add(btnDecrypt);
        mainPanel.Controls.Add(listFiles);
        mainPanel.Controls.Add(statusFiles);
        mainPanel.Controls.Add(lblStatus);

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
                statusFiles.Items.Clear();
                foreach (var file in ofd.FileNames)
                {
                    selectedFiles.Add(file);
                    listFiles.Items.Add(file);
                }
            }
        };

        btnSelectFolder.Click += (s, e) =>
        {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                selectedFiles.Clear();
                listFiles.Items.Clear();
                statusFiles.Items.Clear();

                var files = Encryption.GetAllFilesInFolder(fbd.SelectedPath);
                foreach (var file in files)
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
                    statusFiles.Items.Add($"Encrypting: {file}");
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"❌ Error: {ex.Message}";
                    return;
                }
            }
            statusFiles.Items.Add("Encryption completed.");
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
                    statusFiles.Items.Add($"Decrypting: {file}");
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"❌ Error: {ex.Message}";
                    return;
                }
            }
            statusFiles.Items.Add("Decryption completed.");
            lblStatus.Text = "✅ Files decrypted.";
        };
        // Apply dark theme to the form and its controls
        Style.ApplyDarkTheme(this);
    }
}