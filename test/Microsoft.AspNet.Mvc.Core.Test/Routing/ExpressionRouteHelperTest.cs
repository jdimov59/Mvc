// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.Infrastructure;
using Moq;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test.Routing
{
    public class ExpressionRouteHelperTest
    {
        [Fact]
        public void Test()
        {

        }

        public IActionDescriptorsCollectionProvider CreateActionDescriptorsCollectionProvider()
        {
            var actionDescriptorsCollectionProvider = new Mock<IActionDescriptorsCollectionProvider>();
            
            var applicationModel = new ApplicationModel();

            var controllerModel = new ControllerModel(typeof(NormalController).GetTypeInfo(), new List<object>());
            controllerModel.Application = applicationModel;

            var methodInfo = typeof(NormalController).GetMethod("ActionWithoutParameters");
            var actionModel = new ActionModel(methodInfo, new List<object>() { });
            actionModel.Controller = controllerModel;
            controllerModel.Actions.Add(actionModel);
            
            var descriptors = ControllerActionDescriptorBuilder.Build(applicationModel);

            return actionDescriptorsCollectionProvider.Object;
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
