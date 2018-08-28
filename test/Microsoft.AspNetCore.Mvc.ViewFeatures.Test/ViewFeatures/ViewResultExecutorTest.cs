// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class ViewResultExecutorTest
    {
        [Fact]
        public void FindView_UsesViewEngine_FromViewResult()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var viewName = "my-view";
            var viewEngine = new Mock<ICompositeViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.GetView(/*executingFilePath*/ null, viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound(viewName, Enumerable.Empty<string>()))
                .Verifiable();
            viewEngine
                .Setup(e => e.FindView(context, viewName, /*isMainPage*/ true))
                .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                .Verifiable();

            var viewResult = new ViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = viewName,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.Equal(viewName, viewEngineResult.ViewName);
            viewEngine.Verify();
        }

        [Fact]
        public void FindView_UsesActionDescriptorName_IfViewNameIsNull()
        {
            // Arrange
            var viewName = "some-view-name";
            var context = GetActionContext(viewName);
            var executor = GetViewExecutor();

            var viewResult = new ViewResult
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.Equal(viewName, viewEngineResult.ViewName);
        }

        [Fact]
        public void FindView_ReturnsExpectedNotFoundResult_WithGetViewLocations()
        {
            // Arrange
            var expectedLocations = new[] { "location1", "location2" };
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var viewName = "myview";
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", expectedLocations));
            viewEngine
                .Setup(e => e.FindView(context, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var result = executor.FindView(context, viewResult);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.View);
            Assert.Equal(expectedLocations, result.SearchedLocations);
        }

        [Fact]
        public void FindView_ReturnsExpectedNotFoundResult_WithFindViewLocations()
        {
            // Arrange
            var expectedLocations = new[] { "location1", "location2" };
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var viewName = "myview";
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()));
            viewEngine
                .Setup(e => e.FindView(context, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", expectedLocations));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var result = executor.FindView(context, viewResult);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.View);
            Assert.Equal(expectedLocations, result.SearchedLocations);
        }

        [Fact]
        public void FindView_ReturnsExpectedNotFoundResult_WithAllLocations()
        {
            // Arrange
            var expectedLocations = new[] { "location1", "location2", "location3", "location4" };
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var viewName = "myview";
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", new[] { "location1", "location2" }));
            viewEngine
                .Setup(e => e.FindView(context, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", new[] { "location3", "location4" }));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var result = executor.FindView(context, viewResult);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.View);
            Assert.Equal(expectedLocations, result.SearchedLocations);
        }

        [Fact]
        public void FindView_WritesDiagnostic_ViewFound()
        {
            // Arrange
            var diagnosticListener = new DiagnosticListener("Test");
            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            var context = GetActionContext();
            var executor = GetViewExecutor(diagnosticListener);

            var viewName = "myview";
            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.Equal(viewName, viewEngineResult.ViewName);

            Assert.NotNull(listener.ViewFound);
            Assert.NotNull(listener.ViewFound.ActionContext);
            Assert.NotNull(listener.ViewFound.Result);
            Assert.NotNull(listener.ViewFound.View);
            Assert.True(listener.ViewFound.IsMainPage);
            Assert.Equal("myview", listener.ViewFound.ViewName);
        }

        [Fact]
        public void FindView_WritesDiagnostic_ViewNotFound()
        {
            // Arrange
            var diagnosticListener = new DiagnosticListener("Test");
            var listener = new TestDiagnosticListener();
            diagnosticListener.SubscribeWithAdapter(listener);

            var context = GetActionContext();
            var executor = GetViewExecutor(diagnosticListener);

            var viewName = "myview";
            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.GetView(/*executingFilePath*/ null, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", Enumerable.Empty<string>()));
            viewEngine
                .Setup(e => e.FindView(context, "myview", /*isMainPage*/ true))
                .Returns(ViewEngineResult.NotFound("myview", new string[] { "location/myview" }));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            var viewEngineResult = executor.FindView(context, viewResult);

            // Assert
            Assert.False(viewEngineResult.Success);

            Assert.NotNull(listener.ViewNotFound);
            Assert.NotNull(listener.ViewNotFound.ActionContext);
            Assert.NotNull(listener.ViewNotFound.Result);
            Assert.Equal(new string[] { "location/myview" }, listener.ViewNotFound.SearchedLocations);
            Assert.Equal("myview", listener.ViewNotFound.ViewName);
        }

        [Fact]
        public async Task ExecuteAsync_UsesContentType_FromViewResult()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var contentType = "application/x-my-content-type";

            var viewResult = new ViewResult
            {
                ViewName = "my-view",
                ContentType = contentType,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await executor.ExecuteAsync(context, viewResult);

            // Assert
            Assert.Equal("application/x-my-content-type", context.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ExecuteAsync_UsesStatusCode_FromViewResult()
        {
            // Arrange
            var context = GetActionContext();
            var executor = GetViewExecutor();

            var contentType = MediaTypeHeaderValue.Parse("application/x-my-content-type");

            var viewResult = new ViewResult
            {
                ViewName = "my-view",
                StatusCode = 404,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await executor.ExecuteAsync(context, viewResult);

            // Assert
            Assert.Equal(404, context.HttpContext.Response.StatusCode);
        }

        private ActionContext GetActionContext(string actionName = null)
        {
            var routeData = new RouteData();
            routeData.Values["action"] = actionName;

            return new ActionContext(new DefaultHttpContext(), routeData, new ControllerActionDescriptor() { ActionName = actionName });
        }

        private ViewResultExecutor GetViewExecutor(DiagnosticListener diagnosticListener = null)
        {
            if (diagnosticListener == null)
            {
                diagnosticListener = new DiagnosticListener("Test");
            }

            var viewEngine = new Mock<IViewEngine>(MockBehavior.Strict);
            viewEngine
                .Setup(e => e.GetView(/*executingFilePath*/ null, It.IsAny<string>(), /*isMainPage*/ true))
                .Returns<string, string, bool>(
                    (path, name, partial) => ViewEngineResult.NotFound(name, Enumerable.Empty<string>()));
            viewEngine
                .Setup(e => e.FindView(It.IsAny<ActionContext>(), It.IsAny<string>(), /*isMainPage*/ true))
                .Returns<ActionContext, string, bool>(
                    (context, name, partial) => ViewEngineResult.Found(name, Mock.Of<IView>()));

            var options = Options.Create(new MvcViewOptions());
            options.Value.ViewEngines.Add(viewEngine.Object);

            var viewExecutor = new ViewResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                new CompositeViewEngine(options),
                new TempDataDictionaryFactory(new SessionStateTempDataProvider()),
                diagnosticListener,
                NullLoggerFactory.Instance,
                new EmptyModelMetadataProvider());

            return viewExecutor;
        }
    }
}
