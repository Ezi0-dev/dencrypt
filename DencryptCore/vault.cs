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
                foreach (string file in filePaths)
                {
                    if (!File.Exists(file)) continue;
                    string entryName = Path.GetFileName(file);
                    archive.CreateEntryFromFile(file, entryName);
                    log($"[VAULT] Added to vault: {entryName}");
                }
            }

            // 2. Encrypt the ZIP and make it a .vault file
            string encryptedPath = Path.ChangeExtension(outputVaultPath, ".vault");
            Encryption.EncryptFileOverwrite(tempZip, password, log);

            //3. Change name of the temp from .enc to .vault (can be done better, might change)
            string encPath = Path.ChangeExtension(tempZip, ".enc");
            File.Move(encPath, encryptedPath, overwrite: true);
            File.Delete(tempZip);

            log($"[VAULT] Done. Vault saved as: {encryptedPath}");
        }

        public static void ExtractVault(string vaultPath, string outputDir, string password, Encryption.LogHandler log = null)
        {
            log ??= _ => { };
            log($"[VAULT] Extracting vault: {vaultPath}");

            if (!File.Exists(vaultPath))
                throw new FileNotFoundException("Vault file not found", vaultPath);

            if (!IsValidVaultFile(vaultPath))
                throw new Exception("❌ Not a valid .vault file.");

            // 1. Decrypt .vault to tempZip
            string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

            Encryption.DecryptFileOverwrite(vaultPath, password, log);

            // 2. Rename decrypted output (which has original extension .zip)
            string zipPath = Path.ChangeExtension(vaultPath, ".zip");
            File.Move(zipPath, tempZipPath, overwrite: true);

            // 3. Extract zip
            ZipFile.ExtractToDirectory(tempZipPath, outputDir);

            log($"[VAULT] Extracted to: {outputDir}");
            
            // 4. Clean up
            File.Delete(tempZipPath);
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