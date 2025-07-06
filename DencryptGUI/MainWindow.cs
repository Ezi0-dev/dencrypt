namespace DencryptGUI;
using DencryptCore;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using System.Drawing.Text;

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

        TableLayoutPanel topPanel = new TableLayoutPanel()
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Dock = DockStyle.Top,
        };

        TableLayoutPanel passwordRow = new TableLayoutPanel()
        {
            ColumnCount = 4,
            RowCount = 2,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
        };

        TableLayoutPanel buttonRow = new TableLayoutPanel()
        {
            ColumnCount = 4,
            RowCount = 2,
            Dock = DockStyle.Fill,
            AutoSize = true,
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        TableLayoutPanel vaultRow = new TableLayoutPanel()
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            AutoSize = true,
        };
        vaultRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        vaultRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        TableLayoutPanel listRow = new TableLayoutPanel()
        {
            ColumnCount = 2,
            RowCount = 3,
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

        Panel strengthBar = new Panel()
        {
            Name = "strengthBar",
            Height = 5,
            Width = txtPassword.Width,
            BackColor = Color.FromArgb(40, 40, 40),
            Margin = new Padding(5),
        };

        PictureBox gifBox = new PictureBox()
        {
            Image = Image.FromFile("Assets/bee.gif"),
            Size = new Size(80, 80),
            SizeMode = PictureBoxSizeMode.Zoom,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(5),
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

        Button btnCreateVault = new Button()
        {
            Text = "Create Vault",
            AutoSize = true,
        };

        Button btnExtractVault = new Button()
        {
            Text = "Extract Vault",
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

        Button btnShowLogs = new Button()
        {
            Text = "📃 Show logs",
            AutoSize = true,
            Width = 500,
            Anchor = AnchorStyles.Bottom
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
            Margin = new Padding(5),
        };

        Style.ApplyListViewStyle(statusFiles);
        statusFiles.Columns.Add("Time", 100);
        statusFiles.Columns.Add("Status", 800);

        Label lblStatus = new Label()
        {
            Text = "Ready.",
            AutoSize = true,
        };

        Label fileCounter = new Label
        {
            Text = "",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14),
            Anchor = AnchorStyles.Left,
        };

        mainPanel.Controls.Add(topPanel);
        topPanel.Controls.Add(passwordRow);
        topPanel.Controls.Add(gifBox);
        passwordRow.Controls.Add(lblPassword);
        passwordRow.Controls.Add(txtPassword);
        passwordRow.SetColumnSpan(txtPassword, 2);
        passwordRow.Controls.Add(btnShowPassword);
        passwordRow.Controls.Add(strengthBar, 1, 1);
        passwordRow.SetColumnSpan(strengthBar, 2);

        txtPassword.TextChanged += passwordCheck;
        gifBox.Click += Windows.secretPopup;
        btnShowLogs.Click += showLogs;

        mainPanel.Controls.Add(buttonRow);
        buttonRow.Controls.Add(btnSelectFiles);
        buttonRow.Controls.Add(btnSelectFolder);
        buttonRow.Controls.Add(btnEncrypt);
        buttonRow.Controls.Add(btnDecrypt);
        mainPanel.Controls.Add(vaultRow);
        vaultRow.Controls.Add(btnCreateVault);
        vaultRow.Controls.Add(btnExtractVault);
        
        mainPanel.Controls.Add(listRow);
        listRow.Controls.Add(listFiles);
        listRow.SetRowSpan(listFiles, 3);
        listRow.Controls.Add(btnDelete);
        listRow.Controls.Add(btnClear);
        listRow.Controls.Add(btnClear, 1, 1);
        listRow.Controls.Add(btnShowLogs, 2, 1);

        void UpdateStatusColumnWidth() // It must look nice idc
        {
            int widest = 800;

            foreach (ListViewItem item in statusFiles.Items)
            {
                int textWidth = TextRenderer.MeasureText(item.SubItems[1].Text, statusFiles.Font).Width;
                if (textWidth > widest)
                    widest = textWidth;
            }

            // Make sure it’s wider than the visible area to trigger horizontal scroll
            statusFiles.Columns[1].Width = Math.Max(widest + 20, statusFiles.ClientSize.Width - 200);
        }

        mainPanel.Controls.Add(statusFiles);
        mainPanel.Controls.Add(progressBar);
        mainPanel.Controls.Add(lblStatus);
        mainPanel.Controls.Add(fileCounter);

        buttonRow.Width = listFiles.Width;
        
        btnSelectFolder.Dock = DockStyle.Fill;
        btnEncrypt.Dock = DockStyle.Fill;
        btnDecrypt.Dock = DockStyle.Fill;
        btnSelectFiles.Dock = DockStyle.Fill;
        btnCreateVault.Dock = DockStyle.Fill;
        btnExtractVault.Dock = DockStyle.Fill;

        List<string> selectedFiles = new();
        List<string> selectedFolders = new List<string>();

        string selectedFolderPath = "";
        bool isFolder = false;
        bool isEncrypting = true;

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

                selectedFiles.Add(fbd.SelectedPath);
                listFiles.Items.Add(fbd.SelectedPath);
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

            progressBar.Value = 0;
            progressBar.Maximum = isFolder ?
            
            Directory.GetFiles(selectedFolderPath, "*", SearchOption.AllDirectories).Length :
            selectedFiles.Count;

            btnEncrypt.Enabled = false;
            isEncrypting = true;
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
                    addStatus("🔐 Encrypting files...", Color.White);
                });
                
                // Logger 
                var logger = Encryption.CreateFileLogger();

                for (int i = 0; i < selectedFiles.Count; i++)
                {

                    string file = selectedFiles[i];

                    try
                    {
                        Encryption.EncryptFileOverwrite(file, txtPassword.Text, logger);
                        Invoke(() => addStatus($"Encrypted: {file}", Color.Green));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => addStatus($"❌ Error encrypting {file}: {ex.Message}", Color.Red));
                    }

                    Invoke(() => progressBar.Value = i + 1);

                    string verb = isEncrypting ? "encrypted" : "decrypted";
                    fileCounter.Text = $"Files {verb}: {i + 1}/{selectedFiles.Count}";
                }

                Invoke(() =>
                {
                    UpdateStatusColumnWidth();
                    btnEncrypt.Enabled = true;
                    addStatus("✅ Encryption completed (っ◔◡◔)っ", Color.White);
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
            isEncrypting = false;
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
                    addStatus("🔐 Decrypting files...", Color.White);
                });

                // Logger 
                var logger = Encryption.CreateFileLogger();

                for (int i = 0; i < selectedFiles.Count; i++)
                {

                    string file = selectedFiles[i];

                    try
                    {
                        Encryption.DecryptFileOverwrite(file, txtPassword.Text, logger);
                        Invoke(() => addStatus($"Decrypted: {file}", Color.Green));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => addStatus($"❌ Error decrypting {file}: {ex.Message}", Color.Red));
                    }

                    Invoke(() => progressBar.Value = i + 1);

                    string verb = isEncrypting ? "encrypted" : "decrypted";
                    fileCounter.Text = $"Files {verb}: {i + 1}/{selectedFiles.Count}";
                }

                Invoke(() =>
                {
                    UpdateStatusColumnWidth();
                    btnDecrypt.Enabled = true;
                    addStatus("✅ Decryption completed (っ◔◡◔)っ", Color.White);
                    lblStatus.Text = "✅ Files decrypted.";
                });

            }
            );
        };

        btnCreateVault.Click += async (s, e) =>
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

            using SaveFileDialog outputVaultPath = new SaveFileDialog();
            outputVaultPath.Filter = "Vault Files (*.vault)|*.vault";
            outputVaultPath.Title = "Save your Vault as...";
            outputVaultPath.FileName = "MyVault.vault";

            string savePath = outputVaultPath.FileName;
            string password = txtPassword.Text;

            btnCreateVault.Enabled = false;
            isEncrypting = true;
            lblStatus.Text = "🔄 Creating Vault...";

            if (outputVaultPath.ShowDialog() == DialogResult.OK)
            {
                await Task.Run(() =>
                {
                    if (selectedFiles.Count == 0)
                    {
                        Invoke(() => lblStatus.Text = "❌ No files or folder selected.");
                        btnCreateVault.Enabled = true;
                        return;
                    }

                    Invoke(() =>
                    {
                        progressBar.Maximum = selectedFiles.Count;
                        addStatus("🔐 Creating Vault...", Color.White);
                    });

                    // Logger 
                    var logger = Encryption.CreateFileLogger();

                    for (int i = 0; i < selectedFiles.Count; i++)
                    {

                        string file = selectedFiles[i];

                        if (!File.Exists(file))
                        {
                            Invoke(() => addStatus($"Encrypted: {file}", Color.Green));
                            continue;
                        }

                        Invoke(() => progressBar.Value = i + 1);

                        fileCounter.Text = $"Files added to vault: {i + 1}/{selectedFiles.Count}";
                    }

                    try
                    {
                        Vault.CreateVault(selectedFiles, outputVaultPath.FileName, txtPassword.Text, logger);
                        Invoke(() => addStatus($"Vault created: {outputVaultPath.FileName}", Color.Green));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => addStatus($"❌ Failed to create vault: {ex.Message}", Color.Red));
                    }
                });

                Invoke(() =>
                {
                    UpdateStatusColumnWidth();
                    btnCreateVault.Enabled = true;
                    addStatus("✅ Vault creation completed (っ◔◡◔)っ", Color.White);
                    lblStatus.Text = "✅ Done.";
                });
            }
        };

        btnExtractVault.Click += async (s, e) =>
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

            string password = txtPassword.Text;

            btnExtractVault.Enabled = false;
            isEncrypting = true;
            lblStatus.Text = "🔄 Extracting files from Vault...";

            using FolderBrowserDialog outputDir = new FolderBrowserDialog();
            outputDir.Description = "Extract vault to...";

            if (outputDir.ShowDialog() == DialogResult.OK)
            {
                await Task.Run(() =>
                {
                    if (selectedFiles.Count == 0)
                    {
                        Invoke(() => lblStatus.Text = "❌ No files or folder selected.");
                        btnExtractVault.Enabled = true;
                        return;
                    }

                    Invoke(() =>
                    {
                        progressBar.Maximum = selectedFiles.Count;
                        addStatus("🔐 Extracting Vault...", Color.White);
                    });

                    // Logger 
                    var logger = Encryption.CreateFileLogger();

                    for (int i = 0; i < selectedFiles.Count; i++)
                    {

                        string file = selectedFiles[i];

                        if (!File.Exists(file))
                        {
                            Invoke(() => addStatus($"Extracted: {file}", Color.Green));
                            continue;
                        }

                        Invoke(() => progressBar.Value = i + 1);

                        fileCounter.Text = $"Files extracted from vault: {i + 1}/{selectedFiles.Count}";
                    }

                    try
                    {
                        foreach (var vaultFile in selectedFiles)
                        {
                            Vault.ExtractVault(vaultFile, outputDir.SelectedPath, txtPassword.Text, logger);
                        }
                        Invoke(() => addStatus($"✅ Vault extracted: {outputDir.SelectedPath}", Color.Green));
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => addStatus($"❌ Failed to extract vault: {ex.Message}", Color.Red));
                    }
                });

                Invoke(() =>
                {
                    UpdateStatusColumnWidth();
                    btnExtractVault.Enabled = true;
                    addStatus("✅ Vault extraction completed (っ◔◡◔)っ", Color.White);
                    lblStatus.Text = "✅ Done.";
                });
            }
        };

        void addStatus(string message, Color color)
        {
            ListViewItem item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(message);
            item.ForeColor = color;
            statusFiles.Items.Add(item);
        };

        void passwordCheck(object sender, EventArgs e)
        {
            string password = ((TextBox)sender).Text;
            Encryption.PasswordStrength strength = Encryption.EvaluateStrength(password);

            Panel strengthBar = Controls.Find("strengthBar", true).FirstOrDefault() as Panel;
            if (strengthBar == null) return;

            strengthBar.BackColor = strength switch
            {
                Encryption.PasswordStrength.VeryWeak => Color.DarkRed,
                Encryption.PasswordStrength.Weak => Color.Red,
                Encryption.PasswordStrength.Medium => Color.Orange,
                Encryption.PasswordStrength.Strong => Color.YellowGreen,
                Encryption.PasswordStrength.VeryStrong => Color.Green,
                _ => Color.FromArgb(40, 40, 40)
            };
        }

        void showLogs(object sender, EventArgs e)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
            string logDirectory = Path.Combine(projectRoot, "logs");
            string logFilePath = Path.Combine(logDirectory, "dencrypt_log.txt");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = logFilePath,
                UseShellExecute = true // required for .NET Core and modern Windows
            });
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