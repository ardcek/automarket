// ============================================================================
// YÖNETİM PANELİ ORTAK ATASI
// [Area("Yonetim")]  → adresler /Yonetim/... ile başlar, view'lar Areas/Yonetim/Views altında.
// [Authorize(Roles)] → sadece Admin ve Personel girebilir; müşteri "Erişim engellendi" görür.
// Bütün yönetim controller'ları bu sınıftan türer.
// ============================================================================
using AracKiralama.Controllers;
using AracKiralama.Yardimcilar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AracKiralama.Areas.Yonetim.Controllers;

[Area("Yonetim")]
[Authorize(Roles = Roller.Yonetim)]
public abstract class YonetimController : TemelController
{
}
