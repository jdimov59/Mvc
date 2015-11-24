namespace Microsoft.AspNet.Mvc.Core.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    public static class ExpressionRouteResolver
    {
        public static ExpressionRouteValues Resolve<TController>(Expression<Action<TController>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var methodCallExpression = expression.Body as MethodCallExpression;
            if (methodCallExpression != null)
            {
                var argumentRouteValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                var controllerType = methodCallExpression.Object?.Type; // TODO: null? get actual controller name and cache, area?
                var methodInfo = methodCallExpression.Method; // TODO: get actual action name and cache
                var arguments = methodCallExpression.Arguments.ToArray();

                var methodParameterNames = methodInfo.GetParameters().Select(p => p.Name).ToArray();

                for (var i = 0; i < arguments.Length; i++)
                {
                    var methodParameter = methodParameterNames[i];
                    var expressionArgument = arguments[i];

                    if (expressionArgument.NodeType == ExpressionType.Constant)
                    {
                        var value = ((ConstantExpression)expressionArgument).Value;
                        if (value != null)
                        {
                            argumentRouteValues[methodParameter] = value;
                        }
                    }
                    else
                    {
                        var convertExpression = Expression.Convert(expressionArgument, typeof(object));
                        var value = Expression.Lambda<Func<object>>(convertExpression).Compile().Invoke();
                        argumentRouteValues[methodParameter] = value;
                    }
                }

                return new ExpressionRouteValues
                {
                    ControllerName = null, // TODO: add
                    ActionName = null, // TODO: add,
                    RouteValues = argumentRouteValues
                };
            }

            // TODO: throw
            return null;
        }
    }
}
