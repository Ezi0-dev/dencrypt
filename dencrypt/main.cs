using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Welcome to Dencrypt! ===");

        Console.Write("Enter file path: ");
        string filePath = Console.ReadLine();

        Console.Write("Encrypt or Decrypt? (E/D): ");
        string choice = Console.ReadLine()?.ToLower();

        Console.Write("Enter password: ");
        string password = Console.ReadLine();

        try
        {
            if (choice == "e")
            {
                Console.Write("⚠️ This will overwrite the original file. Proceed? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                    EncryptFileOverwrite(filePath, password);
                else
                    Console.WriteLine("Operation canceled.");
            }
            else if (choice == "d")
            {
                Console.Write("⚠️ This will decrypt the file content. Proceed? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                    DecryptFileOverwrite(filePath, password);
                else
                    Console.WriteLine("Operation canceled.");
            }
            else
            {
                Console.WriteLine("Invalid option.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error: " + ex.Message);
        }
    }
    static (byte[] AesKey, byte[] AesIV, byte[] HmacKey) DeriveKeysFromPassword(string password, byte[] salt)
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

    static byte[] GenerateRandomBytes(int length)
    {
        // Salt generator :D
        byte[] bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    static byte[] ComputeHmacOverStream(Stream stream, byte[] hmacKey)
    {
        using var hmac = new HMACSHA256(hmacKey);
        // Read in chunks, good for big files
        byte[] buffer = new byte[8192];
        int bytesRead;
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            hmac.TransformBlock(buffer, 0, bytesRead, null, 0);
        }
        // Finalize
        hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return hmac.Hash; // 32 bytes
    }

    static void EncryptFileOverwrite(string inputFilePath, string password)
    {
        const int saltLength = 16;

        // Generates salt
        byte[] salt = GenerateRandomBytes(saltLength);

        // Derive keys
        var (aesKey, aesIV, hmacKey) = DeriveKeysFromPassword(password, salt);

        string tempFilePath = inputFilePath + ".tmp";

        FileStream fsTemp = null;
        try
        {
            fsTemp = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite);
            // -- Starts writing data --
            // Write salt
            fsTemp.Write(salt, 0, salt.Length);
            // Write IV
            fsTemp.Write(aesIV, 0, aesIV.Length);

            // Encrypt plaintext to cipher
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIV;
                // Default CBC + PKCS7 padding

                // Position fsTemp (after salt+IV)
                using (CryptoStream csEncrypt = new CryptoStream(fsTemp, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                using (FileStream fsIn = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                {
                    fsIn.CopyTo(csEncrypt);
                }
                // fsTemp still open, ciphertext fully written.
            }

            // Adding HMAC over salt+IV+Cipher
            // Seek to start
            fsTemp.Seek(0, SeekOrigin.Begin);
            byte[] hmac = ComputeHmacOverStream(fsTemp, hmacKey);

            // Append HMAC
            fsTemp.Seek(0, SeekOrigin.End);
            fsTemp.Write(hmac, 0, hmac.Length);

            // Ensure data is flushed to disk
            fsTemp.Flush();
        }
        finally
        {
            // Close temp stream so the file is not locked
            if (fsTemp != null)
                fsTemp.Dispose();
        }
        // All streams closed, temp file structure = [salt|IV|Cipher|HMAC]

        // Replace original, might add backup system just in case.

        File.Delete(inputFilePath);         // Delete original file
        File.Move(tempFilePath, inputFilePath); // Rename temp file to original name

        Console.WriteLine("File encrypted.");
    }

    static void DecryptFileOverwrite(string inputFilePath, string password)
    {
        const int saltLength = 16;
        const int ivLength = 16;
        const int hmacLength = 32;

        // Open the encrypted file
        using (FileStream fsIn = new FileStream(inputFilePath, FileMode.Open, FileAccess.ReadWrite))
        {
            long totalLength = fsIn.Length;
            if (totalLength < saltLength + ivLength + hmacLength)
            {
                throw new Exception("File too small to be valid encrypted data.");
            }

            // 1. Reads the Salt
            byte[] salt = ReadExact(fsIn, saltLength, "Salt");

            // 2. Derive the keys
            var (aesKey, aesIV, hmacKey) = DeriveKeysFromPassword(password, salt);

            // 3. Reads IV
            byte[] iv = ReadExact(fsIn, ivLength, "IV");

            // 4. Compute ciphertext length
            long ciphertextLength = totalLength - saltLength - ivLength - hmacLength;

            // 5. Verify HMAC, compute it over salt+IV+ciphertext
            // Seek back to start then reads salt+IV+ciphertext
            fsIn.Seek(0, SeekOrigin.Begin);
            // Reusing ComputeHmacOverStream but limit it to reading Salt+Iv+Ciphertext only
            byte[] computedHmac;
            using (var hmac = new HMACSHA256(hmacKey))
            {
                // Read salt + IV + Ciphertext in chuncks to avoid big files making program unstable. 8kb chuncks currently
                byte[] buffer = new byte[8192];
                long bytesToRead = saltLength + ivLength + ciphertextLength;
                long totalRead = 0;
                while (totalRead < bytesToRead)
                {
                    int toRead = (int)Math.Min(buffer.Length, bytesToRead - totalRead);
                    int read = fsIn.Read(buffer, 0, toRead);
                    if (read <= 0) break;
                    hmac.TransformBlock(buffer, 0, read, null, 0);
                    totalRead += read;
                }
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                computedHmac = hmac.Hash;
            }

            // 6. Read stored HMAC from file
            fsIn.Seek(saltLength + ivLength + ciphertextLength, SeekOrigin.Begin);
            byte[] storedHmac = new byte[hmacLength];
            fsIn.Read(storedHmac, 0, hmacLength);
            
            // 6.1 Constant time comparison
            if (!ByteArraysEqualConstantTime(computedHmac, storedHmac))
            {
                throw new Exception("❌ HMAC mismatch: wrong password or corrupted file. Decryption aborted, original file left intact.");
            }

            // 7. If HMAC is OK -> decrypt to temp
            string tempFile = inputFilePath + ".tmp";
            // Position fsIn at start of ciphertext
            fsIn.Seek(saltLength + ivLength, SeekOrigin.Begin);

            using (FileStream fsOut = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = iv;

                // Decrypt only ciphertextLength bytes
                using (CryptoStream csDecrypt = new CryptoStream(fsIn, aes.CreateDecryptor(), CryptoStreamMode.Read))
                // Reusing the ComputeHmacOverStream again (but a bit modified)
                {
                    byte[] buffer = new byte[8192];
                    long remaining = ciphertextLength;
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(buffer.Length, remaining);
                        int read = csDecrypt.Read(buffer, 0, toRead);
                        if (read <= 0) break;
                        fsOut.Write(buffer, 0, read);
                        remaining -= read;
                    }
                }
            }
            // All Done. Close streams.

            // 8. Replace original encrypted file with decrypted file
            File.Delete(inputFilePath);
            File.Move(tempFile, inputFilePath);

            Console.WriteLine("File decrypted.");
        }
    }
    static bool ByteArraysEqualConstantTime(byte[] a, byte[] b)
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

    static byte[] ReadExact(FileStream fs, int count, string label)
    {
        byte[] buffer = new byte[count];
        int bytesRead = fs.Read(buffer, 0, count);
        if (bytesRead != count)
            throw new Exception($"Failed to read {label}");
        return buffer;
    }
}