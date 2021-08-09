// <copyright file="QueryConditionTranslator.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Azure.Devices;
using Mmm.Iot.Common.Services.Exceptions;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.Services.Helpers
{
    public static class QueryConditionTranslator
    {
        private static readonly Dictionary<string, string> OperatorMap = new Dictionary<string, string>
        {
            { "EQ", "=" },
            { "NE", "!=" },
            { "LT", "<" },
            { "LE", "<=" },
            { "GT", ">" },
            { "GE", ">=" },
            { "IN", "IN" },
            { "LK", "LIKE" },
        };

        private static readonly Dictionary<string, string> KustoOperatorMap = new Dictionary<string, string>
        {
            { "EQ", "==" },
            { "NE", "!=" },
            { "LT", "<" },
            { "LE", "<=" },
            { "GT", ">" },
            { "GE", ">=" },
            { "IN", "in" },
            { "LK", "contains" },
        };

        private static readonly string DeviceTwinTags = "tags";
        private static readonly string DeviceTwinDesired = "properties.desired";
        private static readonly string DeviceTwinReported = "properties.reported";

        public static string ToQueryString(string conditions)
        {
            IEnumerable<QueryConditionClause> clauses = null;

            try
            {
                clauses = JsonConvert.DeserializeObject<IEnumerable<QueryConditionClause>>(conditions);
            }
            catch
            {
                // Any exception raised in deserializing will be ignored
            }

            if (clauses == null)
            {
                // Condition is not a valid clause list. Assume it a query string
                return conditions;
            }

            var clauseStrings = clauses.Select(c =>
            {
                string op;
                if (!OperatorMap.TryGetValue(c.Operator.ToUpperInvariant(), out op))
                {
                    throw new InvalidInputException();
                }

                // Reminder: string value will be surrounded by single quotation marks
                StringBuilder value = new StringBuilder();
                using (StringWriter sw = new StringWriter(value))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        writer.QuoteChar = '\'';

                        JsonSerializer ser = new JsonSerializer();
                        ser.Serialize(writer, c.Value);
                    }
                }

                if (op == "IN")
                {
                    List<string> values = JsonConvert.DeserializeObject<List<string>>(value.ToString());
                    string joinValues = string.Join(" or ", values.Select(v => $"{c.Key} = '{v}'"));
                    return $"({joinValues})";
                }

                if (c.Key == "firmwareVersion")
                {
                    return $"( properties.reported.firmware.currentFwVersion {op} {value.ToString()} or properties.reported.Firmware {op} {value.ToString()} )";
                }

                return $"{c.Key} {op} {value.ToString()}";
            });

            return string.Join(" and ", clauseStrings);
        }

        public static string ToADXQueryString(string conditions)
        {
            IEnumerable<QueryConditionClause> clauses = null;

            try
            {
                clauses = JsonConvert.DeserializeObject<IEnumerable<QueryConditionClause>>(conditions);
            }
            catch
            {
                // Any exception raised in deserializing will be ignored
            }

            if (clauses == null)
            {
                // Condition is not a valid clause list. Assume it a query string
                return conditions;
            }

            var clauseStrings = clauses.Select(c =>
            {
                string op;
                if (!KustoOperatorMap.TryGetValue(c.Operator.ToUpperInvariant(), out op))
                {
                    throw new InvalidInputException();
                }

                StringBuilder value = new StringBuilder();
                using (StringWriter sw = new StringWriter(value))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        writer.QuoteChar = '\"';

                        JsonSerializer ser = new JsonSerializer();
                        ser.Serialize(writer, c.Value);
                    }
                }

                if (op == "IN")
                {
                    List<string> values = JsonConvert.DeserializeObject<List<string>>(value.ToString());
                    string joinValues = string.Join(" or ", values.Select(v => $"{DeviceTwinKustoKeyBuilder(c.Key)} == \"{v}\""));
                    return $"({joinValues})";
                }

                if (c.Key == "firmwareVersion")
                {
                    return $"( Twin.properties.reported.firmware.currentFwVersion {op} {value.ToString()} or Twin.properties.reported.Firmware {op} {value.ToString()} )";
                }

                return $"{DeviceTwinKustoKeyBuilder(c.Key)} {op} {value.ToString()}";
            });

            return string.Join(" and ", clauseStrings);
        }

        private static string DeviceTwinKustoKeyBuilder(string twinPath)
        {
            switch (twinPath)
            {
                case var someVal when someVal.StartsWith(DeviceTwinTags, System.StringComparison.OrdinalIgnoreCase):
                    return DeviceTwinKustoKey(someVal, DeviceTwinTags);
                case var someVal when someVal.StartsWith(DeviceTwinDesired, System.StringComparison.OrdinalIgnoreCase):
                    return DeviceTwinKustoKey(someVal, DeviceTwinDesired);
                case var someVal when someVal.StartsWith(DeviceTwinReported, System.StringComparison.OrdinalIgnoreCase):
                    return DeviceTwinKustoKey(someVal, DeviceTwinReported);
                default:
                    return $"Twin.{twinPath}";
            }
        }

        private static string DeviceTwinKustoKey(string twinPath, string twinPrefix)
        {
            string subString = twinPath.Substring(twinPrefix.Length);
            return $"Twin.{twinPrefix}{subString}";
        }
    }
}