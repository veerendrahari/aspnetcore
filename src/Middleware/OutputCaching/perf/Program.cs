// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run(Assembly.GetExecutingAssembly(), args: args);
