namespace DencryptGUI;
using DencryptCore;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

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
        this.Icon = new Icon("Assets/icon.ico");

        FlowLayoutPanel mainPanel = new FlowLayoutPanel()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
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
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        TableLayoutPanel listRow = new TableLayoutPanel()
        {
            ColumnCount = 2,
            RowCount = 2,
            Dock = DockStyle.Fill,
            AutoSize = true,
        };
        listRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
        listRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        CustomProgressBar progressBar = new CustomProgressBar()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 20,
            Width = 900,
            Dock = DockStyle.Fill,
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
            Text = "📁 Select Folders",
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

        Button btnDelete = new Button()
        {
            Text = "🗑 Delete selected items",
            AutoSize = true,
            Width = 500,
        };

        Button btnClear = new Button()
        {
            Text = "💥 Clear items",
            AutoSize = true,
            Width = 500,
        };

        Button btnShowPassword = new Button()
        {
            Text = "👁",
            Margin = new Padding(5),
            Dock = DockStyle.Fill,
        };

        ListBox listFiles = new ListBox()
        {
            Width = 900,
            Height = 400,
            SelectionMode = SelectionMode.MultiExtended,
            BackColor = Color.FromArgb(35, 35, 35) // Drag and drop becomes white if this is not defined
        };

        ListView statusFiles = new ListView()
        {
            View = View.Details,
            Width = 900,
            Height = 300,
            GridLines = false,
            FullRowSelect = true,
            Margin = new Padding(5)
        };

        Style.ApplyListViewStyle(statusFiles);
        statusFiles.Columns.Add("Time", 200);
        statusFiles.Columns.Add("Status", 700);

        Label lblStatus = new Label()
        {
            Text = "Ready.",
            AutoSize = true,
        };

        mainPanel.Controls.Add(passwordRow);
        passwordRow.Controls.Add(lblPassword);
        passwordRow.Controls.Add(txtPassword);
        passwordRow.Controls.Add(btnShowPassword);

        mainPanel.Controls.Add(buttonRow);
        buttonRow.Controls.Add(btnSelectFiles);
        buttonRow.Controls.Add(btnSelectFolder);
        buttonRow.Controls.Add(btnEncrypt);
        buttonRow.Controls.Add(btnDecrypt);

        mainPanel.Controls.Add(listRow);
        listRow.Controls.Add(listFiles);
        listRow.SetRowSpan(listFiles, 2);
        listRow.Controls.Add(btnDelete);
        listRow.Controls.Add(btnClear);
        listRow.Controls.Add(btnClear, 1, 1);  

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

        listFiles.AllowDrop = true;
        Color originalColor = listFiles.BackColor;

        listFiles.DragEnter += (s, e) =>
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                listFiles.BackColor = Color.FromArgb(50, 70, 100);
            }
        };

        listFiles.DragLeave += (s, e) =>
        {
            listFiles.BackColor = originalColor;
        };

        listFiles.DragDrop += (s, e) =>
        {
            listFiles.BackColor = originalColor;

            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    selectedFiles.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                    isFolder = true;
                    selectedFolderPath = path;
                }
                else if (File.Exists(path))
                {
                    selectedFiles.Add(path);
                }

                listFiles.Items.Add(path);
            }
        };

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
                if (selectedFiles.Count == 0)
                {
                    Invoke(() => lblStatus.Text = "❌ No files or folder selected.");
                    btnEncrypt.Enabled = true;
                    return;
                }

                Invoke(() =>
                {
                    progressBar.Maximum = selectedFiles.Count;
                    statusFiles.Items.Add("🔐 Encrypting files...");
                });

                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    string file = selectedFiles[i];

                    try
                    {
                        Encryption.EncryptFileOverwrite(file, txtPassword.Text);
                        Invoke(() => addStatus($"✅ Encrypted: {file}", Color.Green));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => addStatus($"❌ Error encrypting {file}: {ex.Message}", Color.Red));
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
                if (selectedFiles.Count == 0)
                {
                    Invoke(() => lblStatus.Text = "❌ No files or folder selected.");
                    btnDecrypt.Enabled = true;
                    return;
                }

                Invoke(() =>
                {
                    progressBar.Maximum = selectedFiles.Count;
                    statusFiles.Items.Add("🔐 Decrypting files...");
                });

                for (int i = 0; i < selectedFiles.Count; i++)
                {
                    string file = selectedFiles[i];
                    try
                    {
                        Encryption.DecryptFileOverwrite(file, txtPassword.Text);
                        Invoke(() => addStatus($"✅ Decrypted: {file}", Color.Green));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => addStatus($"❌ Error decrypting {file}: {ex.Message}", Color.Red));
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

        void addStatus(string message, Color color)
        {
            ListViewItem item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(message);
            item.ForeColor = color;
            statusFiles.Items.Add(item);
        }
    
        btnDelete.Click += (s, e) =>
        {
            var selectedItems = listFiles.SelectedItems.Cast<string>().ToList();

            foreach (var item in selectedItems)
            {
                listFiles.Items.Remove(item);
                selectedFiles.Remove(item);
            }

            if (listFiles.Items.Count == 0)
            {
                isFolder = false;
                selectedFolderPath = "";
            }
        };

        listFiles.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Delete)
                btnDelete.PerformClick();
        };

        btnClear.Click += (s, e) =>
        {
            selectedFiles.Clear();
            listFiles.Items.Clear();
        };

        btnShowPassword.Click += (s, e) =>
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            btnShowPassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "❌";
        };



        // Apply dark theme to the form and its controls
        Style.ApplyDarkTheme(this);
    }
}

// (っ◔◡◔)っ ♥ by Ezi0 ♥