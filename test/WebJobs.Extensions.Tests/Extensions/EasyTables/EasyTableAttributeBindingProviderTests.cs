// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.EasyTables;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Tests.EasyTables
{
    public class EasyTableAttributeBindingProviderTests
    {
        private JobHostConfiguration _jobConfig;
        private EasyTableConfiguration _easyTableConfig;

        public EasyTableAttributeBindingProviderTests()
        {
            _jobConfig = new JobHostConfiguration();
            _easyTableConfig = new EasyTableConfiguration
            {
                MobileAppUri = new Uri("http://someuri")
            };
        }

        public static IEnumerable<object[]> ValidBindings
        {
            get
            {
                var validParameters = EasyTableTestHelper.GetAllValidParameters().ToArray();
                var jobjectCollectorType = typeof(CommonCollectorBinding<JObject, EasyTableContext>);
                var pocoCollectorType = typeof(CommonCollectorBinding<TodoItem, EasyTableContext>);
                var itemBindingType = typeof(EasyTableItemBinding);
                var tableBindingType = typeof(EasyTableTableBinding);
                var queryBindingType = typeof(EasyTableQueryBinding);

                return new[]
                {
                    new object[] { validParameters[0], jobjectCollectorType },
                    new object[] { validParameters[1], pocoCollectorType },
                    new object[] { validParameters[2], jobjectCollectorType },
                    new object[] { validParameters[3], pocoCollectorType },
                    new object[] { validParameters[4], jobjectCollectorType },
                    new object[] { validParameters[5], pocoCollectorType },
                    new object[] { validParameters[6], jobjectCollectorType },
                    new object[] { validParameters[7], pocoCollectorType },
                    new object[] { validParameters[8], itemBindingType },
                    new object[] { validParameters[9], itemBindingType },
                    new object[] { validParameters[10], tableBindingType },
                    new object[] { validParameters[11], tableBindingType },
                    new object[] { validParameters[12], queryBindingType }
                };
            }
        }

        public static IEnumerable<object[]> ValidOutputProviders
        {
            get
            {
                var validParameters = EasyTableTestHelper.GetValidOutputParameters().ToArray();

                return new[]
                {
                    new object[] { validParameters[0], typeof(OutValueProvider<JObject>) },
                    new object[] { validParameters[1], typeof(OutValueProvider<TodoItem>) },
                    new object[] { validParameters[2], typeof(OutArrayValueProvider<JObject>) },
                    new object[] { validParameters[3], typeof(OutArrayValueProvider<TodoItem>) },
                    new object[] { validParameters[4], typeof(CommonAsyncCollectorValueProvider<IAsyncCollector<JObject>, JObject>) },
                    new object[] { validParameters[5], typeof(CommonAsyncCollectorValueProvider<IAsyncCollector<TodoItem>, TodoItem>) },
                    new object[] { validParameters[6], typeof(CommonAsyncCollectorValueProvider<ICollector<JObject>, JObject>) },
                    new object[] { validParameters[7], typeof(CommonAsyncCollectorValueProvider<ICollector<TodoItem>, TodoItem>) },
                };
            }
        }

        public static IEnumerable<object[]> InvalidBindings
        {
            get
            {
                var invalidParameters = typeof(EasyTableAttributeBindingProviderTests)
                    .GetMethod("GetInvalidBindings", BindingFlags.Instance | BindingFlags.NonPublic).GetParameters();

                return new[]
                {
                    new object[] { invalidParameters[0] },
                    new object[] { invalidParameters[1] },
                    new object[] { invalidParameters[2] },
                    new object[] { invalidParameters[3] },
                    new object[] { invalidParameters[4] },
                    new object[] { invalidParameters[5] },
                    new object[] { invalidParameters[6] },
                };
            }
        }

        [Theory]
        [MemberData("ValidBindings")]
        public async Task ValidParameter_Returns_CorrectBinding(ParameterInfo parameter, Type expectedBindingType)
        {
            // Arrange
            var provider = new EasyTableAttributeBindingProvider(_jobConfig, _easyTableConfig, _jobConfig.NameResolver);
            var context = new BindingProviderContext(parameter, null, CancellationToken.None);

            // Act
            IBinding binding = await provider.TryCreateAsync(context);

            // Assert
            Assert.Equal(expectedBindingType, binding.GetType());
        }

        [Theory]
        [MemberData("ValidOutputProviders")]
        public async Task ValidOutputParameter_Returns_CorrectValueProvider(ParameterInfo parameter, Type expectedBindingType)
        {
            // Note: this test is mostly testing the GenericBinder scenarios that EasyTable uses for output bindings. 
            // It should eventually make its way to those unit tests.

            // Arrange
            var provider = new EasyTableAttributeBindingProvider(_jobConfig, _easyTableConfig, _jobConfig.NameResolver);
            var context = new BindingProviderContext(parameter, null, CancellationToken.None);
            IBinding binding = await provider.TryCreateAsync(context);

            // Act
            IValueProvider valueProvider = await binding.BindAsync(null, null);

            // Assert
            Assert.Equal(expectedBindingType, valueProvider.GetType());
        }

        [Theory]
        [MemberData("InvalidBindings")]
        public async Task InvalidParameter_Returns_Null(ParameterInfo parameter)
        {
            // Arrange
            var provider = new EasyTableAttributeBindingProvider(_jobConfig, _easyTableConfig, _jobConfig.NameResolver);
            var context = new BindingProviderContext(parameter, null, CancellationToken.None);

            // Act
            IBinding binding = await provider.TryCreateAsync(context);

            // Assert
            Assert.Null(binding);
        }

        [Fact]
        public void CreateContext_ResolvesNames()
        {
            // Arrange
            var resolver = new TestNameResolver();
            resolver.Values.Add("MyTableName", "TestTable");
            resolver.Values.Add("MyId", "abc123");

            var attribute = new EasyTableAttribute
            {
                TableName = "%MyTableName%",
                Id = "%MyId%"
            };

            // Act
            var context = EasyTableAttributeBindingProvider.CreateContext(_easyTableConfig, attribute, resolver);

            // Assert
            Assert.Equal("TestTable", context.ResolvedTableName);
            Assert.Equal("abc123", context.ResolvedId);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "This is a test method used for generiating ParameterInfo via Reflection.")]
        private void GetInvalidBindings(
            [EasyTable] out NoId pocoOut,
            [EasyTable] out NoId[] pocoArrayOut,
            [EasyTable] IAsyncCollector<NoId> pocoAsyncCollector,
            [EasyTable] ICollector<NoId> pocoCollector,
            [EasyTable] NoId poco,
            [EasyTable] IMobileServiceTable<NoId> pocoTable,
            [EasyTable] IMobileServiceTableQuery<NoId> query)
        {
            pocoOut = null;
            pocoArrayOut = null;
        }
    }
}