namespace KikoleSite.Interfaces
{
    public interface ICrypter
    {
        string Encrypt(string data);

        string Generate();
    }
}
