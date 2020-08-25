using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Azure;
using System.Net;
using System.Collections.Generic;
using Azure.AI.TextAnalytics;
using System.Reflection.Metadata;
using Microsoft.Extensions.Azure;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;
using System.Linq;

namespace Day5Function
{
    public static class SAFunction
    {
        private static readonly AzureKeyCredential credentials = new AzureKeyCredential(Environment.GetEnvironmentVariable("SemanticKey"));
        private static readonly Uri endpoint = new Uri(Environment.GetEnvironmentVariable("Endpoint"));

        [FunctionName("SAFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://christmaswishes.azurewebsites.net/api/Wishes");

            HttpClient apiClient = new HttpClient();
            apiClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("SemanticKey"));

            if (response.IsSuccessStatusCode)
            {
                string xml = await response.Content.ReadAsStringAsync();
                List<Message> data = JsonConvert.DeserializeObject<List<Message>>(xml);

                var sentimentClient = new TextAnalyticsClient(endpoint, credentials);

                Dictionary<string, int> children = new Dictionary<string, int>();

                //Get overall sentiment per child
                foreach (Message msg in data)
                {
                    if (String.IsNullOrEmpty(msg.message)) continue;

                    DetectedLanguage detectedLanguage = sentimentClient.DetectLanguage(msg.message);

                    //Swedish 'sv' is unsupported by v3.0 at time of writing
                    DocumentSentiment documentSentiment = (!detectedLanguage.Iso6391Name.Equals("sv")) ? sentimentClient.AnalyzeSentiment(msg.message, detectedLanguage.Iso6391Name) : sentimentClient.AnalyzeSentiment(msg.message);

                    int score = documentSentiment.Sentiment switch
                    {
                        TextSentiment.Positive => 1,
                        TextSentiment.Negative => -1,
                        _ => 0
                    };

                    if (!children.ContainsKey(msg.who)) children.Add(msg.who, 0);

                    children[msg.who] += score;
                }

                //Assume '0' is nice

                string results = JsonConvert.SerializeObject(children.AsEnumerable().Select(
                    item => new { 
                        child = item.Key, 
                        personality = (item.Value >= 0) ? "nice" : "naughty" 
                    }).ToList());


                return new OkObjectResult(results);

            }
            else
            {
                return new BadRequestObjectResult("Response error from Christmas Wishes API");
            }

        }

        public class Message
        {
            public string who
            {
                get; set;
            }

            public string message
            {
                get; set;
            }
        }
    }
}
