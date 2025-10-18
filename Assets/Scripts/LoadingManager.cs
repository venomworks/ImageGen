using TMPro;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TextMeshProUGUI status;


    public void ShowLoadingScreen(string message)
    {
        loadingScreen.SetActive(true);
        status.text = message;
    }

    public void HideLoadingScreen()
    {
        loadingScreen.SetActive(false);
    }
}
