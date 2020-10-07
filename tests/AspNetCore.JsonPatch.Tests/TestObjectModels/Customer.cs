// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace AspNetCore.JsonPatch.Tests.TestObjectModels
{
    internal class Customer
    {
        private int _age;
        private string _name;

        public Customer(string name, int age)
        {
            _name = name;
            _age = age;
        }
    }
}
