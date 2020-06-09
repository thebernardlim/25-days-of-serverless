using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO.Enumeration;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PushWebHook
{
    public static class PushWebHookFunction
    {
        [FunctionName("PushWebHookFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string t = "";
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                string repoURL = data.repository.html_url + "/master"; //Assume we only consider master branch pushes

                List<string> imageFileURLs = new List<string>();
                dynamic commits = data?.commits;
                foreach (dynamic comm in commits)
                {
                    foreach (string file in comm.added)
                        if (file.Trim().EndsWith(".png")) imageFileURLs.Add(string.Format("{0}/{1}", repoURL, file));
                }

                t += "a";
                using (SqlConnection sqlConn = new System.Data.SqlClient.SqlConnection(Environment.GetEnvironmentVariable("DatabaseConnection")))
                {
                    sqlConn.Open();
                    t += "b";
                    foreach (string imgURL in imageFileURLs)
                    {
                        t += "c";
                        System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = string.Format("INSERT Images (FileUrl) VALUES ('{0}')", imgURL);
                        cmd.Connection = sqlConn;
                        t += "d";
                        cmd.ExecuteNonQuery();
                        t += "e";

                    }
                    sqlConn.Close();
                    t += "f";
                }
            }
            catch(Exception ex)
            {
                t += ", " + ex.Message;
            }
            

            return new OkObjectResult(t);
        }
    }
}
