// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Data.Entity.FunctionalTests
{
    public abstract class NullKeysTestBase<TFixture> : IClassFixture<TFixture>
        where TFixture : NullKeysTestBase<TFixture>.NullKeysFixtureBase, new()
    {
        protected NullKeysTestBase(TFixture fixture)
        {
            Fixture = fixture;
        }

        protected virtual TFixture Fixture { get; }

        protected DbContext CreateContext()
        {
            return Fixture.CreateContext();
        }

        [Fact] // Issue #1093
        public virtual void Include_with_null_FKs_and_nullable_PK()
        {
            using (var context = CreateContext())
            {
                var results = context.Set<WithStringFk>()
                    .OrderBy(e => e.Id)
                    .Include(e => e.Principal)
                    .ToList();

                Assert.Equal(
                    new[] { "And", "By", "George", "Me", "Rodrigue", "Wendy" },
                    results.Select(e => e.Id));

                Assert.Equal(
                    new[] { null, null, "Empire", "Fire", "Stereo", "Stereo" },
                    results.Select(e => e.Fk));

                Assert.Equal(
                    new WithStringKey[] { null, null },
                    results.Take(2).Select(e => e.Principal));

                Assert.Equal(
                    new[] { "Empire", "Fire", "Stereo", "Stereo" },
                    results.Skip(2).Select(e => e.Principal.Id));
            }
        }

        [Fact]
        public virtual void Include_with_non_nullable_FKs_and_nullable_PK()
        {
            using (var context = CreateContext())
            {
                var results = context.Set<WithIntFk>()
                    .OrderBy(e => e.Id)
                    .Include(e => e.Principal)
                    .ToList();

                Assert.Equal(
                    new[] { 1, 2, 3 },
                    results.Select(e => e.Id));

                Assert.Equal(
                    new[] { 1, 1, 3 },
                    results.Select(e => e.Fk));

                Assert.Equal(
                    new int?[] { 1, 1, 3 },
                    results.Select(e => e.Principal.Id));
            }
        }

        [Fact] // Issue #1093
        public virtual void Include_with_null_fKs_and_non_nullable_PK()
        {
            using (var context = CreateContext())
            {
                var results = context.Set<WithNullableIntFk>()
                    .OrderBy(e => e.Id)
                    .Include(e => e.Principal)
                    .ToList();

                Assert.Equal(
                    new[] { 1, 2, 3, 4, 5, 6 },
                    results.Select(e => e.Id));

                Assert.Equal(
                    new int?[] { null, 1, null, 2, null, null },
                    results.Select(e => e.Fk));

                Assert.Null(results[0].Principal);
                Assert.Equal(1, results[1].Principal.Id);
                Assert.Null(results[2].Principal);
                Assert.Equal(2, results[3].Principal.Id);
                Assert.Null(results[4].Principal);
                Assert.Null(results[5].Principal);
            }
        }

        [Fact] // Issue #1292
        public virtual void One_to_one_self_ref_Include()
        {
            using (var context = CreateContext())
            {
                var results = context.Set<WithStringFk>()
                    .OrderBy(e => e.Id)
                    .Include(e => e.Self)
                    .ToList();

                Assert.Equal(
                    new[] { "And", "By", "George", "Me", "Rodrigue", "Wendy" },
                    results.Select(e => e.Id));

                Assert.Equal(
                    new[] { "By", null, null, null, null, "Rodrigue" },
                    results.Select(e => e.SelfFk));

                Assert.Null(results[0].Self);
                Assert.Equal("And", results[1].Self.Id);
                Assert.Null(results[2].Self);
                Assert.Null(results[3].Self);
                Assert.Equal("Wendy", results[4].Self.Id);
                Assert.Null(results[5].Self);
            }
        }

        protected class WithStringKey
        {
            public string Id { get; set; }

            public ICollection<WithStringFk> Dependents { get; set; }
        }

        protected class WithStringFk
        {
            public string Id { get; set; }

            public string Fk { get; set; }
            public WithStringKey Principal { get; set; }

            public string SelfFk { get; set; }
            public WithStringFk Self { get; set; }
        }

        protected class WithIntKey
        {
            public int Id { get; set; }

            public ICollection<WithNullableIntFk> Dependents { get; set; }
        }

        protected class WithNullableIntFk
        {
            public int Id { get; set; }

            public int? Fk { get; set; }
            public WithIntKey Principal { get; set; }
        }

        protected class WithNullableIntKey
        {
            public int? Id { get; set; }

            public ICollection<WithIntFk> Dependents { get; set; }
        }

        protected class WithIntFk
        {
            public int Id { get; set; }

            public int Fk { get; set; }
            public WithNullableIntKey Principal { get; set; }
        }

        public abstract class NullKeysFixtureBase : IDisposable
        {
            public abstract DbContext CreateContext();

            public virtual void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<WithStringKey>()
                    .Collection(e => e.Dependents).InverseReference(e => e.Principal)
                    .ForeignKey(e => e.Fk);

                modelBuilder.Entity<WithStringFk>()
                    .Reference<WithStringFk>()
                    .InverseReference(e => e.Self)
                    .ForeignKey<WithStringFk>(e => e.SelfFk);

                modelBuilder.Entity<WithIntKey>()
                    .Collection(e => e.Dependents)
                    .InverseReference(e => e.Principal)
                    .ForeignKey(e => e.Fk);

                modelBuilder.Entity<WithNullableIntKey>()
                    .Collection(e => e.Dependents)
                    .InverseReference(e => e.Principal)
                    .ForeignKey(e => e.Fk);
            }

            protected void EnsureCreated()
            {
                using (var context = CreateContext())
                {
                    if (context.Database.EnsureCreated())
                    {
                        context.Add(new WithStringKey { Id = "Stereo" });
                        context.Add(new WithStringKey { Id = "Fire" });
                        context.Add(new WithStringKey { Id = "Empire" });

                        context.Add(new WithStringFk { Id = "Wendy", Fk = "Stereo", SelfFk = "Rodrigue" });
                        context.Add(new WithStringFk { Id = "And", SelfFk = "By" });
                        context.Add(new WithStringFk { Id = "Me", Fk = "Fire" });
                        context.Add(new WithStringFk { Id = "By" });
                        context.Add(new WithStringFk { Id = "George", Fk = "Empire" });
                        context.Add(new WithStringFk { Id = "Rodrigue", Fk = "Stereo" });

                        context.Add(new WithIntKey { Id = 1 });
                        context.Add(new WithIntKey { Id = 2 });
                        context.Add(new WithIntKey { Id = 3 });

                        context.Add(new WithNullableIntFk { Id = 1 });
                        context.Add(new WithNullableIntFk { Id = 2, Fk = 1 });
                        context.Add(new WithNullableIntFk { Id = 3 });
                        context.Add(new WithNullableIntFk { Id = 4, Fk = 2 });
                        context.Add(new WithNullableIntFk { Id = 5 });
                        context.Add(new WithNullableIntFk { Id = 6 });

                        context.Add(new WithNullableIntKey { Id = 1 });
                        context.Add(new WithNullableIntKey { Id = 2 });
                        context.Add(new WithNullableIntKey { Id = 3 });

                        context.Add(new WithIntFk { Id = 1, Fk = 1 });
                        context.Add(new WithIntFk { Id = 2, Fk = 1 });
                        context.Add(new WithIntFk { Id = 3, Fk = 3 });

                        context.SaveChanges();
                    }
                }
            }

            public void Dispose()
            {
                using (var context = CreateContext())
                {
                    context.Database.EnsureDeleted();
                }
            }
        }
    }
}
