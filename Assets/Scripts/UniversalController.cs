using UnityEngine;
using UnityEngine.UI;

public class UniversalController : MonoBehaviour
{
    public FaceDetector faceDetector;
    public LoadingManager loadingManager;

    public static UniversalController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    
}
