// ============================================================================
// GÜVENLİK ANAHTARLARI VERİTABANINDA (oturum çerezleri ve form koruması için)
// ----------------------------------------------------------------------------
// ASP.NET, giriş çerezini ve formlardaki gizli güvenlik kodunu (anti-forgery) bu anahtarlarla şifreler.
// Anahtarlar diskte durursa Render'ın ücretsiz planında sorun çıkar: site boşta kalınca uyur, uyanınca
// disk sıfırlanır → yeni anahtar üretilir → herkesin oturumu kapanır, açık kalmış formlar "400 Bad Request" verir.
// Canlı sitede (Neon) anahtarlar bu yüzden GuvenlikAnahtarlari tablosunda tutulur (Program.cs → "GİRİŞ SİSTEMİ").
// Bilgisayarda (SQLite) eskisi gibi diskte kalır.
// Tablo kodda tanımlıdır; Neon'da yoksa SemaGuncelleyici açılışta kendiliğinden ekler.
// ============================================================================
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AracKiralama.Veri;

/// <summary>Bir güvenlik anahtarı (ASP.NET'in ürettiği XML, olduğu gibi saklanır).</summary>
public class GuvenlikAnahtari
{
    public int Id { get; set; }
    public string? Ad { get; set; }           // ASP.NET'in verdiği ad, örn. "key-3f2a..."
    public string Xml { get; set; } = "";
}

/// <summary>ASP.NET'in anahtar deposu arayüzü (IXmlRepository): anahtarları okur ve yeni anahtarı kaydeder.</summary>
public class VeritabaniAnahtarDeposu(IServiceScopeFactory kapsamlar) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var kapsam = kapsamlar.CreateScope();
        var vt = kapsam.ServiceProvider.GetRequiredService<VeriTabani>();
        return vt.GuvenlikAnahtarlari.AsNoTracking().OrderBy(a => a.Id).Select(a => a.Xml).ToList()
                 .Select(XElement.Parse).ToList().AsReadOnly();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var kapsam = kapsamlar.CreateScope();
        var vt = kapsam.ServiceProvider.GetRequiredService<VeriTabani>();
        vt.GuvenlikAnahtarlari.Add(new GuvenlikAnahtari { Ad = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting) });
        vt.SaveChanges();
    }
}
