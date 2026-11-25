// ============================================================================
// KİLOMETRE ALANI: "82.020", "82 020" ya da "82020" yazılsa da 82020 olarak okunur.
// Tarayıcının sayı kutusu "82.020"yi 82,02 sanıp formu göndermiyordu; km kutuları artık yazı kutusu
// (genel.js → data-km, yazarken binlik noktaları koyar) ve sunucu tarafını bu sınıf okur.
// Kullanım: form modelindeki int alana [Km] yazın.
// ============================================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AracKiralama.Yardimcilar;

public class KmAttribute() : ModelBinderAttribute(typeof(KmBaglayici));

public class KmBaglayici : IModelBinder
{
    public const int EnFazla = 3_000_000;

    public Task BindModelAsync(ModelBindingContext baglam)
    {
        var deger = baglam.ValueProvider.GetValue(baglam.ModelName);
        if (deger == ValueProviderResult.None) return Task.CompletedTask;
        baglam.ModelState.SetModelValue(baglam.ModelName, deger);

        var ad = baglam.ModelMetadata.DisplayName ?? "Kilometre";
        var km = Oku(deger.FirstValue);
        if (km == null)
            baglam.ModelState.TryAddModelError(baglam.ModelName, $"{ad} giriniz (sadece rakam, örn. 82.450).");
        else if (km > EnFazla)
            baglam.ModelState.TryAddModelError(baglam.ModelName, $"{ad} en fazla {Bicim.Sayi(EnFazla)} olabilir.");
        else
            baglam.Result = ModelBindingResult.Success(km.Value);
        return Task.CompletedTask;
    }

    /// <summary>Rakam dışındaki her şeyi (nokta, boşluk, "km") atar. Boşsa ya da rakam yoksa null.</summary>
    public static int? Oku(string? yazi)
    {
        var rakamlar = new string((yazi ?? "").Where(char.IsAsciiDigit).ToArray());
        if (rakamlar.Length == 0 || rakamlar.Length > 9) return rakamlar.Length > 9 ? int.MaxValue : null;
        return int.Parse(rakamlar);
    }
}
