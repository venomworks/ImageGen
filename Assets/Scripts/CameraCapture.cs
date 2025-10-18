using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CameraCapture : MonoBehaviour
{
    public RawImage cameraPreview;
    public RawImage capturedImageDisplay;

    public RawImage imageInput;

    private WebCamTexture webcamTexture;
    private Texture2D capturedImage;


    public Button captureButton;
    public Button doneButton;


    public GameObject cameraPanel;

    public void StartCamera()
    {
        if (WebCamTexture.devices.Length > 0)
        {
            cameraPanel.SetActive(true);
            webcamTexture = new WebCamTexture();
            cameraPreview.texture = webcamTexture;
            cameraPreview.material.mainTexture = webcamTexture;
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("No camera found on this device!");
        }
    }

    public void StopCamera()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }

    public void CaptureFrame()
    {
        if (webcamTexture == null) return;

        // Create Texture2D from webcam frame
        capturedImage = new Texture2D(webcamTexture.width, webcamTexture.height);
        capturedImage.SetPixels(webcamTexture.GetPixels());
        capturedImage.Apply();

        // Display it
        capturedImageDisplay.texture = capturedImage;


        doneButton.gameObject.SetActive(true);
        captureButton.GetComponentInChildren<TextMeshProUGUI>().text = "Retake";

    }

    public Texture2D GetCapturedImage()
    {
        return capturedImage;
    }


    public void UseCapturedImage()
    {
        if (capturedImage != null)
        {
            UniversalController.instance.loadingManager.ShowLoadingScreen("Detecting Face...");
            StartCoroutine(UniversalController.instance.faceDetector.DetectFace(capturedImage, (croppedFace) =>
            {
                if(croppedFace == null)
                {
                    Debug.LogWarning("Face detection failed or no face found.");
                    return;
                }
                imageInput.texture = croppedFace;
                StopCamera();
                cameraPanel.SetActive(false);
                UniversalController.instance.loadingManager.HideLoadingScreen();
            }));

        }
    }



}
