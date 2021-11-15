// <copyright file="MethodResultApiModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Linq;
using Mmm.Iot.IoTHubManager.Services.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Models
{
    public class MethodResultApiModel
    {
        public MethodResultApiModel()
        {
        }

        public MethodResultApiModel(MethodResultServiceModel model)
        {
            this.Status = model.Status;
            if (!string.IsNullOrEmpty(model.JsonPayload) && model.JsonPayload != "null")
            {
                this.JsonPayload = this.GetJsonData(model.JsonPayload);
            }
        }

        public int Status { get; set; }

        public Payload[] JsonPayload { get; set; }

        internal Payload[] GetJsonData(string jsonPayload)
        {
            var result = JsonConvert.DeserializeObject<PayloadApiModel[]>(jsonPayload);

            return result.Select(x => JsonConvert.DeserializeObject<Payload[]>(x.Payload)).First();
        }
    }
}