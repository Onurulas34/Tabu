using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    public GameSettings settings;

    [Header("UI")]
    public TextMeshProUGUI teamNameUI;
    public TextMeshProUGUI scoreUI;
    public TextMeshProUGUI timerUI;
    public TextMeshProUGUI passUI;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI finalScoreText;

    [Header("Transition Panel")]
    public GameObject transitionPanel;
    public TextMeshProUGUI nextTeamNameText;

    [Header("Cards")]
    public CardScript cardScript;
    public RectTransform cardRect;  // Kartın tüm UI elemanlarını içeren panel

    private float canvasWidth;

    [Header("Animation")]
    public float animDuration = 0.2f;

    private List<CardData> cards = new List<CardData>();

    private int currentTeamIndex = 0;
    private int currentCardIndex = 0;

    private int remainingPass;
    private float timer;
    private bool isGameOver = false;
    private bool isAnimating = false;

    private void Start()
    {
        DOTween.Init();
        canvasWidth = cardRect.GetComponentInParent<Canvas>()
                              .GetComponent<RectTransform>().rect.width;
        gameOverPanel.SetActive(false);
        transitionPanel.SetActive(false);
        LoadCards();
        StartTurn();
    }

    void LoadCards()
    {
        CardData[] loaded = Resources.LoadAll<CardData>("CardSO");
        cards = new List<CardData>(loaded);
    }

    void Update()
    {
        if (!isGameOver)
            HandleTimer();
    }

    // 🔁 TUR BAŞLAT
    void StartTurn()
    {
        cardRect.DOKill();
        isAnimating = false;

        timer = settings.roundTime;
        remainingPass = settings.maxPass;

        ShuffleCards();
        currentCardIndex = 0;
        ShowCard();
        UpdateUI();
    }

    // ⏱️ TIMER
    void HandleTimer()
    {
        if (transitionPanel.activeSelf) return; // Panel açıkken timer durur

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
        if (isAnimating) return;
        settings.teams[currentTeamIndex].score += cards[currentCardIndex].cardScore;
        UpdateUI();
        CheckWinCondition();
        if (!isGameOver) AnimateNextCard();
    }

    // 🔴 YASAK KELİME
    public void ForbiddenUsed()
    {
        if (isAnimating) return;
        settings.teams[currentTeamIndex].score -= cards[currentCardIndex].negativeScore;
        UpdateUI();
        AnimateNextCard();
    }

    // 🟠 PAS
    public void Pass()
    {
        if (isAnimating || remainingPass <= 0) return;
        remainingPass--;
        UpdateUI();
        AnimateNextCard();
    }

    // ⏭️ KART DEĞİŞTİR (index güncelle)
    void AdvanceIndex()
    {
        currentCardIndex++;
        if (currentCardIndex >= cards.Count)
        {
            ShuffleCards();
            currentCardIndex = 0;
        }
    }

    void ShowCard()
    {
        cardRect.anchoredPosition = Vector2.zero;
        cardScript.SetCard(cards[currentCardIndex]);
    }

    // 🎬 ANİMASYON (DOTween)
    // Telefon dik → mevcut kart sola çıkar, yeni kart sağdan girer
    void AnimateNextCard()
    {
        isAnimating = true;

        cardRect.DOAnchorPosX(-canvasWidth, animDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                AdvanceIndex();
                cardScript.SetCard(cards[currentCardIndex]);
                cardRect.anchoredPosition = new Vector2(canvasWidth, 0);

                cardRect.DOAnchorPosX(0f, animDuration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() => isAnimating = false);
            });
    }

    // 🔄 TAKIM DEĞİŞ → Önce transition panel göster
    void ChangeTeam()
    {
        currentTeamIndex++;

        if (currentTeamIndex >= settings.teams.Length)
            currentTeamIndex = 0;

        transitionPanel.SetActive(true);
        nextTeamNameText.text = settings.teams[currentTeamIndex].teamName;
    }

    // ✅ HAZIR BUTONU → Panel kapanır, tur başlar
    public void OnReadyButton()
    {
        transitionPanel.SetActive(false);
        StartTurn();
    }

    void UpdateUI()
    {
        teamNameUI.text = settings.teams[currentTeamIndex].teamName;
        scoreUI.text = settings.teams[currentTeamIndex].score.ToString();
        passUI.text = remainingPass.ToString();
    }

    // 🏆 KAZANMA KONTROLÜ
    void CheckWinCondition()
    {
        TeamData currentTeam = settings.teams[currentTeamIndex];

        if (currentTeam.score >= settings.targetScore)
        {
            GameOver(currentTeam);
        }
    }

    void GameOver(TeamData winner)
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);

        winnerText.text = winner.teamName;

        string scores = "";
        for (int i = 0; i < settings.teams.Length; i++)
        {
            scores += settings.teams[i].teamName + ": " + settings.teams[i].score + "\n";
        }
        finalScoreText.text = scores;
    }

    // 🔄 TEKRAR OYNA
    public void RestartGame()
    {
        cardRect.DOKill();

        for (int i = 0; i < settings.teams.Length; i++)
        {
            settings.teams[i].score = 0;
        }

        isGameOver = false;
        isAnimating = false;
        currentTeamIndex = 0;
        gameOverPanel.SetActive(false);
        transitionPanel.SetActive(false);
        StartTurn();
    }

    // 🏠 MENÜYE DÖN
    public void GoToMenu()
    {
        for (int i = 0; i < settings.teams.Length; i++)
        {
            settings.teams[i].score = 0;
        }

        SceneManager.LoadScene("Menu");
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
