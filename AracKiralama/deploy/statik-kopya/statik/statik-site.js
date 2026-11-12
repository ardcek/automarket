// =====================================================================
// STATİK KOPYA DAVRANIŞLARI (ardcek.com/hafta3)
// Bu klasör sitenin sunucusuz (sabit) kopyasıdır. Gerçek sistemde sunucunun
// yaptığı işleri burada tarayıcı yapar; kurallar gerçek sistemle aynıdır:
//   • Araç listesi : filtreler, sıralama ve sayfalama (Controllers/AraclarController.cs)
//   • Araç detayı  : fiyat hesabı (Servisler/FiyatServisi.cs)
//   • Giriş / kayıt: formlar görünür, gönderilince "yakında açılacak" uyarısı
// Veriler statik-veri.js dosyasındadır (window.ROTA).
// =====================================================================
(function () {
    const V = window.ROTA;
    if (!V) return;
    const parametreler = new URLSearchParams(location.search);
    const para = sayi => new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 }).format(sayi) + " ₺";
    const iki = n => String(n).padStart(2, "0");
    const kutuTarihi = t => `${t.getFullYear()}-${iki(t.getMonth() + 1)}-${iki(t.getDate())}T${iki(t.getHours())}:${iki(t.getMinutes())}`;
    const aylar = ["Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara"];
    const tarihSaat = t => `${iki(t.getDate())} ${aylar[t.getMonth()]} ${t.getFullYear()} ${iki(t.getHours())}:${iki(t.getMinutes())}`;
    const gunSayisi = (bas, bit) => bit <= bas ? 1 : Math.max(1, Math.ceil((bit - bas) / 36e5 / 24));
    const tarihOku = deger => { const t = deger ? new Date(deger) : null; return t && !isNaN(t) ? t : null; };

    // URL'deki tarihler (arama formundan gelir)
    const urlBaslangic = tarihOku(parametreler.get("Baslangic"));
    const urlBitis = tarihOku(parametreler.get("Bitis"));
    const tarihSecili = urlBaslangic && urlBitis && urlBitis > urlBaslangic;
    const tarihParametreleri = () => {
        const p = new URLSearchParams();
        for (const ad of ["Baslangic", "Bitis", "AlisSubeId", "IadeSubeId"]) if (parametreler.get(ad)) p.set(ad, parametreler.get(ad));
        return p;
    };

    // ---- 1) Tarih kutuları: kopya hazırlandığı günün tarihleri eskimesin ----
    // Gerçek sistemdeki gibi: alış = yarın 10:00, iade = 3 gün sonrası. URL'de tarih varsa o kullanılır.
    const yarin = new Date(); yarin.setDate(yarin.getDate() + 1); yarin.setHours(10, 0, 0, 0);
    const ucGunSonra = new Date(yarin); ucGunSonra.setDate(ucGunSonra.getDate() + 3);
    const bugun = new Date(); bugun.setHours(0, 0, 0, 0);
    document.querySelectorAll("[data-tarih-baslangic]").forEach(kutu => {
        kutu.value = kutuTarihi(urlBaslangic ?? yarin);
        kutu.min = kutuTarihi(bugun);
    });
    document.querySelectorAll("[data-tarih-bitis]").forEach(kutu => {
        kutu.value = kutuTarihi(urlBitis ?? ucGunSonra);
        kutu.min = kutuTarihi(urlBaslangic ?? yarin);
    });
    // Şube seçimleri de URL'den
    for (const ad of ["AlisSubeId", "IadeSubeId"]) {
        const deger = parametreler.get(ad);
        if (deger !== null) document.querySelectorAll(`select[name="${ad}"]`).forEach(s => { if ([...s.options].some(o => o.value === deger)) s.value = deger; });
    }

    // ---- 2) Araç listesi: filtre + sıralama + sayfalama ----
    const kartlarKutusu = document.querySelector("[data-statik-kartlar]");
    if (kartlarKutusu) {
        const kartlar = [...kartlarKutusu.querySelectorAll("[data-arac]")];
        const filtreFormu = document.querySelector("form.filtre-paneli");
        const sayi = p => { const d = parametreler.get(p); return d === null || d === "" ? null : Number(d); };
        const f = {
            arama: (parametreler.get("Arama") || "").trim().toUpperCase(),
            sube: sayi("AlisSubeId"), kategori: sayi("KategoriId"), marka: sayi("MarkaId"),
            vites: sayi("VitesTuruId"), yakit: sayi("YakitTuruId"), koltuk: sayi("EnAzKoltuk"),
            enDusuk: sayi("EnDusukFiyat"), enYuksek: sayi("EnYuksekFiyat"), yil: sayi("EnAzYil"),
            siralama: parametreler.get("Siralama") || V.ayarlar.siralama,
            sayfa: Math.max(1, sayi("Sayfa") || 1),
        };

        // Filtre kutularını URL'deki değerlere getir (sayfa yeniden açıldığında seçili kalsın)
        if (filtreFormu) {
            for (const [ad, deger] of parametreler) {
                if (["Baslangic", "Bitis", "AlisSubeId", "IadeSubeId", "Sayfa"].includes(ad)) continue;
                filtreFormu.querySelectorAll(`[name="${ad}"]`).forEach(kutu => {
                    if (kutu.type === "radio") kutu.checked = kutu.value === deger;
                    else kutu.value = deger;
                });
            }
            // Tarih seçiliyse filtre değişince tarihler kaybolmasın (gerçek sistemdeki gizli alanlar)
            for (const [ad, deger] of tarihParametreleri()) {
                const gizli = document.createElement("input");
                gizli.type = "hidden"; gizli.name = ad; gizli.value = deger;
                filtreFormu.prepend(gizli);
            }
        }

        // Filtreleme (AraclarController.Index'teki "if" satırlarının aynısı)
        let sonuc = kartlar.filter(kart => {
            const a = V.araclar[kart.dataset.arac];
            if (!a) return false;
            if (f.arama && !(a.marka + " " + a.model).toUpperCase().includes(f.arama)) return false;
            if (f.sube !== null && a.sube !== f.sube) return false;
            if (f.kategori !== null && a.kategori !== f.kategori) return false;
            if (f.marka !== null && a.markaId !== f.marka) return false;
            if (f.vites !== null && a.vites !== f.vites) return false;
            if (f.yakit !== null && a.yakit !== f.yakit) return false;
            if (f.koltuk !== null && a.koltuk < f.koltuk) return false;
            if (f.enDusuk !== null && a.fiyat < f.enDusuk) return false;
            if (f.enYuksek !== null && a.fiyat > f.enYuksek) return false;
            if (f.yil !== null && a.yil < f.yil) return false;
            return true;
        });

        // Sıralama (eşitlikte ilk sıradaki kalır)
        const veri = kart => V.araclar[kart.dataset.arac];
        const sira = kart => Number(kart.dataset.sira);
        const karsilastir = {
            "fiyat-azalan": (x, y) => veri(y).fiyat - veri(x).fiyat || sira(x) - sira(y),
            "yeni": (x, y) => veri(y).yil - veri(x).yil || veri(x).fiyat - veri(y).fiyat || sira(x) - sira(y),
            "km": (x, y) => veri(x).km - veri(y).km || sira(x) - sira(y),
            "ad": (x, y) => veri(x).marka.localeCompare(veri(y).marka, "tr") || veri(x).model.localeCompare(veri(y).model, "tr") || sira(x) - sira(y),
        }[f.siralama] ?? ((x, y) => veri(x).fiyat - veri(y).fiyat || sira(x) - sira(y));
        sonuc.sort(karsilastir);

        // Sayfalama
        const sayfaBasina = V.ayarlar.sayfaBasina;
        const toplamSayfa = Math.max(1, Math.ceil(sonuc.length / sayfaBasina));
        const sayfa = Math.min(f.sayfa, toplamSayfa);
        const gosterilen = sonuc.slice((sayfa - 1) * sayfaBasina, sayfa * sayfaBasina);

        kartlar.forEach(k => k.remove());
        gosterilen.forEach(k => kartlarKutusu.appendChild(k));
        document.querySelector("[data-statik-sayi]").textContent = sonuc.length;
        const bos = document.querySelector("[data-statik-bos]");
        if (bos) bos.hidden = sonuc.length > 0;

        // Tarih seçildiyse: başlıkta tarih aralığı, kartlarda toplam tutar, linklerde tarihler
        if (tarihSecili) {
            const gun = gunSayisi(urlBaslangic, urlBitis);
            const baslik = document.querySelector("[data-statik-tarih]");
            if (baslik) baslik.innerHTML = `<span><i class="bi bi-calendar-range me-1"></i>${tarihSaat(urlBaslangic)} → ${tarihSaat(urlBitis)} · <b>${gun} gün</b></span>`;
            const ek = tarihParametreleri().toString();
            gosterilen.forEach(kart => {
                const fiyat = kart.querySelector(".fiyat");
                if (fiyat && !kart.querySelector("[data-statik-toplam]")) {
                    const satir = document.createElement("div");
                    satir.className = "small text-muted text-nowrap";
                    satir.dataset.statikToplam = "";
                    satir.textContent = `${gun} gün · ≈${para(veri(kart).fiyat * gun)}`;
                    fiyat.before(satir);
                }
                kart.querySelectorAll('a[href^="arac-"]').forEach(a => a.href = a.getAttribute("href").split("?")[0] + "?" + ek);
            });
        }

        // Sayfa numaraları (Views/Shared/_Sayfalama.cshtml ile aynı görünüm)
        const sayfalama = document.querySelector("[data-statik-sayfalama]");
        if (sayfalama && toplamSayfa > 1) {
            const link = s => { const p = new URLSearchParams(location.search); p.set("Siralama", f.siralama); if (s > 1) p.set("Sayfa", s); else p.delete("Sayfa"); return "araclar.html?" + p.toString(); };
            const ilk = Math.max(1, sayfa - 2), son = Math.min(toplamSayfa, sayfa + 2);
            let html = `<nav class="mt-4" aria-label="Sayfalar"><ul class="pagination justify-content-center flex-wrap">`;
            html += `<li class="page-item ${sayfa === 1 ? "disabled" : ""}"><a class="page-link" href="${link(sayfa - 1)}" aria-label="Önceki"><i class="bi bi-chevron-left"></i></a></li>`;
            for (let i = ilk; i <= son; i++) html += `<li class="page-item ${i === sayfa ? "active" : ""}"><a class="page-link" href="${link(i)}">${i}</a></li>`;
            html += `<li class="page-item ${sayfa === toplamSayfa ? "disabled" : ""}"><a class="page-link" href="${link(sayfa + 1)}" aria-label="Sonraki"><i class="bi bi-chevron-right"></i></a></li>`;
            html += `</ul><div class="text-center small text-muted">Sayfa ${sayfa} / ${toplamSayfa}</div></nav>`;
            sayfalama.innerHTML = html;
        }
    }

    // ---- 3) Araç detayı: fiyat hesabı (FiyatServisi.Hesapla ile aynı kurallar) ----
    const fiyatFormu = document.querySelector("[data-fiyat-formu]");
    if (fiyatFormu) {
        const arac = V.araclar[fiyatFormu.dataset.aracId];
        const dokum = fiyatFormu.querySelector("[data-fiyat-dokumu]");
        const hatalar = fiyatFormu.querySelector("[data-fiyat-hatalari]");
        const deger = ad => fiyatFormu.querySelector(`[name="${ad}"]`)?.value;

        function hesapla() {
            const bas = tarihOku(deger("Baslangic")), bit = tarihOku(deger("Bitis"));
            if (!arac || !bas || !bit) return;

            // RezervasyonServisi.TarihHatalari
            const mesajlar = [];
            if (bas < new Date(Date.now() - 5 * 60000)) mesajlar.push("Alış tarihi geçmişte olamaz.");
            if (bit <= bas) mesajlar.push("İade tarihi alış tarihinden sonra olmalıdır.");
            else if ((bit - bas) / 864e5 > V.ayarlar.enUzun) mesajlar.push(`En fazla ${V.ayarlar.enUzun} günlük kiralama yapılabilir.`);
            hatalar.replaceChildren(...mesajlar.map(m => { const d = document.createElement("div"); d.className = "alert alert-warning py-2 px-3 small mb-2"; d.textContent = m; return d; }));
            if (mesajlar.length) return;

            // FiyatServisi.Hesapla
            const gun = gunSayisi(bas, bit);
            const kira = gun * arac.fiyat;
            const oran = gun >= V.ayarlar.aylikGun ? V.ayarlar.aylikOran : gun >= V.ayarlar.haftalikGun ? V.ayarlar.haftalikOran : 0;
            const indirim = Math.round(kira * oran * 100) / 100;
            const ekstralar = [...fiyatFormu.querySelectorAll('[name="EkstraIdleri"]:checked')].map(k => V.ekstralar[k.value]).filter(Boolean);
            const tekYon = deger("AlisSubeId") !== deger("IadeSubeId") ? V.ayarlar.tekYon : 0;
            const toplam = kira - indirim + ekstralar.reduce((t, e) => t + e.fiyat * gun, 0) + tekYon;

            const satirlar = [[`${gun} gün × ${para(arac.fiyat)}`, para(kira)]];
            if (indirim > 0) satirlar.push([`Uzun dönem indirimi (%${Math.round(oran * 100)})`, "− " + para(indirim), "indirim"]);
            ekstralar.forEach(e => satirlar.push([e.ad, para(e.fiyat * gun)]));
            if (tekYon > 0) satirlar.push(["Farklı şubeye iade", para(tekYon)]);
            satirlar.push(["Toplam", para(toplam), "toplam"]);
            dokum.replaceChildren(...satirlar.map(([yazi, tutar, sinif]) => {
                const li = document.createElement("li");
                if (sinif) li.className = sinif;
                li.innerHTML = "<span></span><span class='rakam'></span>";
                li.children[0].textContent = yazi;
                li.children[1].textContent = tutar;
                return li;
            }));
            const depozito = fiyatFormu.querySelector("[data-fiyat-depozito]");
            if (depozito) depozito.textContent = para(arac.depozito);
        }
        fiyatFormu.addEventListener("change", hesapla);
        hesapla();

        // "← Araçlar" linki tarihleri taşısın
        if (tarihSecili) document.querySelectorAll('a[href="araclar.html"]').forEach(a => a.href = "araclar.html?" + tarihParametreleri());
    }

    // ---- 4) Giriş ve kayıt formları: görünür ama çalışmaz (sunum modu) ----
    document.querySelectorAll("form[data-statik-hesap]").forEach(form => {
        form.addEventListener("submit", olay => {
            olay.preventDefault();
            if (window.jQuery && jQuery(form).valid && !jQuery(form).valid()) return;   // önce normal form uyarıları
            // Hata kutusu: sunucu onu yalnızca hata varken çizer, bu yüzden yoksa burada oluşturulur.
            let ozet = form.querySelector("[data-statik-uyari]");
            if (!ozet) {
                ozet = document.createElement("div");
                ozet.dataset.statikUyari = "";
                ozet.className = "text-danger small validation-summary-errors " + (form.classList.contains("row") ? "col-12" : "mb-2");
                form.prepend(ozet);
            }
            const mesaj = form.dataset.statikHesap === "kayit" ? V.metinler.kayitKapali : V.metinler.girisKapali;
            ozet.innerHTML = "<ul><li></li></ul>";
            ozet.querySelector("li").textContent = mesaj;
        });
    });
})();
