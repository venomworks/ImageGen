using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeonardoAIWithPose : MonoBehaviour
{
    private LeonardoGenerationManager _generationManager;

    public LeonardoConfig leonardoConfig;
    [SerializeField] LeonardoUploadManager leonardoUploadManager;

    private void Start()
    {
        _generationManager = new(leonardoConfig);
    }

    public async Task<string> StartGenerate(string prompt, string faceID, string poseID)
    {
        return await _generationManager.GenerateImageAsync(prompt, faceID, poseID);
    }

    public async Task<JToken> FetchImage(string generationID)
    {
        return await _generationManager.FetchImageAsync(generationID);
    }

    public async Task<Texture2D> GetGeneratedTexture(JToken imageJtoken)
    {
        return await _generationManager.DownloadAndDisplayImageAsync(imageJtoken);
    }
}
