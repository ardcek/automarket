// ============================================================================
// ALTYAPI YARDIMCILARI (bir kere yazılır, pek değişmez)
// - GuvenlikBasliklari : her sayfaya güvenlik başlığı (CSP) ekler
// - Turkce...Mesajlari : giriş/kayıt ve form hata mesajlarının Türkçesi
// ============================================================================
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace AracKiralama.Yardimcilar;

/// <summary>
/// Content-Security-Policy: tarayıcı sadece BU sitenin script / stil / yazı tipini çalıştırır.
/// Sayfaya dışarıdan zararlı kod sokulsa bile çalışmaz (XSS koruması).
/// Bu yüzden view'larda &lt;script&gt;...&lt;/script&gt; veya onclick="..." yazılmaz; JS dosyaları wwwroot/js altındadır.
/// </summary>
public static class GuvenlikBasliklari
{
    public static IApplicationBuilder UseGuvenlikBasliklari(this IApplicationBuilder uygulama)
    {
        return uygulama.Use(async (istek, sonraki) =>
        {
            var basliklar = istek.Response.Headers;
            basliklar["X-Content-Type-Options"] = "nosniff";
            basliklar["Referrer-Policy"] = "strict-origin-when-cross-origin";
            basliklar["X-Frame-Options"] = "SAMEORIGIN";

            // Swagger (API dokümantasyonu) kendi script'lerini kullandığı için orada uygulanmaz.
            if (!istek.Request.Path.StartsWithSegments("/swagger"))
                basliklar["Content-Security-Policy"] =
                    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; " +
                    "font-src 'self'; connect-src 'self'; frame-ancestors 'self'; base-uri 'self'; form-action 'self'";

            await sonraki();
        });
    }
}

/// <summary>ASP.NET Identity'nin (kayıt / şifre) hata mesajları.</summary>
public class TurkceIdentityMesajlari : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string e) => new() { Code = nameof(DuplicateEmail), Description = $"'{e}' adresiyle kayıtlı bir hesap zaten var." };
    public override IdentityError DuplicateUserName(string k) => new() { Code = nameof(DuplicateUserName), Description = $"'{k}' zaten kullanılıyor." };
    public override IdentityError InvalidEmail(string? e) => new() { Code = nameof(InvalidEmail), Description = "Geçersiz e-posta adresi." };
    public override IdentityError InvalidUserName(string? k) => new() { Code = nameof(InvalidUserName), Description = "E-posta adresi geçersiz karakter içeriyor." };
    public override IdentityError PasswordTooShort(int n) => new() { Code = nameof(PasswordTooShort), Description = $"Şifre en az {n} karakter olmalıdır." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam içermelidir." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf içermelidir." };
}

/// <summary>Form doldururken çıkan otomatik hata mesajları ("'abc' geçerli bir sayı değil" gibi).</summary>
public static class TurkceFormMesajlari
{
    public static void Uygula(DefaultModelBindingMessageProvider m)
    {
        m.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
        m.SetMissingBindRequiredValueAccessor(ad => $"'{ad}' alanı zorunludur.");
        m.SetAttemptedValueIsInvalidAccessor((deger, _) => $"'{deger}' geçerli bir değer değil.");
        m.SetUnknownValueIsInvalidAccessor(_ => "Girilen değer geçersiz.");
        m.SetValueIsInvalidAccessor(deger => $"'{deger}' geçersiz.");
        m.SetValueMustBeANumberAccessor(_ => "Bu alan sayı olmalıdır.");
        m.SetNonPropertyValueMustBeANumberAccessor(() => "Bu alan sayı olmalıdır.");
    }
}

/// <summary>
/// [Required], [Range], [MaxLength] gibi etiketlerde mesaj yazılmamışsa Türkçe varsayılan mesaj verir.
/// Böylece yeni bir alan eklerken ErrorMessage yazmak zorunda değilsiniz.
/// {0} = alanın adı ([Display] etiketindeki), {1} ve {2} = sınır değerleri.
/// </summary>
public class TurkceDogrulamaMesajlari : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext baglam)
    {
        // int, DateTime, bool gibi boş olamayan alanlara ASP.NET gizlice İngilizce bir [Required] ekler.
        // Biz önce kendimiz ekliyoruz ki mesajı aşağıda Türkçe olsun.
        var etiketler = baglam.ValidationMetadata.ValidatorMetadata;
        var tur = baglam.Key.ModelType;
        var bosOlamaz = tur.IsValueType && Nullable.GetUnderlyingType(tur) == null;
        if (baglam.Key.MetadataKind == ModelMetadataKind.Property && bosOlamaz && !etiketler.OfType<RequiredAttribute>().Any())
            etiketler.Add(new RequiredAttribute());

        foreach (var etiket in baglam.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (etiket.ErrorMessage != null || etiket.ErrorMessageResourceName != null) continue;   // özel mesaj varsa dokunma

            if (etiket is RequiredAttribute) etiket.ErrorMessage = "{0} zorunludur.";
            else if (etiket is RangeAttribute) etiket.ErrorMessage = "{0} {1} ile {2} arasında olmalıdır.";
            else if (etiket is MaxLengthAttribute) etiket.ErrorMessage = "{0} en fazla {1} karakter olabilir.";
            else if (etiket is MinLengthAttribute) etiket.ErrorMessage = "{0} en az {1} karakter olmalıdır.";
            else if (etiket is StringLengthAttribute) etiket.ErrorMessage = "{0} en az {2}, en fazla {1} karakter olmalıdır.";
            else if (etiket is EmailAddressAttribute) etiket.ErrorMessage = "Geçerli bir e-posta adresi giriniz.";
            else if (etiket is PhoneAttribute) etiket.ErrorMessage = "Geçerli bir telefon numarası giriniz.";
            else if (etiket is CompareAttribute) etiket.ErrorMessage = "{0} ile {1} aynı olmalıdır.";
        }
    }
}
