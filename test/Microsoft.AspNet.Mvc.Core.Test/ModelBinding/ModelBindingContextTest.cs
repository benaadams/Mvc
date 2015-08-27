// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ModelBindingContextTest
    {
        [Fact]
        public void CreateChildBindingContext()
        {
            // Arrange
            var originalBindingContext = new ModelBindingContext
            {
                ModelMetadata = new TestModelMetadataProvider().GetMetadataForType(typeof(object)),
                ModelName = "theName",
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider()
            };

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<object>().BindingDetails(d =>
                {
                    d.BindingSource = BindingSource.Custom;
                    d.BinderType = typeof(TestModelBinder);
                    d.BinderModelName = "custom";
                });

            var newModelMetadata = metadataProvider.GetMetadataForType(typeof(object));
            
            // Act
            var newBindingContext = ModelBindingContext.CreateChildBindingContext(
                originalBindingContext,
                newModelMetadata,
                fieldName: "parameter",
                modelName: "parameter",
                model: null);

            // Assert
            Assert.Same(newModelMetadata, newBindingContext.ModelMetadata);
            Assert.Same(newModelMetadata.BindingSource, newBindingContext.BindingSource);
            Assert.Same(newModelMetadata.BinderModelName, newBindingContext.BinderModelName);
            Assert.Same(newModelMetadata.BinderType, newBindingContext.BinderType);
            Assert.Equal("parameter", newBindingContext.ModelName);
            Assert.Same(originalBindingContext.OperationBindingContext, newBindingContext.OperationBindingContext);
            Assert.Same(originalBindingContext.ValueProvider, newBindingContext.ValueProvider);
        }

        [Fact]
        public void ModelTypeAreFedFromModelMetadata()
        {
            // Act
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int))
            };

            // Assert
            Assert.Equal(typeof(int), bindingContext.ModelType);
        }

        private class TestModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                throw new NotImplementedException();
            }
        }
    }
}
