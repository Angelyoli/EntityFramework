﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity;

namespace Microsoft.Data.Relational
{
    public class RelationalDataStore : DataStore
    {
        private readonly string _nameOrConnectionString;

        public RelationalDataStore([NotNull] string nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }

        public string NameOrConnectionString
        {
            get { return _nameOrConnectionString; }
        }
    }
}