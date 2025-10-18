using UnityEngine;

[CreateAssetMenu(fileName = "LeonardoConfig", menuName = "Rayqube/Leonardo Generation Config")]
public class LeonardoConfig : ScriptableObject
{
    [Header("API Settings")]
    public string apiKey;
    public string apiBaseUrl = "https://cloud.leonardo.ai/api/rest/v1";
    public string modelId = "aa77f04e-3eec-4034-9c07-d0f619684628";


    [Header("Generation Settings")]
    public int width = 576;
    public int height = 1024;
    public int numberOfVariants = 1;
    public string presetStyle = "CINEMATIC";
    public bool photoReal = true;
    public string photoRealVersion = "v2";
    public bool alchemy = true;
}
