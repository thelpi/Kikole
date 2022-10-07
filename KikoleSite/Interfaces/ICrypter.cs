namespace KikoleSite.Interfaces
{
    public interface ICrypter
    {
        string Encrypt(string data);

        string Generate();

        public string EncryptCookie(string plainText);

        string DecryptCookie(string encryptedText);
    }
}
