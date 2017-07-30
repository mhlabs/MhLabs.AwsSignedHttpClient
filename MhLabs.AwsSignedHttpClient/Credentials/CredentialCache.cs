using System;

namespace signed_request_test.Http.Credentials
{
    internal class CredentialCache
    {
        readonly object _syncRoot = new object();
        readonly Func<AwsCredentials> _producer;
        readonly TimeSpan _ttl;
        CachedValue _value;

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
                        ValidUntil = DateTime.UtcNow + _ttl,
                    };
                    return _value.Value;
                }
            }
        }

        private class CachedValue
        {
            public AwsCredentials Value;
            public DateTime ValidUntil;
        }
    }
}
