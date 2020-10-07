// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace AspNetCore.JsonPatch.Tests.TestObjectModels
{
    public class ObjectWithJObject
    {
        public JsonDocument CustomData { get; set; } = JsonDocument.Parse("{}");
    }
}
