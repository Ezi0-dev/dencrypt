using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using DencryptCore;

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

        if (!Encryption.IsPasswordStrong(password)) throw new Exception("Password is too weak! Minimum length is 12 characters with at least one uppercase character, one number and one symbol");

        try
        {
            if (choice == "e")
            {
                Console.Write("⚠️ This will overwrite the original file. Proceed? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                    Encryption.EncryptFileOverwrite(filePath, password);
                else
                    Console.WriteLine("Operation canceled.");
            }
            else if (choice == "d")
            {
                Console.Write("⚠️ This will decrypt the file content. Proceed? (y/n): ");
                if (Console.ReadLine()?.ToLower() == "y")
                    Encryption.DecryptFileOverwrite(filePath, password);
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
}