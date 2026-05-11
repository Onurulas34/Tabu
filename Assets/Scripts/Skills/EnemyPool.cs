using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stack tabanlı Object Pool — EnemyController nesneleri için.
///
/// Single Responsibility: Sadece nesne yaşam döngüsünü (al / geri ver) yönetir.
/// Spawn pozisyonu, hareket yönü veya oyun mantığı hakkında hiçbir bilgisi yoktur.
///
/// Zero-GC Garantisi:
///   • Instantiate yalnızca Awake'te (pre-warm) veya pool dolduğunda çağrılır.
///   • Get() ve Return() sırasında hiçbir allocation gerçekleşmez.
///   • Stack<T>, referans tiplerini boxing yapmadan saklar.
/// </summary>
public class EnemyPool : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Pool Ayarları")]
    [SerializeField] private EnemyController _prefab;

    [Tooltip("Oyun başlangıcında oluşturulacak enemy sayısı. " +
             "Yoğun anlarda pool'un bitmesini önlemek için yeterli tutun.")]
    [SerializeField] private int _initialPoolSize = 20;

    // ── State ─────────────────────────────────────────────────────────────────

    // Stack: O(1) Push/Pop, son kullanılan önce gelir (cache locality)
    private readonly Stack<EnemyController> _available = new Stack<EnemyController>();

    // Debug: kaç nesne aktif olduğunu izlemek için
    private int _activeCount;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        PreWarm(_initialPoolSize);

        // Enemy'lerin birbirleriyle çarpışmasını global olarak devre dışı bırak.
        // Physics2D.IgnoreLayerCollision yalnızca bir kez çağrılır → runtime overhead yok.
        int enemyLayer = _prefab.gameObject.layer;
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }

    // ── Pre-Warm ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Belirtilen sayıda enemy'i önceden instantiate eder ve pool'a ekler.
    /// Gameplay sırasında çağrılmamalıdır.
    /// </summary>
    private void PreWarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            EnemyController enemy = Instantiate(_prefab, transform);
            enemy.gameObject.SetActive(false);
            _available.Push(enemy);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Pool'dan bir enemy alır. Pool boşsa yeni bir nesne instantiate edilir
    /// (bu durum sadece _initialPoolSize yetersiz kaldığında oluşur — bir Debug.LogWarning üretir).
    /// </summary>
    public EnemyController Get()
    {
        EnemyController enemy;

        if (_available.Count > 0)
        {
            enemy = _available.Pop();
        }
        else
        {
            // Pool bitti — sessizce genişlet ama uyar
            Debug.LogWarning("[EnemyPool] Pool tükendi! _initialPoolSize artırılmalı.");
            enemy = Instantiate(_prefab, transform);
        }

        enemy.gameObject.SetActive(true);
        _activeCount++;
        return enemy;
    }

    /// <summary>
    /// Bir enemy'i pool'a geri döndürür. SetActive(false) burada yapılır.
    /// EnemyController dışarıdan bu metodu çağırır.
    /// </summary>
    public void Return(EnemyController enemy)
    {
        enemy.gameObject.SetActive(false);
        _available.Push(enemy);
        _activeCount--;
    }

    // ── Debug / Editor ────────────────────────────────────────────────────────

    public int AvailableCount => _available.Count;
    public int ActiveCount    => _activeCount;
}
