using UnityEngine;
using UnityEngine.UI;

public class UniversalController : MonoBehaviour
{
    public CameraCapture cameraCapture;
    public FaceDetector faceDetector;
    public LeonardoAIWithPose leonardoAIWithPose;
    public LoadingManager loadingManager;
    public ImageUploader imageUploader;

    public static UniversalController instance;


    public RawImage faceRawImage;
    public RawImage poseRawImage;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void OpenCamera()
    {
        cameraCapture.StartCamera();
    }


    public void UploadFaceImage()
    {
        imageUploader.UploadImage(faceRawImage);
    }

    public void UploadPoseImage()
    {
        imageUploader.UploadImageWihoutFaceDetection(poseRawImage);
    }
}
