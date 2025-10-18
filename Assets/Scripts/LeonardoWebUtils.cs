using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class LeonardoWebUtils
{
    public static UnityWebRequest CreateWebRequest(string url, string method, string apiKey, string jsonData = null)
    {
        var request = new UnityWebRequest(url, method);

        if (!string.IsNullOrEmpty(jsonData))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        }

        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("content-type", "application/json");
        request.SetRequestHeader("authorization", "Bearer " + apiKey);

        return request;
    }

    public static async Task<UnityWebRequest.Result> SendWebRequest(this UnityWebRequest request)
    {
        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        return request.result;
    }
}
