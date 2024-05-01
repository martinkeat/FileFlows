using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Decryption/Encryption methods
/// </summary>
public class Decrypter
{
    /// <summary>
    /// Decrypts a string
    /// </summary>
    /// <param name="text">the string to decrypt</param>
    /// <param name="key">the encryption key</param>
    /// <returns>the decrypted string</returns>
    public static string Decrypt(string text, string key)
    {
        try
        {
            byte[] IV = Convert.FromBase64String(text.Substring(0, 20));
            string work = text.Substring(20).Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(work);
            using (Aes encryptor = Aes.Create())
            {
#pragma warning disable SYSLIB0041
                var pdb = new Rfc2898DeriveBytes(key, IV);
#pragma warning restore SYSLIB0041
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }

                    work = Encoding.Unicode.GetString(ms.ToArray());
                }
            }

            return work;
        }
        catch (Exception)
        {
            return text;
        }

    }

    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="text">the text to encrypt</param>
    /// <param name="key">the encryption key</param>
    /// <returns>the encrypted text</returns>
    public static string Encrypt(string text, string key)
    {
        byte[] clearBytes = Encoding.Unicode.GetBytes(text);
        Random rand= new Random(DateTime.UtcNow.Millisecond);
        using (Aes encryptor = Aes.Create())
        {
            byte[] IV = new byte[15];
            rand.NextBytes(IV);
#pragma warning disable SYSLIB0041
            var pdb = new Rfc2898DeriveBytes(key, IV);
#pragma warning restore SYSLIB0041
            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearBytes, 0, clearBytes.Length);
                    cs.Close();
                }
                text = Convert.ToBase64String(IV) + Convert.ToBase64String(ms.ToArray());
            }
        }
        return text;
    }
    
    
    /// <summary>
    /// Checks if the given input string appears to be encrypted.
    /// </summary>
    /// <param name="input">The input string to be checked.</param>
    /// <returns>True if the input string appears to be encrypted; otherwise, false.</returns>
    public static bool IsPossiblyEncrypted(string input)
    {
        // Single line regular expression to check for potential encrypted strings
        return Regex.IsMatch(input, @"^[a-zA-Z0-9/+]{40,}[=]{0,2}$");
    }
}
