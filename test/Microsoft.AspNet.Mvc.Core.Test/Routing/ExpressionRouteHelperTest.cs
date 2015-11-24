// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Core.Routing;
using Microsoft.AspNet.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.Routing
{
    public class ExpressionRouteHelperTest
    {
        [Theory]
        [MemberData(nameof(NormalActionsWithNoParametersData))]
        public void Resolve_ControllerAndActionWithoutParameters_ControllerAndActionNameAreResolved(
            Expression<Action<NormalController>> action, string controllerName, string actionName)
        {
            // Arrange
            var actionDescriptorsCollectionProvider = CreateActionDescriptorsCollectionProvider();

            // Act
            var result = ExpressionRouteHelper.Resolve(
                action,
                actionDescriptorsCollectionProvider);

            // Assert
            Assert.Equal(controllerName, result.ControllerName);
            Assert.Equal(actionName, result.ActionName);
            Assert.Empty(result.RouteValues);
        }

        [Theory]
        [MemberData(nameof(NormalActionssWithParametersData))]
        public void Resolve_ControllerAndActionWithPrimitiveParameters_ControllerActionNameAndParametersAreResolved(
            Expression<Action<NormalController>> action, string controllerName, string actionName, IDictionary<string, object> routeValues)
        {
            // Arrange
            var actionDescriptorsCollectionProvider = CreateActionDescriptorsCollectionProvider();

            // Act
            var result = ExpressionRouteHelper.Resolve(
                action,
                actionDescriptorsCollectionProvider);

            // Assert
            Assert.Equal(controllerName, result.ControllerName);
            Assert.Equal(actionName, result.ActionName);
            Assert.Equal(routeValues.Count, result.RouteValues.Count);

            foreach (var routeValue in routeValues)
            {
                Assert.True(result.RouteValues.ContainsKey(routeValue.Key));
                Assert.Equal(routeValue.Value, result.RouteValues[routeValue.Key]);
            }
        }

        [Fact]
        public void Resolve_ControllerAndActionWithObjectParameters_ControllerActionNameAndParametersAreResolved()
        {
            // Arrange
            var actionDescriptorsCollectionProvider = CreateActionDescriptorsCollectionProvider();

            // Act
            var result = ExpressionRouteHelper.Resolve<NormalController>(
                c => c.ActionWithMultipleParameters(1, "string", new RequestModel { Integer = 1, String = "Text" }),
                actionDescriptorsCollectionProvider);

            // Assert
            Assert.Equal("Normal", result.ControllerName);
            Assert.Equal("ActionWithMultipleParameters", result.ActionName);
            Assert.Equal(3, result.RouteValues.Count);
            Assert.Equal(1, result.RouteValues["id"]);
            Assert.Equal("string", result.RouteValues["text"]);
            Assert.IsAssignableFrom<RequestModel>(result.RouteValues["model"]);

            var model = (RequestModel)result.RouteValues["model"];
            Assert.Equal(1, model.Integer);
            Assert.Equal("Text", model.String);
        }

        public static TheoryData<Expression<Action<NormalController>>, string, string> NormalActionsWithNoParametersData
        {
            get
            {
                var data = new TheoryData<Expression<Action<NormalController>>, string, string>();

                const string controllerName = "Normal";
                data.Add(c => c.ActionWithoutParameters(), controllerName, "ActionWithoutParameters");
                data.Add(c => c.ActionWithOverloads(), controllerName, "ActionWithOverloads");
                data.Add(c => c.VoidAction(), controllerName, "VoidAction");
                data.Add(c => c.ActionWithChangedName(), controllerName, "AnotherName");

                return data;
            }
        }

        public static TheoryData<
            Expression<Action<NormalController>>,
            string,
            string,
            IDictionary<string, object>> NormalActionssWithParametersData
        {
            get
            {
                var data = new TheoryData<Expression<Action<NormalController>>, string, string, IDictionary<string, object>>();

                const string controllerName = "Normal";
                data.Add(
                    c => c.ActionWithOverloads(1),
                    controllerName,
                    "ActionWithOverloads",
                    new Dictionary<string, object> { ["id"] = 1 });

                data.Add(
                    c => c.ActionWithMultipleParameters(1, "string", null),
                    controllerName,
                    "ActionWithMultipleParameters",
                    new Dictionary<string, object> { ["id"] = 1, ["text"] = "string" });

                return data;
            }
        }

        private IActionDescriptorsCollectionProvider CreateActionDescriptorsCollectionProvider()
        {
            // run the full controller and action model building 
            // in order to simulate the default MVC behavior
            var controllerTypes = typeof(ExpressionRouteHelperTest)
                .GetNestedTypes()
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

        public class RequestModel
        {
            public int Integer { get; set; }

            public string String { get; set; }
        }
        
        public class NormalController : Controller
        {
            public IActionResult ActionWithoutParameters()
            {
                return null;
            }

            public IActionResult ActionWithMultipleParameters(int id, string text, RequestModel model)
            {
                return null;
            }

            public IActionResult ActionWithOverloads()
            {
                return null;
            }

            public IActionResult ActionWithOverloads(int id)
            {
                return null;
            }

            [ActionName("AnotherName")]
            public IActionResult ActionWithChangedName()
            {
                return null;
            }

            public void VoidAction()
            {
            }
        }
    }
}
