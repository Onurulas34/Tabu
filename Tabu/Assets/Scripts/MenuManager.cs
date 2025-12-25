using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public GameSettings settings;
    public TMP_InputField[] teamNameInputs;

    public void StartGame()
    {
        for (int i = 0; i < settings.teams.Length; i++)
        {
            // TEAM NAME
            string inputName = teamNameInputs[i].text;

            if (string.IsNullOrWhiteSpace(inputName))
                settings.teams[i].teamName = (i == 0) ? "Red" : "Blue";
            else
                settings.teams[i].teamName = inputName.Trim();

            settings.teams[i].score = 0;
        }

        SceneManager.LoadScene("Game");
    }
}
