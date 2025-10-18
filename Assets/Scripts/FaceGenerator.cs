using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class FaceGenerator : MonoBehaviour
{
    public RawImage outputImage; // Where the generated image will be displayed
    public string prompt;
    public RawImage inputFace; // Where the generated image will be displayed
    public TMP_Text statusText;  // Optional: to show status messages

    private string apiUrl = "https://api.replicate.com/v1/models/deepseek-ai/deepseek-r1/predictions";
    private string apiToken = "r8_eO55s8BPJUFkCeg66TpuG0GRdNDthSp04sZG6"; // Replace with your actual token




    public void Generate()
    {
        /*RenderTexture rt = rawImage.texture as RenderTexture;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);

        // Backup current active RenderTexture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set active RenderTexture to the one we want to read
        RenderTexture.active = rt;

        // Read pixels
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        // Restore previous active RenderTexture
        RenderTexture.active = currentActiveRT;*/

        StartCoroutine(GenerateCoroutine(inputFace.texture as Texture2D, prompt));
    }

    private IEnumerator GenerateCoroutine(Texture2D faceTexture, string prompt)
    {
        statusText.text = "Uploading face...";

        // Encode texture to PNG
        byte[] imageBytes = faceTexture.EncodeToPNG();

        // Create JSON payload for Replicate
        string jsonPayload = JsonUtility.ToJson(new
        {
            input = new
            {
                image = "data:image/png;base64," + System.Convert.ToBase64String(imageBytes),
                prompt = prompt


            }
        });

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", "Bearer " + apiToken);
        www.SetRequestHeader("Content-Type", "application/json");

        statusText.text = "Generating image...";
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            statusText.text = "Error: " + www.error;
            Debug.LogError(www.error);
        }
        else
        {
            // Parse JSON response
            string responseText = www.downloadHandler.text;
            Debug.Log("Response: " + responseText);

            // Replicate usually returns a "output" array with URLs to the generated image
            string imageUrl = ExtractImageUrl(responseText);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                StartCoroutine(LoadImageFromUrl(imageUrl));
            }
            else
            {
                statusText.text = "Failed to get image URL.";
            }
        }
    }

    private string ExtractImageUrl(string json)
    {
        // Very simple extraction (use JSON parser if needed)
        int start = json.IndexOf("https://");
        if (start == -1) return null;
        int end = json.IndexOf("\"", start);
        if (end == -1) return null;
        return json.Substring(start, end - start);
    }

    private IEnumerator LoadImageFromUrl(string url)
    {
        statusText.text = "Downloading generated image...";
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            statusText.text = "Download failed: " + www.error;
            Debug.LogError(www.error);
        }
        else
        {
            Texture2D generatedTex = DownloadHandlerTexture.GetContent(www);
            outputImage.texture = generatedTex;
            outputImage.SetNativeSize();
            statusText.text = "Done!";
        }
    }
}
