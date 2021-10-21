// <copyright file="JsonHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Functions.Messaging.Shared.Helpers
{
    public static class JsonHelper
    {
        public static JObject UpdateJson(this JObject mainJson, JObject fragmentJson)
        {
            Parallel.ForEach(
                fragmentJson.Properties(),
                new ParallelOptions { MaxDegreeOfParallelism = 32 },
                (item) =>
                {
                    if (mainJson.ContainsKey(item.Name))
                    {
                        switch (item.Value.Type)
                        {
                            case JTokenType.Object:
                                mainJson[item.Name] = UpdateJson(mainJson.GetValue(item.Name) as JObject, item.Value as JObject);
                                break;
                            case JTokenType.Null:
                                mainJson.Remove(item.Name);
                                break;
                            default:
                                mainJson[item.Name] = item.Value;
                                break;
                        }
                    }
                    else
                    {
                        mainJson.Add(item);
                    }
                });

            return mainJson;
        }
    }
}