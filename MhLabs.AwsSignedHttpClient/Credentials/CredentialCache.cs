using System;

namespace MhLabs.AwsSignedHttpClient.Credentials
{
    internal class CredentialCache
    {
        private readonly Func<AwsCredentials> _producer;
        private readonly object _syncRoot = new object();
        private readonly TimeSpan _ttl;
        private CachedValue _value;

        public CredentialCache(Func<AwsCredentials> producer, TimeSpan ttl)
        {
            _producer = producer;
            _ttl = ttl;
        }

        public AwsCredentials Value
        {
            get
            {
                var value = _value;
                if (value?.ValidUntil < DateTime.UtcNow) return value.Value;
                lock (_syncRoot)
                {
                    value = _value;
                    if (value?.ValidUntil < DateTime.UtcNow) return value.Value;
                    _value = new CachedValue
                    {
                        Value = _producer(),
                        ValidUntil = DateTime.UtcNow + _ttl
                    };
                    return _value.Value;
                }
            }
        }

        private class CachedValue
        {
            public DateTime ValidUntil;
            public AwsCredentials Value;
        }
    }
}