using UnityEngine;

/// <summary>
/// Oyuncu hareketini yönetir.
/// Single Responsibility: Sadece Rigidbody2D hareketi ve kamera sınırları içinde kalmak.
///
/// Tasarım kararları:
///   - Kinematic Rigidbody2D: Fizik motorunun yerçekimi / kuvvet hesaplamalarını
///     tamamen devre dışı bırakır; hareket tamamen kod tarafından kontrol edilir.
///     Bu sayede ek fizik overhead yoktur.
///   - MovePosition: Collision detection'ı korurken kinematic hareketi sağlar.
///   - Interpolate: Görsel titreme olmadan akıcı render sağlar.
///   - Zero-GC: FixedUpdate içinde hiçbir allocation yoktur.
///     Tüm değerler value type (Vector2, float) ile hesaplanır.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Bağlantılar")]
    [SerializeField] private VirtualJoystick _joystick;

    [Header("Hareket")]
    [SerializeField] private float _moveSpeed = 5f;

    // ── Cached References ────────────────────────────────────────────────────

    private Rigidbody2D _rb;
    private Camera      _mainCamera;

    // ── Camera Bounds (value types, GC yok) ──────────────────────────────────

    private float _halfWidth;
    private float _halfHeight;

    // Oyuncu sprite'ının yarı boyutları (sınır hesabında yumuşak kenar için)
    private float _spriteHalfW;
    private float _spriteHalfH;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Rigidbody2D kurulumu — referans cache'leme
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType                 = RigidbodyType2D.Kinematic;
        _rb.collisionDetectionMode   = CollisionDetectionMode2D.Continuous;
        _rb.interpolation            = RigidbodyInterpolation2D.Interpolate;
        _rb.constraints              = RigidbodyConstraints2D.FreezeRotation;

        _mainCamera = Camera.main;
    }

    private void Start()
    {
        CacheBounds();
    }

    // ── Bounds Hesaplama ─────────────────────────────────────────────────────

    /// <summary>
    /// Kamera sınırlarını ve sprite boyutlarını hesaplar.
    /// Sadece Start'ta çağrılır; gameplay sırasında hiç çağrılmaz.
    /// </summary>
    private void CacheBounds()
    {
        _halfHeight = _mainCamera.orthographicSize;
        _halfWidth  = _halfHeight * _mainCamera.aspect;

        // Eğer SpriteRenderer varsa sprite'ın yarı boyutunu sınıra ekle
        if (TryGetComponent<SpriteRenderer>(out var sr))
        {
            _spriteHalfW = sr.bounds.extents.x;
            _spriteHalfH = sr.bounds.extents.y;
        }
    }

    // ── FixedUpdate — Zero GC ────────────────────────────────────────────────

    private void FixedUpdate()
    {
        // VirtualJoystick'ten normalize yön oku (value type, allocation yok)
        Vector2 input = _joystick.InputVector;

        // Yeni pozisyonu hesapla
        Vector2 newPos = _rb.position + input * (_moveSpeed * Time.fixedDeltaTime);

        // Kamera sınırları içine kısıtla (sprite boyutu dikkate alınır)
        newPos.x = Mathf.Clamp(newPos.x, -_halfWidth  + _spriteHalfW,  _halfWidth  - _spriteHalfW);
        newPos.y = Mathf.Clamp(newPos.y, -_halfHeight + _spriteHalfH, _halfHeight - _spriteHalfH);

        // Fizik motoruna güvenli hareket komutu ver
        _rb.MovePosition(newPos);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Hareket hızını dışarıdan (örn. GameManager) ayarlamak için.
    /// Difficulty scaling gerekirse buradan çağrılır.
    /// </summary>
    public void SetSpeed(float newSpeed) => _moveSpeed = newSpeed;

    /// <summary>Mevcut hareket hızı.</summary>
    public float MoveSpeed => _moveSpeed;
}
