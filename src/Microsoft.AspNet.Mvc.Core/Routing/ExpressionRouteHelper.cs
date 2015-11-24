﻿// Copyright (c) .NET Foundation. All rights reserved.
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
                    // Method call is not valid because it is static.
                    throw new InvalidOperationException(); // TODO: message from resource, test exceptions
                }
                
                var methodInfo = methodCallExpression.Method;

                // Find controller action descriptor from the provider with the same extracted method info.
                // This search is potentially slow, so it is cached after the first lookup.
                var controllerActionDescriptor = GetActionDescriptorFromCache(methodInfo, actionDescriptorsCollectionProvider);
                
                var controllerName = controllerActionDescriptor.ControllerName;
                var actionName = controllerActionDescriptor.Name;

                var additionalRouteValues = GetAdditionalRouteValues(methodInfo, methodCallExpression, controllerActionDescriptor);

                // If there is a route constraint with specific expected value, add it to the result.
                var routeConstraints = controllerActionDescriptor.RouteConstraints;
                foreach (var routeConstraint in routeConstraints)
                {
                    var routeKey = routeConstraint.RouteKey;
                    var routeValue = routeConstraint.RouteValue;

                    if (routeValue != string.Empty)
                    {
                        // Override the 'default' values, if they are found.
                        if (string.Equals(routeKey, "controller", StringComparison.OrdinalIgnoreCase))
                        {
                            controllerName = routeValue;
                        }
                        else if (string.Equals(routeKey, "action", StringComparison.OrdinalIgnoreCase))
                        {
                            actionName = routeValue;
                        }
                        else
                        {
                            additionalRouteValues[routeConstraint.RouteKey] = routeValue;
                        }
                    }
                }
                
                return new ExpressionRouteValues
                {
                    ControllerName = controllerName,
                    ActionName = actionName,
                    RouteValues = additionalRouteValues
                };
            }

            // Expression is invalid because it is not a method call.
            throw new InvalidOperationException(); // TODO: message from resource, test exceptions
        }

        private static ControllerActionDescriptor GetActionDescriptorFromCache(
            MethodInfo methodInfo,
            IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider)
        {
            return _controllerActionDescriptorCache.GetOrAdd(methodInfo, _ =>
            {
                var foundControllerActionDescriptor = actionDescriptorsCollectionProvider
                    .ActionDescriptors
                    .Items
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault(ca => ca.MethodInfo == methodInfo);

                if (foundControllerActionDescriptor == null)
                {
                    throw new InvalidOperationException(); // TODO: message from resource, test exceptions
                }

                return foundControllerActionDescriptor;
            });
        }

        private static IDictionary<string, object> GetAdditionalRouteValues(
            MethodInfo methodInfo,
            MethodCallExpression methodCallExpression,
            ControllerActionDescriptor controllerActionDescriptor)
        {
            var parameterDescriptors = controllerActionDescriptor
                    .Parameters
                    .Where(p => p.BindingInfo != null)
                    .ToDictionary(p => p.Name, p => p.BindingInfo.BinderModelName);

            var arguments = methodCallExpression.Arguments.ToArray();
            var methodParameterNames = methodInfo.GetParameters().Select(p => p.Name).ToArray();

            var additionalRouteValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < arguments.Length; i++)
            {
                var methodParameterName = methodParameterNames[i];
                if (parameterDescriptors.ContainsKey(methodParameterName))
                {
                    methodParameterName = parameterDescriptors[methodParameterName];
                }

                var expressionArgument = arguments[i];

                object value = null;
                if (expressionArgument.NodeType == ExpressionType.Constant)
                {
                    // Expression of type c => c.Action({const}) - value can be extracted without compiling.
                    value = ((ConstantExpression)expressionArgument).Value;
                }
                else
                {
                    // Expresion needs compiling because it is not of constant type.
                    var convertExpression = Expression.Convert(expressionArgument, typeof(object));
                    value = Expression.Lambda<Func<object>>(convertExpression).Compile().Invoke();
                }

                // We are interested only in not null route values.
                if (value != null)
                {
                    additionalRouteValues[methodParameterName] = value;
                }
            }

            return additionalRouteValues;
        }
    }
}
