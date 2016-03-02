// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using WebJobs.Extensions.EasyTables;

namespace WebJobs.Mobile.Test.EasyTables
{
    internal class EasyTableTestHelper
    {
        public static IEnumerable<ParameterInfo> GetAllValidParameters()
        {
            var outputParams = GetValidOutputParameters();
            var inputItemParams = GetValidInputItemParameters();
            var inputTableParams = GetValidInputTableParameters();
            var inputQueryParams = GetValidInputQueryParameters();

            return outputParams.Concat(inputItemParams.Concat(inputTableParams.Concat(inputQueryParams)));
        }

        public static IEnumerable<ParameterInfo> GetValidOutputParameters()
        {
            return typeof(EasyTableTestHelper)
                .GetMethod("OutputParameters", BindingFlags.Instance | BindingFlags.NonPublic).GetParameters();
        }

        public static IEnumerable<ParameterInfo> GetValidInputItemParameters()
        {
            return typeof(EasyTableTestHelper)
               .GetMethod("InputItemParameters", BindingFlags.Instance | BindingFlags.NonPublic).GetParameters();
        }

        public static IEnumerable<ParameterInfo> GetValidInputTableParameters()
        {
            return typeof(EasyTableTestHelper)
               .GetMethod("InputTableParameters", BindingFlags.Instance | BindingFlags.NonPublic).GetParameters();
        }

        public static IEnumerable<ParameterInfo> GetValidInputQueryParameters()
        {
            return typeof(EasyTableTestHelper)
               .GetMethod("InputQueryParameters", BindingFlags.Instance | BindingFlags.NonPublic).GetParameters();
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "This is a test method used for generiating ParameterInfo via Reflection.")]
        private void OutputParameters(
            [EasyTable] out JObject jobjectOut,
            [EasyTable] out TodoItem pocoOut,
            [EasyTable] out JObject[] jobjectArrayOut,
            [EasyTable] out TodoItem[] pocoArrayOut,
            [EasyTable] IAsyncCollector<JObject> jobjectAsyncCollector,
            [EasyTable] IAsyncCollector<TodoItem> pocoAsyncCollector,
            [EasyTable] ICollector<JObject> jobjectCollector,
            [EasyTable] ICollector<TodoItem> pocoCollector)
        {
            jobjectOut = null;
            pocoOut = null;
            jobjectArrayOut = null;
            pocoArrayOut = null;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "This is a test method used for generiating ParameterInfo via Reflection.")]
        private void InputItemParameters(
            [EasyTable] JObject jobject,
            [EasyTable] TodoItem poco)
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "This is a test method used for generiating ParameterInfo via Reflection.")]
        private void InputTableParameters(
            [EasyTable] IMobileServiceTable jobjectTable,
            [EasyTable] IMobileServiceTable<TodoItem> pocoTable)
        {
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "This is a test method used for generiating ParameterInfo via Reflection.")]
        private void InputQueryParameters(
            [EasyTable] IMobileServiceTableQuery<TodoItem> query)
        {
        }
    }
}