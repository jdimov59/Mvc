// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Core.Routing;
using Microsoft.AspNet.Mvc.Infrastructure;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.Routing
{
    public class ExpressionRouteHelperTest
    {
        [Fact]
        public void Resolve_ControllerAndActionWithoutParametersAndFilters_ControllerAndActionNamesAreDefault()
        {
            // Arrange
            var actionDescriptorsCollectionProvider = CreateActionDescriptorsCollectionProvider();

            // Act
            var result = ExpressionRouteHelper.Resolve<NormalController>(
                c => c.ActionWithoutParameters(),
                actionDescriptorsCollectionProvider);

            // Assert
            Assert.Equal("Normal", result.ControllerName);
            Assert.Equal("ActionWithoutParameters", result.ActionName);
            Assert.Empty(result.RouteValues);
        }

        private IActionDescriptorsCollectionProvider CreateActionDescriptorsCollectionProvider()
        {
            // run the full controller and action model building 
            // in order to simulate the default MVC behavior
            var controllerTypes = typeof(ExpressionRouteHelperTest)
                .GetNestedTypes(BindingFlags.NonPublic)
                .Select(t => t.GetTypeInfo())
                .ToList();

            var options = new TestOptionsManager<MvcOptions>();

            var controllerTypeProvider = new StaticControllerTypeProvider(controllerTypes);
            var modelProvider = new DefaultApplicationModelProvider(options);

            var provider = new ControllerActionDescriptorProvider(
                controllerTypeProvider,
                new[] { modelProvider },
                options);

            var serviceContainer = new ServiceContainer();
            var list = new List<IActionDescriptorProvider>()
            {
                provider,
            };

            serviceContainer.AddService(typeof(IEnumerable<IActionDescriptorProvider>), list);

            var actionDescriptorCollectionProvider = new DefaultActionDescriptorsCollectionProvider(serviceContainer);

            return actionDescriptorCollectionProvider;
        }

        private class NormalController : Controller
        {
            public IActionResult ActionWithoutParameters()
            {
                return null;
            }
        }
    }
}
