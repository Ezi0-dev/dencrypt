using System.IO;
using System.Collections.Generic;
using System.IO.Compression;

namespace DencryptCore
{
    public static class Vault
    {
        private const string VaultExtension = ".vault";

        public static void CreatVault(List<string> filePaths, string outputVaultPath, string password, Encryption.LogHandler log = null)
        {
            log ??= _ => { };
            log($"[VAULT] Creating vault: {outputVaultPath}");

            if (!outputVaultPath.EndsWith(VaultExtension))
                outputVaultPath += VaultExtension;

            // 1. ZIP everything into a tempfile
            string tempZip = Path.GetTempFileName();
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

            // 1. Decrypt .vault to tempZip
            string tempEncrypted = vaultPath;
            string tempZip = Path.GetTempFileName();
            File.Copy(tempEncrypted, tempZip + ".enc", overwrite: true);
            Encryption.DecryptFileOverwrite(tempZip + ".enc", password, log);

            // 2. Excract ZIP to otuputDir
            string finalZip = Path.ChangeExtension(tempZip + ".enc", null);
            using (ZipArchive archive = ZipFile.OpenRead(finalZip))
            {
                foreach (var entry in archive.Entries)
                {
                    string outPath = Path.Combine(outputDir, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                    entry.ExtractToFile(outPath, overwrite: true);
                    log($"[VAULT] Extracted: {entry.FullName}");
                }
            }
            File.Delete(finalZip);

            log($"[VAULT] Extraction complete to: {outputDir}");
        }
    }
}