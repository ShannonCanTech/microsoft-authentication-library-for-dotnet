﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2.Throttling;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Unit;
using NSubstitute;

namespace Microsoft.Identity.Test.Common
{
    internal static class TestCommon
    {
        public static void ResetInternalStaticCaches()
        {
            // This initializes the classes so that the statics inside them are fully initialized, and clears any cached content in them.
            new InstanceDiscoveryManager(
                Substitute.For<IHttpManager>(),
                true, null, null);
            new AuthorityResolutionManager(true);
            SingletonThrottlingManager.GetInstance().ResetCache();
        }

        public static object GetPropValue(object src, string propName)
        {
            object result = null;
            try
            {
                result = src.GetType().GetProperty(propName).GetValue(src, null);
            }
            catch
            {
                Console.WriteLine($"Property with name {propName}");
            }

            return result;
        }

        public static IServiceBundle CreateServiceBundleWithCustomHttpManager(
            IHttpManager httpManager,
            TelemetryCallback telemetryCallback = null,
            LogCallback logCallback = null,
            string authority = ClientApplicationBase.DefaultAuthority,
            bool isExtendedTokenLifetimeEnabled = false,
            bool enablePiiLogging = false,
            string clientId = TestConstants.ClientId,
            bool clearCaches = true,
            bool validateAuthority = true,
            bool isLegacyCacheEnabled = true)
        {
            
            var appConfig = new ApplicationConfiguration()
            {
                ClientId = clientId,
                HttpManager = httpManager,
                RedirectUri = PlatformProxyFactory.CreatePlatformProxy(null).GetDefaultRedirectUri(clientId),
                TelemetryCallback = telemetryCallback,
                LoggingCallback = logCallback,
                LogLevel = LogLevel.Verbose,
                EnablePiiLogging = enablePiiLogging,
                IsExtendedTokenLifetimeEnabled = isExtendedTokenLifetimeEnabled,
                AuthorityInfo = AuthorityInfo.FromAuthorityUri(authority, validateAuthority),
                LegacyCacheCompatibilityEnabled = isLegacyCacheEnabled
            };            
            return new ServiceBundle(appConfig, clearCaches);
        }

        public static IServiceBundle CreateDefaultServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null);
        }

        public static IServiceBundle CreateDefaultAdfsServiceBundle()
        {
            return CreateServiceBundleWithCustomHttpManager(null, authority: TestConstants.OnPremiseAuthority);
        }

        public static AuthenticationRequestParameters CreateAuthenticationRequestParameters(
            IServiceBundle serviceBundle,
            Authority authority = null,
            HashSet<string> scopes = null,
            RequestContext requestContext = null)
        {
            var commonParameters = new AcquireTokenCommonParameters
            {
                Scopes = scopes ?? TestConstants.s_scope,
            };

            authority = authority ?? Authority.CreateAuthority(TestConstants.AuthorityTestTenant);
            requestContext = requestContext ?? new RequestContext(serviceBundle, Guid.NewGuid())
            {
                ApiEvent = new Client.TelemetryCore.Internal.Events.ApiEvent(
                    serviceBundle.ApplicationLogger,
                    serviceBundle.PlatformProxy.CryptographyManager,
                    Guid.NewGuid().ToString())
            };

            return new AuthenticationRequestParameters(
                serviceBundle,
                new TokenCache(serviceBundle, false),
                commonParameters,
                requestContext,
                authority)
            {                
            };
        }

        public static KeyValuePair<string, IEnumerable<string>> GetCcsHeaderFromSnifferFactory(HttpSnifferClientFactory factory)
        {
            if (factory.RequestsAndResponses.Any())
            {
                var (req, res) = factory.RequestsAndResponses.Single(x => x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token") &&
                x.Item2.StatusCode == HttpStatusCode.OK);

                return req.Headers.Single(h => h.Key == Constants.CcsRoutingHintHeader);
            }

            throw new MsalClientException("Could not find CCS Header in sniffer factory.");
        }
    }
}
