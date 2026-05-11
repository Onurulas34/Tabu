using UnityEngine;

/// <summary>
/// Oyuncunun düşmanla çarpışmasını tespit eder.
///
/// Single Responsibility: Sadece çarpışma tespiti ve GameManager'a bildirim.
/// Skor, UI veya spawn mantığı içermez.
///
/// Kurulum gereksinimleri:
///   • Player objesinde Collider2D (IsTrigger = true) olmalı.
///   • Enemy objesinde Collider2D (IsTrigger = false veya true) olmalı.
///   • Enemy'nin Layer'ı Inspector'da "Enemy" olarak ayarlanmalı.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [SerializeField] private SkillsGameManager _gameManager;

    [Tooltip("Sadece bu layer'daki objelerle çarpışma sayılır.")]
    [SerializeField] private LayerMask _enemyLayer;

    // ── Çarpışma Tespiti ──────────────────────────────────────────────────────

    /// <summary>
    /// Trigger tabanlı tespit — Collider2D.IsTrigger = true gerektirir.
    /// Physics2D'de OnTriggerEnter2D heap allocation üretmez.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // LayerMask bit kontrolü: allocation yok
        if ((_enemyLayer.value & (1 << other.gameObject.layer)) == 0) return;

        _gameManager.TriggerGameOver();
    }
}
