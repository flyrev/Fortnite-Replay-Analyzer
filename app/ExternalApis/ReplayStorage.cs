﻿using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FortniteReplayAnalyzer.ExternalApis
{
    public class ReplayStorage
    {
        private readonly ILogger<ReplayStorage> logger;
        private readonly string key;
        private readonly string secret;
        private readonly string bucket;

        public ReplayStorage(ILogger<ReplayStorage> logger, string key, string secret, string bucket)
        {
            this.logger = logger;
            this.key = key;
            this.secret = secret;
            this.bucket = bucket;

            logger.LogInformation($"Using bucket {bucket}");
        }

        public async Task<S3Response> UploadJson(string guid, string json)
        {
            logger.LogInformation("Uploading json information from replay");
            var client = new AmazonS3Client(key, secret, Amazon.RegionEndpoint.EUNorth1);

            PutObjectResponse response = null;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = guid,
                    InputStream = stream,
                    ContentType = "application/json"
                };

                response = await client.PutObjectAsync(request);
            };

            logger.LogInformation($"Json upload finished. Key: {guid}");

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return new S3Response
                {
                    Success = true,
                    Guid = Guid.Parse(guid).ToString()
                };
            }
            else
            {
                return new S3Response
                {
                    Success = false,
                    Guid = Guid.Parse(guid).ToString()
                };
            }
        }

        /*
        public async Task<S3Response> UploadObject(IFormFile file)
        {
            logger.LogInformation("Uploading replay");
            var client = new AmazonS3Client(key, secret, Amazon.RegionEndpoint.EUNorth1);

            byte[] fileBytes = new Byte[file.Length];
            file.OpenReadStream().Read(fileBytes, 0, Int32.Parse(file.Length.ToString()));

            var guid = Guid.NewGuid().ToString();

            PutObjectResponse response = null;

            using (var stream = new MemoryStream(fileBytes))
            {
                var request = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = guid,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                response = await client.PutObjectAsync(request);
            };

            logger.LogInformation("Replay upload finished");

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return new S3Response
                {
                    Success = true,
                    Guid = guid
                };
            }
            else
            {
                return new S3Response
                {
                    Success = false,
                    Guid = guid
                };
            }
        }
        */

        /*
        public async Task<FortniteReplay> ReadObjectDataAsync(string objectKey)
        {
            var client = new AmazonS3Client(key, secret, Amazon.RegionEndpoint.EUNorth1);
            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = objectKey
                };
                using (GetObjectResponse response = await client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                {
                    var reader = new ReplayReader();
                    logger.LogInformation("Parsing replay");
                    return reader.ReadReplay(responseStream);
                }
            }
            catch (AmazonS3Exception e)
            {
                logger.LogError("Error encountered ***. Message:'{0}' when reading object", e.Message);
            }
            catch (Exception e)
            {
                logger.LogError("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
            }
            return new FortniteReplay();
        }
        */

        public async Task<string> ReadJsonDataAsync(string guid)
        {
            var client = new AmazonS3Client(key, secret, Amazon.RegionEndpoint.EUNorth1);
            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = guid
                };

                logger.LogInformation($"Downloading json. Key: {guid}");
                using (GetObjectResponse response = await client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader responseStreamReader = new StreamReader(response.ResponseStream))
                {
                    return responseStreamReader.ReadToEnd();
                }
            }
            catch (AmazonS3Exception e)
            {
                logger.LogError("Error encountered ***. Message:'{0}' when reading object", e.Message);
            }
            catch (Exception e)
            {
                logger.LogError("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
            }
            return "{}";
        }
    }
}
