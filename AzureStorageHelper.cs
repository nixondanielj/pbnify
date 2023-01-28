using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure;
using Azure.Storage;
using System.Net.Http;

namespace AIPBN.Functions
{
    class AzureStorageHelper
    {
        private static readonly string AZURE_STORAGE_CONNECTION_STRING = Environment.GetEnvironmentVariable(nameof(AZURE_STORAGE_CONNECTION_STRING));
        private static readonly string AZURE_STORAGE_CONTAINER_NAME = Environment.GetEnvironmentVariable(nameof(AZURE_STORAGE_CONTAINER_NAME));

        public async Task<string> UploadFromUrl(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var imageTask = httpClient.GetStreamAsync(url);
                // Create a Blob client object
                BlobContainerClient container = new BlobContainerClient(AZURE_STORAGE_CONNECTION_STRING, AZURE_STORAGE_CONTAINER_NAME);
                await container.CreateIfNotExistsAsync();
                var blobClient = container.GetBlobClient(Guid.NewGuid().ToString());
                // Upload the image to the blob
                await blobClient.UploadAsync(await imageTask, true);
                // Return the public URL of the uploaded image
                return blobClient.Uri.AbsoluteUri;
            }
        }
    }
}