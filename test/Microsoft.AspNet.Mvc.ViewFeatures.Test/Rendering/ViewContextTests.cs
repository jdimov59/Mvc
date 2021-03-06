// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewContextTests
    {
        [Fact]
        public void SettingViewData_AlsoUpdatesViewBag()
        {
            // Arrange
            var originalViewData = new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider());
            var context = new ViewContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                view: Mock.Of<IView>(),
                viewData: originalViewData,
                tempData: new TempDataDictionary(new HttpContextAccessor(), Mock.Of<ITempDataProvider>()),
                writer: TextWriter.Null,
                htmlHelperOptions: new HtmlHelperOptions());
            var replacementViewData = new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider());

            // Act
            context.ViewBag.Hello = "goodbye";
            context.ViewData = replacementViewData;
            context.ViewBag.Another = "property";

            // Assert
            Assert.NotSame(originalViewData, context.ViewData);
            Assert.Same(replacementViewData, context.ViewData);
            Assert.Null(context.ViewBag.Hello);
            Assert.Equal("property", context.ViewBag.Another);
            Assert.Equal("property", context.ViewData["Another"]);
        }

        [Fact]
        public void CopyConstructor_CopiesExpectedProperties()
        {
            // Arrange
            var originalContext = new ViewContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
                view: Mock.Of<IView>(),
                viewData: new ViewDataDictionary(metadataProvider: new EmptyModelMetadataProvider()),
                tempData: new TempDataDictionary(new HttpContextAccessor(), Mock.Of<ITempDataProvider>()),
                writer: TextWriter.Null,
                htmlHelperOptions: new HtmlHelperOptions());
            var view = Mock.Of<IView>();
            var viewData = new ViewDataDictionary(originalContext.ViewData);
            var writer = new StringCollectionTextWriter(Encoding.UTF8);

            // Act
            var context = new ViewContext(originalContext, view, viewData, writer);

            // Assert
            Assert.Same(originalContext.ActionDescriptor, context.ActionDescriptor);
            Assert.Equal(originalContext.ClientValidationEnabled, context.ClientValidationEnabled);
            Assert.Same(originalContext.ExecutingFilePath, context.ExecutingFilePath);
            Assert.Same(originalContext.FormContext, context.FormContext);
            Assert.Equal(originalContext.Html5DateRenderingMode, context.Html5DateRenderingMode);
            Assert.Same(originalContext.HttpContext, context.HttpContext);
            Assert.Same(originalContext.ModelState, context.ModelState);
            Assert.Same(originalContext.RouteData, context.RouteData);
            Assert.Same(originalContext.TempData, context.TempData);
            Assert.Same(originalContext.ValidationMessageElement, context.ValidationMessageElement);
            Assert.Same(originalContext.ValidationSummaryMessageElement, context.ValidationSummaryMessageElement);

            Assert.Same(view, context.View);
            Assert.Same(viewData, context.ViewData);
            Assert.Same(writer, context.Writer);
        }
    }
}
