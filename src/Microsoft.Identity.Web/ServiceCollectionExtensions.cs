﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TokenCache.Tests.Core")]

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Extensions for IServiceCollection for startup initialization of Web APIs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the token acquisition service.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>the service collection</returns>
        /// <example>
        /// This method is typically called from the Startup.ConfigureServices(IServiceCollection services)
        /// Note that the implementation of the token cache can be chosen separately.
        ///
        /// <code>
        /// // Token acquisition service and its cache implementation as a session cache
        /// services.AddTokenAcquisition()
        /// .AddDistributedMemoryCache()
        /// .AddSession()
        /// .AddSessionBasedTokenCache()
        ///  ;
        /// </code>
        /// </example>
        public static IServiceCollection AddTokenAcquisition(this IServiceCollection services)
        {
            // Token acquisition service
            services.AddScoped<ITokenAcquisition>(factory =>
            {
                var config = factory.GetRequiredService<IConfiguration>();
                var apptokencacheprovider = factory.GetService<IMsalAppTokenCacheProvider>();
                var usertokencacheprovider = factory.GetService<IMsalUserTokenCacheProvider>();

                return new TokenAcquisition(config, apptokencacheprovider, usertokencacheprovider);
            });
            return services;
        }
    }
}
