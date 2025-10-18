using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LeonardoGenerationManager
{
    private readonly LeonardoConfig _config;

    public LeonardoGenerationManager(LeonardoConfig config)
    {
        _config = config;
    }


    public async Task<string> GenerateImageAsync(string prompt, string faceID, string poseID, CancellationToken ct = default)
    {
        UniversalController.instance.loadingManager.ShowLoadingScreen("Generating final image...");

        string generateUrl = $"{_config.apiBaseUrl}/generations";

        var payload = CreateGenerationPayload(prompt, faceID, poseID);
        string jsonPayload = JsonConvert.SerializeObject(payload, Formatting.Indented);

        Debug.Log("Generation Payload:\n" + jsonPayload);

        using (UnityWebRequest generateRequest = LeonardoWebUtils.CreateWebRequest(generateUrl, "POST", _config.apiKey, jsonPayload))
        {
            var op = generateRequest.SendWebRequest();

            // Proper async polling loop
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    generateRequest.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

#if UNITY_2020_1_OR_NEWER
            if (generateRequest.result != UnityWebRequest.Result.Success)
#else
        if (generateRequest.isNetworkError || generateRequest.isHttpError)
#endif
            {
                Debug.LogError($"Generation failed: {generateRequest.error}\n{generateRequest.downloadHandler.text}");
                return null;
            }

            string responseText = generateRequest.downloadHandler.text;
            Debug.Log("Generation Response:\n" + responseText);

            try
            {
                var finalResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseText);

                if (finalResponse.TryGetValue("sdGenerationJob", out var jobToken) && jobToken is JObject job)
                {
                    string finalGenerationId = job["generationId"]?.ToString();

                    if (string.IsNullOrEmpty(finalGenerationId))
                    {
                        Debug.LogError("Missing generationId in response.");
                        return null;
                    }

                    await Task.Delay(5000, ct);

                    return finalGenerationId;
                }
                else
                {
                    Debug.LogError("Unexpected response structure (no sdGenerationJob).");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse response JSON: {ex.Message}");
                return null;
            }
            finally
            {
                UniversalController.instance.loadingManager.HideLoadingScreen();
            }
        }
    }

    public Dictionary<string, object> CreateGenerationPayload(string prompt, string faceImageID, string poseImageID)
    {
        return new Dictionary<string, object>
        {
            { "height", _config.height },
            { "width", _config.width },
            { "num_images", _config.numberOfVariants },
            { "modelId", _config.modelId },
            { "prompt", prompt},
            { "presetStyle", _config.presetStyle },
            { "photoReal", _config.photoReal },
            { "photoRealVersion", _config.photoRealVersion },
            { "alchemy", _config.alchemy },
            { "controlnets", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "initImageId", faceImageID },
                        { "initImageType", "UPLOADED" },
                        { "preprocessorId", 133 }, // Character Reference
                        { "strengthType", "High" }
                    },
                    new Dictionary<string, object>
                    {
                        { "initImageId", poseImageID },
                        { "initImageType", "UPLOADED" },
                        { "preprocessorId", 100 }, // Content Reference
                        { "strengthType", "Mid" }
                    }
                }
            }
        };
    }

    public async Task<Texture2D> DownloadAndDisplayImageAsync(JToken generationData)
    {
        JArray images = generationData["generated_images"] as JArray;
        if (images == null || images.Count == 0)
        {
            Debug.LogError("No images in generation response");
            return null;
        }

        string imageUrl = images[0]["url"].ToString();
        Debug.Log($"Downloading image from: {imageUrl}");

        using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            await textureRequest.SendWebRequest();

            if (textureRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download image: {textureRequest.error}");
                return null;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(textureRequest);
            //resultImage.texture = texture;

            Debug.Log("Final image fetched and displayed!");
            UniversalController.instance.loadingManager.HideLoadingScreen();
            return texture;
        }
    }

    public async Task<JToken> FetchImageAsync(string generationId)
    {
        UniversalController.instance.loadingManager.ShowLoadingScreen("Fetching generated image...");

        string url = $"{_config.apiBaseUrl}/generations/{generationId}";
        int maxAttempts = 30; // 5 minutes max
        int attempt = 0;

        while (attempt < maxAttempts)
        {
            attempt++;

            using (UnityWebRequest request = LeonardoWebUtils.CreateWebRequest(url, "GET", _config.apiKey))
            {
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to get generation details: {request.error}");
                    await Task.Delay(5000);
                    continue;
                }

                var data = JObject.Parse(request.downloadHandler.text);
                var gen = data["generations_by_pk"];

                if (gen == null)
                {
                    Debug.Log("No generation data yet.");
                    await Task.Delay(5000);
                    continue;
                }

                string status = gen["status"].ToString();
                Debug.Log($"Generation status: {status} (Attempt {attempt}/{maxAttempts})");

                switch (status)
                {
                    case "COMPLETE":
                        UniversalController.instance.loadingManager.ShowLoadingScreen("Downloading image...");
                        return gen;

                    case "FAILED":
                        Debug.LogError("Generation failed on server side");
                        return null;

                    default:
                        await Task.Delay(5000);
                        break;
                }
            }
        }

        Debug.LogError("Generation timeout after maximum attempts");
        return false;
    }
}
