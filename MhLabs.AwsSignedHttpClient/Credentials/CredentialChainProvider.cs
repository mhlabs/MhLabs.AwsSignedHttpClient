using System;
using System.Collections.Generic;
using System.Linq;

namespace MhLabs.AwsSignedHttpClient.Credentials
{
    public class CredentialChainProvider : ICredentialsProvider
    {
        private readonly ICredentialsProvider[] _credentialChain;

        public CredentialChainProvider(params ICredentialsProvider[] credentialProviders)
            : this((IEnumerable<ICredentialsProvider>)credentialProviders)
        {
        }

        public CredentialChainProvider(IEnumerable<ICredentialsProvider> credentialProviders)
        {
            _credentialChain = credentialProviders.ToArray();
        }

        public static ICredentialsProvider Default { get; } = new CredentialChainProvider(
            new EnvironmentVariableCredentialsProvider()
        );

        public AwsCredentials GetCredentials()
        {
            foreach (var provider in _credentialChain)
            {
                var creds = provider.GetCredentials();

                if (creds != null)
                {
                    if (SignV4Util.DebugLogging)
                    {
                        Console.WriteLine(creds.GetType().Name);
                        Console.WriteLine(creds.AccessKey);
                        Console.WriteLine(creds.SecretKey);
                        Console.WriteLine(creds.Token);
                    }
                    return creds;
                }
            }
            return null;
        }
    }
}