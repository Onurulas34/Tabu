using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class BenKimimManager : MonoBehaviour
{
    [Header("Settings")]
    public GameSettings settings;
    public float roundTime = 60f;

    [Header("UI - Top Bar")]
    public TextMeshProUGUI team1NameText;
    public TextMeshProUGUI team1ScoreText;
    public TextMeshProUGUI team2NameText;
    public TextMeshProUGUI team2ScoreText;

    [Header("UI - Card")]
    public RectTransform nameRect;
    public TextMeshProUGUI cardNameText;

    [Header("UI - Timer")]
    public TextMeshProUGUI timerText;

    [Header("UI - Guess Panel")]
    public GameObject guessPanel;
    public TextMeshProUGUI guessingTeamText;

    [Header("UI - Transition Panel")]
    public GameObject transitionPanel;
    public TextMeshProUGUI nextTeamText;

    [Header("UI - Win Panel")]
    public GameObject winPanel;
    public TextMeshProUGUI winText;

    [Header("Swipe Settings")]
    public float swipeThreshold = 50f;
    public float animDuration = 0.25f;

    private List<CardData> cards = new List<CardData>();
    private int currentIndex = 0;

    private Vector2 touchStart;
    private bool isSwiping = false;
    private bool isAnimating = false;
    private bool gameOver = false;

    private int currentTeam = 0;
    private float canvasHeight;

    private float timeRemaining;
    private bool timerRunning = false;

    private int pendingNextIndex;
    private bool pendingIsDown;

    // ── BAŞLANGIÇ ──

    void Start()
    {
        DOTween.Init();
        canvasHeight = nameRect.GetComponentInParent<Canvas>()
                               .GetComponent<RectTransform>().rect.height;

        for (int i = 0; i < settings.teams.Length; i++)
            settings.teams[i].score = 0;

        LoadCards();
        winPanel.SetActive(false);
        guessPanel.SetActive(false);
        transitionPanel.SetActive(false);
        UpdateTeamUI();
        ShowCard();
        StartTimer();
    }

    // ── KART YÜKLEMESİ ──

    void LoadCards()
    {
        CardData[] loaded = Resources.LoadAll<CardData>("CardSO");
        cards = new List<CardData>(loaded);
        Shuffle();
    }

    void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int r = Random.Range(i, cards.Count);
            CardData temp = cards[i];
            cards[i] = cards[r];
            cards[r] = temp;
        }
    }

    void ShowCard()
    {
        cardNameText.text = cards[currentIndex].cardName;
        nameRect.anchoredPosition = Vector2.zero;
    }

    // ── TIMER ──

    void StartTimer()
    {
        timeRemaining = roundTime;
        timerRunning = true;
        UpdateTimerText();
    }

    void StopTimer()
    {
        timerRunning = false;
    }

    void UpdateTimerText()
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
    }

    // ── SKOR UI ──

    void UpdateTeamUI()
    {
        team1NameText.text = settings.teams[0].teamName;
        team1ScoreText.text = settings.teams[0].score.ToString();
        team2NameText.text = settings.teams[1].teamName;
        team2ScoreText.text = settings.teams[1].score.ToString();

        // Sadece sırası gelen takımı göster
        team1NameText.gameObject.SetActive(currentTeam == 0);
        team1ScoreText.gameObject.SetActive(currentTeam == 0);
        team2NameText.gameObject.SetActive(currentTeam == 1);
        team2ScoreText.gameObject.SetActive(currentTeam == 1);
    }

    // ── UPDATE ──

    void Update()
    {
        if (gameOver) return;

        // Timer sayımı
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerText();

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                UpdateTimerText();
                StopTimer();
                OpenGuessPanel();
                return;
            }
        }

        if (isAnimating || transitionPanel.activeSelf) return;
        HandleSwipe();
    }

    // ── TAHMIN PANELİ ──

    void OpenGuessPanel()
    {
        guessPanel.SetActive(true);

        if (guessingTeamText != null)
            guessingTeamText.text = settings.teams[currentTeam].teamName;
    }

    // Doğru butonu → +1
    public void OnCorrect()
    {
        guessPanel.SetActive(false);
        NextTurn(1);
    }

    // Yanlış butonu → 0
    public void OnWrong()
    {
        guessPanel.SetActive(false);
        NextTurn(0);
    }

    // ── SWIPE ──

    void HandleSwipe()
    {
        if (guessPanel.activeSelf || transitionPanel.activeSelf) return;

        // Touch (mobil)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                isSwiping = true;
            }
            else if (touch.phase == TouchPhase.Ended && isSwiping)
            {
                float delta = touch.position.y - touchStart.y;
                isSwiping = false;

                if (Mathf.Abs(delta) >= swipeThreshold)
                {
                    StopTimer();
                    ProcessSwipe(delta < 0); // aşağı = bildi = +1
                }
            }
        }

        // Mouse (editor test)
        if (Input.GetMouseButtonDown(0))
        {
            touchStart = Input.mousePosition;
            isSwiping = true;
        }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            float delta = ((Vector2)Input.mousePosition).y - touchStart.y;
            isSwiping = false;

            if (Mathf.Abs(delta) >= swipeThreshold)
            {
                StopTimer();
                ProcessSwipe(delta < 0); // aşağı = bildi = +1
            }
        }
    }

    // ── SKOR MANTIĞI ──
    // Aşağı kaydır (kamera → şarj) = Bildi   = +1
    // Yukarı kaydır (şarj → kamera) = Bilmedi =  0

    void ProcessSwipe(bool isDown)
    {
        NextTurn(isDown ? 1 : 0);
    }

    void NextTurn(int scoreChange)
    {
        settings.teams[currentTeam].score += scoreChange;

        // Kazanma kontrolü
        if (settings.teams[currentTeam].score >= 10)
        {
            UpdateTeamUI();
            ShowWinPanel(currentTeam);
            return;
        }

        // Sıradaki takıma geç
        currentTeam = (currentTeam + 1) % 2;
        UpdateTeamUI();

        // Slide için değerleri sakla, transition panel göster
        pendingNextIndex = (currentIndex + 1) % cards.Count;
        pendingIsDown = scoreChange > 0;

        if (nextTeamText != null)
            nextTeamText.text = settings.teams[currentTeam].teamName;

        transitionPanel.SetActive(true);
    }

    // ✅ HAZIR BUTONU
    public void OnReadyButton()
    {
        transitionPanel.SetActive(false);
        SlideAndRestart(pendingNextIndex, pendingIsDown);
    }

    void ShowWinPanel(int teamIndex)
    {
        gameOver = true;
        winPanel.SetActive(true);
        winText.text = settings.teams[teamIndex].teamName + " Kazandı!";
    }

    // ── ANİMASYON (DOTween) ──

    void SlideAndRestart(int nextIndex, bool isDown)
    {
        isAnimating = true;

        float exitY  = isDown ? -canvasHeight :  canvasHeight;
        float enterY = isDown ?  canvasHeight : -canvasHeight;

        // Mevcut kartı çıkar
        nameRect.DOAnchorPosY(exitY, animDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                // Yeni kartı gizli konuma al ve metni güncelle
                currentIndex = nextIndex;
                cardNameText.text = cards[currentIndex].cardName;
                nameRect.anchoredPosition = new Vector2(0, enterY);

                // Yeni kartı içeri çek
                nameRect.DOAnchorPosY(0f, animDuration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        isAnimating = false;
                        StartTimer();
                    });
            });
    }

    // ── TEKRAR OYNA ──

    public void RestartGame()
    {
        // Aktif tween varsa iptal et
        nameRect.DOKill();

        for (int i = 0; i < settings.teams.Length; i++)
            settings.teams[i].score = 0;

        currentTeam = 0;
        currentIndex = 0;
        gameOver = false;
        isAnimating = false;
        isSwiping = false;

        Shuffle();

        winPanel.SetActive(false);
        guessPanel.SetActive(false);
        transitionPanel.SetActive(false);
        UpdateTeamUI();
        ShowCard();
        StartTimer();
    }

    // ── MENÜ ──

    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void BackToHub()
    {
        SceneManager.LoadScene("Menu");
    }
}
