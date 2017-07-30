namespace signed_request_test.Http.Credentials
{
    public interface ICredentialsProvider
    {
        AwsCredentials GetCredentials();
    }
}
