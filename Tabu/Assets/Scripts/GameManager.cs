using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    public GameSettings settings;

    [Header("UI")]
    public TextMeshProUGUI teamNameUI;
    public TextMeshProUGUI scoreUI;
    public TextMeshProUGUI timerUI;
    public TextMeshProUGUI passUI;

    [Header("Cards")]
    public List<CardData> cards;
    public CardScript cardScript;

    private int currentTeamIndex = 0;
    private int currentCardIndex = 0;

    private int remainingPass;
    private float timer;

    private void Start()
    {
        StartTurn();
    }

    void Update()
    {
        HandleTimer();
    }

    // 🔁 TUR BAŞLAT
    void StartTurn()
    {
        timer = settings.roundTime;
        remainingPass = settings.maxPass;

        ShuffleCards();
        ShowCard();
        UpdateUI();
    }

    // ⏱️ TIMER
    void HandleTimer()
    {
        timer -= Time.deltaTime;
        timerUI.text = Mathf.Ceil(timer).ToString();

        if (timer <= 0)
        {
            ChangeTeam();
        }
    }

    // 🟢 DOĞRU
    public void CorrectAnswer()
    {
        settings.teams[currentTeamIndex].score += cards[currentCardIndex].cardScore;
        NextCard();
        UpdateUI();
    }

    // 🔴 YASAK KELİME
    public void ForbiddenUsed()
    {
        settings.teams[currentTeamIndex].score -= cards[currentCardIndex].negativeScore;
        NextCard();
        UpdateUI();
    }

    // 🟠 PAS
    public void Pass()
    {
        if (remainingPass <= 0) return;

        remainingPass--;
        NextCard();
        UpdateUI();
    }

    // ⏭️ KART DEĞİŞTİR
    void NextCard()
    {
        currentCardIndex = Random.Range(0, cards.Count);
        ShowCard();
    }

    void ShowCard()
    {
        cardScript.SetCard(cards[currentCardIndex]);
    }

    // 🔄 TAKIM DEĞİŞ
    void ChangeTeam()
    {
        currentTeamIndex++;

        if (currentTeamIndex >= settings.teams.Length)
            currentTeamIndex = 0;

        StartTurn();
    }

    void UpdateUI()
    {
        teamNameUI.text = settings.teams[currentTeamIndex].teamName;
        scoreUI.text = settings.teams[currentTeamIndex].score.ToString();
        passUI.text = remainingPass.ToString();
    }

    // 🎲 SHUFFLE (TUR BAŞI)
    void ShuffleCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int r = Random.Range(i, cards.Count);

            CardData temp = cards[i];
            cards[i] = cards[r];
            cards[r] = temp;
        }
    }
}
