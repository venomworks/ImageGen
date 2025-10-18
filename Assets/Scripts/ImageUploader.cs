using UnityEngine;
using UnityEngine.UI;
using SFB; // For Standalone File Browser
using System.Collections;
using System.IO;

public class ImageUploader : MonoBehaviour
{
    public void UploadImage(RawImage uploadedImageDisplay)
    {
        // Open file picker (works on PC/Mac builds)
        var extensions = new[] {
    new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
    new ExtensionFilter("All Files", "*"),
};
        var paths = StandaloneFileBrowser.OpenFilePanel("Select an image", "", extensions, false);
        if (paths.Length > 0)
        {
            StartCoroutine(LoadImageAndDetect(paths[0], uploadedImageDisplay));
        }
    }


    private IEnumerator LoadImageAndDetect(string filePath, RawImage uploadedImageDisplay)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        if (uploadedImageDisplay)
            uploadedImageDisplay.texture = tex;

        UniversalController.instance.loadingManager.ShowLoadingScreen("Detecting Face...");
        yield return StartCoroutine(UniversalController.instance.faceDetector.DetectFace(tex, (croppedTex) =>
        {
            if (croppedTex == null)
            {
                Debug.Log("No face detected, try again.");
                return;
            }
            uploadedImageDisplay.texture = croppedTex;
            UniversalController.instance.loadingManager.HideLoadingScreen();
        }));

        AspectRatioFitter fitter = uploadedImageDisplay.GetComponent<AspectRatioFitter>();
        fitter.aspectRatio = (float)uploadedImageDisplay.texture.width / uploadedImageDisplay.texture.height;
    }


    public void UploadImageWihoutFaceDetection(RawImage uploadedImageDisplay)
    {
        // Open file picker (works on PC/Mac builds)
        var extensions = new[] {
    new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
    new ExtensionFilter("All Files", "*"),
};
        var paths = StandaloneFileBrowser.OpenFilePanel("Select an image", "", extensions, false);
        if (paths.Length > 0)
        {
            byte[] bytes = File.ReadAllBytes(paths[0]);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            if (uploadedImageDisplay)
                uploadedImageDisplay.texture = tex;
        }


        AspectRatioFitter fitter = uploadedImageDisplay.GetComponent<AspectRatioFitter>();
        fitter.aspectRatio = (float)uploadedImageDisplay.texture.width / uploadedImageDisplay.texture.height;

    }
}
