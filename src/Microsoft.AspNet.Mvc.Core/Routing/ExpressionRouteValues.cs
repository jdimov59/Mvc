namespace Microsoft.AspNet.Mvc.Core.Routing
{
    using System.Collections.Generic;
    
    public class ExpressionRouteValues
    {
        public string ControllerName { get; set; }

        public string ActionName { get; set; }

        public IDictionary<string, object> RouteValues { get; set; }
    }
}
