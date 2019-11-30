using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace UnziptoAzureFiles
{
    public static class Unzipthis
    {
        [FunctionName("Unzipthis")]
        public static async Task Run([BlobTrigger("input-files/{name}", Connection = "funcstorage")]CloudBlockBlob myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name}");

            string destinationStorage = Environment.GetEnvironmentVariable("destinationStorage");
            string destinationContainer = Environment.GetEnvironmentVariable("destinationContainer");

            try
            {
                if (name.Split('.').Last().ToLower() == "zip")
                {

                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(destinationStorage);
                    CloudFileClient fileClient = storageAccount.CreateCloudFileClient();
                    CloudFileShare fileShare = fileClient.GetShareReference(destinationContainer);

                    using (MemoryStream fileMemStream = new MemoryStream())
                    {

                        await myBlob.DownloadToStreamAsync(fileMemStream);

                        using (ZipArchive archive = new ZipArchive(fileMemStream))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                log.LogInformation($"Now processing {entry.FullName}");

                                //Replace all NO digits, letters, or "-" by a "-" Azure storage is specific on valid characters
                                string valideName = Regex.Replace(entry.Name, @"[^a-zA-Z0-9\-]", "-").ToLower();

                                CloudFile file = fileShare.GetFileReference(valideName);
                                using (var fileStream = entry.Open())
                                {
                                    await file.UploadFromStreamAsync(fileStream);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error! Something went wrong: {ex.Message}");

            }
        }
    }
}