/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal interface IOidcExternalAuthenticationCredentialsProvider : ICredentialsCache<OidcCredentials>
    {
        OidcCredentials CreateCredentialsFromExternalSource(OidcCredentials invalidCredentials, BsonDocument saslStartResponse, CancellationToken cancellationToken = default);
        Task<OidcCredentials> CreateCredentialsFromExternalSourceAsync(OidcCredentials invalidCredentials, BsonDocument saslStartResponse, CancellationToken cancellationToken = default);
    }

    internal sealed class OidcExternalAuthenticationCredentialsProvider : IOidcExternalAuthenticationCredentialsProvider, IDisposable
    {
        private readonly CancellationTokenSource _globalCancellationTokenSource;
        private readonly CancellationToken _globalCancellationToken;
        private readonly OidcInputConfiguration _inputConfiguration;
        private OidcCredentials _cachedValue;
        private readonly IClock _oidcClock;
        private readonly SemaphoreSlim _semaphore;
        private readonly InterlockedInt32 _state;
        private readonly TimeSpan _timeout;

        public OidcExternalAuthenticationCredentialsProvider(
            OidcInputConfiguration inputConfiguration,
            IClock oidcClock)
        {
            _globalCancellationTokenSource = new CancellationTokenSource();
            _globalCancellationToken = _globalCancellationTokenSource.Token;
            _oidcClock = Ensure.IsNotNull(oidcClock, nameof(oidcClock));
            _inputConfiguration = Ensure.IsNotNull(inputConfiguration, nameof(inputConfiguration));
            _semaphore = new SemaphoreSlim(1, 1);
            _state = new InterlockedInt32(State.Ready);
            _timeout = TimeSpan.FromMinutes(5);
        }

        public OidcCredentials CachedCredentials => _cachedValue;

        public void Clear() => InternalClear(onlyExpire: false);

        public OidcCredentials CreateCredentialsFromExternalSource(OidcCredentials invalidCredentials, BsonDocument saslStartResponse, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            OidcCredentials oidcCredentials = null;
            using (var fetchCredentialsHelper = new FetchCredentialsHelper(_semaphore, _inputConfiguration, _oidcClock, _timeout, _globalCancellationToken, cancellationToken))
            {
                fetchCredentialsHelper.AcquireSlot();

                var cachedValue = _cachedValue;
                if (cachedValue != null)
                {
                    if (cachedValue.ShouldBeRefreshed || ShouldForceExpire(invalidCredentials))
                    {
                        InternalClear();
                        oidcCredentials = fetchCredentialsHelper.GetCredentialsWithRefreshTokenIfConfigured(saslStartResponse, cachedValue.CallbackAuthenticationData);
                    }
                    else
                    {
                        return _cachedValue = cachedValue;
                    }
                }

                if (oidcCredentials == null)
                {
                    try
                    {
                        oidcCredentials = fetchCredentialsHelper.GetCredentialsWithRequestTokenIfConfigured(saslStartResponse);
                    }
                    catch
                    {
                        // at least we can cache server response
                        _cachedValue = OidcCredentials.Create(saslStartResponse, _oidcClock);
                        throw;
                    }
                }

                if (oidcCredentials != null)
                {
                    // save it regardless expiration details to be able to use it in refresh callback
                    _cachedValue = oidcCredentials;
                }
            }

            return oidcCredentials ?? throw CreateException("OIDC credentials have not been provided.");
        }

        public async Task<OidcCredentials> CreateCredentialsFromExternalSourceAsync(OidcCredentials invalidCredentials, BsonDocument saslStartResponse, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            OidcCredentials oidcCredentials = null;
            using (var fetchCredentialsHelper = new FetchCredentialsHelper(_semaphore, _inputConfiguration, _oidcClock, _timeout, _globalCancellationToken, cancellationToken))
            {
                await fetchCredentialsHelper.AcquireSlotAsync().ConfigureAwait(false);

                var cachedValue = _cachedValue;
                if (cachedValue != null)
                {
                    if (cachedValue.ShouldBeRefreshed || ShouldForceExpire(invalidCredentials))
                    {
                        InternalClear();
                        oidcCredentials = await fetchCredentialsHelper.GetCredentialsWithRefreshTokenIfConfiguredAsync(saslStartResponse, cachedValue.CallbackAuthenticationData).ConfigureAwait(false);
                    }
                    else
                    {
                        return _cachedValue = cachedValue;
                    }
                }

                if (oidcCredentials == null)
                {
                    try
                    {
                        oidcCredentials = await fetchCredentialsHelper.GetCredentialsWithRequestTokenIfConfiguredAsync(saslStartResponse).ConfigureAwait(false);
                    }
                    catch
                    {
                        // at least we can cache server response
                        _cachedValue = OidcCredentials.Create(saslStartResponse, _oidcClock);
                        throw;
                    }
                }

                if (oidcCredentials != null)
                {
                    // save it regardless expiration details to be able to use it in refresh callback
                    _cachedValue = oidcCredentials;
                }
            }

            return oidcCredentials ?? throw CreateException("OIDC credentials have not been provided.");
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Ready, State.Disposed))
            {
                _globalCancellationTokenSource.Cancel();
                _globalCancellationTokenSource.Dispose();
                _semaphore.Dispose();
            }
        }

        // private methods
        private bool ShouldForceExpire(OidcCredentials invalidCredentials) => _cachedValue != null && invalidCredentials != null && _cachedValue.AccessToken == invalidCredentials.AccessToken;

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(nameof(OidcExternalAuthenticationCredentialsProvider));
            }
        }

        // private method
        private Exception CreateException(string message) => new InvalidOperationException(message);
        private void InternalClear(bool onlyExpire = true)
        {
            if (onlyExpire)
            {
                _cachedValue?.Expire();
            }
            else
            {
                _cachedValue = default;
            }
        }

        // nested types
        private sealed class FetchCredentialsHelper : IDisposable
        {
            private bool _acquired = false;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly CancellationToken _cancellationToken;
            private readonly IClock _clock;
            private readonly OidcInputConfiguration _inputConfiguration;
            private readonly SemaphoreSlim _semaphore;
            private readonly TimeSpan _timeout;
            private readonly string _timeoutErrorMessage;

            public FetchCredentialsHelper(
                SemaphoreSlim semaphore,
                OidcInputConfiguration inputConfiguration,
                IClock clock,
                TimeSpan timeout,
                params CancellationToken[] cancellationTokens)
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
                _cancellationToken = _cancellationTokenSource.Token;
                _clock = Ensure.IsNotNull(clock, nameof(clock));
                _inputConfiguration = Ensure.IsNotNull(inputConfiguration, nameof(inputConfiguration));
                _semaphore = Ensure.IsNotNull(semaphore, nameof(semaphore));
                _timeout = timeout;
                _timeoutErrorMessage = $"Waiting for fetching OIDC credentials exceeded timeout {_timeout}.";
            }

            public void Dispose()
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                try
                {
                    if (_acquired)
                    {
                        _semaphore.Release();
                    }
                }
                catch
                {
                    // ignore it, semaphore might be already disposed by provider.
                }
            }

            public OidcCredentials GetCredentialsWithRequestTokenIfConfigured(BsonDocument saslStartResponse)
            {
                ThrowIfNotAcquired();

                if (_inputConfiguration.RequestCallbackProvider != null)
                {
                    var task = Task.Factory.StartNew(() => _inputConfiguration.RequestCallbackProvider.GetTokenResult(_inputConfiguration.CreateClientInfo(), saslStartResponse, _cancellationToken), _cancellationToken);
                    var clientResponse = TaskUtils.RunCallbackOrThrow(task, _timeout, _timeoutErrorMessage, _cancellationToken);
                    return OidcCredentials.Create(callbackAuthenticationData: clientResponse, saslStartResponse, _clock);
                }
                else
                {
                    return null;
                }
            }

            public async Task<OidcCredentials> GetCredentialsWithRequestTokenIfConfiguredAsync(BsonDocument saslStartResponse)
            {
                ThrowIfNotAcquired();

                if (_inputConfiguration.RequestCallbackProvider != null)
                {
                    var task = Task.Run(() => _inputConfiguration.RequestCallbackProvider.GetTokenResultAsync(_inputConfiguration.CreateClientInfo(), saslStartResponse, _cancellationToken), _cancellationToken);
                    var clientResponse = await TaskUtils.RunAsyncCallbackOrThrow(task, _timeout, _timeoutErrorMessage, _cancellationToken).ConfigureAwait(false);
                    return OidcCredentials.Create(callbackAuthenticationData: clientResponse, saslStartResponse, _clock);
                }
                else
                {
                    return null;
                }
            }

            public OidcCredentials GetCredentialsWithRefreshTokenIfConfigured(BsonDocument saslStartResponse, BsonDocument cachedCallbackAuthenticationData)
            {
                ThrowIfNotAcquired();

                if (_inputConfiguration.RefreshCallbackProvider != null)
                {
                    var task = Task.Factory.StartNew(() => _inputConfiguration.RefreshCallbackProvider.GetTokenResult(_inputConfiguration.CreateClientInfo(), saslStartResponse, cachedCallbackAuthenticationData, _cancellationToken), _cancellationToken);
                    var clientResponse = TaskUtils.RunCallbackOrThrow(task, _timeout, _timeoutErrorMessage, _cancellationToken);
                    return OidcCredentials.Create(callbackAuthenticationData: clientResponse, saslStartResponse, _clock);
                }
                else
                {
                    return null;
                }
            }

            public async Task<OidcCredentials> GetCredentialsWithRefreshTokenIfConfiguredAsync(BsonDocument saslStartResponse, BsonDocument cachedCallbackAuthenticationData)
            {
                ThrowIfNotAcquired();

                if (_inputConfiguration.RefreshCallbackProvider != null)
                {
                    var task = Task.Run(() => _inputConfiguration.RefreshCallbackProvider.GetTokenResultAsync(_inputConfiguration.CreateClientInfo(), saslStartResponse, cachedCallbackAuthenticationData, _cancellationToken), _cancellationToken);
                    var clientResponse = await TaskUtils.RunAsyncCallbackOrThrow(task, _timeout, _timeoutErrorMessage, _cancellationToken).ConfigureAwait(false);
                    return OidcCredentials.Create(callbackAuthenticationData: clientResponse, saslStartResponse, _clock);
                }
                else
                {
                    return null;
                }
            }

            public void AcquireSlot()
            {
                if (!(_acquired = _semaphore.Wait(_timeout, _cancellationToken)))
                {
                    throw CreateException();
                }
            }

            public async Task AcquireSlotAsync()
            {
                if (!(_acquired = await _semaphore.WaitAsync(_timeout, _cancellationToken).ConfigureAwait(false)))
                {
                    throw CreateException();
                }
            }

            // private methods
            private Exception CreateException() => new TimeoutException(_timeoutErrorMessage);

            private void ThrowIfNotAcquired()
            {
                if (!_acquired)
                {
                    // should not be reached
                    throw new InvalidOperationException("The OIDC lock must be acquired.");
                }
            }
        }

        private static class State
        {
            public static int Ready = 0;
            public static int Disposed = 1;
        }
    }
}
