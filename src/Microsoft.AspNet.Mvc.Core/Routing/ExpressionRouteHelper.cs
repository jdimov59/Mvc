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
                    // method call is not valid because it is static
                    throw new InvalidOperationException(); // TODO: message from resource, test exceptions
                }

                // TODO: extract methods
                var methodInfo = methodCallExpression.Method;

                var arguments = methodCallExpression.Arguments.ToArray();
                var methodParameterNames = methodInfo.GetParameters().Select(p => p.Name).ToArray();

                var additionalRouteValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                
                // find controller action descriptor from the provider with the same extracted method info
                // this search is potentially slow, so it is cached after the first lookup
                var controllerActionDescriptor = _controllerActionDescriptorCache.GetOrAdd(methodInfo, _ =>
                {
                    var foundControllerActionDescriptor = actionDescriptorsCollectionProvider
                        .ActionDescriptors
                        .Items
                        .OfType<ControllerActionDescriptor>()
                        .FirstOrDefault(ca => ca.MethodInfo == methodInfo);

                    if (foundControllerActionDescriptor == null)
                    {
                        throw new InvalidOperationException(); // TODO: message from resource
                    }

                    return foundControllerActionDescriptor;
                });

                var parameterDescriptors = controllerActionDescriptor
                    .Parameters
                    .Where(p => p.BindingInfo != null)
                    .ToDictionary(p => p.Name, p => p.BindingInfo.BinderModelName);

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
                        // expression of type c => c.Action({const}) - value can be extracted without compiling
                        value = ((ConstantExpression)expressionArgument).Value;
                    }
                    else
                    {
                        // expresion needs compiling because it is not of constant type
                        var convertExpression = Expression.Convert(expressionArgument, typeof(object));
                        value = Expression.Lambda<Func<object>>(convertExpression).Compile().Invoke();
                    }

                    // we are interested only in not null route values 
                    if (value != null)
                    {
                        additionalRouteValues[methodParameterName] = value;
                    }
                }

                var controllerName = controllerActionDescriptor.ControllerName;
                var actionName = controllerActionDescriptor.Name;

                // if there is a route constraint with specific expected value, add it to the result
                var routeConstraints = controllerActionDescriptor.RouteConstraints;
                foreach (var routeConstraint in routeConstraints)
                {
                    var routeKey = routeConstraint.RouteKey;
                    var routeValue = routeConstraint.RouteValue;

                    if (routeValue != string.Empty)
                    {
                        // override the 'default' values, if they are found
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

            // expression is invalid because it is not method call
            throw new InvalidOperationException(); // TODO: message resource
        }
    }
}
