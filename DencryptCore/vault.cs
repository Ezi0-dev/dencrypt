using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System;
using System.Text;

namespace DencryptCore
{
    public static class Vault
    {
        private const string VaultExtension = ".vault";

        public static void CreateVault(List<string> filePaths, string outputVaultPath, string password, Encryption.LogHandler log = null)
        {
            log ??= _ => { };
            log($"[VAULT] Creating vault: {outputVaultPath}");

            if (!outputVaultPath.EndsWith(VaultExtension))
                outputVaultPath += VaultExtension;

            // 1. ZIP everything into a tempfile
            string tempZip = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".vault");
            using (FileStream fs = new FileStream(tempZip, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (string path in filePaths)
                {
                    if (File.Exists(path))
                    {
                        // Files
                        string entryName = Path.GetFileName(path);
                        archive.CreateEntryFromFile(path, entryName);
                        log($"[VAULT] Added to vault: {entryName}");
                    }
                    else if (Directory.Exists(path))
                    {
                        // Folders - with subfolders hopefully O_O
                        string baseFolderName = Path.GetFileName(path);
                        var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        foreach (string file in allFiles)
                        {
                            // Adds selected folder to the vault, (before it deleted the selected folder but kept the subfolders.)
                            string relativePath = Path.Combine(baseFolderName, Path.GetRelativePath(path, file));
                            archive.CreateEntryFromFile(file, relativePath);
                            log($"[VAULT] Added from folder: {relativePath}");
                        }
                    }
                    
                }
            }

            // 2. Encrypt the ZIP and make it a .vault file
            string encryptedPath = Path.ChangeExtension(outputVaultPath, ".vault");
            Encryption.EncryptFileOverwrite(tempZip, password, log);

            // 3. Change name of the temp from .enc to .vault (can be done better, might change)
            string encPath = Path.ChangeExtension(tempZip, ".enc");
            File.Move(encPath, encryptedPath, overwrite: true);

            // 4. Cleanup
            File.Delete(tempZip);

            // 5. Delete original files and folders
            foreach (string path in filePaths)
            {
                try
                {
                    if (SettingsManager.Current.RemoveOriginalFiles)
                    {
                        File.Delete(path);
                        log($"[VAULT] Deleted file: {path}");
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                        log($"[VAULT] Deleted folder: {path}");
                    }
                }
                catch (Exception ex)
                {
                    log($"⚠️ Failed to delete: {path} -- {ex.Message}");
                }
            }

            log($"[VAULT] Done. Vault saved as: {encryptedPath}");
        }

        public static void ExtractVault(string vaultPath, string outputDir, string password, Encryption.LogHandler log = null)
        {
            log ??= _ => { };
            log($"[VAULT] Extracting vault: {vaultPath}");

            if (!File.Exists(vaultPath))
                throw new FileNotFoundException("Vault file not found", vaultPath);

            if (!IsValidVaultFile(vaultPath))
                throw new Exception("❌ Not a valid Dencrypt Vault File.");

            
            string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

            try
            {
                // 1. Decrypt vault → temp zip
                Encryption.DecryptFileOverwrite(vaultPath, password, log);

                string decryptedPath = Path.ChangeExtension(vaultPath, ".zip");
                File.Move(decryptedPath, tempZipPath);

                log("Vault decrypted to temp ZIP.");

                // 2. Extract zip into folder chosen through GUI
                ZipFile.ExtractToDirectory(tempZipPath, outputDir);
            }
            catch (Exception ex)
            {
                log($"❌ Vault extraction failed: {ex.Message}");

                // Clean up temporary files if they exist
                if (File.Exists(tempZipPath))
                    File.Delete(tempZipPath);

                throw; // Lets GUI display the error
            }
            finally
            {
                // More cleanup
                if (File.Exists(tempZipPath))
                    File.Delete(tempZipPath);
            }
            log($"[VAULT] Extracted to: {outputDir}");
        }

        public static bool IsValidVaultFile(string filePath)
        {
            const string expectedHeader = "DENCRYPT01";
            const string expectedExtension = "vault";

            if (!File.Exists(filePath)) return false;
            if (!filePath.EndsWith(".vault", StringComparison.OrdinalIgnoreCase)) return false;

            try
            {
                using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                // 1. Read header
                byte[] headerBytes = Encryption.ReadExact(fs, expectedHeader.Length, "Header");
                string header = Encoding.UTF8.GetString(headerBytes);
                if (header != expectedHeader)
                    return false;

                // 2. Read extension length (1 byte)
                int extLength = fs.ReadByte();
                if (extLength <= 0 || extLength > 20)
                    return false;

                // 3. Read extension 
                byte[] extBytes = Encryption.ReadExact(fs, extLength, "Extension");
                string extension = Encoding.UTF8.GetString(extBytes);
                return extension == expectedExtension;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}