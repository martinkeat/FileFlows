using System.Security.Cryptography;

namespace FileFlows.FlowRunner;

class ConfigDecrypter
{
    private const int SaltSize = 8;
    
    internal static string DecryptConfig(string file, string password)
    {
        // read salt
        var fileStream = new FileInfo(file).OpenRead();
        var salt = new byte[SaltSize];
        fileStream.Read( salt, 0, SaltSize );

        // initialize algorithm with salt
#pragma warning disable SYSLIB0041
        var keyGenerator = new Rfc2898DeriveBytes( password, salt );
#pragma warning restore SYSLIB0041
        var aes = Aes.Create();
        aes.Padding = PaddingMode.PKCS7;
        aes.KeySize = 128;
        aes.Key = keyGenerator.GetBytes( aes.KeySize / 8 );
        aes.IV = keyGenerator.GetBytes( aes.KeySize / 8 );

        // decrypt
        using var cryptoStream = new CryptoStream(fileStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        
        // read data
        StreamReader reader = new StreamReader(cryptoStream);
        return reader.ReadToEnd();
    }
}
