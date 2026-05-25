using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButtonController : MonoBehaviour
{
    public void OnPlayButtonClicked()
    {
        SceneManager.LoadScene("Game Play"); // Replace with your scene name
    }
}