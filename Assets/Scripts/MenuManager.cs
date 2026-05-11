using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Settings")]
    public GameSettings settings;

    [Header("Panels")]
    public GameObject hubPanel;
    public GameObject tabuSetupPanel;
    public GameObject benKimimSetupPanel;

    [Header("Tabu Setup")]
    public TMP_InputField[] teamNameInputs;

    [Header("Ben Kimim Setup")]
    public TMP_InputField[] benKimimTeamNameInputs;

    void Start()
    {
        ShowHub();
    }

    // ── HUB ──

    public void ShowHub()
    {
        hubPanel.SetActive(true);
        tabuSetupPanel.SetActive(false);
        benKimimSetupPanel.SetActive(false);
    }

    // ── SKILLS ──

    public void StartSkills()
    {
        SceneManager.LoadScene("Skills");
    }

    // ── TABU ──

    public void OpenTabuSetup()
    {
        hubPanel.SetActive(false);
        tabuSetupPanel.SetActive(true);

        for (int i = 0; i < teamNameInputs.Length; i++)
            teamNameInputs[i].text = "";
    }

    public void StartTabu()
    {
        for (int i = 0; i < settings.teams.Length; i++)
        {
            string inputName = teamNameInputs[i].text;

            if (string.IsNullOrWhiteSpace(inputName))
                settings.teams[i].teamName = (i == 0) ? "Red" : "Blue";
            else
                settings.teams[i].teamName = inputName.Trim();

            settings.teams[i].score = 0;
        }

        SceneManager.LoadScene("Game");
    }

    public void BackToHub()
    {
        ShowHub();
    }

    // ── BEN KİMİM ──

    public void OpenBenKimimSetup()
    {
        hubPanel.SetActive(false);
        benKimimSetupPanel.SetActive(true);

        for (int i = 0; i < benKimimTeamNameInputs.Length; i++)
            benKimimTeamNameInputs[i].text = "";
    }

    public void StartBenKimim()
    {
        for (int i = 0; i < settings.teams.Length; i++)
        {
            string inputName = benKimimTeamNameInputs[i].text;

            if (string.IsNullOrWhiteSpace(inputName))
                settings.teams[i].teamName = (i == 0) ? "Red" : "Blue";
            else
                settings.teams[i].teamName = inputName.Trim();

            settings.teams[i].score = 0;
        }

        SceneManager.LoadScene("BenKimim");
    }
}
