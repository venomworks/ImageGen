using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using Newtonsoft.Json.Linq; // Install Newtonsoft JSON via Package Manager

public class FaceDetector : MonoBehaviour
{
    [Header("Face++ Credentials")]
    public string apiKey = "YOUR_API_KEY";
    public string apiSecret = "YOUR_API_SECRET";

    private string apiUrl = "https://api-us.faceplusplus.com/facepp/v3/detect";

    public IEnumerator DetectFace(Texture2D imageTex, Action<Texture2D> onFaceCropped)
    {
        // Convert image to Base64
        byte[] imageBytes = imageTex.EncodeToJPG();
        string imageBase64 = Convert.ToBase64String(imageBytes);

        WWWForm form = new WWWForm();
        form.AddField("api_key", apiKey);
        form.AddField("api_secret", apiSecret);
        form.AddField("image_base64", imageBase64);

        using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Face detection failed: " + www.error);
                yield break;
            }

            string json = www.downloadHandler.text;
            JObject data = JObject.Parse(json);
            var faces = data["faces"];

            if (faces != null && faces.HasValues)
            {
                var faceRect = faces[0]["face_rectangle"];
                int top = (int)faceRect["top"];
                int left = (int)faceRect["left"];
                int width = (int)faceRect["width"];
                int height = (int)faceRect["height"];

                // --- Determine if face is already zoomed ---
                float faceWidthRatio = (float)width / imageTex.width;
                float faceHeightRatio = (float)height / imageTex.height;
                float cropThreshold = 0.6f; // If face occupies >60% of image, skip heavy cropping

                if (faceWidthRatio > cropThreshold || faceHeightRatio > cropThreshold)
                {
                    // Face is already zoomed in, return original image
                    Debug.Log("Face is already zoomed in, skipping cropping.");
                    onFaceCropped?.Invoke(imageTex);
                    yield break;
                }

                UniversalController.instance.loadingManager.ShowLoadingScreen("Cropping Face...");

                // --- Margin cropping for normal-sized faces ---
                float marginTopScale = 0.8f;    // extra space above face
                float marginSideScale = 0.6f;   // left/right
                float marginBottomScale = 0.3f; // bottom

                int marginTop = Mathf.RoundToInt(height * marginTopScale);
                int marginSide = Mathf.RoundToInt(width * marginSideScale);
                int marginBottom = Mathf.RoundToInt(height * marginBottomScale);

                int newLeft = Mathf.Max(0, left - marginSide);
                int newTop = Mathf.Max(0, top - marginTop);
                int newWidth = Mathf.Min(imageTex.width - newLeft, width + 2 * marginSide);
                int newHeight = Mathf.Min(imageTex.height - newTop, height + marginTop + marginBottom);

                // Flip Y for Unity coordinates
                int unityY = imageTex.height - newTop - newHeight;

                // Safety clamp
                newWidth = Mathf.Clamp(newWidth, 1, imageTex.width);
                newHeight = Mathf.Clamp(newHeight, 1, imageTex.height);

                // Crop image
                Color[] pixels = imageTex.GetPixels(newLeft, unityY, newWidth, newHeight);
                Texture2D cropped = new Texture2D(newWidth, newHeight);
                cropped.SetPixels(pixels);
                cropped.Apply();

                // Make square
                int size = Mathf.Min(newWidth, newHeight);
                Texture2D square = new Texture2D(size, size);
                square.SetPixels(cropped.GetPixels(
                    (newWidth - size) / 2,
                    (newHeight - size) / 2,
                    size, size
                ));
                square.Apply();

                // Return cropped image
                onFaceCropped?.Invoke(square);
            }
            else
            {
                Debug.LogWarning("No face detected in the image.");
                UniversalController.instance.loadingManager.HideLoadingScreen();
                onFaceCropped?.Invoke(null); // fallback to original
            }
        }

    }
}
