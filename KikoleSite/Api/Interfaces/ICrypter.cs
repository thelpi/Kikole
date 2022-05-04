namespace KikoleSite.Api.Interfaces
{
    public interface ICrypter
    {
        string Encrypt(string data);

        string Generate();
    }
}
