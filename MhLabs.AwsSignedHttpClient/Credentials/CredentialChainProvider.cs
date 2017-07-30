using System;
using System.Collections.Generic;
using System.Linq;

namespace signed_request_test.Http.Credentials
{
    public class CredentialChainProvider : ICredentialsProvider
    {
        public static ICredentialsProvider Default { get; } = new CredentialChainProvider(
            new EnvironmentVariableCredentialsProvider()

        );

        public CredentialChainProvider(params ICredentialsProvider[] credentialProviders)
            : this((IEnumerable<ICredentialsProvider>)credentialProviders)
        {
        }

        readonly ICredentialsProvider[] _credentialChain;

        public CredentialChainProvider(IEnumerable<ICredentialsProvider> credentialProviders)
        {
            _credentialChain = credentialProviders.ToArray();
        }

        public AwsCredentials GetCredentials()
        {
            foreach (var provider in _credentialChain)
            {
                var creds = provider.GetCredentials();
                Console.WriteLine(creds.GetType().Name);
                if (creds != null)
                {
                    Console.WriteLine(creds.AccessKey);
                    Console.WriteLine(creds.SecretKey);
                    Console.WriteLine(creds.Token);

                    return creds;
                }
            }
            return null;
        }
    }
}
