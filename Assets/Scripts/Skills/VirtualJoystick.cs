using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI Canvas-based Virtual Joystick.
/// Single Responsibility: Sadece dokunmatik giriş okur ve normalize edilmiş
/// bir yön vektörü (InputVector) dışa açar. Hareket mantığı içermez.
///
/// Zero-GC: Update/FixedUpdate çağrısı yok. Tüm referanslar Awake'te cache'lenir.
/// PointerEventData bir struct wrapper olduğundan OnDrag içinde allocation yoktur.
/// </summary>
public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Joystick UI")]
    [SerializeField] private RectTransform _background;  // Dış halka
    [SerializeField] private RectTransform _handle;      // İç top (knob)

    [Header("Ayarlar")]
    [SerializeField, Range(0f, 1f)]
    private float _handleRange = 0.9f;   // Handle'ın arka plana göre max uzaklık oranı

    [SerializeField, Range(0f, 0.3f)]
    private float _deadZone = 0.05f;     // Bu değerin altındaki input sıfırlanır

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Normalize edilmiş hareket yönü [-1, 1].
    /// PlayerMovement tarafından FixedUpdate'te okunur.
    /// Vector2 bir value type olduğu için GC allocation yoktur.
    /// </summary>
    public Vector2 InputVector { get; private set; }

    // ── Cached References ─────────────────────────────────────────────────

    private Camera _canvasCamera;   // ScreenOverlay modda null
    private float  _backgroundRadius;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Canvas render moduna göre kamera referansı (allocation yok)
        var canvas = GetComponentInParent<Canvas>();
        _canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        // Yarıçapı bir kez hesapla; ekran çözünürlüğü değişmediği sürece geçerli
        _backgroundRadius = _background.sizeDelta.x * 0.5f;
    }

    // ── IPointerDownHandler ───────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        // Parmak ilk değdiğinde sürükleme ile aynı mantığı uygula
        OnDrag(eventData);
    }

    // ── IDragHandler ─────────────────────────────────────────────────────

    public void OnDrag(PointerEventData eventData)
    {
        // Ekran koordinatını canvas local koordinatına çevir
        // ScreenPointToLocalPointInRectangle out parametresi kullandığından GC yoktur
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _background,
            eventData.position,
            _canvasCamera,
            out var localPoint
        );

        // Yarıçapa göre normalize et
        Vector2 direction = localPoint / _backgroundRadius;

        // Çemberin dışına çıkmayı engelle
        Vector2 clamped = direction.magnitude > 1f
            ? direction.normalized
            : direction;

        // Dead zone: çok küçük inputları sıfırla (titreme önleme)
        InputVector = clamped.magnitude < _deadZone
            ? Vector2.zero
            : clamped;

        // Handle'ı görsel olarak taşı (sıklıkla çağrılan ama küçük bir op)
        _handle.anchoredPosition = InputVector * (_backgroundRadius * _handleRange);
    }

    // ── IPointerUpHandler ────────────────────────────────────────────────

    public void OnPointerUp(PointerEventData eventData)
    {
        // Parmak kalktığında input ve handle'ı sıfırla
        InputVector = Vector2.zero;
        _handle.anchoredPosition = Vector2.zero;
    }
}
