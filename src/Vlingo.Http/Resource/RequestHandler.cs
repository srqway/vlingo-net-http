﻿// Copyright (c) 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Vlingo.Actors;
using Vlingo.Common;

namespace Vlingo.Http.Resource
{
    public abstract class RequestHandler
    {
        public Method Method { get; }
        public string Path { get; }
        public string ActionSignature { get; }
        private readonly Regex _pattern = new Regex("\\{(.*?)\\}", RegexOptions.Compiled);
        protected MediaTypeMapper MediaTypeMapper { get; set; }
        protected IErrorHandler ErrorHandler { get; set; }

        internal RequestHandler(Method method, string path, IList<IParameterResolver> parameterResolvers)
        {
            Method = method;
            Path = path;
            ActionSignature = GenerateActionSignature(parameterResolvers);
            ErrorHandler = DefaultErrorHandler.Instance;
            MediaTypeMapper = DefaultMediaTypeMapper.Instance;
        }

        internal RequestHandler(
            Method method,
            string path,
            IList<IParameterResolver> parameterResolvers,
            IErrorHandler errorHandler,
            MediaTypeMapper mediaTypeMapper)
        {
            Method = method;
            Path = path;
            ActionSignature = GenerateActionSignature(parameterResolvers);
            ErrorHandler = errorHandler;
            MediaTypeMapper = mediaTypeMapper;
        }

        protected ContentType ContentType => ContentType.Of("text/plain", "us-ascii");

        protected internal ICompletes<Response> RunParamExecutor(object? paramExecutor, Func<ICompletes<Response>?> executeRequest)
        {
            if (paramExecutor == null)
            {
                throw new HandlerMissingException($"No handler defined for {Method} {Path}");
            }
            return executeRequest?.Invoke()!;
        }

        internal abstract ICompletes<Response> Execute(
            Request request, 
            Action.MappedParameters mappedParameters,
            ILogger logger);

        private string GenerateActionSignature(IList<IParameterResolver> parameterResolvers)
        {
            CheckOrder(parameterResolvers);

            if (Path.Replace(" ", "").Contains("{}"))
            {
                throw new ArgumentException($"Empty path parameter name for {Method} {Path}");
            }

            var result = new StringBuilder();
            var matcher = _pattern.Match(Path);
            var first = true;
            foreach (var resolver in parameterResolvers)
            {
                if (resolver.Type == ParameterResolver.Type.Path)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        result.Append(", ");
                    }
                    result.Append(resolver.ParamClass.Name).Append(" ").Append(matcher.Groups[1]);
                    matcher = matcher.NextMatch();
                }
            }
            return result.ToString();
        }

        private void CheckOrder(IList<IParameterResolver> parameterResolvers)
        {
            var firstNonPathResolver = false;
            foreach (var resolver in parameterResolvers)
            {
                if (resolver.Type != ParameterResolver.Type.Path)
                {
                    firstNonPathResolver = true;
                }
                if (firstNonPathResolver && resolver.Type == ParameterResolver.Type.Path)
                {
                    throw new ArgumentException("Path parameters are unsorted");
                }
            }
        }

        protected internal IMapper MapperFrom(Type mapperClass)
        {
            try
            {
                return (IMapper)Activator.CreateInstance(mapperClass);
            }
            catch (Exception)
            {
                throw new ArgumentException("Cannot instantiate mapper class: " + mapperClass.FullName);
            }
        }
    }
}
