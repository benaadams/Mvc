// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Contains the result of model binding.
    /// </summary>
    public struct ModelBindingResult : IEquatable<ModelBindingResult>
    {
        public static readonly ModelBindingResult NoResult = new ModelBindingResult();

        public static readonly Task<ModelBindingResult> NoResultAsync = Task.FromResult(NoResult);

        public static ModelBindingResult Failed(string key)
        {
            return new ModelBindingResult(key, model: null, isModelSet: false, validationNode: null);
        }

        public static Task<ModelBindingResult> FailedAsync(string key)
        {
            return Task.FromResult(Failed(key));
        }

        public static ModelBindingResult Success(string key, object model)
        {
            return Success(key, model, validationNode: null);
        }

        public static Task<ModelBindingResult> SuccessAsync(string key, object model)
        {
            return Task.FromResult(Success(key, model, validationNode: null));
        }

        public static ModelBindingResult Success(string key, object model, ModelValidationNode validationNode)
        {
            return new ModelBindingResult(key, model, isModelSet: true, validationNode: validationNode);
        }

        public static Task<ModelBindingResult> SuccessAsync(string key, object model, ModelValidationNode validationNode)
        {
            return Task.FromResult(Success(key, model, validationNode));
        }

        public ModelBindingResult(string key, object model, bool isModelSet, ModelValidationNode validationNode)
        {
            Key = key;
            Model = model;
            IsModelSet = isModelSet;
            ValidationNode = validationNode;
        }

        /// <summary>
        /// Gets the model associated with this context.
        /// </summary>
        public object Model { get; }

        /// <summary>
        /// <para>
        /// Gets the model name which was used to bind the model.
        /// </para>
        /// <para>
        /// This property can be used during validation to add model state for a bound model.
        /// </para>
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// <para>
        /// Gets a value indicating whether or not the <see cref="Model"/> value has been set.
        /// </para>
        /// <para>
        /// This property can be used to distinguish between a model binder which does not find a value and
        /// the case where a model binder sets the <c>null</c> value.
        /// </para>
        /// </summary>
        public bool IsModelSet { get; }

        /// <summary>
        /// A <see cref="ModelValidationNode"/> associated with the current <see cref="ModelBindingResult"/>.
        /// </summary>
        public ModelValidationNode ValidationNode { get; }

        public override bool Equals(object obj)
        {
            var other = obj as ModelBindingResult?;
            if (other == null)
            {
                return false;
            }
            else
            {
                return Equals(other.Value);
            }
        }

        public override int GetHashCode()
        {
            if (Key == null)
            {
                return 0;
            }
            else if (IsModelSet)
            {
                Debug.Assert(Key != null);
                return Key.GetHashCode() * Model?.GetHashCode() ?? 17;
            }
            else
            {
                Debug.Assert(Key != null);
                return Key.GetHashCode();
            }
        }

        public bool Equals(ModelBindingResult other)
        {
            return
                string.Equals(Key, other.Key, StringComparison.Ordinal) &&
                IsModelSet == other.IsModelSet &&
                object.Equals(Model, other.Model);
        }

        public static bool operator ==(ModelBindingResult x, ModelBindingResult y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(ModelBindingResult x, ModelBindingResult y)
        {
            return !x.Equals(y);
        }
    }
}
