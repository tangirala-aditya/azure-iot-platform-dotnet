// <copyright file="JSONConverter.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.Functions.DeploymentSync.Shared.Helpers
{
    public static class JSONConverter
    {
        public static Dictionary<string, object> DotNotationToDictionary(IDictionary<string, object> dotNotation)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            foreach (var dotObject in dotNotation)
            {
                var hierarcy = dotObject.Key.Split('.');

                Dictionary<string, object> bottom = dictionary;

                for (int i = 0; i < hierarcy.Length; i++)
                {
                    var key = hierarcy[i];

                    if (i == hierarcy.Length - 1)
                    {
                        bottom.Add(key, dotObject.Value);
                    }
                    else
                    {
                        if (!bottom.ContainsKey(key))
                        {
                            bottom.Add(key, new Dictionary<string, object>());
                        }

                        bottom = (Dictionary<string, object>)bottom[key];
                    }
                }
            }

            return dictionary;
        }
    }
}