// =====================================================================
// GENEL DAVRANIŞLAR — her sayfada yüklenir.
// View'larda <script> yazmak yerine HTML'e "data-..." özelliği eklenir,
// davranış buradan otomatik bağlanır. Mevcut özellikler:
//   data-tema-degistir          → açık / koyu tema butonu
//   data-onay="Emin misiniz?"   → tıklamadan önce onay sorar
//   data-otomatik-gonder        → form içindeki seçim değişince formu gönderir
//   data-otomatik-gonder="genis-ekran" → aynısı, ama sadece geniş ekranda (telefonda "Gönder" butonu beklenir)
//   data-tarih-cifti            → içindeki [data-tarih-baslangic] / [data-tarih-bitis] kutularını eşler
//   data-bs-toggle="tooltip"    → fareyle üstüne gelince açıklama
//   data-demo="e-posta"         → giriş sayfasında e-postayı doldurur
//   data-gorsel-onizle="#id"    → dosya seçilince resmi önizler
//   data-ikon-onizle="#id"      → ikon adı yazılınca ikonu gösterir
//   data-sifre-goster           → (şifre kutusunun yanındaki buton) şifreyi göster / gizle
//   data-sifre-gucu="#Sifre"    → şifre yazıldıkça 4 parçalı güç çubuğunu ve yazısını günceller
//   data-telefon                → yazarken "0532 123 45 67" biçimine sokar
//   data-km (+ data-km-min)     → kilometre kutusu: "82.450" biçimi, en az değerden küçükse form gitmez
//   data-yukleniyor="Yazı..."   → (form) gönderilince buton kilitlenir, dönen ikon ve bu yazı çıkar
//   data-ikon-onizle kutularına  → yanına "Seç" düğmesi eklenir: aranabilir ikon galerisi açılır (liste: /Yonetim/Panel/Ikonlar)
//   data-sablon-ara             → (Filtreler ve Özellikler) hazır özellik kartlarında arama
//   data-tc                     → T.C. Kimlik No kutusu: sadece rakam, 11 hanede resmi algoritmayla anında kontrol
//                                 (sunucudaki kontrol: Yardimcilar/Dogrulama.cs → TcKimlikNo.GecerliMi)
// =====================================================================

