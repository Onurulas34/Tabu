using UnityEngine;

/// <summary>
/// Düşman spawn mantığını yönetir.
///
/// Single Responsibility: Sadece NEREDE, NE ZAMAN ve HANGİ HIZDA spawn yapılacağını belirler.
/// Pool yönetimi EnemyPool'a, skor/zorluk hesabı SkillsGameManager'a aittir.
///
/// Zero-GC Garantisi:
///   • Coroutine veya WaitForSeconds kullanılmaz; manuel timer ile Update'te sayım yapılır.
///   • Spawn pozisyonu Vector2 (value type) ile hesaplanır — allocation yok.
///   • Random.Range yalnızca spawn anında çağrılır — Update'te her frame çağrılmaz.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Bağlantılar")]
    [SerializeField] private EnemyPool          _pool;
    [SerializeField] private SkillsGameManager  _gameManager;
    [SerializeField] private Transform          _playerTransform;
    [SerializeField] private EnemyController    _enemyPrefab;  // Boyut hesabı için

    [Header("Spawn Aralığı (saniye)")]
    [Tooltip("Oyun başındaki spawn aralığı.")]
    [SerializeField] private float _baseSpawnDelay  = 2.5f;

    [Tooltip("Ulaşılabilecek minimum spawn aralığı (en yoğun zorluk).")]
    [SerializeField] private float _minSpawnDelay   = 0.4f;

    [Header("Düşman Hızı")]
    [SerializeField] private float _baseEnemySpeed  = 2.5f;
    [SerializeField] private float _maxEnemySpeed   = 8f;

    [Header("Difficulty Scaling")]
    [Tooltip("Bu skor değerinde zorluk maksimuma ulaşır.")]
    [SerializeField] private float _maxDifficultyScore = 300f;

    [Tooltip("Spawn noktasının ekran kenarına olan temel mesafesi (world unit). " +
             "Enemy sprite boyutu bu değere otomatik eklenir.")]
    [SerializeField] private float _spawnBuffer = 1.5f;

    // ── Cached — Camera Bounds ────────────────────────────────────────────────

    private float _camHalfWidth;
    private float _camHalfHeight;

    // ── State ─────────────────────────────────────────────────────────────────

    private float _spawnTimer;
    private float _currentSpawnDelay;
    private float _currentEnemySpeed;
    private bool  _isRunning;

    // Spawn pozisyonunu her seferinde new Vector2 yapmadan tutmak için
    private Vector2 _spawnPos;
    private Vector2 _moveDir;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Kamera sınırlarını bir kez cache'le
        Camera cam     = Camera.main;
        _camHalfHeight = cam.orthographicSize;
        _camHalfWidth  = _camHalfHeight * cam.aspect;

        // Enemy prefab'ının sprite yarıçapını buffer'a ekle.
        // Böylece spawn anında enemy tamamen ekran dışında olur.
        if (_enemyPrefab != null &&
            _enemyPrefab.TryGetComponent<SpriteRenderer>(out var sr))
        {
            float enemyRadius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
            _spawnBuffer += enemyRadius;
        }
    }

    private void Start()
    {
        _currentSpawnDelay = _baseSpawnDelay;
        _currentEnemySpeed = _baseEnemySpeed;
        _spawnTimer        = _currentSpawnDelay;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void StartSpawning() => _isRunning = true;
    public void StopSpawning()  => _isRunning = false;

    // ── Update — Zero GC Timer ────────────────────────────────────────────────

    private void Update()
    {
        if (!_isRunning) return;

        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0f)
        {
            UpdateDifficulty(_gameManager.Score);
            SpawnEnemy();
            _spawnTimer = _currentSpawnDelay;
        }
    }

    // ── Difficulty Scaling ────────────────────────────────────────────────────

    /// <summary>
    /// Score'a bağlı olarak spawn aralığını ve düşman hızını günceller.
    /// Tüm hesaplamalar float aritmetiği — allocation yok.
    /// </summary>
    private void UpdateDifficulty(float score)
    {
        // 0-1 arası normalize edilmiş zorluk değeri
        float t = Mathf.Clamp01(score / _maxDifficultyScore);

        // Lerp: başlangıç → hedef değerlere doğrusal geçiş
        _currentSpawnDelay = Mathf.Lerp(_baseSpawnDelay, _minSpawnDelay,  t);
        _currentEnemySpeed = Mathf.Lerp(_baseEnemySpeed, _maxEnemySpeed,  t);
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnEnemy()
    {
        EnemyController enemy = _pool.Get();

        // 1) Ekran dışında rastgele bir spawn noktası belirle
        CalculateSpawnPosition(out _spawnPos);

        // 2) Spawn ANINDAKİ oyuncu pozisyonunu oku (bir daha güncellenmez — no homing)
        Vector2 playerPos = _playerTransform.position;

        // 3) Yön vektörünü normalize et (struct, allocation yok)
        _moveDir = (playerPos - _spawnPos).normalized;

        // 4) Enemy'i başlat
        enemy.Initialize(
            spawnPosition : _spawnPos,
            moveDirection : _moveDir,
            speed         : _currentEnemySpeed,
            camHalfWidth  : _camHalfWidth,
            camHalfHeight : _camHalfHeight,
            returnToPool  : _pool.Return
        );
    }

    /// <summary>
    /// Ekranın 4 kenarından birini rastgele seçer ve o kenarda
    /// görünür alanın hemen dışında bir nokta üretir.
    /// Tüm hesaplamalar value type — allocation yok.
    /// </summary>
    private void CalculateSpawnPosition(out Vector2 result)
    {
        // 0=Sol 1=Sağ 2=Alt 3=Üst
        int side = Random.Range(0, 4);

        float x, y;

        switch (side)
        {
            case 0: // Sol kenar
                x = -_camHalfWidth - _spawnBuffer;
                y = Random.Range(-_camHalfHeight, _camHalfHeight);
                break;
            case 1: // Sağ kenar
                x = _camHalfWidth + _spawnBuffer;
                y = Random.Range(-_camHalfHeight, _camHalfHeight);
                break;
            case 2: // Alt kenar
                x = Random.Range(-_camHalfWidth, _camHalfWidth);
                y = -_camHalfHeight - _spawnBuffer;
                break;
            default: // Üst kenar
                x = Random.Range(-_camHalfWidth, _camHalfWidth);
                y = _camHalfHeight + _spawnBuffer;
                break;
        }

        result = new Vector2(x, y);
    }
}
