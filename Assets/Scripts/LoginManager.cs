using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// Simple login screen manager. Attach to a Canvas in the Login scene.
public class LoginManager : MonoBehaviour
{
    public Button signInWithAppleButton;
    public TextMeshProUGUI feedbackText;

    // Optional username/password fields for a slightly more realistic demo
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public string expectedUsername = "user";
    public string expectedPassword = "pass";
    public string successSceneName = "HumanoidScene";

    void Start()
    {
        if (signInWithAppleButton != null)
            signInWithAppleButton.onClick.AddListener(OnSignInClicked);
        if (feedbackText != null)
            feedbackText.text = "";
    }

    void OnSignInClicked()
    {
        bool success = false;
        if (usernameField != null && passwordField != null)
        {
            success = (usernameField.text == expectedUsername && passwordField.text == expectedPassword);
        }
        else
        {
            success = SimulateLogin();
        }

        if (success)
        {
            SceneManager.LoadScene(successSceneName);
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text = "Invalid login";
        }
    }

    bool SimulateLogin()
    {
        int sec = System.DateTime.Now.Second;
        return (sec % 2) == 0;
    }
}
