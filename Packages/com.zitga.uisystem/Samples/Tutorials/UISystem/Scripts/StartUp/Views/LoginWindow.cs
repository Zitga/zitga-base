using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Loxodon.Framework.Views;
using UnityEngine;
using UnityEngine.UI;

public class LoginWindowProperties : AWindowProperties
{
    public string name = "Hai";
    public int level = 5;
}

public class LoginWindow : Window<LoginWindowProperties>
{
    public InputField username;
    public InputField password;
    public Button confirmButton;
    public Button cancelButton;
    public Text signIn;

    protected override void OnCreate(IBundle bundle)
    {
        confirmButton.onClick.AddListener(OnClickConfirm);
        cancelButton.onClick.AddListener(OnClickCancel);
    }

    protected override void OnPropertiesSet()
    {
        log.Debug(Properties.name);
    }

    protected override async UniTaskVoid OnLocalizeChanged()
    {
        // signIn.text = await R.mail.arena_season_ranking_reward_content;
        Debug.Log("Localize change: " + signIn.text);
    }

    private void OnClickConfirm()
    {
        // Localization.Current.CultureInfo = Locale.GetCultureInfoByLanguage(SystemLanguage.English);
        // if (!(ValidateUsername(username.text) && ValidatePassword(password.text)))
        // {
        //     log.Debug("Input is not valid");
        //     return;
        // }

        CurrentGlobalWindowManager.CloseWindow(this);
    }

    private void OnClickCancel()
    {
        CurrentGlobalWindowManager.CloseWindow(this);
    }

    private bool ValidateUsername(string username)
    {
        if (string.IsNullOrEmpty(username) || !Regex.IsMatch(username, "^[a-zA-Z0-9_-]{4,12}$"))
        {
            // localization.GetText("login.validation.username.error", "Please enter a valid username.");
            return false;
        }
        return true;
    }

    private bool ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || !Regex.IsMatch(password, "^[a-zA-Z0-9_-]{4,12}$"))
        {
            // "login.validation.password.error", "Please enter a valid password.";
            return false;
        }
        return true;
    }
}