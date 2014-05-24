// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Entity.Infrastructure;
using Xunit;

namespace Microsoft.Data.Entity.InMemory.Tests
{
    public class InMemoryDbContextOptionsExtensionsTest
    {
        [Fact]
        public void Can_add_extension_with_connection_string()
        {
            var options = new DbContextOptions();

            options = options.UseInMemoryStore(persist: false);

            var extension = ((IDbContextOptionsExtensions)options).Extensions.OfType<InMemoryOptionsExtension>().Single();

            Assert.False(extension.Persist);
        }

        [Fact]
        public void Can_add_extension_with_connection_string_using_generic_options()
        {
            var options = new DbContextOptions<DbContext>();

            options = options.UseInMemoryStore(persist: false);

            var extension = ((IDbContextOptionsExtensions)options).Extensions.OfType<InMemoryOptionsExtension>().Single();

            Assert.False(extension.Persist);
        }

        [Fact]
        public void UseInMemoryStore_throws_if_options_are_locked()
        {
            var options = new DbContextOptions<DbContext>();
            options.Lock();

            Assert.Equal(
                GetString("FormatEntityConfigurationLocked", "UseInMemoryStore"),
                Assert.Throws<InvalidOperationException>(() => options.UseInMemoryStore()).Message);
        }

        private static string GetString(string stringName, params object[] parameters)
        {
            var strings = typeof(DbContext).GetTypeInfo().Assembly.GetType(typeof(DbContext).Namespace + ".Strings");
            return (string)strings.GetTypeInfo().GetDeclaredMethods(stringName).Single().Invoke(null, parameters);
        }
    }
}
