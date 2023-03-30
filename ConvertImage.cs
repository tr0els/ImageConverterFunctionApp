using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ImageMagick;
using System.Net.Http;

namespace ConvertImage
{
    public class ConvertImage
    {
        private readonly ILogger<ConvertImage> _logger;

        public ConvertImage(ILogger<ConvertImage> log)
        {
            _logger = log;
        }

        [FunctionName("ImageConverter")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // blob store base url
            var blobStoreBaseUrl = "https://storageproductcatalog.blob.core.windows.net/";

            // parse query parameter
            string url = req.Query["url"];
            int width;
            int.TryParse(req.Query["width"], out width);
            int height;
            int.TryParse(req.Query["height"], out height);
            int quality;
            int.TryParse(req.Query["quality"], out quality);

            // validate url
            if (string.IsNullOrEmpty(url))
                return new BadRequestObjectResult("Error: url is not valid");

            // set default quality if none is given
            if (quality == 0)
                quality = 90;
                
            try
            {
                using (var client = new HttpClient())
                {
                    // read file as stream
                    var content = await client.GetStreamAsync(blobStoreBaseUrl + url);
                    var imgByteArr = Resize(content, height, width, quality);

                    // get image info
                    MagickImageInfo info = new MagickImageInfo(imgByteArr);
                    Console.WriteLine($"result width: {info.Width}");
                    Console.WriteLine($"result height: {info.Height}");
                    Console.WriteLine($"result colorspace: {info.ColorSpace}");
                    Console.WriteLine($"result format: {info.Format}");
                    Console.WriteLine($"result filesize = " + imgByteArr.Length);

                    // return final image result
                    return new FileContentResult(imgByteArr, "image/jpeg");
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex);
            }
        }

        // Convert image
        private static byte[] Resize(System.IO.Stream content, int height, int width, int quality)
        {
            byte[] result = null;

            // memory stream
            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Position = 0;

                using (var imageMagick = new MagickImage(content))
                {
                    //imageMagick.Thumbnail(new MagickGeometry(width, height));
                    imageMagick.Strip();
                    imageMagick.Quality = quality;
                    // Using resize since this maintains image aspect ratio
                    imageMagick.Resize(width, height);
                    imageMagick.Format = MagickFormat.Jpg;
                    imageMagick.Write(memoryStream);
                }

                // convert stream to byte[]
                result = memoryStream.ToArray();
                return result;
            }
        }
    }
}