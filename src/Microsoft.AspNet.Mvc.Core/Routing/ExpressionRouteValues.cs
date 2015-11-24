// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Core.Routing
{
    using System.Collections.Generic;
    
    public class ExpressionRouteValues
    {
        public string Controller { get; set; }

        public string Action { get; set; }

        public IDictionary<string, object> RouteValues { get; set; }
    }
}
