using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace DencryptCore
{
    public static class Encryption
    {
        public delegate void LogHandler(string message);
        public static void EncryptFileOverwrite(string inputFilePath, string password, LogHandler log = null)
        {
            log ??= _ => { };
            log("Starting encryption...");

            const string header = "DENCRYPT01";
            const int saltLength = 16;
            const string encryptedExtension = ".enc";
            string tempFile = Path.ChangeExtension(inputFilePath, encryptedExtension);

            byte[] aesKey, aesIV, hmacKey, fileHash;
            string fileExtension = Path.GetExtension(inputFilePath).TrimStart('.');
            byte[] extBytes = Encoding.UTF8.GetBytes(fileExtension);
            byte extLength = (byte)extBytes.Length;

            // Generates SHA256 of the original file to verify that its the same after decryption
            log("Computing SHA256 hash of original file...");
            using (var sha = SHA256.Create())
            using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                fileHash = sha.ComputeHash(fs); // 32 bytes
            }

            // Generates salt
            byte[] salt = GenerateRandomBytes(saltLength);

            // Derive keys
            log("Deriving keys from password...");
            (aesKey, aesIV, hmacKey) = DeriveKeysFromPassword(password, salt);

            long ciphertextStart;
            long ciphertextEnd;

            byte[] headerBytes = Encoding.UTF8.GetBytes(header);

            // Prevents encrypting an already encrypted file
            log("Checking if file is already encrypted...");
            using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[headerBytes.Length];
                int read = fs.Read(buffer, 0, headerBytes.Length);
                if (read == headerBytes.Length && Encoding.UTF8.GetString(buffer) == header)
                {
                    throw new Exception("File is already encrypted.");
                }
            }

            try
            {
                using (FileStream fsIn = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                using (FileStream fsOut = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = aesIV;
                    aes.Padding = PaddingMode.PKCS7;
                    // Default CBC + PKCS7 padding

                    Console.WriteLine($"Encrypting file : " + inputFilePath);
                    log($"Encrypting file : " + inputFilePath);

                    // -- Starts writing data --

                    // Write header, salt, IV, extension length and extension
                    fsOut.Write(Encoding.UTF8.GetBytes(header));
                    fsOut.WriteByte(extLength);
                    fsOut.Write(extBytes);
                    fsOut.Write(salt);
                    fsOut.Write(aesIV);

                    // Start encrypting plaintext to cipher
                    using (CryptoStream csEncrypt = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            csEncrypt.Write(buffer, 0, bytesRead);
                        }
                        csEncrypt.FlushFinalBlock();
                    }
                    fsOut.Write(fileHash, 0, fileHash.Length); // Writes SHA256
                }

                // Ciphertext fully written
                // Get the entire written content for HMAC and Ensure data is flushed to disk
                // Computes HMAC and Appends it.
                byte[] hmac;
                using (FileStream fsOutForHmac = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    hmac = ComputeHmacOverStream(fsOutForHmac, hmacKey);
                }

                // Append HMAC
                using (FileStream fsOutAppend = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    fsOutAppend.Write(hmac, 0, hmac.Length);
                }

                // All streams closed, temp file structure = [salt|IV|Cipher|HMAC] - old
                // Replace original, might add backup system just in case.

                log("Replacing original file with encrypted file...");
                File.Delete(inputFilePath);         // Delete original file
                File.Move(tempFile, Path.ChangeExtension(inputFilePath, encryptedExtension)); // Rename temp file to original name

                Console.WriteLine("File encrypted.");
                log("File encrypted successfully.");
            }
            finally
            {
                // Zero out keys (security)
                Array.Clear(aesKey, 0, aesKey.Length);
                Array.Clear(hmacKey, 0, hmacKey.Length);
            }
        }
        public static void DecryptFileOverwrite(string inputFilePath, string password, LogHandler log = null)
        {
            log ??= _ => { };
            log("Starting decryption...");

            const int saltLength = 16;
            const int ivLength = 16;
            const int hmacLength = 32;
            const int hashLength = 32;
            string expectedHeader = "DENCRYPT01"; // Better
            int headerLength = expectedHeader.Length;

            byte[] salt, iv, storedHmac, computedHmac;
            byte[] aesKey, aesIV, hmacKey;
            byte[] storedHash;

            string tempOutputPath = inputFilePath + ".dec";
            string finalOutputPath;

            // First: extract metadata and compute HMAC from input file
            string originalExtension;
            long cipherStart, cipherLength, totalLength;

            // Open the encrypted file
            using (FileStream fsIn = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                totalLength = fsIn.Length;
                if (totalLength < headerLength + 1 + saltLength + ivLength + hmacLength)
                {
                    throw new Exception("File too small to be valid encrypted data.");
                }

                // 0. Verify header
                byte[] headerBytes = new byte[headerLength];
                int readHeader = fsIn.Read(headerBytes, 0, headerLength);
                if (readHeader != headerLength || Encoding.UTF8.GetString(headerBytes) != expectedHeader)
                {
                    log("File too small to be valid encrypted data.");
                    throw new Exception("❌ Invalid file header.");
                }

                // 1. Read extension
                int extLength = fsIn.ReadByte();
                if (extLength <= 0) throw new Exception("❌ Failed to read extension length.");
                byte[] extBytes = ReadExact(fsIn, extLength, "Extension");
                originalExtension = Encoding.UTF8.GetString(extBytes);

                // 2. Reads the Salt and IV
                salt = ReadExact(fsIn, saltLength, "Salt");
                iv = ReadExact(fsIn, ivLength, "IV");

                // 3. Derive the keys
                (aesKey, aesIV, hmacKey) = DeriveKeysFromPassword(password, salt);

                // 4. Compute ciphertext length
                cipherStart = headerLength + 1 + extLength + saltLength + ivLength;
                cipherLength = totalLength - cipherStart - hashLength - hmacLength;

                // 5. Verify HMAC, compute it over salt+IV+ciphertext
                // Seek back to start then reads salt+IV+ciphertext

                fsIn.Seek(0, SeekOrigin.Begin);
                using (var hmac = new HMACSHA256(hmacKey))
                {
                    byte[] buffer = new byte[8192];
                    long bytesToRead = totalLength - hmacLength;
                    long totalRead = 0;
                    while (totalRead < bytesToRead)
                    {
                        int read = fsIn.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesToRead - totalRead));
                        if (read <= 0) break;
                        hmac.TransformBlock(buffer, 0, read, null, 0);
                        totalRead += read;
                    }
                    hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    computedHmac = hmac.Hash;
                }

                // 5.1. Read stored SHA256
                fsIn.Seek(cipherStart + cipherLength, SeekOrigin.Begin);
                storedHash = ReadExact(fsIn, hashLength, "SHA256 Hash");

                // 6. Read stored HMAC
                fsIn.Seek(totalLength - hmacLength, SeekOrigin.Begin);
                storedHmac = ReadExact(fsIn, hmacLength, "HMAC");

                // 6.1 Constant time comparison
                if (!ByteArraysEqualConstantTime(computedHmac, storedHmac))
                {
                    log("❌ HMAC mismatch: wrong password or corrupted file. Decryption aborted, original file left intact.");
                    throw new Exception("❌ HMAC mismatch: wrong password or corrupted file. Decryption aborted, original file left intact.");
                }
            }

            // 7. If HMAC is OK -> decrypt to temp, using substream
            log("HMAC OK. Starting decryption...");
            try
            {
                using (FileStream fsDecrypt = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fsDecrypt.Seek(cipherStart, SeekOrigin.Begin);

                    log($"Decrypting file : {inputFilePath}");

                    using (SubStream cipherStream = new SubStream(fsDecrypt, cipherStart, cipherLength))
                    using (FileStream fsOut = new FileStream(tempOutputPath, FileMode.Create, FileAccess.Write))
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = aesKey;
                        aes.IV = aesIV;
                        aes.Padding = PaddingMode.PKCS7;

                        using (CryptoStream csDecrypt = new CryptoStream(cipherStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            csDecrypt.CopyTo(fsOut);
                        }
                    }

                    log("Decryption complete.");
                }
            }
            catch (Exception ex)
            {
                log($"Exception during decryption : {ex.Message}");
                if (File.Exists(tempOutputPath))
                    File.Delete(tempOutputPath);
                throw;
            }

            // Compute SHA256 hash 
            log("Computing SHA256 hash of decrypted file...");
            byte[] decryptedHash;
            using (var sha = SHA256.Create())
            using (var fs = new FileStream(tempOutputPath, FileMode.Open, FileAccess.Read))
            {
                decryptedHash = sha.ComputeHash(fs);
            }

            // SHA256 verification feedback
            log("Comparing SHA256 hashes...");
            if (!ByteArraysEqualConstantTime(decryptedHash, storedHash))
            {
                log("❌ SHA256 hash mismatch: Decrypted file integrity check failed.");
                if (File.Exists(tempOutputPath))
                    File.Delete(tempOutputPath);
                throw new Exception("❌ SHA256 hash mismatch: Decrypted file integrity check failed.");
            }

            // All Done. Close streams.
            // 8. Replace original encrypted file with decrypted file
            log("SHA256 OK. Replacing original file...");
            finalOutputPath = Path.ChangeExtension(inputFilePath, originalExtension);
            File.Delete(inputFilePath);
            File.Move(tempOutputPath, finalOutputPath);

            Console.WriteLine("File decrypted.");
            log("File decrypted successfully.");

            // 9. Zero out sensitive data
            Array.Clear(aesKey, 0, aesKey.Length);
            Array.Clear(hmacKey, 0, hmacKey.Length);
        }

        public static bool ByteArraysEqualConstantTime(byte[] a, byte[] b)
        {
            // To avoid timing attacks, because response time can gradually reveal the value to an attacker.
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        public static byte[] ReadExact(FileStream fs, int count, string label)
        {
            byte[] buffer = new byte[count];
            int bytesRead = fs.Read(buffer, 0, count);
            if (bytesRead != count)
                throw new Exception($"Failed to read {label}");
            return buffer;
        }

        public static (byte[] AesKey, byte[] AesIV, byte[] HmacKey) DeriveKeysFromPassword(string password, byte[] salt)
        {
            // 80 bytes of key material
            const int keyLength = 32;  // AES-256
            const int ivLength = 16;   // AES block size
            const int hmacKeyLength = 32; // for HMAC-SHA256
            int totalBytes = keyLength + ivLength + hmacKeyLength;

            // PBKDF2 with SHA256, 100k iterations
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] allBytes = pbkdf2.GetBytes(totalBytes);

            byte[] aesKey = new byte[keyLength];
            byte[] aesIV = new byte[ivLength];
            byte[] hmacKey = new byte[hmacKeyLength];

            Array.Copy(allBytes, 0, aesKey, 0, keyLength);
            Array.Copy(allBytes, keyLength, aesIV, 0, ivLength);
            Array.Copy(allBytes, keyLength + ivLength, hmacKey, 0, hmacKeyLength);

            return (aesKey, aesIV, hmacKey);
        }

        public static byte[] GenerateRandomBytes(int length)
        {
            // Salt generator :D
            byte[] bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        public static bool IsPasswordStrong(string password)
        {
            return password.Length >= 12 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        public static byte[] ComputeHmacOverStream(Stream stream, byte[] hmacKey)
        {
            using var hmac = new HMACSHA256(hmacKey);
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                hmac.TransformBlock(buffer, 0, bytesRead, null, 0);
            }
            hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return hmac.Hash;
        }

        public static List<string> GetAllFilesInFolder(string folderPath)
        {
            return Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).ToList();
        }
        
        public static LogHandler CreateFileLogger(string logFilePath)
        {
            return msg =>
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}";
                File.AppendAllText(logFilePath, line);
            };
        }
    }
}



// (っ◔◡◔)っ ♥ by Ezi0 ♥