// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class QueryStringValueProviderFactoryTest
    {
        [Fact]
        public async Task GetValueProvider_ReturnsQueryStringValueProviderInstanceWithInvariantCulture()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.SetupGet(f => f.Query).Returns(Mock.Of<IQueryCollection>());
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Items).Returns(new Dictionary<object, object>());
            context.SetupGet(c => c.Request).Returns(request.Object);
            var factoryContext = new ValueProviderFactoryContext(
                context.Object,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            var factory = new QueryStringValueProviderFactory();

            // Act
            var result = await factory.GetValueProviderAsync(factoryContext);

            // Assert
            var valueProvider = Assert.IsType<QueryStringValueProvider>(result);
            Assert.Equal(CultureInfo.InvariantCulture, valueProvider.Culture);
        }
    }
}
