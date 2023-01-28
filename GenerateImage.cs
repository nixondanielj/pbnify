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
using System.Net.Http.Headers;
using System.Text;

namespace AIPBN.Functions
{
    public static class GenerateImage
    {
        private static readonly string OPENAI_KEY = Environment.GetEnvironmentVariable(nameof(OPENAI_KEY));

        [FunctionName("GenerateImage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            /*
            1) Get prompt from request body
            2) Call DALL-E to generate image
            3) Store image to Azure Blob Storage
            4) Return image URL
            */
            // Parse the request body to JSON and get the prompt property
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string prompt = data?.prompt;

            // Call DALL-E to generate image
            var imageUrl = await GetImageFromPrompt(prompt);

            // Store image to Azure Blob Storage
            var azureStorageHelper = new AzureStorageHelper();
            var imageUri = await azureStorageHelper.UploadFromUrl(imageUrl);

            // Return image URL
            return new JsonResult(new { URL = imageUri });
        }

        public static async Task<string> GetImageFromPrompt(string prompt)
        {
            var input = new
            {
                Prompt = prompt,
                N = "1",
                Size = "1024x1024"
            };

            dynamic resp = null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization =
                     new AuthenticationHeaderValue("Bearer", OPENAI_KEY);
                var Message = await client.
                      PostAsync("https://api.openai.com/v1/images/generations",
                      new StringContent(JsonConvert.SerializeObject(input),
                      Encoding.UTF8, "application/json"));
                
                if (Message.IsSuccessStatusCode)
                {
                    var content = await Message.Content.ReadAsStringAsync();
                    resp = JsonConvert.DeserializeObject<dynamic>(content);
                }
            }

            return resp.data[0].url;
        }
    }
}
