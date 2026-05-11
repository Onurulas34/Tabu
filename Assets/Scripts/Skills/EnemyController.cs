using System;
using UnityEngine;

/// <summary>
/// Tek bir düşmanın davranışını yönetir.
///
/// Single Responsibility: Sadece doğrusal hareket ve ekran dışı tespiti.
/// Spawn mantığı (nereden çıkacağı, hangi hızla geleceği) EnemySpawner'a aittir.
///
/// Hareket Mimarisi:
///   Dynamic Rigidbody2D + gravityScale=0 + linearVelocity yalnızca Initialize'da SET edilir.
///   Fizik motoru hareketi işler → Update'te MovePosition çağrısı yoktur → CPU tasarrufu.
///
/// Zero-GC:
///   • Update'te struct (Vector2, float) ile yapılan matematiksel karşılaştırmalar → allocation yok.
///   • Action<EnemyController> delegate Initialize'da atanır, Update'te çağrılmaz (sadece ReturnToPool'da).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("Ekranın kenarından ne kadar uzaklaştığında pool'a dönüleceği (world unit).")]
    [SerializeField] private float _offScreenBuffer = 1f;

    // ── Cached References ─────────────────────────────────────────────────────

    private Rigidbody2D _rb;

    // ── Camera Bounds (Initialize'da set edilir, değişmez) ───────────────────

    private float _camHalfWidth;
    private float _camHalfHeight;

    // ── Pool Callback ─────────────────────────────────────────────────────────

    // EnemyPool.Return(this) çağrısını sarar; EnemyController EnemyPool'u doğrudan bilmez (SOLID - DIP)
    private Action<EnemyController> _returnToPool;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isActive;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Unity 6 API kullanımı
        _rb.bodyType         = RigidbodyType2D.Dynamic;
        _rb.gravityScale     = 0f;
        _rb.linearDamping    = 0f;   // Sürtünme yok: sabit hızda gider
        _rb.angularDamping   = 0f;
        _rb.constraints      = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnDisable()
    {
        // Pool'a dönerken hızı sıfırla; aksi halde reaktive edildiğinde eski velocity kalır
        _isActive = false;
        _rb.linearVelocity = Vector2.zero;
    }

    // ── Public API — Spawner tarafından çağrılır ──────────────────────────────

    /// <summary>
    /// Enemy'i canlandırır. Pool'dan alındıktan hemen sonra EnemySpawner tarafından çağrılır.
    /// </summary>
    /// <param name="spawnPosition">Ekranın dışındaki başlangıç noktası.</param>
    /// <param name="moveDirection">Normalize edilmiş hareket yönü (spawn anında hesaplanır, sonra değişmez).</param>
    /// <param name="speed">Hareket hızı (difficulty scaling ile değişir).</param>
    /// <param name="camHalfWidth">Kamera yarı genişliği (world units).</param>
    /// <param name="camHalfHeight">Kamera yarı yüksekliği (world units).</param>
    /// <param name="returnToPool">Pool'a dönüş callback'i.</param>
    public void Initialize(
        Vector2                  spawnPosition,
        Vector2                  moveDirection,
        float                    speed,
        float                    camHalfWidth,
        float                    camHalfHeight,
        Action<EnemyController>  returnToPool)
    {
        // Pozisyonu ayarla (Rigidbody2D.position: physics-safe, allocation yok)
        _rb.position = spawnPosition;

        // Hız yalnızca burada set edilir; fizik motoru gerisini halleder
        // Normalize güvencesi: EnemySpawner normalize ederek gönderir
        _rb.linearVelocity = moveDirection * speed;

        _camHalfWidth  = camHalfWidth;
        _camHalfHeight = camHalfHeight;
        _returnToPool  = returnToPool;
        _isActive      = true;
    }

    // ── Update — Ekran Dışı Tespiti (Zero GC) ────────────────────────────────

    private void Update()
    {
        if (!_isActive) return;

        // Rigidbody2D.position: struct döndürür, allocation yok
        Vector2 pos = _rb.position;

        float boundX = _camHalfWidth  + _offScreenBuffer;
        float boundY = _camHalfHeight + _offScreenBuffer;

        // Dört kenar için matematiksel kontrol — branch predictor dostu yapı
        if (pos.x < -boundX || pos.x > boundX ||
            pos.y < -boundY || pos.y > boundY)
        {
            ReturnToPool();
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void ReturnToPool()
    {
        _isActive = false;
        // Delegate çağrısı: pool hangi sınıf olursa olsun bağımsız (DIP)
        _returnToPool?.Invoke(this);
    }
}
