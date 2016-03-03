﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExtensionsSample.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EasyTables;
using Microsoft.WindowsAzure.MobileServices;

namespace ExtensionsSample
{
    public static class EasyTableSamples
    {
        // POCO output binding 
        //   The binding inserts the newly created item into the Easy Table when the 
        //   method successfully exits.         
        public static void InsertItem([EasyTable] out Item newItem)
        {
            newItem = new Item()
            {
                Id = Guid.NewGuid().ToString(),
                Text = new Random().Next().ToString()
            };
        }

        // Query input binding 
        //   The binding creates a strongly-typed query against the Item table. 
        //   The binding does not do anything with the results when the function exits.  
        //  This example takes the results of the query and puts them in a queue for further 
        //  processing. 
        public static async Task EnqueueItemToProcess(
            [TimerTrigger("00:01")] TimerInfo timer,
            [EasyTable] IMobileServiceTableQuery<Item> itemQuery,
            [Queue("ToProcess")] IAsyncCollector<string> queueItems)
        {
            IEnumerable<string> itemsToProcess = await itemQuery
                .Where(i => !i.IsProcessed)
                .Select(i => i.Id)
                .ToListAsync();

            foreach (string itemId in itemsToProcess)
            {
                await queueItems.AddAsync(itemId);
            }
        }

        // POCO input/output binding 
        //   This binding requires the 'id' constructor parameter to be specified. The binding uses 
        //   that id to perform a lookup against the Easy Table. The resulting object is supplied to the 
        //   function parameter (or null if not found). 
        //   Any changes made to the item are updated when the function exits successfully. If there are  
        //   no changes, nothing is sent.         
        // This example uses the binding template "{QueueTrigger}" to specify that the Id should come from 
        // the string value of the queued item.
        public static void DequeueAndProcess(
            [QueueTrigger("ToProcess")] string itemId,
            [EasyTable(Id = "{QueueTrigger}")] Item itemToProcess)
        {
            itemToProcess.IsProcessed = true;
            itemToProcess.ProcessedAt = DateTimeOffset.Now;
        }

        // Table input binding 
        //   This binding supplies a strongly-typed IMobileServiceTable<T> to the function. This allows 
        //   for queries, inserts, updates, and deletes to all be made against the table. 
        //   See the 'Input bindings' section below for more info. 
        public static async Task DeleteProcessedItems(
            [TimerTrigger("00:05")] TimerInfo timerInfo,
            [EasyTable] IMobileServiceTable<Item> table)
        {
            IEnumerable<Item> processedItems = await table.CreateQuery()
                .Where(i => i.IsProcessed && i.ProcessedAt < DateTime.Now.AddMinutes(-5))
                .ToListAsync();

            foreach (Item i in processedItems)
            {
                await table.DeleteAsync(i);
            }
        }
    }
}