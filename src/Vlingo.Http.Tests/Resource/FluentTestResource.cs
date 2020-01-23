// Copyright © 2012-2020 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Http.Resource;
using Vlingo.Http.Resource.Serialization;

namespace Vlingo.Http.Tests.Resource
{
    public class FluentTestResource : ResourceHandler
    {
        private readonly ConcurrentDictionary<string, Data> _entities;

        public FluentTestResource(World world)
        {
            _entities = new ConcurrentDictionary<string, Data>();
        }

        public ICompletes<Response> DefineWith(Data data)
        {
            var taggedData = new Data(data, Thread.CurrentThread.ManagedThreadId);

            _entities.AddOrUpdate(data.Id, taggedData, (k, value) => value);

            return Common.Completes.WithSuccess(Response.Of(Response.ResponseStatus.Created,
                JsonSerialization.Serialized(taggedData)));
        }

        public ICompletes<Response> QueryRes(string resId)
        {
            var gotData = _entities.TryGetValue(resId, out var data);

            return Common.Completes.WithSuccess(!gotData
                ? Response.Of(Response.ResponseStatus.NotFound)
                : Response.Of(Response.ResponseStatus.Ok, JsonSerialization.Serialized(data)));
        }

        public override Http.Resource.Resource Routes() =>
            ResourceBuilder.Resource("Resource", 5,
                ResourceBuilder.Post("/res")
                    .Body<Data>()
                    .Handle(DefineWith),
                ResourceBuilder.Get("/res/{resId}")
                    .Param<string>()
                    .Handle(QueryRes));
    }

    public class Data
    {
        private static readonly AtomicInteger NextId = new AtomicInteger(0);

        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public long ResourceHandlerId { get; }

        public static Data With(string name, string description) =>
            new Data(NextId.IncrementAndGet().ToString(), name, description, -1L);

        public Data(Data data, long resourceHandlerId) : this(data.Id, data.Name, data.Description, resourceHandlerId)
        {
        }

        public Data(string id, string name, string description, long resourceHandlerId)
        {
            Id = id;
            Name = name;
            Description = description;
            ResourceHandlerId = resourceHandlerId;
        }

        public override int GetHashCode()
        {
            var prime = 31;
            var result = 1;
            result = prime * result + (Description == null ? 0 : Description.GetHashCode());
            result = prime * result + (Id == null ? 0 : Id.GetHashCode());
            result = prime * result + (Name == null ? 0 : Name.GetHashCode());
            return result;
        }

#pragma warning disable 8632
        public override bool Equals(object? other)
        {
            if (this == other)
            {
                return true;
            }

            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            var otherData = (Data) other;

            return Id.Equals(otherData.Id) && Name.Equals(otherData.Name) && Description.Equals(otherData.Description);
        }
    }
#pragma warning restore 8632
}