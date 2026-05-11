using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// Skills mini-oyununun merkezi yöneticisi.
///
/// Single Responsibility: Oyun durumu (Playing / GameOver), skor takibi ve UI güncellemesi.
/// Spawn, hareket veya fizik mantığı içermez.
///
/// Zero-GC:
///   • Score string formatlaması Update'te değil, yalnızca gerçek değer değiştiğinde yapılır.
///   • Oyun durumu bir enum ile yönetilir — no string comparison.
///   • UI metni yalnızca integer değer değiştiğinde güncellenir (her frame değil).
/// </summary>
public class SkillsGameManager : MonoBehaviour
{
    // ── Oyun Durumu ───────────────────────────────────────────────────────────

    private enum GameState { WaitingToStart, Playing, GameOver }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Bağlantılar")]
    [SerializeField] private EnemySpawner _spawner;

    [Header("UI — Oyun İçi")]
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("UI — Countdown")]
    [SerializeField] private GameObject      _countdownPanel;
    [SerializeField] private TextMeshProUGUI _countdownText;

    [Header("UI — Game Over Panel")]
    [SerializeField] private GameObject      _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;

    [Header("Sahne")]
    [SerializeField] private string _menuSceneName = "Menu";

    // ── State ─────────────────────────────────────────────────────────────────

    private GameState _state = GameState.WaitingToStart;

    // Float score: Time.deltaTime birikimiyle artar (hassas)
    private float _score;

    // UI sadece integer değer değiştiğinde güncellenir (GC optimizasyonu)
    private int _lastDisplayedScore = -1;

    // PlayerPrefs key (string literal — gameplay'de kullanılmaz, GC sorun değil)
    private const string HighScoreKey = "SkillsHighScore";
    private int _highScore;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>EnemySpawner ve difficulty scaling için dışa açılan skor.</summary>
    public float Score => _score;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        _gameOverPanel.SetActive(false);
        _countdownPanel.SetActive(false);
    }

    private void Start()
    {
        StartCountdown();
    }

    // ── Countdown ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 3–2–1 geri sayımını DOTween Sequence ile çalıştırır.
    /// Her sayı için: scale punch → 1 saniye bekle → sonraki sayı.
    /// Allocation yok: string sabitler, Sequence/Tweener DOTween iç havuzundan gelir.
    /// </summary>
    private void StartCountdown()
    {
        _countdownPanel.SetActive(true);
        _countdownText.transform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // 3 - 2 - 1
        for (int i = 3; i >= 1; i--)
        {
            int number = i; // closure için yerel kopya

            seq.AppendCallback(() =>
            {
                _countdownText.text = number.ToString();
                _countdownText.transform.localScale = Vector3.one;
                _countdownText.transform
                    .DOPunchScale(Vector3.one * 0.5f, 0.6f, 5, 0.5f)
                    .SetUpdate(true);   // Time.timeScale'den bağımsız (ileride gerekirse)
            });

            seq.AppendInterval(1f);
        }

        // Geri sayım bitti → paneli kapat ve oyunu başlat
        seq.AppendCallback(() =>
        {
            _countdownPanel.SetActive(false);
            BeginGame();
        });
    }

    // ── Oyun Akışı ────────────────────────────────────────────────────────────

    private void BeginGame()
    {
        _score              = 0f;
        _lastDisplayedScore = -1;
        _state              = GameState.Playing;

        _gameOverPanel.SetActive(false);
        _spawner.StartSpawning();
    }

    // ── Update — Zero GC ──────────────────────────────────────────────────────

    private void Update()
    {
        if (_state != GameState.Playing) return;

        // Skor: zaman geçtikçe artar
        _score += Time.deltaTime;

        // UI: integer değer değiştiğinde güncelle (her frame string allocation önlenir)
        int displayScore = (int)_score;
        if (displayScore != _lastDisplayedScore)
        {
            _lastDisplayedScore = displayScore;
            UpdateScoreUI(displayScore);
        }
    }

    // ── Score UI ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Score textini günceller. Update'te her frame değil,
    /// yalnızca integer değer değiştiğinde çağrılır.
    /// int.ToString() bir allocation üretir; bunu minimize etmek için
    /// yalnızca bu metodun içinde kalması sağlanır.
    /// </summary>
    private void UpdateScoreUI(int score)
    {
        if (_scoreText != null)
            _scoreText.text = score.ToString();
    }

    // ── Game Over ─────────────────────────────────────────────────────────────

    /// <summary>
    /// PlayerHealth veya EnemyController tarafından çağrılır (collision).
    /// Spawner durdurulur, skor kaydedilir, Game Over paneli açılır.
    /// </summary>
    public void TriggerGameOver()
    {
        if (_state == GameState.GameOver) return; // Çift tetiklemeyi önle

        _state = GameState.GameOver;
        _spawner.StopSpawning();

        // High score kontrolü ve kaydı
        int finalScore = (int)_score;
        if (finalScore > _highScore)
        {
            _highScore = finalScore;
            PlayerPrefs.SetInt(HighScoreKey, _highScore);
            PlayerPrefs.Save();
        }

        ShowGameOverPanel(finalScore);
    }

    private void ShowGameOverPanel(int finalScore)
    {
        _gameOverPanel.SetActive(true);

        if (_finalScoreText != null)
            _finalScoreText.text = finalScore.ToString();

        if (_highScoreText != null)
            _highScoreText.text = _highScore.ToString();
    }

    // ── Buton Metodları ───────────────────────────────────────────────────────

    /// <summary>Tekrar Oyna butonu.</summary>
    public void RestartGame()
    {
        // Sahneyi yeniden yükle — tüm state sıfırlanır
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Ana Menüye Dön butonu.</summary>
    public void GoToMenu()
    {
        SceneManager.LoadScene(_menuSceneName);
    }
}
