﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Builders;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using Microsoft.Data.Entity.Relational;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.Data.Entity.Migrations.Tests.Infrastructure
{
    public class MigrationAssemblyTest
    {
        [Fact]
        public void Create_migration_assembly()
        {
            using (var context = new Context())
            {
                var migrationAssembly = new MigrationAssembly(context.Configuration);

                Assert.Equal("Microsoft.Data.Entity.Migrations.Tests", migrationAssembly.Assembly.GetName().Name);
                Assert.Equal("Microsoft.Data.Entity.Migrations.Tests.Infrastructure.Migrations", migrationAssembly.Namespace);
            }
        }

        [Fact]
        public void Configure_assembly_and_namespace()
        {
            using (var context
                = new Context
                      {
                          MigrationAssembly = new MockAssembly(),
                          MigrationNamespace = "MyNamespace"
                      })
            {
                var migrationAssembly = new MigrationAssembly(context.Configuration);

                Assert.Equal("MockAssembly", migrationAssembly.Assembly.FullName);
                Assert.Equal("MyNamespace", migrationAssembly.Namespace);
            }
        }

        [Fact]
        public void Load_and_cache_migrations()
        {
            using (var context = new Context())
            {
                var migrationAssembly = new MigrationAssembly(context.Configuration);

                var migrations1 = migrationAssembly.Migrations;
                var migrations2 = migrationAssembly.Migrations;

                Assert.Same(migrations1, migrations2);
                Assert.Equal(2, migrations1.Count);
                Assert.Equal("Migration1", migrations1[0].GetType().Name);
                Assert.Equal("Migration2", migrations1[1].GetType().Name);                
            }
        }

        [Fact]
        public void Loads_and_cache_model_snapshot()
        {
            using (var context = new Context())
            {
                var migrationAssembly = new MigrationAssembly(context.Configuration);

                var model1 = migrationAssembly.Model;
                var model2 = migrationAssembly.Model;

                Assert.Same(model1, model2);
                Assert.Equal("ContextModelSnapshot", model1.StorageName);                
            }
        }

        #region Fixture

        public class Context : DbContext
        {
            internal Assembly MigrationAssembly { get; set; }
            internal string MigrationNamespace { get; set; }

            protected override void OnConfiguring(DbContextOptions builder)
            {
                builder.AddBuildAction(c => c.AddOrUpdateExtension<MyRelationalConfigurationExtension>(x => x.ConnectionString = "ConnectionString"));

                if (MigrationAssembly != null)
                {
                    builder.AddBuildAction(c => c.AddOrUpdateExtension<MyRelationalConfigurationExtension>(x => x.MigrationAssembly = MigrationAssembly));
                }

                if (MigrationNamespace != null)
                {
                    builder.AddBuildAction(c => c.AddOrUpdateExtension<MyRelationalConfigurationExtension>(x => x.MigrationNamespace = MigrationNamespace));
                }
            }
        }

        public class MyRelationalConfigurationExtension : RelationalConfigurationExtension
        {
            protected override void ApplyServices(EntityServicesBuilder builder)
            {
            }
        }

        public class MockAssembly : Assembly
        {
            public override string FullName
            {
                get { return "MockAssembly"; }
            }
        }

        #endregion
    }

    #region Fixture

    namespace Migrations
    {
        public class Migration2 : Migration, IMigrationMetadata
        {
            public override void Up(MigrationBuilder migrationBuilder)
            {
                throw new NotImplementedException();
            }

            public override void Down(MigrationBuilder migrationBuilder)
            {
                throw new NotImplementedException();
            }

            string IMigrationMetadata.Timestamp
            {
                get { return "2"; }
            }
        }

        public class Migration1 : Migration, IMigrationMetadata
        {
            public override void Up(MigrationBuilder migrationBuilder)
            {
                throw new NotImplementedException();
            }

            public override void Down(MigrationBuilder migrationBuilder)
            {
                throw new NotImplementedException();
            }

            string IMigrationMetadata.Timestamp
            {
                get { return "1"; }
            }
        }

        public class ContextModelSnapshot : ModelSnapshot
        {
            public override IModel Model
            {
                get { return new Metadata.Model() { StorageName = "ContextModelSnapshot" }; }
            }
        }
    }
    
    #endregion
}
