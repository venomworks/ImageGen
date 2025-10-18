using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LeonardoUploadManager
{
    public async Task<string> UploadImageAsync(Texture2D texture, string apiKey)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null, cannot upload.");
            return null;
        }

        try
        {
            const string extension = "png";
            string mimeType = extension == "png" ? "image/png" : "image/jpeg";
            byte[] imageData = extension == "png" ? texture.EncodeToPNG() : texture.EncodeToJPG();

            // Initialize upload
            var initResult = await InitializeImageUploadAsync(extension, apiKey);
            if (initResult == null) return null;

            // Upload actual image
            return await UploadImageDataAsync(imageData, initResult.Value, extension, mimeType);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Upload failed with exception: {ex.Message}");
            return null;
        }
    }

    private async Task<(string Url, string Id, Dictionary<string, string> Fields)?> InitializeImageUploadAsync(string extension, string  apiKey)
    {
        string initImageUrl = "https://cloud.leonardo.ai/api/rest/v1/init-image";
        var initPayload = new Dictionary<string, string> { { "extension", extension } };
        string jsonInit = JsonConvert.SerializeObject(initPayload);

        using (UnityWebRequest initRequest = LeonardoWebUtils.CreateWebRequest(initImageUrl, "POST", apiKey,  jsonInit))
        {
            await initRequest.SendWebRequest();

            if (initRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Init Image Request Failed: {initRequest.error}\n{initRequest.downloadHandler.text}");
                return null;
            }

            var initResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(initRequest.downloadHandler.text);
            var uploadInitImage = initResponse["uploadInitImage"] as JObject;

            string presignedUrl = uploadInitImage["url"].ToString();
            string imageId = uploadInitImage["id"].ToString();
            var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(uploadInitImage["fields"].ToString());

            return (presignedUrl, imageId, fields);
        }
    }

    private async Task<string> UploadImageDataAsync(byte[] imageData, (string Url, string Id, Dictionary<string, string> Fields) uploadInfo, string extension, string mimeType)
    {
        WWWForm form = new WWWForm();
        foreach (var kvp in uploadInfo.Fields)
        {
            form.AddField(kvp.Key, kvp.Value);
        }
        form.AddBinaryData("file", imageData, "image." + extension, mimeType);

        using (UnityWebRequest uploadRequest = UnityWebRequest.Post(uploadInfo.Url, form))
        {
            await uploadRequest.SendWebRequest();

            if (uploadRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Image Upload Failed: {uploadRequest.error}\n{uploadRequest.downloadHandler.text}");
                return null;
            }

            Debug.Log($"Image uploaded successfully! ID: {uploadInfo.Id}");
            return uploadInfo.Id;
        }
    }
}
