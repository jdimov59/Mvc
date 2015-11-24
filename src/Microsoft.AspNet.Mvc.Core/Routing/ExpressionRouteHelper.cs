// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Controllers;

namespace Microsoft.AspNet.Mvc.Core.Routing
{
    public static class ExpressionRouteHelper
    {
        private static readonly ConcurrentDictionary<MethodInfo, ControllerActionDescriptor> _controllerActionDescriptorCache =
            new ConcurrentDictionary<MethodInfo, ControllerActionDescriptor>();

        public static ExpressionRouteValues Resolve<TController>(
            Expression<Action<TController>> expression,
            IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (actionDescriptorsCollectionProvider == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptorsCollectionProvider));
            }

            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression != null)
            {
                var controllerType = methodCallExpression.Object?.Type;
                if (controllerType == null)
                {
                    throw new InvalidOperationException(); // TODO: message from resource
                }

                var methodInfo = methodCallExpression.Method; // TODO: ActionName attribute?

                var arguments = methodCallExpression.Arguments.ToArray();
                var methodParameterNames = methodInfo.GetParameters().Select(p => p.Name).ToArray();

                var additionalRouteValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < arguments.Length; i++)
                {
                    var methodParameter = methodParameterNames[i];
                    var expressionArgument = arguments[i];

                    if (expressionArgument.NodeType == ExpressionType.Constant)
                    {
                        var value = ((ConstantExpression)expressionArgument).Value;
                        if (value != null)
                        {
                            additionalRouteValues[methodParameter] = value; // TODO: parameter bindings?
                        }
                    }
                    else
                    {
                        var convertExpression = Expression.Convert(expressionArgument, typeof(object));
                        var value = Expression.Lambda<Func<object>>(convertExpression).Compile().Invoke();
                        additionalRouteValues[methodParameter] = value; // TODO: parameter bindings?
                    }
                }

                var controllerActionDescriptor = _controllerActionDescriptorCache.GetOrAdd(methodInfo, _ =>
                {
                    return actionDescriptorsCollectionProvider
                        .ActionDescriptors
                        .Items
                        .OfType<ControllerActionDescriptor>()
                        .FirstOrDefault(ca => ca.MethodInfo == methodInfo);
                });

                var defaultRouteValues = controllerActionDescriptor.RouteValueDefaults;
                foreach (var routeValue in defaultRouteValues) // TODO: maybe foreach is not needed, only area
                {
                    var key = routeValue.Key;
                    if (!additionalRouteValues.ContainsKey(key))
                    {
                        additionalRouteValues[key] = defaultRouteValues[key];
                    }
                }
                
                return new ExpressionRouteValues
                {
                    ControllerName = controllerActionDescriptor.ControllerName,
                    ActionName = controllerActionDescriptor.Name,
                    RouteValues = additionalRouteValues
                };
            }

            throw new InvalidOperationException(); // TODO: message resource
        }
    }
}
