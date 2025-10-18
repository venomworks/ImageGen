using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeonardoUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField promptInputField;
    [SerializeField] private TextMeshProUGUI errorStatusText;
    [SerializeField] private RawImage faceImage;
    [SerializeField] private RawImage poseImage;
    [SerializeField] private RawImage resultImage;

    [SerializeField] private ImageUploader imageUploader;
    [SerializeField] private LeonardoAIWithPose aiManager;
    [SerializeField] private CameraCapture cameraCapture;


    public async void OnGenerateButtonPressed()
    {
        if(!ValidateInputs())
        {
            UpdateErrorStatusText("Please provide all required inputs: face image, pose image, and prompt.");
            return;
        }

        string prompt = promptInputField.text.Trim();

        LeonardoUploadManager uploadManager = new LeonardoUploadManager();

        string faceID = await uploadManager.UploadImageAsync((Texture2D)faceImage.texture, aiManager.leonardoConfig.apiKey);
        string poseID = await uploadManager.UploadImageAsync((Texture2D)poseImage.texture, aiManager.leonardoConfig.apiKey); 

        string imageID = await aiManager.StartGenerate(prompt, faceID, poseID);
        JToken generationData = await aiManager.FetchImage(imageID);
        resultImage.texture = await aiManager.GetGeneratedTexture(generationData);

        ResetAll();
    }


    public void ResetAll()
    {
        faceImage.texture = null;
        poseImage.texture = null;

    }


    public void UpdateErrorStatusText(string message)
    {
        errorStatusText.text = message;
    }

    private bool ValidateInputs()
    {
        return faceImage.texture != null &&
               poseImage.texture != null &&
               !string.IsNullOrWhiteSpace(promptInputField.text);
    }

    public void UploadFaceImage()
    {
        imageUploader.UploadImage(faceImage);
    }

    public void UploadPoseImage()
    {
        imageUploader.UploadImageWihoutFaceDetection(poseImage);
    }

    public void OpenCamera()
    {
        cameraCapture.StartCamera();
    }
}

