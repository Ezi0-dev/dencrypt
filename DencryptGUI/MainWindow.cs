namespace DencryptGUI;
using DencryptCore;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;

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

        TableLayoutPanel buttonRow = new TableLayoutPanel()
        {
            ColumnCount = 4,
            RowCount = 1,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Width = 640,
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        CustomProgressBar progressBar = new CustomProgressBar()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 20,
            Width = 640,
            Margin = new Padding(5),
            BackgroundColor = Color.FromArgb(40, 40, 40),
            BarColor = Color.Indigo
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
            AutoSize = false,
        };

        Button btnSelectFolder = new Button()
        {
            Text = "📁 Select Folder/s",
            AutoSize = false,
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
        mainPanel.Controls.Add(progressBar);
        mainPanel.Controls.Add(lblStatus);

        buttonRow.Width = listFiles.Width;

        btnSelectFolder.Dock = DockStyle.Fill;
        btnEncrypt.Dock = DockStyle.Fill;
        btnDecrypt.Dock = DockStyle.Fill;
        btnSelectFiles.Dock = DockStyle.Fill;

        List<string> selectedFiles = new();
        List<string> selectedFolders = new List<string>();

        string selectedFolderPath = "";
        bool isFolder = false;



        // btnEncrypt.Width = listFiles.Width;
        // btnDecrypt.Width = listFiles.Width;
        // btnSelectFolder.Width = listFiles.Width;
        // btnSelectFiles.Width = listFiles.Width;

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
                selectedFolderPath = fbd.SelectedPath;
                isFolder = true;

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

        btnEncrypt.Click += async (s, e) =>
        {
            statusFiles.Items.Clear();
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblStatus.Text = "❌ Enter a password.";
                return;
            }

            if (!Encryption.IsPasswordStrong(txtPassword.Text))
            {
                lblStatus.Font = new Font("Segoe UI", 10);
                lblStatus.Text = "❌ Password too weak. Use at least 12 characters, with upper/lowercase, a digit, and a symbol.";
                return;
            }

            progressBar.Value = 0;
            progressBar.Maximum = isFolder ?
                Directory.GetFiles(selectedFolderPath, "*", SearchOption.AllDirectories).Length :
                selectedFiles.Count;

            btnEncrypt.Enabled = false;
            lblStatus.Text = "🔄 Encrypting...";

            await Task.Run(() =>
            {
                string[] filesToEncrypt;

                if (isFolder && Directory.Exists(selectedFolderPath))
                {
                    filesToEncrypt = Directory.GetFiles(selectedFolderPath, "*", SearchOption.AllDirectories);
                }

                else if (selectedFiles.Count > 0)
                {
                    filesToEncrypt = selectedFiles.ToArray();
                }

                else
                {
                    Invoke(() => lblStatus.Text = "❌ No files or folder selected.");
                    return;
                }

                Invoke(() =>
                {
                    progressBar.Maximum = filesToEncrypt.Length;
                    statusFiles.Items.Add("🔐 Encrypting files...");
                });

                for (int i = 0; i < filesToEncrypt.Length; i++)
                {
                    string file = filesToEncrypt[i];
                    try
                    {
                        Encryption.EncryptFileOverwrite(file, txtPassword.Text);
                        Invoke(() => statusFiles.Items.Add($"✅ Encrypted: {file}"));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => statusFiles.Items.Add($"❌ Error encrypting {file}: {ex.Message}"));
                    }

                    Invoke(() => progressBar.Value = i + 1);
                }

                Invoke(() =>
                {
                    btnEncrypt.Enabled = true;
                    statusFiles.Items.Add("Encryption completed (っ◔◡◔)っ");
                    lblStatus.Font = new Font("Segoe UI", 15);
                    lblStatus.Text = "✅ Files encrypted.";
                });

            }
            );
        };


        btnDecrypt.Click += async (s, e) =>
        {
            statusFiles.Items.Clear();
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblStatus.Text = "❌ Enter a password.";
                return;
            }

            progressBar.Value = 0;
            progressBar.Maximum = isFolder ?
                Directory.GetFiles(selectedFolderPath, "*", SearchOption.AllDirectories).Length :
                selectedFiles.Count;

            btnDecrypt.Enabled = false;
            lblStatus.Text = "🔄 Decrypting...";

            await Task.Run(() =>
            {
                string[] filesToDecrypt;

                if (isFolder && Directory.Exists(selectedFolderPath))
                {
                    filesToDecrypt = Directory.GetFiles(selectedFolderPath, "*", SearchOption.AllDirectories);
                }

                else if (selectedFiles.Count > 0)
                {
                    filesToDecrypt = selectedFiles.ToArray();
                }

                else
                {
                    Invoke(() => lblStatus.Text = "❌ No files or folder selected.");
                    return;
                }

                Invoke(() =>
                {
                    progressBar.Maximum = filesToDecrypt.Length;
                    statusFiles.Items.Add("🔐 Decrypting files...");
                });

                for (int i = 0; i < filesToDecrypt.Length; i++)
                {
                    string file = filesToDecrypt[i];
                    try
                    {
                        Encryption.DecryptFileOverwrite(file, txtPassword.Text);
                        Invoke(() => statusFiles.Items.Add($"✅ Decrypted: {file}"));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => statusFiles.Items.Add($"❌ Error decrypting {file}: {ex.Message}"));
                    }

                    Invoke(() => progressBar.Value = i + 1);
                }

                Invoke(() =>
                {
                    btnDecrypt.Enabled = true;
                    statusFiles.Items.Add("Decryption completed (っ◔◡◔)っ");
                    lblStatus.Text = "✅ Files decrypted.";
                });

            }
            );
        };
        // Apply dark theme to the form and its controls
        Style.ApplyDarkTheme(this);
    }
}

// (っ◔◡◔)っ ♥ by Ezi0 ♥