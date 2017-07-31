namespace MhLabs.AwsSignedHttpClient.Credentials
{
    public interface ICredentialsProvider
    {
        AwsCredentials GetCredentials();
    }
}