﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace WebJobs.Mobile.Test.EasyTables
{
    internal class TodoItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public bool Complete { get; set; }
        public DateTimeOffset CompletedDate { get; set; }
    }
}