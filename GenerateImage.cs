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
            log.LogInformation($"Prompt received: {prompt}");

            // Call DALL-E to generate image
            var imageUrl = await GetImageFromPrompt(prompt, log);
            log.LogInformation($"Generated image URL: {imageUrl}");

            // Store image to Azure Blob Storage
            var azureStorageHelper = new AzureStorageHelper();
            var imageUri = await azureStorageHelper.UploadFromUrl(imageUrl);

            // Return image URL
            log.LogInformation($"Azure Blob Storage image URL: {imageUri}");
            return new JsonResult(new { url = imageUri });
        }

        public static async Task<string> GetImageFromPrompt(string prompt, ILogger log)
        {
            var input = JsonConvert.SerializeObject(new
            {
                prompt = prompt,
                n = 1,
                size = "1024x1024"
            });

            log.LogInformation($"OpenAI request: {input}");

            dynamic resp = null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization =
                     new AuthenticationHeaderValue("Bearer", OPENAI_KEY);
                var response = await client.
                      PostAsync("https://api.openai.com/v1/images/generations",
                      new StringContent(input,
                      Encoding.UTF8, "application/json"));
                
                log.LogInformation($"OpenAI response status code: {response.StatusCode.ToString()}");
                log.LogInformation($"OpenAI response: {response.ToString()}");
                log.LogInformation($"OpenAI response content: {await response.Content.ReadAsStringAsync()}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    resp = JsonConvert.DeserializeObject<dynamic>(content);
                }
            }

            return resp.data[0].url;
        }
    }
}
