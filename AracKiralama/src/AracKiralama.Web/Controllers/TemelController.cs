// ============================================================================
// TEMEL CONTROLLER: bütün controller'ların ortak atası.
// Tekrar eden küçük işler burada: giriş yapan kullanıcının Id'si, ekranda
// yeşil/kırmızı bildirim göstermek, PDF'i tarayıcıda açmak.
// ============================================================================
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AracKiralama.Controllers;

public abstract class TemelController : Controller
{
    /// <summary>Giriş yapan kullanıcının Id'si (giriş yoksa boş).</summary>
    protected string KullaniciId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    /// <summary>Bir sonraki sayfada yeşil bildirim gösterir (Views/Shared/_Bildirim.cshtml).</summary>
    protected void Basarili(string mesaj) => TempData["Basarili"] = mesaj;

    /// <summary>Bir sonraki sayfada kırmızı bildirim gösterir.</summary>
    protected void Hata(string mesaj) => TempData["Hata"] = mesaj;

    /// <summary>PDF'i indirmeye zorlamadan tarayıcıda açar.</summary>
    protected IActionResult PdfGoster(byte[] pdf, string dosyaAdi)
    {
        Response.Headers.ContentDisposition = $"inline; filename=\"{dosyaAdi}\"";
        return File(pdf, "application/pdf");
    }
}
