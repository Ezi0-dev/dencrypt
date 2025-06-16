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

    static bool IsPasswordStrong(string password)
    {
        return password.Length >= 12 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsDigit) &&
            password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    static byte[] ComputeHmacOverStream(Stream stream, byte[] hmacKey)
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

    static void EncryptFileOverwrite(string inputFilePath, string password)
    {
        const string header = "DENCRYPT01";
        string tempFile = inputFilePath + ".enc";
        const int saltLength = 16;
        byte[] aesKey, aesIV, hmacKey;

        string fileExtension = Path.GetExtension(inputFilePath).TrimStart('.');
        byte[] extBytes = Encoding.UTF8.GetBytes(fileExtension);
        byte extLength = (byte)extBytes.Length;

        // Generates salt
        byte[] salt = GenerateRandomBytes(saltLength);

        // Derive keys
        (aesKey, aesIV, hmacKey) = DeriveKeysFromPassword(password, salt);

        if (!IsPasswordStrong(password)) throw new Exception("Password is too weak! Minimum length is 12 characters with at least one uppercase character, one number and one symbol");

        long ciphertextStart;
        long ciphertextEnd;

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

                Console.WriteLine("Encrypting file...");

                // -- Starts writing data --

                // Write Salt, IV and Header
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                fsOut.Write(headerBytes, 0, headerBytes.Length);
                fsOut.Write(salt, 0, salt.Length);
                fsOut.Write(aesIV, 0, aesIV.Length);

                // Get file extension
                fsOut.WriteByte(extLength);
                fsOut.Write(extBytes, 0, extBytes.Length);

                ciphertextStart = fsOut.Position;

                // Encrypt plaintext to cipher

                // Position fsOut (after salt+IV)
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

                ciphertextEnd = fsOut.Position; // Sets position
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

            // All streams closed, temp file structure = [salt|IV|Cipher|HMAC]
            // Replace original, might add backup system just in case.

            File.Delete(inputFilePath);         // Delete original file
            File.Move(tempFile, inputFilePath); // Rename temp file to original name

            Console.WriteLine("File encrypted.");
        }
        finally
        {
            // Zero out keys (security)
            Array.Clear(aesKey, 0, aesKey.Length);
            Array.Clear(hmacKey, 0, hmacKey.Length);
        }
    }
    static void DecryptFileOverwrite(string inputFilePath, string password)
    {
        const int saltLength = 16;
        const int ivLength = 16;
        const int hmacLength = 32;
        string expectedHeader = "DENCRYPT01"; // Better
        int headerLength = expectedHeader.Length;
        
        byte[] salt, iv, storedHmac, computedHmac;
        byte[] aesKey, aesIV, hmacKey;
        long ciphertextLength;
        string originalExtension;
        
        string tempOutputPath = inputFilePath + ".enc";

        // Open the encrypted file
        using (FileStream fsIn = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
        {
            long totalLength = fsIn.Length;
            if (totalLength < headerLength + 1 + saltLength + ivLength + hmacLength)
            {
                throw new Exception("File too small to be valid encrypted data.");
            }

            // 0. Verify header
            byte[] headerBytes = new byte[headerLength];
            int readHeader = fsIn.Read(headerBytes, 0, headerLength);
            if (readHeader != headerLength || Encoding.UTF8.GetString(headerBytes) != expectedHeader)
            {
                throw new Exception("❌ Invalid file header.");
            }

            // 1. Read extension
            int extLength = fsIn.ReadByte();
            byte[] extBytes = new byte[extLength];
            fsIn.Read(extBytes, 0, extLength);
            string originalExtension = Encoding.UTF8.GetString(extBytes);

            // 2. Reads the Salt and IV
            salt = ReadExact(fsIn, saltLength, "Salt");
            iv = ReadExact(fsIn, ivLength, "IV");

            // 3. Derive the keys
            (aesKey, aesIV, hmacKey) = DeriveKeysFromPassword(password, salt);

            // 4. Compute ciphertext length
            long cipherStart = headerLength + 1 + extLength + saltLength + ivLength;
            long cipherLength = totalLength - cipherStart - hmacLength;

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

            // 6. Read stored HMAC
            fsIn.Seek(totalLength - hmacLength, SeekOrigin.Begin);
            storedHmac = ReadExact(fsIn, hmacLength, "HMAC");

            // 6.1 Constant time comparison
            if (!ByteArraysEqualConstantTime(computedHmac, storedHmac))
                throw new Exception("❌ HMAC mismatch: wrong password or corrupted file. Decryption aborted, original file left intact.");

            // Done reading, fsIn closed here ---
        }

        // 7. If HMAC is OK -> decrypt to temp, using substream


        // using (FileStream fsIn = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
        // using (var subStream = new SubStream(fsIn, headerLength + saltLength + ivLength, ciphertextLength))
        // using (FileStream fsOut = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
        
        using (FileStream fsInDecrypt = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
        using (SubStream cipherStream = new SubStream(fsInDecrypt, cipherStart, cipherLength))
        using (FileStream fsOut = new FileStream(tempOutputPath, FileMode.Create, FileAccess.Write))
        using (Aes aes = Aes.Create())
            try
            {
                aes.Key = aesKey;
                aes.IV = aesIV;
                aes.Padding = PaddingMode.PKCS7;

                using (CryptoStream csDecrypt = new CryptoStream(subStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    csDecrypt.CopyTo(fsOut); // Nice
                }
            }
            catch (Exception)
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                throw;
            }

        // All Done. Close streams.
        // 8. Replace original encrypted file with decrypted file
        File.Delete(inputFilePath);
        File.Move(tempOutputPath, Path.ChangeExtension(inputFilePath, originalExtension));

        // 9. Zero out sensitive data
        Array.Clear(aesKey, 0, aesKey.Length);
        Array.Clear(hmacKey, 0, hmacKey.Length);

        Console.WriteLine("File decrypted.");
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