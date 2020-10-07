// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AspNetCore.JsonPatch.Internal;

namespace AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    ///     The default AdapterFactory to be used for resolving <see cref="IAdapter" />.
    /// </summary>
    public class AdapterFactory : IAdapterFactory
    {
        /// <inheritdoc />
        public virtual IAdapter Create(object target, Type targetType)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (target is IList)
                return new ListAdapter();

            var dictionaryInterface = targetType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (dictionaryInterface != null)
            {
                var genericArguments = dictionaryInterface.GetGenericArguments();

                var type = typeof(DictionaryAdapter<,>).MakeGenericType(genericArguments[0], genericArguments[1]);
                return (IAdapter) (Activator.CreateInstance(type) ?? throw new InvalidOperationException());
            }

            if (target is IDynamicMetaObjectProvider)
                return new DynamicObjectAdapter();

            return new PocoAdapter();
        }
    }
}
