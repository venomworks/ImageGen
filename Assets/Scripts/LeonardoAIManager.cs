using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeonardoAIWithPose : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey = "<YOUR_API_KEY>";

    [Header("File Settings")]
    public string mainImagePath = "Assets/mainImage.jpg"; // Main reference image
    public string poseImagePath = "Assets/poseImage.jpg"; // Pose reference image

    public float width;
    public float height;


    public TMP_InputField promptInputField;
    public TextMeshProUGUI errorStatusText;

    public void StartGenerate()
    {
        if(faceImage.texture == null || poseImage.texture == null || promptInputField.text == "")
        {
            //Debug.LogError("Please upload both face and pose images before generating.");
            errorStatusText.text = "Please upload both face and pose images and enter prompt before generating.";
            return;
        }

        errorStatusText.text = "";

        StartCoroutine(RunLeonardoWithPoseWorkflow());
    }

    public string faceImageID;
    public string poseImageID;

    public RawImage faceImage;
    public RawImage poseImage;
    public RawImage resultImage;



    public void FaceUploaded(string id)
    {
        faceImageID = id;
    }

    public void PoseUploaded(string id)
    {
        poseImageID = id;
    }

    IEnumerator RunLeonardoWithPoseWorkflow()
    {
        // 1. Upload Main Image
        UniversalController.instance.loadingManager.ShowLoadingScreen("Uploading face image...");

        if (faceImageID == "")
        {
            yield return StartCoroutine(UploadImage(faceImage.texture as Texture2D, FaceUploaded));

        }
        // 2. Upload Pose Image
        
        UniversalController.instance.loadingManager.ShowLoadingScreen("Uploading pose image...");

        if (poseImageID == "")
        {
            yield return StartCoroutine(UploadImage(poseImage.texture as Texture2D, PoseUploaded));

        }

        UniversalController.instance.loadingManager.ShowLoadingScreen("Generating final image...");

        // 3. Generate final image using both uploaded images as ControlNets
        string generateUrl = "https://cloud.leonardo.ai/api/rest/v1/generations";

        var payload = new Dictionary<string, object>
        {
            { "height", 1024 },
            { "width", 576 },
            { "num_images", 1 },
            { "modelId", "aa77f04e-3eec-4034-9c07-d0f619684628" },
            { "prompt", promptInputField.text },
            { "presetStyle", "CINEMATIC" },
            { "photoReal", true },
            { "photoRealVersion", "v2" },
            { "alchemy", true },
            { "controlnets", new List<Dictionary<string, object>>{
                new Dictionary<string, object>{
                    { "initImageId", faceImageID },
                    { "initImageType", "UPLOADED" },
                    { "preprocessorId", 133 }, // Character Reference
                    { "strengthType", "High" }
                },
                new Dictionary<string, object>{
                    { "initImageId", poseImageID },
                    { "initImageType", "UPLOADED" },
                    { "preprocessorId", 100 }, // Content Reference
                    { "strengthType", "Mid" }
                }
            }}
        };

        Debug.Log("Generation Payload: " + JsonConvert.SerializeObject(payload, Formatting.Indented));

        
        string jsonPayload = JsonConvert.SerializeObject(payload);
        UnityWebRequest generateRequest = new UnityWebRequest(generateUrl, "POST");
        generateRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
        generateRequest.downloadHandler = new DownloadHandlerBuffer();
        generateRequest.SetRequestHeader("accept", "application/json");
        generateRequest.SetRequestHeader("content-type", "application/json");
        generateRequest.SetRequestHeader("authorization", "Bearer " + apiKey);

        yield return generateRequest.SendWebRequest();

        if (generateRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Final Generation Failed: " + generateRequest.error);
            yield break;
        }

        var finalResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(generateRequest.downloadHandler.text);
        string finalGenerationId = ((Newtonsoft.Json.Linq.JObject)finalResponse["sdGenerationJob"])["generationId"].ToString();
        
        // Wait for generation to complete
        yield return new WaitForSeconds(20);

        UnityWebRequest getFinal = UnityWebRequest.Get(generateUrl + "/" + finalGenerationId);
        getFinal.SetRequestHeader("accept", "application/json");
        getFinal.SetRequestHeader("authorization", "Bearer " + apiKey);
        yield return getFinal.SendWebRequest();

        if (getFinal.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Get Final Generation Failed: " + getFinal.error);
            yield break;
        }

        Debug.Log("Final Generated Image Response: " + getFinal.downloadHandler.text);

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(getFinal.downloadHandler.text);
        var generationsByPk = response["generations_by_pk"] as Newtonsoft.Json.Linq.JObject;

        // This is the generation ID you use for polling
        string generationId = generationsByPk["id"].ToString();

        Debug.Log(generationId);

        StartCoroutine(FetchAndDisplayImage(generationId));
    }

    IEnumerator FetchAndDisplayImage(string generationId)
    {
        UniversalController.instance.loadingManager.ShowLoadingScreen("Fetching generated image...");

        string url = $"https://cloud.leonardo.ai/api/rest/v1/generations/{generationId}";
        bool done = false;

        while (!done)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to get generation details: " + request.error);
                yield break;
            }

            var data = JObject.Parse(request.downloadHandler.text);
            var gen = data["generations_by_pk"];

            if (gen == null)
            {
                Debug.LogError("No generation data yet.");
                yield return new WaitForSeconds(5);
                continue;
            }

            string status = gen["status"].ToString();
            Debug.Log("Generation status: " + status);

            if (status == "COMPLETE")
            {
                UniversalController.instance.loadingManager.ShowLoadingScreen("Fetch Completed!");
                var images = gen["generated_images"] as JArray;
                if (images != null && images.Count > 0)
                {
                    string imageUrl = images[0]["url"].ToString();
                    Debug.Log("Image URL: " + imageUrl);

                    // Download the image
                    using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(imageUrl))
                    {
                        yield return textureRequest.SendWebRequest();

                        if (textureRequest.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError("Failed to download image: " + textureRequest.error);
                            yield break;
                        }

                        Texture2D texture = DownloadHandlerTexture.GetContent(textureRequest);
                        resultImage.texture = texture;

                        ResetAll();
                        //rawImage.SetNativeSize();
                    }
                }
                done = true;
            }
            else
            {
                // Not completed yet, wait a few seconds and poll again
                yield return new WaitForSeconds(5);
            }
        }

        Debug.Log("Final image fetched and displayed!");
        UniversalController.instance.loadingManager.HideLoadingScreen();
    }


    public void ResetAll()
    {
        faceImageID = "";
        poseImageID = "";

        faceImage.texture = null;
        poseImage.texture = null;

    }

    public void DeleteFaceImage()
    {
        faceImage.texture = null;
        faceImageID = "";
    }

    public void DeletePoseImage()
    {
        poseImage.texture = null;
        poseImageID = "";
    }


    IEnumerator UploadImage(Texture2D texture, System.Action<string> callback)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null, cannot upload.");
            yield break;
        }

        string extension = "png"; // you can choose "jpg" if you prefer
        string mimeType = extension == "png" ? "image/png" : "image/jpeg";

        byte[] imageData = extension == "png" ? texture.EncodeToPNG() : texture.EncodeToJPG();


        string initImageUrl = "https://cloud.leonardo.ai/api/rest/v1/init-image";
        var initPayload = new Dictionary<string, string> { { "extension", extension } };
        string jsonInit = JsonConvert.SerializeObject(initPayload);

        UnityWebRequest initRequest = new UnityWebRequest(initImageUrl, "POST");
        initRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonInit));
        initRequest.downloadHandler = new DownloadHandlerBuffer();
        initRequest.SetRequestHeader("accept", "application/json");
        initRequest.SetRequestHeader("content-type", "application/json");
        initRequest.SetRequestHeader("authorization", "Bearer " + apiKey);

        yield return initRequest.SendWebRequest();

        if (initRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Init Image Request Failed: {initRequest.error}\n{initRequest.downloadHandler.text}");
            yield break;
        }

        var initResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(initRequest.downloadHandler.text);
        var uploadInitImage = initResponse["uploadInitImage"] as Newtonsoft.Json.Linq.JObject;

        string presignedUrl = uploadInitImage["url"].ToString();
        string imageId = uploadInitImage["id"].ToString();
        var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(uploadInitImage["fields"].ToString());


        WWWForm form = new WWWForm();
        foreach (var kvp in fields)
        {
            form.AddField(kvp.Key, kvp.Value);
        }
        form.AddBinaryData("file", imageData, "image." + extension, mimeType);

        UnityWebRequest uploadRequest = UnityWebRequest.Post(presignedUrl, form);
        yield return uploadRequest.SendWebRequest();

        if (uploadRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Image Upload Failed: {uploadRequest.error}\n{uploadRequest.downloadHandler.text}");
            yield break;
        }

        Debug.Log($"Image uploaded successfully! ID: {imageId}");

        callback?.Invoke(imageId);
    }


}
