// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using Vlingo.Actors;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Http.Tests.Resource
{
    public class TestResponseChannelConsumer : Actor, IResponseChannelConsumer
    {
        private ResponseParser _parser;
        private readonly Progress _progress;

        public TestResponseChannelConsumer(Progress progress)
        {
            _progress = progress;
        }

        public void Consume(IConsumerByteBuffer buffer)
        {
            if (_parser == null)
            {
                _parser = ResponseParser.ParserFor(buffer.ToArray());
            }
            else
            {
                _parser.ParseNext(buffer.ToArray());
            }
            
            buffer.Release();

            while (_parser.HasFullResponse())
            {
                var response = _parser.FullResponse();
                _progress.ConsumeCalls.WriteUsing("consume", response);
            }
        }
    }
    
    public class Progress
    {
        internal AccessSafely ConsumeCalls = AccessSafely.AfterCompleting(0);

        public ConcurrentQueue<Response> Responses { get; } = new ConcurrentQueue<Response>();
        
        public AtomicInteger ConsumeCount { get; } = new AtomicInteger(0);
        
        /// <summary>
        /// Answer with an AccessSafely which writes responses to "consume" and reads the write count from "completed".
        /// </summary>
        /// <param name="n">Number of times consume(response) must be called before readFrom(...) will return.</param>
        /// <returns>AccessSafely</returns>
        /// <remarks>Clients can replace the default lambdas with their own via readingWith/writingWith.</remarks>
        public AccessSafely ExpectConsumeTimes(int n)
        {
            ConsumeCalls = AccessSafely.AfterCompleting(n);
            ConsumeCalls.WritingWith<Response>("consume", response =>
                {
                    Responses.Enqueue(response);
                    ConsumeCount.IncrementAndGet();
                })
                .ReadingWith("completed", () => ConsumeCalls.TotalWrites);
            
            return ConsumeCalls;
        }
    }
}