using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public abstract class SelectionAttributeTestsBase
    {
        private readonly static Foo[] SAMPLE =
            new[] {
                Foo.Create("aa", 1),
                Foo.Create("bb", 2) };

        private readonly IResolverProvider _provider;

        protected SelectionAttributeTestsBase(IResolverProvider provider)
        {
            _provider = provider;
        }

        [Fact]
        public void Execute_Selection_MultipleScalar()
        {
            // arrange
            IServiceCollection services;
            Func<IResolverContext, IEnumerable<Foo>> resolver;
            (services, resolver) = _provider.CreateResolver(SAMPLE);

            IQueryable<Foo> resultCtx = null;
            ISchema schema = SchemaBuilder.New()
                .AddServices(services.BuildServiceProvider())
                .AddQueryType<Query>(d =>
                    d.Field(t => t.Foos)
                        .Resolver(resolver)
                        .Use(next => async ctx =>
                        {
                            await next(ctx).ConfigureAwait(false);
                            resultCtx = ctx.Result as IQueryable<Foo>;
                        }))
                .Create();
            IQueryExecutor executor = schema.MakeExecutable();

            // act
            executor.Execute(
                 "{ foos { bar baz } }");

            // assert
            Assert.NotNull(resultCtx);
            Assert.Collection(resultCtx.ToArray(),
                x =>
                {
                    Assert.Equal("aa", x.Bar);
                    Assert.Equal(1, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                },
                x =>
                {
                    Assert.Equal("bb", x.Bar);
                    Assert.Equal(2, x.Baz);
                    Assert.Null(x.Nested);
                    Assert.Null(x.NestedCollection);
                });
        }

        public class Query
        {
            [UseSelection]
            public IQueryable<Foo> Foos { get; }
        }

        public class Foo
        {
            private static int idCounter = 1;
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }

            public int Baz { get; set; }

            public NestedFoo Nested { get; set; }

            public List<NestedFoo> NestedCollection { get; set; }

            public static Foo Create(string bar, int baz)
            {
                idCounter++;
                return new Foo
                {
                    Id = idCounter * 2,
                    Bar = bar,
                    Baz = baz,
                    Nested = new NestedFoo()
                    {
                        Id = idCounter * 2,
                        Bar = "nested" + bar,
                        Baz = baz * 2
                    },
                    NestedCollection = new List<NestedFoo>()
                       {
                        new NestedFoo()
                        {
                            Id = (idCounter * 2)-1,
                            Bar = "nestedcollection" + bar,
                            Baz = baz * 3
                        },
                       }
                };
            }
        }

        public class NestedFoo
        {
            [Key]
            public int Id { get; set; }

            public string Bar { get; set; }

            public int Baz { get; set; }
        }
    }
}