// ---- Açık / koyu tema ----
function temaIkonunuGuncelle() {
    const koyu = document.documentElement.getAttribute("data-bs-theme") === "dark";
    document.querySelectorAll("[data-tema-degistir] i").forEach(i => i.className = koyu ? "bi bi-sun" : "bi bi-moon-stars");
}
document.querySelectorAll("[data-tema-degistir]").forEach(buton => buton.addEventListener("click", () => {
    const kok = document.documentElement;
    const yeni = kok.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
    const uygula = () => {
        kok.setAttribute("data-bs-theme", yeni);
        try { localStorage.setItem("tema", yeni); } catch { }
        temaIkonunuGuncelle();
        document.dispatchEvent(new Event("temadegisti"));   // grafikler renklerini yeniler
    };
    // Destekleyen tarayıcıda yeni tema, butonun olduğu yerden büyüyen bir daireyle gelir (stil.css → .tema-gecisi)
    if (!document.startViewTransition || !kok.classList.contains("animasyon")) { uygula(); return; }
    const r = buton.getBoundingClientRect(), x = r.left + r.width / 2, y = r.top + r.height / 2;
    const yaricap = Math.hypot(Math.max(x, innerWidth - x), Math.max(y, innerHeight - y));
    kok.classList.add("tema-gecisi");
    const gecis = document.startViewTransition(uygula);
    gecis.ready.then(() => kok.animate(
        { clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${yaricap}px at ${x}px ${y}px)`] },
        { duration: 700, easing: "cubic-bezier(.16, 1, .3, 1)", pseudoElement: "::view-transition-new(root)" }
    )).catch(() => { });
    gecis.finished.finally(() => kok.classList.remove("tema-gecisi"));
}));
temaIkonunuGuncelle();

// ---- Bildirimleri göster (Controller'da Basarili("...") / Hata("...")) ----
document.querySelectorAll(".toast").forEach(t => bootstrap.Toast.getOrCreateInstance(t, { delay: 5000 }).show());

// ---- Onay soran butonlar ----
document.addEventListener("click", olay => {
    const eleman = olay.target.closest("[data-onay]");
    if (eleman && !confirm(eleman.dataset.onay)) {
        olay.preventDefault();
        olay.stopPropagation();
    }
});

// ---- Seçim değişince formu otomatik gönder (yazı kutuları hariç) ----
document.querySelectorAll("form[data-otomatik-gonder]").forEach(form => form.addEventListener("change", olay => {
    if (olay.target.matches("input[type=text], input[type=search]")) return;
    if (form.dataset.otomatikGonder === "genis-ekran" && !matchMedia("(min-width: 992px)").matches) return;   // telefonda birden çok seçim yapılabilsin
    form.requestSubmit();
}));

// ---- İade tarihi alış tarihinden önce olamasın ----
document.querySelectorAll("[data-tarih-cifti]").forEach(grup => {
    const baslangic = grup.querySelector("[data-tarih-baslangic]");
    const bitis = grup.querySelector("[data-tarih-bitis]");
    if (!baslangic || !bitis) return;
    const esle = () => {
        bitis.min = baslangic.value;
        if (baslangic.value && bitis.value && bitis.value <= baslangic.value) {
            const tarih = new Date(baslangic.value);
            tarih.setDate(tarih.getDate() + 3);
            const iki = n => String(n).padStart(2, "0");
            bitis.value = `${tarih.getFullYear()}-${iki(tarih.getMonth() + 1)}-${iki(tarih.getDate())}T${iki(tarih.getHours())}:${iki(tarih.getMinutes())}`;
            bitis.dispatchEvent(new Event("change", { bubbles: true }));
        }
    };
    baslangic.addEventListener("change", esle);
    esle();
});

// ---- Bilgi balonları ----
document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(e => new bootstrap.Tooltip(e, { placement: "top" }));

// ---- Giriş sayfası demo hesap butonları ----
document.querySelectorAll("[data-demo]").forEach(buton => buton.addEventListener("click", () => {
    document.getElementById("Eposta").value = buton.dataset.demo;
    document.getElementById("Sifre").focus();
}));

// ---- Görsel önizleme ----
document.querySelectorAll("[data-gorsel-onizle]").forEach(kutu => kutu.addEventListener("change", () => {
    const resim = document.querySelector(kutu.dataset.gorselOnizle);
    if (kutu.files[0] && resim) resim.src = URL.createObjectURL(kutu.files[0]);
}));

// ---- İkon önizleme ----
document.querySelectorAll("[data-ikon-onizle]").forEach(kutu => kutu.addEventListener("input", () => {
    const ikon = document.querySelector(kutu.dataset.ikonOnizle);
    if (ikon) ikon.className = "bi " + kutu.value;
}));

// ---- Şifreyi göster / gizle ----
document.querySelectorAll("[data-sifre-goster]").forEach(buton => buton.addEventListener("click", () => {
    const kutu = buton.parentElement.querySelector("input");
    const goster = kutu.type === "password";
    kutu.type = goster ? "text" : "password";
    buton.setAttribute("aria-pressed", goster);
    buton.setAttribute("aria-label", goster ? "Şifreyi gizle" : "Şifreyi göster");
    buton.querySelector("i").className = goster ? "bi bi-eye-slash" : "bi bi-eye";
    kutu.focus();
}));

// ---- Şifre gücü: uzunluk + harf/rakam karışımı + büyük harf + işaret → 0..4 ----
document.querySelectorAll("[data-sifre-gucu]").forEach(gosterge => {
    const kutu = document.querySelector(gosterge.dataset.sifreGucu);
    const yazi = gosterge.querySelector("small");
    if (!kutu || !yazi) return;
    const ilkYazi = yazi.textContent;
    kutu.addEventListener("input", () => {
        const s = kutu.value;
        let puan = 0;
        if (s.length >= 6) puan++;
        if (s.length >= 10) puan++;
        if (/[a-zçğıöşü]/i.test(s) && /\d/.test(s)) puan++;
        if (/[A-ZÇĞİÖŞÜ]/.test(s) && /[^A-Za-z0-9çğıöşüÇĞİÖŞÜ]/.test(s)) puan++;
        if (s.length < 6) puan = s.length ? 1 : 0;
        gosterge.dataset.seviye = puan;
        yazi.textContent = !s.length ? ilkYazi
            : s.length < 6 ? `${6 - s.length} karakter daha`
            : ["", "Zayıf", "Orta: rakam veya büyük harf ekleyin", "İyi", "Güçlü şifre"][puan];
    });
});

// ---- Telefon: sadece rakam, "0532 123 45 67" biçiminde ----
document.querySelectorAll("[data-telefon]").forEach(kutu => kutu.addEventListener("input", () => {
    let r = kutu.value.replace(/\D/g, "");
    if (r.startsWith("90")) r = r.slice(2);           // +90 ile yapıştırıldıysa
    if (r && !r.startsWith("0")) r = "0" + r;
    r = r.slice(0, 11);
    kutu.value = [r.slice(0, 4), r.slice(4, 7), r.slice(7, 9), r.slice(9, 11)].filter(Boolean).join(" ");
}));

// ---- Kilometre: yazarken "82.450" biçimi; data-km-min varsa ondan az girilemez (teslim / iade) ----
// Sunucu tarafı: Yardimcilar/KmBaglayici.cs noktaları atıp okur, RezervasyonServisi en az değeri tekrar kontrol eder.
const kmBicim = n => n.toLocaleString("tr-TR");
document.querySelectorAll("[data-km]").forEach(kutu => {
    const enAz = kutu.dataset.kmMin ? Number(kutu.dataset.kmMin) : null;
    const bilgi = document.createElement("small");
    bilgi.className = "km-bilgi d-block";
    bilgi.setAttribute("aria-live", "polite");
    kutu.insertAdjacentElement("afterend", bilgi);
    kutu.kmHatasi = () => {
        const r = kutu.value.replace(/\D/g, "");
        if (!r) return enAz != null ? "Göstergedeki kilometreyi yazın." : "";
        return enAz != null && Number(r) < enAz ? `${kmBicim(enAz)} km'den az olamaz (${kutu.dataset.kmMinYazi || "kayıtlı"} km).` : "";
    };
    const kontrol = (yaz) => {
        const r = kutu.value.replace(/\D/g, "").slice(0, 7);
        if (yaz !== false) kutu.value = r ? kmBicim(Number(r)) : "";
        const hata = r ? kutu.kmHatasi() : "";
        kutu.classList.toggle("is-invalid", !!hata);
        const fark = r && enAz != null && !hata ? Number(r) - enAz : null;
        bilgi.className = "km-bilgi d-block small " + (hata ? "text-danger" : "text-success");
        bilgi.textContent = hata || (fark != null ? `+${kmBicim(fark)} km yol` : "");
    };
    kutu.addEventListener("input", () => kontrol());
    kontrol(kutu.value !== "");
});
// Hatalı km varsa form gönderilmez (doğrulama kütüphanesinden önce çalışsın diye "capture")
document.addEventListener("submit", olay => {
    const hatali = [...olay.target.querySelectorAll("[data-km]")].find(k => k.kmHatasi && k.kmHatasi());
    if (!hatali) return;
    olay.preventDefault();
    olay.stopImmediatePropagation();
    hatali.classList.add("is-invalid");
    const bilgi = hatali.nextElementSibling;
    if (bilgi?.classList.contains("km-bilgi")) { bilgi.className = "km-bilgi d-block small text-danger"; bilgi.textContent = hatali.kmHatasi(); }
    hatali.focus();
}, true);

// ---- Gönderilirken buton kilitlensin (iki kez tıklanıp iki kayıt olmasın) ----
// Doğrulama formu durdurduysa (hatalı alan) buton kilitlenmez: kontrol, diğer dinleyiciler çalıştıktan sonra yapılır.
document.querySelectorAll("form[data-yukleniyor]").forEach(form => form.addEventListener("submit", olay => setTimeout(() => {
    if (olay.defaultPrevented) return;
    const buton = form.querySelector("button:not([type=button])");
    if (!buton) return;
    buton.dataset.ilkHali = buton.innerHTML;
    buton.disabled = true;
    buton.innerHTML = `<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>${form.dataset.yukleniyor}`;
})));
// Tarayıcının "Geri" tuşuyla sayfaya dönülürse buton eski hâline gelsin
window.addEventListener("pageshow", () => document.querySelectorAll("form[data-yukleniyor] button[data-ilk-hali]").forEach(buton => {
    buton.innerHTML = buton.dataset.ilkHali;
    buton.disabled = false;
    delete buton.dataset.ilkHali;
}));

// ---- T.C. Kimlik No: resmi algoritma (Yardimcilar/Dogrulama.cs'teki TcKimlikNo.GecerliMi ile aynı kural) ----
// 11 hane, ilk hane 0 değil; 10. hane = ((1+3+5+7+9. haneler) × 7 − (2+4+6+8. haneler)) mod 10; 11. hane = ilk 10 hanenin toplamı mod 10
function tcGecerliMi(no) {
    if (!/^[1-9]\d{10}$/.test(no)) return false;
    const h = [...no].map(Number);
    const onuncu = (((h[0] + h[2] + h[4] + h[6] + h[8]) * 7 - (h[1] + h[3] + h[5] + h[7])) % 10 + 10) % 10;
    const onbirinci = h.slice(0, 10).reduce((a, b) => a + b, 0) % 10;
    return h[9] === onuncu && h[10] === onbirinci;
}
document.querySelectorAll("[data-tc]").forEach(kutu => {
    const bilgi = document.createElement("small");
    bilgi.className = "tc-bilgi";
    bilgi.setAttribute("aria-live", "polite");
    kutu.insertAdjacentElement("afterend", bilgi);
    const kontrol = () => {
        kutu.value = kutu.value.replace(/\D/g, "").slice(0, 11);
        const n = kutu.value.length;
        const gecerli = n === 11 && tcGecerliMi(kutu.value);
        kutu.classList.toggle("is-valid", gecerli);
        kutu.classList.toggle("is-invalid", n === 11 && !gecerli);
        bilgi.className = "tc-bilgi " + (n === 11 ? (gecerli ? "text-success" : "text-danger") : "text-muted");
        bilgi.textContent = n === 0 ? "" : n < 11 ? `${11 - n} hane kaldı` : gecerli ? "✓ Geçerli T.C. Kimlik No" : "Bu numara geçerli bir T.C. Kimlik No değil";
    };
    kutu.addEventListener("input", kontrol);
    kontrol();
});

// ---- Hazır özellik şablonlarında arama (Yönetim → Filtreler ve Özellikler) ----
document.querySelectorAll("[data-sablon-ara]").forEach(kutu => kutu.addEventListener("input", () => {
    const aranan = kutu.value.toLocaleLowerCase("tr");
    document.querySelectorAll("[data-sablon]").forEach(kart => kart.hidden = !kart.dataset.sablon.toLocaleLowerCase("tr").includes(aranan));
}));

// ---- İkon seçici: ikon adı yazılan her kutunun yanına "Seç" düğmesi (Bootstrap Icons, 2000+ ikon) ----
// Türkçe aramayı İngilizce ikon adlarına çevirir (ikon adları İngilizce: car, key, snow...)
const IKON_SOZLUK = { araba: "car", arac: "car", araç: "car", anahtar: "key", kar: "snow", klima: "snow", yakit: "fuel", yakıt: "fuel", benzin: "fuel",
    kisi: "person", kişi: "person", koltuk: "person", kalp: "heart", yildiz: "star", yıldız: "star", konum: "geo", harita: "map", telefon: "phone",
    kamera: "camera", muzik: "music", müzik: "music", para: "cash", indirim: "percent", takvim: "calendar", saat: "clock", ayar: "gear", motor: "gear",
    kalkan: "shield", guvenlik: "shield", güvenlik: "shield", sigorta: "shield", bagaj: "suitcase", çanta: "bag", canta: "bag", ev: "house", bina: "building",
    hediye: "gift", etiket: "tag", sepet: "basket", kapi: "door", kapı: "door", isik: "light", ışık: "light", gunes: "sun", güneş: "sun", su: "droplet" };
let ikonListesi = null, ikonPenceresi = null, ikonHedefi = null;

async function ikonSeciciAc(kutu) {
    ikonHedefi = kutu;
    if (!ikonPenceresi) {
        const kap = document.createElement("div");
        kap.className = "modal fade"; kap.tabIndex = -1; kap.setAttribute("aria-label", "İkon seç");
        kap.innerHTML = `<div class="modal-dialog modal-lg modal-dialog-scrollable"><div class="modal-content">
            <div class="modal-header gap-2"><input type="search" class="form-control" placeholder="Ara: araba, anahtar, kar, car, key…" data-ikon-ara />
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Kapat"></button></div>
            <div class="modal-body"><div class="ikon-izgara" data-ikon-izgara></div><div class="small text-muted mt-2" data-ikon-bilgi></div></div></div></div>`;
        document.body.append(kap);
        ikonPenceresi = new bootstrap.Modal(kap);
        const ara = kap.querySelector("[data-ikon-ara]");
        ara.addEventListener("input", () => ikonlariCiz(ara.value));
        kap.addEventListener("shown.bs.modal", () => ara.focus());
        kap.querySelector("[data-ikon-izgara]").addEventListener("click", olay => {
            const b = olay.target.closest("button[data-ikon]");
            if (!b) return;
            ikonHedefi.value = b.dataset.ikon;
            ikonHedefi.dispatchEvent(new Event("input", { bubbles: true }));   // önizleme ikonu da güncellensin
            ikonPenceresi.hide();
        });
    }
    if (!ikonListesi) ikonListesi = await fetch("/Yonetim/Panel/Ikonlar").then(c => c.json()).catch(() => []);
    const ara = document.querySelector("[data-ikon-ara]");
    ara.value = "";
    ikonlariCiz("");
    ikonPenceresi.show();
}

function ikonlariCiz(aranan) {
    let a = aranan.trim().toLocaleLowerCase("tr");
    a = IKON_SOZLUK[a] ?? a;
    // Tam kelime eşleşenler önce: "car" → car-front, car-front-fill; sonra card, caret...
    const puan = ad => ad === "bi-" + a ? 0 : ad.startsWith("bi-" + a + "-") ? 1 : ad.includes("-" + a + "-") || ad.endsWith("-" + a) ? 2 : 3;
    const eslesen = ikonListesi.filter(ad => ad.includes(a)).sort((x, y) => puan(x) - puan(y));
    const gosterilen = eslesen.slice(0, 300);
    document.querySelector("[data-ikon-izgara]").innerHTML =
        gosterilen.map(ad => `<button type="button" data-ikon="${ad}" title="${ad}"><i class="bi ${ad}"></i>${ad.slice(3)}</button>`).join("");
    document.querySelector("[data-ikon-bilgi]").textContent = eslesen.length > gosterilen.length
        ? `${eslesen.length} ikondan ilk ${gosterilen.length} tanesi gösteriliyor; aramayı daraltın.` : `${eslesen.length} ikon`;
}

document.querySelectorAll("[data-ikon-onizle]").forEach(kutu => {
    if (typeof bootstrap === "undefined") return;
    const buton = document.createElement("button");
    buton.type = "button";
    buton.className = "btn btn-sade";
    buton.innerHTML = '<i class="bi bi-grid-3x3-gap me-1"></i>Seç';
    buton.setAttribute("aria-label", "İkon seç");
    kutu.insertAdjacentElement("afterend", buton);
    buton.addEventListener("click", () => ikonSeciciAc(kutu));
});
