﻿/* |----------------------------------------------------------------|
 * | Copyright (c) 2017, Grid Entertainment                         |
 * | All Rights Reserved                                            |
 * |                                                                |
 * | This source code is to only be used for educational            |
 * | purposes. Distribution of SoundByte source code in             |
 * | any form outside this repository is forbidden. If you          |
 * | would like to contribute to the SoundByte source code, you     |
 * | are welcome.                                                   |
 * |----------------------------------------------------------------|
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using Newtonsoft.Json;
using SoundByte.Core.Helpers;
using SoundByte.API.Endpoints;
using SoundByte.API.Exceptions;

namespace SoundByte.Core.Services
{
    public class SoundByteService
    {
        #region Disconnect Service Helpers

        /// <summary>
        ///     Disconnects a soundbyte service
        /// </summary>
        public void DisconnectService(ServiceType serviceType)
        {
            // Get the password vault
            var vault = new PasswordVault();

            switch (serviceType)
            {
                case ServiceType.SoundCloud:
                    // Remove the token
                    _soundCloudToken = null;
                    // Remove everything in the vault
                    vault.FindAllByResource("SoundByte.SoundCloud").ToList().ForEach(x => vault.Remove(x));
                    break;
                case ServiceType.Fanburst:
                    // Remove the token
                    _fanburstToken = null;
                    // Remove everything in the vault
                    vault.FindAllByResource("SoundByte.FanBurst").ToList().ForEach(x => vault.Remove(x));
                    break;
            }
        }

        #endregion

        #region Static Class Instance

        // Private class instance
        private static readonly Lazy<SoundByteService> InstanceHolder =
            new Lazy<SoundByteService>(() => new SoundByteService());

        /// <summary>
        ///     Get the current soundbyte service
        /// </summary>
        public static SoundByteService Instance => InstanceHolder.Value;
        #endregion

        #region Secret Keys

        private Token _soundCloudToken;
        private Token _fanburstToken;
        private User _currentSoundCloudUser;
        private User _currentFanBurstUser;

        #endregion

        #region Getters and Setters

        /// <summary>
        ///     Get the token needed to access fanburst resources
        /// </summary>
        public Token FanburstToken
        {
            get
            {
                // If we already have the token, return it
                if (_fanburstToken != null)
                    return _fanburstToken;

                // Perform a check to see if we are logged in
                if (!IsFanBurstAccountConnected)
                    return null;

                // Get the password vault
                var vault = new PasswordVault();

                try
                {
                    // Get soundcloud recources
                    var soundCloudResource = vault.FindAllByResource("SoundByte.FanBurst").FirstOrDefault();

                    // If this resource does not exist, return false
                    if (soundCloudResource == null)
                        return null;

                    // Get the soundcloud vault items
                    var token = vault.Retrieve("SoundByte.FanBurst", "Token").Password;

                    // Create a new token class
                    var tokenHolder = new Token
                    {
                        AccessToken = token
                    };

                    // Set the private token
                    _fanburstToken = tokenHolder;

                    // Return the newly created token
                    return tokenHolder;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     Get the token needed to access soundcloud resources
        /// </summary>
        public Token SoundCloudToken
        {
            get
            {
                // If we already have the token, return it
                if (_soundCloudToken != null)
                    return _soundCloudToken;

                // Perform a check to see if we are logged in
                if (!IsSoundCloudAccountConnected)
                    return null;

                // Get the password vault
                var vault = new PasswordVault();

                try
                {
                    // Get soundcloud recources
                    var soundCloudResource = vault.FindAllByResource("SoundByte.SoundCloud").FirstOrDefault();

                    // If this resource does not exist, return false
                    if (soundCloudResource == null)
                        return null;

                    // Get the soundcloud vault items
                    var token = vault.Retrieve("SoundByte.SoundCloud", "Token").Password;
                    var scope = vault.Retrieve("SoundByte.SoundCloud", "Scope").Password;

                    // Create a new token class
                    var tokenHolder = new Token
                    {
                        AccessToken = token,
                        Scope = scope
                    };

                    // Set the private token
                    _soundCloudToken = tokenHolder;

                    // Return the newly created token
                    return tokenHolder;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///     The current logged in fanburst user
        /// </summary>
        public User FanburstUser
        {
            get
            {
                // Handle account disconnect
                if (!IsFanBurstAccountConnected)
                {
                    _currentFanBurstUser = null;
                    return null;
                }

                // If we have a user object, return it
                if (_currentFanBurstUser != null)
                    return _currentFanBurstUser;

                try
                {
                    _currentFanBurstUser =
                        AsyncHelper.RunSync(async () => await GetAsync<User>(ServiceType.Fanburst, "/me"));
                    return _currentFanBurstUser;
                }
                catch
                {
                    DisconnectService(ServiceType.Fanburst);
                    return null;
                }
            }
        }

        /// <summary>
        ///     The current logged in soundcloud user
        /// </summary>
        public User SoundCloudUser
        {
            get
            {
                // Handle account disconnect
                if (!IsSoundCloudAccountConnected)
                {
                    _currentSoundCloudUser = null;
                    return null;
                }

                // If we have a user object, return it
                if (_currentSoundCloudUser != null)
                    return _currentSoundCloudUser;

                try
                {
                    _currentSoundCloudUser = AsyncHelper.RunSync(async () => await GetAsync<User>("/me"));
                    return _currentSoundCloudUser;
                }
                catch
                {
                    DisconnectService(ServiceType.SoundCloud);
                    return null;
                }
            }
        }

        #endregion

        #region Helper Classes and Enums
        [JsonObject]
        public class Token
        {
            /// <summary>
            ///     The access token
            /// </summary>
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            /// <summary>
            ///     The type of token
            /// </summary>
            public string TokenType { get; set; }

            /// <summary>
            ///     Time for this current token to expire
            /// </summary>
            public string ExpireTime { get; set; }

            /// <summary>
            ///     Token used to refresh
            /// </summary>
            public string RefreshToken { get; set; }

            /// <summary>
            ///     What scope we have
            /// </summary>
            [JsonProperty("scope")]
            public string Scope { get; set; }
        }

        #endregion

        #region Account Connected Checks

        public bool IsAccountConnected => IsFanBurstAccountConnected || IsSoundCloudAccountConnected;

        /// <summary>
        ///     Checks to see if the users fanbirst account is connected
        /// </summary>
        public bool IsFanBurstAccountConnected
        {
            get
            {
                // Get the password vault
                var vault = new PasswordVault();

                if (vault.RetrieveAll().FirstOrDefault(x => x.Resource == "SoundByte.FanBurst") == null)
                    return false;

                try
                {
                    // Get FanBurst recources
                    var appBurstResource = vault.FindAllByResource("SoundByte.FanBurst").FirstOrDefault();

                    // If this resource does not exist, return false
                    if (appBurstResource == null)
                        return false;

                    // Get the soundcloud token
                    var tokenValue = vault.Retrieve("SoundByte.FanBurst", "Token").Password;

                    // return true if the token exists
                    return !string.IsNullOrEmpty(tokenValue);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        ///     Checks to see if the users soundcloud account is connected
        /// </summary>
        public bool IsSoundCloudAccountConnected
        {
            get
            {
                // Get the password vault
                var vault = new PasswordVault();

                if (vault.RetrieveAll().FirstOrDefault(x => x.Resource == "SoundByte.SoundCloud") == null)
                    return false;

                try
                {
                    // Get soundcloud recources
                    var soundCloudResource = vault.FindAllByResource("SoundByte.SoundCloud").FirstOrDefault();

                    // If this resource does not exist, return false
                    if (soundCloudResource == null)
                        return false;

                    // Get the soundcloud token
                    var tokenValue = vault.Retrieve("SoundByte.SoundCloud", "Token").Password;

                    // return true if the token exists
                    return !string.IsNullOrEmpty(tokenValue);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion

        #region WebAPI Helpers

        /// <summary>
        ///     Contacts the soundcloud API and posts an object. The posted object will then be
        ///     returned back to the user.
        /// </summary>
        /// <typeparam name="T">The object type we will serialize</typeparam>
        /// <param name="endpoint"></param>
        /// <param name="bodyContent">The content we want to post</param>
        /// <param name="optionalParams">A list of any optional params to send in the URI</param>
        /// <param name="useV2Api">If true, the v2 version of the soundcloud API will be contacted instead.</param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string endpoint, HttpStringContent bodyContent = null,
            Dictionary<string, string> optionalParams = null, bool useV2Api = false)
        {
            // Strip out the / infront of the endpoint if it exists
            endpoint = endpoint.TrimStart('/');

            // Start building the request URL
            var requestUri = useV2Api
                ? $"https://api-v2.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}"
                : $"https://api.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}";

            // Check that there are optional params then loop through all 
            // the params and add them onto the request URL
            if (optionalParams != null)
                requestUri = optionalParams
                    .Where(param => !string.IsNullOrEmpty(param.Key) && !string.IsNullOrEmpty(param.Value))
                    .Aggregate(requestUri, (current, param) => current + "&" + param.Key + "=" + param.Value);

            // Get the resource loader
            var resources = ResourceLoader.GetForViewIndependentUse();

            try
            {
                return await Task.Run(async () =>
                {
                    // Create the client
                    using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                    {
                        // Setup the request readers for user agent
                        // and requested data type.
                        client.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("SoundByte",
                            Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." +
                            Package.Current.Id.Version.Build));
                        client.DefaultRequestHeaders.Accept.Add(
                            new HttpMediaTypeWithQualityHeaderValue("application/json"));

                        // Auth headers for when the user is logged in
                        if (IsSoundCloudAccountConnected)
                            client.DefaultRequestHeaders.Authorization =
                                new HttpCredentialsHeaderValue("OAuth", SoundCloudToken.AccessToken);

                        // escape the url
                        var escapedUri = new Uri(Uri.EscapeUriString(requestUri));

                        // Full the body content if it is null
                        if (bodyContent == null)
                            bodyContent = new HttpStringContent(string.Empty);

                        // Get the URL
                        using (var webRequest = await client.PostAsync(escapedUri, bodyContent))
                        {
                            // Throw exception if the request failed
                            if (webRequest.StatusCode != HttpStatusCode.Ok)
                                throw new SoundByteException("Connection Error", webRequest.ReasonPhrase, "\uEB63");

                            // Get the body of the request as a stream
                            using (var webStream = await webRequest.Content.ReadAsInputStreamAsync())
                            {
                                // Read the stream
                                using (var streamReader = new StreamReader(webStream.AsStreamForRead()))
                                {
                                    // Get the text from the stream
                                    using (var textReader = new JsonTextReader(streamReader))
                                    {
                                        // Used to get the data from JSON
                                        var serializer =
                                            new JsonSerializer {NullValueHandling = NullValueHandling.Ignore};
                                        // Return the data
                                        return serializer.Deserialize<T>(textReader);
                                    }
                                }
                            }
                        }
                    }
                });
            }
            catch (TaskCanceledException)
            {
                throw new SoundByteException(resources.GetString("HttpError_Header"),
                    resources.GetString("HttpError_TaskCancel"), "\uE007");
            }
            catch (JsonSerializationException)
            {
                throw new SoundByteException(resources.GetString("HttpError_Header"),
                    resources.GetString("HttpError_JsonError"), "\uEB63");
            }
            catch (Exception ex)
            {
                throw new SoundByteException(resources.GetString("GeneralError_Header"),
                    string.Format(resources.GetString("GeneralError_Content"), ex.Message), "\uE007");
            }
        }

        public async Task<string> GridEntertainmentSoundByteGetPlaybackKey()
        {
            try
            {
                using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                {
                    var key = await client.GetStringAsync(
                        new Uri("https://gridentertainment.net/api/soundbyte/playback-key"));
                    key = key.Trim('"');
                    return key;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ApiCheck(string url)
        {
            try
            {
                // Create the client
                using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                {
                    // No Auth for this
                    client.DefaultRequestHeaders.Authorization = null;

                    using (var webRequest = await client.GetAsync(new Uri(Uri.EscapeUriString(url))))
                    {
                        return webRequest.IsSuccessStatusCode;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     This method fetches a generic url and returns an object. Note:
        ///     you must provide a valid uri object. If calling the soundcloud api,
        ///     use GetAsync(string endpoint).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="location">The uri location</param>
        /// <param name="optionalParams">A list of any optional params to send in the URI</param>
        /// <returns>The object you specified</returns>
        public async Task<T> GetAsync<T>(Uri location, Dictionary<string, string> optionalParams = null)
        {
            // Get a string of the request URI.
            var requestUri = location.ToString();


            // Check that there are optional params then loop through all 
            // the params and add them onto the request URL
            if (optionalParams != null)
                requestUri = optionalParams
                    .Where(param => !string.IsNullOrEmpty(param.Key) && !string.IsNullOrEmpty(param.Value))
                    .Aggregate(requestUri, (current, param) => current + "&" + param.Key + "=" + param.Value);

            // Get the resource loader
            var resources = ResourceLoader.GetForViewIndependentUse();

            try
            {
                return await Task.Run(async () =>
                {
                    // Create the client
                    using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                    {
                        // Setup the request readers for user agent
                        // and requested data type.
                        client.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("SoundByte",
                            Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." +
                            Package.Current.Id.Version.Build));
                        client.DefaultRequestHeaders.Accept.Add(
                            new HttpMediaTypeWithQualityHeaderValue("application/json"));

                        // escape the url
                        var escapedUri = new Uri(Uri.EscapeUriString(requestUri));

                        // Get the URL
                        using (var webRequest = await client.GetAsync(escapedUri))
                        {
                            // Throw exception if the request failed
                            if (webRequest.StatusCode != HttpStatusCode.Ok)
                                throw new SoundByteException("Connection Error", webRequest.ReasonPhrase, "\uEB63");

                            // Get the body of the request as a stream
                            using (var webStream = await webRequest.Content.ReadAsInputStreamAsync())
                            {
                                // Read the stream
                                using (var streamReader = new StreamReader(webStream.AsStreamForRead()))
                                {
                                    // Get the text from the stream
                                    using (var textReader = new JsonTextReader(streamReader))
                                    {
                                        // Used to get the data from JSON
                                        var serializer =
                                            new JsonSerializer {NullValueHandling = NullValueHandling.Ignore};
                                        // Return the data
                                        return serializer.Deserialize<T>(textReader);
                                    }
                                }
                            }
                        }
                    }
                });
            }
            catch (TaskCanceledException)
            {
                throw new SoundByteException(resources.GetString("HttpError_Header"),
                    resources.GetString("HttpError_TaskCancel"), "\uE007");
            }
            catch (JsonSerializationException)
            {
                throw new SoundByteException(resources.GetString("HttpError_Header"),
                    resources.GetString("HttpError_JsonError"), "\uEB63");
            }
            catch (Exception ex)
            {
                throw new SoundByteException(resources.GetString("GeneralError_Header"),
                    string.Format(resources.GetString("GeneralError_Content"), ex.Message), "\uE007");
            }
        }

        /// <summary>
        ///     A wrapper function moving the service call to the
        ///     front of the function. This should be used for all
        ///     future code.
        /// </summary>
        /// <typeparam name="T">The object type we will serialize</typeparam>
        /// <param name="service">The service that we want to connect to</param>
        /// <param name="endpoint">The service endpoint that we want</param>
        /// <param name="optionalParams">A list of any optional params to send in the URI</param>
        /// <param name="useV2Api">Use the v2 SoundCloud API</param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(ServiceType service, string endpoint,
            Dictionary<string, string> optionalParams = null, bool useV2Api = false)
        {
            return await GetAsync<T>(endpoint, optionalParams, useV2Api, service);
        }

        /// <summary>
        ///     Fetches an object from the soundcloud api and returns it back
        ///     to the user. Note: This class will return a SoundByteException
        ///     if anything goes wrong. Make sure to handle this exception.
        /// </summary>
        /// <typeparam name="T">The object type we will serialize</typeparam>
        /// <param name="endpoint">SoundClouud API endpoint to contact</param>
        /// <param name="optionalParams">A list of any optional params to send in the URI</param>
        /// <param name="useV2Api">If true, the v2 version of the soundcloud API will be contacted instead.</param>
        /// <param name="service">The service that we want to connect to</param>
        /// <returns>The object you specified</returns>
        public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> optionalParams = null,
            bool useV2Api = false, ServiceType service = ServiceType.SoundCloud)
        {
            // Strip out the / infront of the endpoint if it exists
            endpoint = endpoint.TrimStart('/');

            // Start building the request URL
            var requestUri = string.Empty;

            switch (service)
            {
                case ServiceType.SoundCloud:
                    requestUri = useV2Api
                        ? $"https://api-v2.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}"
                        : $"https://api.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}";
                    break;
                case ServiceType.Fanburst:
                    requestUri =
                        $"https://api.fanburst.com/{endpoint}?client_id={ApiKeyService.FanburstClientId}&client_secret={ApiKeyService.FanburstClientSecret}";
                    break;
            }

            // Check that there are optional params then loop through all 
            // the params and add them onto the request URL
            if (optionalParams != null)
                requestUri = optionalParams
                    .Where(param => !string.IsNullOrEmpty(param.Key) && !string.IsNullOrEmpty(param.Value))
                    .Aggregate(requestUri, (current, param) => current + "&" + param.Key + "=" + param.Value);

            // Get the resource loader
            var resources = ResourceLoader.GetForViewIndependentUse();

            try
            {
                return await Task.Run(async () =>
                {
                    // Create the client
                    using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                    {
                        // Setup the request readers for user agent
                        // and requested data type.
                        client.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("SoundByte",
                            Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." +
                            Package.Current.Id.Version.Build));
                        client.DefaultRequestHeaders.Accept.Add(
                            new HttpMediaTypeWithQualityHeaderValue("application/json"));

                        // Auth headers for when the user is logged in
                        // Different for each service
                        switch (service)
                        {
                            case ServiceType.SoundCloud:
                                if (IsSoundCloudAccountConnected)
                                    client.DefaultRequestHeaders.Authorization =
                                        new HttpCredentialsHeaderValue("OAuth", SoundCloudToken.AccessToken);
                                break;

                            case ServiceType.Fanburst:
                                if (IsFanBurstAccountConnected)
                                    client.DefaultRequestHeaders.Authorization =
                                        new HttpCredentialsHeaderValue("OAuth", FanburstToken.AccessToken);
                                break;
                        }

                        // escape the url
                        var escapedUri = new Uri(Uri.EscapeUriString(requestUri));


                        // Get the URL
                        using (var webRequest = await client.GetAsync(escapedUri))
                        {
                            // Throw exception if the request failed
                            if (webRequest.StatusCode != HttpStatusCode.Ok)
                                throw new SoundByteException("Connection Error", webRequest.ReasonPhrase, "\uEB63");

                            // Get the body of the request as a stream
                            using (var webStream = await webRequest.Content.ReadAsInputStreamAsync())
                            {
                                // Read the stream
                                using (var streamReader = new StreamReader(webStream.AsStreamForRead()))
                                {
                                    // Get the text from the stream
                                    using (var textReader = new JsonTextReader(streamReader))
                                    {
                                        // Used to get the data from JSON
                                        var serializer =
                                            new JsonSerializer {NullValueHandling = NullValueHandling.Ignore};
                                        // Return the data
                                        return serializer.Deserialize<T>(textReader);
                                    }
                                }
                            }
                        }
                    }
                });
            }
            catch (TaskCanceledException)
            {
                throw new SoundByteException(resources.GetString("HttpError_Header"),
                    resources.GetString("HttpError_TaskCancel"), "\uE007");
            }
            catch (JsonSerializationException)
            {
                throw new SoundByteException(resources.GetString("HttpError_Header"),
                    resources.GetString("HttpError_JsonError"), "\uEB63");
            }
            catch (Exception ex)
            {
                throw new SoundByteException(resources.GetString("GeneralError_Header"),
                    string.Format(resources.GetString("GeneralError_Content"), ex.Message), "\uE007");
            }
        }

        /// <summary>
        ///     Attempts to delete an object from the soundcloud
        ///     api and returns the result.
        /// </summary>
        /// <param name="endpoint">The endpoint to delete from</param>
        /// <param name="useV2Api">Should we try deleting from the v2 api</param>
        /// <returns>If the delete was successful</returns>
        public async Task<bool> DeleteAsync(string endpoint, bool useV2Api = false)
        {
            // Strip out the / infront of the endpoint if it exists
            endpoint = endpoint.TrimStart('/');

            // Start building the request URL
            var requestUri = useV2Api
                ? $"https://api-v2.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}"
                : $"https://api.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}";

            try
            {
                return await Task.Run(async () =>
                {
                    // Create the client
                    using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                    {
                        // Setup the request readers for user agent
                        // and requested data type.
                        client.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("SoundByte",
                            Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." +
                            Package.Current.Id.Version.Build));
                        client.DefaultRequestHeaders.Accept.Add(
                            new HttpMediaTypeWithQualityHeaderValue("application/json"));

                        // Auth headers for when the user is logged in
                        if (IsSoundCloudAccountConnected)
                            client.DefaultRequestHeaders.Authorization =
                                new HttpCredentialsHeaderValue("OAuth", SoundCloudToken.AccessToken);

                        // escape the url
                        var escapedUri = new Uri(Uri.EscapeUriString(requestUri));

                        // Get the URL
                        using (var webRequest = await client.DeleteAsync(escapedUri))
                        {
                            // Return if successful
                            return webRequest.StatusCode == HttpStatusCode.Ok;
                        }
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Checks to see if an items exists at the specified endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint we are checking</param>
        /// <param name="useV2Api">Should we try checking the v2 api</param>
        /// <returns>If the object exists</returns>
        public async Task<bool> ExistsAsync(string endpoint, bool useV2Api = false)
        {
            // Strip out the / infront of the endpoint if it exists
            endpoint = endpoint.TrimStart('/');

            // Start building the request URL
            var requestUri = useV2Api
                ? $"https://api-v2.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}"
                : $"https://api.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}";

            try
            {
                return await Task.Run(async () =>
                {
                    // Create the client
                    using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                    {
                        // Setup the request readers for user agent
                        // and requested data type.
                        client.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("SoundByte",
                            Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." +
                            Package.Current.Id.Version.Build));
                        client.DefaultRequestHeaders.Accept.Add(
                            new HttpMediaTypeWithQualityHeaderValue("application/json"));

                        // Auth headers for when the user is logged in
                        if (IsSoundCloudAccountConnected)
                        {
                            client.DefaultRequestHeaders.Authorization =
                                new HttpCredentialsHeaderValue("OAuth", SoundCloudToken.AccessToken);
                            requestUri += "&oauth_token=" + SoundCloudToken.AccessToken;
                        }

                        // escape the url
                        var escapedUri = new Uri(Uri.EscapeUriString(requestUri));

                        // Get the URL
                        using (var webRequest = await client.GetAsync(escapedUri))
                        {
                            // lol, why soundcloud. why do I have to do this??
                            if (webRequest.StatusCode == HttpStatusCode.Unauthorized)
                                return true;

                            // Return if the resource exists
                            return webRequest.IsSuccessStatusCode;
                        }
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Puts an object into the soundcloud API.
        /// </summary>
        /// <param name="endpoint">The endpoint we are putting the item into</param>
        /// <param name="bodyContent">The content we want to put</param>
        /// <param name="useV2Api">Should we try putting to the v2 api</param>
        /// <returns>If the task was successful</returns>
        public async Task<bool> PutAsync(string endpoint, HttpStringContent bodyContent = null, bool useV2Api = false)
        {
            // Strip out the / infront of the endpoint if it exists
            endpoint = endpoint.TrimStart('/');

            // Start building the request URL
            var requestUri = useV2Api
                ? $"https://api-v2.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}"
                : $"https://api.soundcloud.com/{endpoint}?client_id={ApiKeyService.SoundCloudClientId}&client_secret={ApiKeyService.SoundCloudClientSecret}";

            try
            {
                return await Task.Run(async () =>
                {
                    // Create the client
                    using (var client = new HttpClient(new HttpBaseProtocolFilter {AutomaticDecompression = true}))
                    {
                        // Setup the request readers for user agent
                        // and requested data type.
                        client.DefaultRequestHeaders.UserAgent.Add(new HttpProductInfoHeaderValue("SoundByte",
                            Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." +
                            Package.Current.Id.Version.Build));
                        client.DefaultRequestHeaders.Accept.Add(
                            new HttpMediaTypeWithQualityHeaderValue("application/json"));

                        // Auth headers for when the user is logged in
                        if (IsSoundCloudAccountConnected)
                            client.DefaultRequestHeaders.Authorization =
                                new HttpCredentialsHeaderValue("OAuth", SoundCloudToken.AccessToken);

                        // escape the url
                        var escapedUri = new Uri(Uri.EscapeUriString(requestUri));

                        // Full the body content if it is null
                        if (bodyContent == null)
                            bodyContent = new HttpStringContent(string.Empty);

                        // Get the URL
                        using (var webRequest = await client.PutAsync(escapedUri, bodyContent))
                        {
                            // Return if tsuccessful
                            return webRequest.IsSuccessStatusCode;
                        }
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}