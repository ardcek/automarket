// =====================================================================
// SİTE DÜZENLEYİCİ (Yönetim → Site Düzenleyici)
// Soldaki kutulara yazdıkça sağdaki önizleme anında değişir:
//   1) Yazı kutuları : önizlemede aynı yazı bulunur ve harf harf değiştirilir (sunucuyu beklemeden).
//   2) Ayarlar       : (renk, aç/kapa, sayı...) "taslak" olarak sunucuya gönderilir, önizleme
//                      ?onizleme=1 ile yeniden yüklenir ve sunucu sayfayı taslakla çizer.
//   3) Kaydet        : gerçek siteye yazılır. Vazgeç: taslak silinir.
// Önizleme aynı siteden açıldığı için (aynı "origin") çerçevenin içine JavaScript ile ulaşabiliyoruz.
// =====================================================================
(function () {
    const form = document.getElementById("duzenleyici-formu");
    const cerceve = document.getElementById("onizleme-cercevesi");
    if (!form || !cerceve) return;

    const durumYazisi = document.querySelector("[data-duz-durum]");
    const kutular = () => [...form.querySelectorAll("[data-metin], [data-ayar], [data-sayfa]")];
    const kutuDegeri = kutu => kutu.type === "checkbox" ? String(kutu.checked) : kutu.value;

    // Yazı anahtarı → { dugumler: önizlemedeki yazı düğümleri, eski: önizlemede şu an yazan hali }
    const eslesmeler = new Map();
    let zamanlayici = null;      // taslağı göndermeden önce kısa bekleme (her harfte değil, yazmayı bırakınca)
    let yenidenYukle = false;    // taslak gidince önizleme yeniden yüklensin mi?
    let kaydirma = 0;            // yeniden yüklemede önizlemenin kaydırma yeri korunur
    let kaydediliyor = false;

    // Her kutunun açılıştaki değeri: "değişti" işareti buna göre konur
    kutular().forEach(kutu => kutu.dataset.ilk = kutuDegeri(kutu));

    const belge = () => { try { return cerceve.contentDocument; } catch { return null; } };
    const onizlemeAdresi = adres => {
        const url = new URL(adres, location.origin);
        url.searchParams.set("onizleme", "1");
        return url.pathname + url.search + url.hash;
    };

    // ---------------- 1) Önizleme yüklenince ----------------
    cerceve.addEventListener("load", () => {
        const d = belge();
        if (!d || !d.body) return;
        d.defaultView.scrollTo(0, kaydirma);
        stilEkle(d);
        yazilariBul(d);
        d.addEventListener("click", onizlemedeTiklama, true);
    });

    // Önizlemede tıklanabilir yazıları ve seçili yazıyı belirgin yapan küçük stil
    function stilEkle(d) {
        const stil = d.createElement("style");
        stil.textContent =
            ".duz-tiklanir{cursor:pointer;outline:2px dashed transparent;outline-offset:2px;transition:outline-color .15s}" +
            ".duz-tiklanir:hover{outline-color:rgba(39,71,214,.55)}" +
            ".duz-vurgu{outline:3px solid #2747d6!important;outline-offset:3px;background:rgba(39,71,214,.08);border-radius:4px}";
        d.head.appendChild(stil);
    }

    // Soldaki her yazının önizlemede nerede durduğunu bulur
    function yazilariBul(d) {
        eslesmeler.clear();
        const dugumler = [];
        const yuruyucu = d.createTreeWalker(d.body, NodeFilter.SHOW_TEXT, {
            acceptNode: n => n.nodeValue.trim() && !n.parentElement.closest("script, style") ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT,
        });
        while (yuruyucu.nextNode()) dugumler.push(yuruyucu.currentNode);

        form.querySelectorAll("[data-metin]").forEach(kutu => {
            const yazi = kutu.value.trim();
            if (yazi.length < 2) return;
            // Önce birebir aynı yazıyı ara; bulunamazsa yazıyı içeren düğümleri kabul et
            let bulunanlar = dugumler.filter(dg => dg.nodeValue.trim() === yazi);
            if (!bulunanlar.length) bulunanlar = dugumler.filter(dg => dg.nodeValue.includes(yazi));
            if (!bulunanlar.length) return;
            eslesmeler.set(kutu.dataset.metin, { dugumler: bulunanlar, eski: yazi });
            bulunanlar.forEach(dg => {
                dg.parentElement.classList.add("duz-tiklanir");
                dg.parentElement.dataset.duzAnahtar = kutu.dataset.metin;
            });
        });
    }

    // ---------------- 2) Önizlemede tıklama ----------------
    function onizlemedeTiklama(olay) {
        // Bir yazıya tıklandıysa: soldaki kutusunu aç
        const oge = olay.target.closest("[data-duz-anahtar]");
        if (oge) {
            olay.preventDefault();
            olay.stopPropagation();
            const kutu = form.querySelector(`[data-metin="${CSS.escape(oge.dataset.duzAnahtar)}"]`);
            if (kutu) {
                kutu.closest(".duz-alan").hidden = false;
                kutu.scrollIntoView({ block: "center", behavior: "smooth" });
                kutu.focus({ preventScroll: true });
            }
            return;
        }
        // Bir linke tıklandıysa: gidilen sayfa da önizleme olarak açılsın
        const link = olay.target.closest("a[href]");
        if (link && link.origin === location.origin && !link.getAttribute("href").startsWith("#")) {
            olay.preventDefault();
            kaydirma = 0;
            cerceve.contentWindow.location.replace(onizlemeAdresi(link.href));
        }
    }

    // ---------------- 3) Kutuya yazınca / değiştirince ----------------
    form.addEventListener("input", olay => degisti(olay.target));
    form.addEventListener("change", olay => { if (olay.target.matches("select, input[type=checkbox], input[type=color]")) degisti(olay.target); });

    function degisti(kutu) {
        if (kutu.dataset.metin !== undefined) {
            const es = eslesmeler.get(kutu.dataset.metin);
            const yeni = kutu.value.trim();
            if (es && yeni) {
                // Önizlemedeki yazıyı anında değiştir
                es.dugumler.forEach(dg => { dg.nodeValue = dg.nodeValue.replace(es.eski, yeni); });
                es.eski = yeni;
            } else {
                yenidenYukle = true;   // önizlemede bulunamadı (ya da boşaltıldı) → sunucu çizsin
            }
        } else if (kutu.dataset.ayar !== undefined) {
            yenidenYukle = true;       // ayarlar sunucuda uygulanır
        }
        isaretle();
        if (kutu.dataset.sayfa === undefined) taslagiGonder();   // kurumsal sayfa düğmeleri Kaydet'te uygulanır
    }

    // Değişen kutulara "değişti" yazar, alttaki durum satırını günceller
    function isaretle() {
        let sayi = 0;
        kutular().forEach(kutu => {
            const farkli = kutuDegeri(kutu) !== kutu.dataset.ilk;
            if (farkli) sayi++;
            kutu.closest(".duz-alan")?.querySelector(".duz-degisti")?.toggleAttribute("hidden", !farkli);
        });
        if (durumYazisi) durumYazisi.textContent = sayi
            ? `${sayi} kaydedilmemiş değişiklik. Sitede görünmesi için Kaydet'e bas.`
            : "Değişiklik yok.";
        form.dataset.degisiklik = sayi ? "var" : "";
    }

    // Yazmayı bırakınca (0,6 sn) taslağı sunucuya gönder
    function taslagiGonder() {
        clearTimeout(zamanlayici);
        zamanlayici = setTimeout(async () => {
            const cevap = await fetch(form.dataset.taslakAdresi, { method: "POST", body: new FormData(form) });
            if (!cevap.ok) {
                if (durumYazisi) durumYazisi.textContent = "Önizleme güncellenemedi (oturum kapanmış olabilir, sayfayı yenileyin).";
                return;
            }
            if (yenidenYukle) { yenidenYukle = false; onizlemeyiYenile(); }
        }, 600);
    }

    function onizlemeyiYenile() {
        const pencere = cerceve.contentWindow;
        kaydirma = pencere?.scrollY ?? 0;
        pencere.location.replace(onizlemeAdresi(pencere.location.href));
    }

    // ---------------- 4) Kutuya girince: önizlemede yerini göster ----------------
    form.addEventListener("focusin", olay => {
        const d = belge();
        if (!d) return;
        d.querySelectorAll(".duz-vurgu").forEach(e => e.classList.remove("duz-vurgu"));
        const es = eslesmeler.get(olay.target.dataset?.metin);
        if (!es) return;
        es.dugumler.forEach(dg => dg.parentElement.classList.add("duz-vurgu"));
        es.dugumler[0].parentElement.scrollIntoView({ block: "center", behavior: "smooth" });
    });

    // ---------------- 5) "İlk haline dön" ----------------
    form.addEventListener("click", olay => {
        const buton = olay.target.closest("[data-varsayilana-don]");
        if (!buton) return;
        const kutu = buton.closest(".duz-alan").querySelector("[data-metin]");
        kutu.value = kutu.dataset.varsayilan;
        kutu.dispatchEvent(new Event("input", { bubbles: true }));
        kutu.focus();
    });

    // ---------------- 6) Arama: "ne değiştirmek istiyorsun?" ----------------
    document.querySelector("[data-duz-ara]")?.addEventListener("input", olay => {
        const aranan = olay.target.value.trim().toLocaleLowerCase("tr");
        form.querySelectorAll(".duz-alan").forEach(alan => {
            const kutu = alan.querySelector("[data-metin], [data-ayar], [data-sayfa]");
            const metin = `${alan.dataset.ara} ${kutu?.value ?? ""}`.toLocaleLowerCase("tr");
            alan.hidden = aranan !== "" && !metin.includes(aranan);
        });
        // İçinde görünen kutu kalmayan bölüm başlıklarını da gizle
        form.querySelectorAll("[data-duz-bolum]").forEach(baslik => {
            let oge = baslik.nextElementSibling, gorunen = false;
            while (oge && !oge.matches("[data-duz-bolum]")) { if (!oge.hidden) gorunen = true; oge = oge.nextElementSibling; }
            baslik.hidden = !gorunen;
        });
    });

    // ---------------- 7) Önizleme düğmeleri ----------------
    document.querySelectorAll("[data-onizleme-adresi]").forEach(buton => buton.addEventListener("click", () => {
        document.querySelectorAll("[data-onizleme-adresi]").forEach(b => b.classList.toggle("active", b === buton));
        kaydirma = 0;
        cerceve.contentWindow.location.replace(onizlemeAdresi(buton.dataset.onizlemeAdresi));
    }));
    // Bilgisayar görünümünde sayfa gerçek masaüstü genişliğinde (1280 px) çizilir ve kutuya sığacak kadar
    // küçültülür; böylece hocanın bilgisayarda göreceği düzen birebir görünür. Telefonda 390 px.
    const cerceveKutusu = document.querySelector(".duz-cerceve-kutu");
    let cihazGenisligi = 1280;
    function olcekle() {
        if (!cerceveKutusu) return;
        const olcek = Math.min(1, cerceveKutusu.clientWidth / cihazGenisligi);
        cerceve.style.width = cihazGenisligi + "px";
        cerceve.style.height = (cerceveKutusu.clientHeight / olcek) + "px";
        cerceve.style.transform = `scale(${olcek})`;
    }
    window.addEventListener("resize", olcekle);
    olcekle();

    document.querySelectorAll("[data-cihaz]").forEach(buton => buton.addEventListener("click", () => {
        document.querySelectorAll("[data-cihaz]").forEach(b => b.classList.toggle("active", b === buton));
        cerceveKutusu?.classList.toggle("telefon", buton.dataset.cihaz === "telefon");
        cihazGenisligi = buton.dataset.cihaz === "telefon" ? 390 : 1280;
        olcekle();
    }));
    document.querySelector("[data-onizleme-yenile]")?.addEventListener("click", onizlemeyiYenile);

    // ---------------- 8) Kaydetmeden çıkılırsa uyar ----------------
    document.addEventListener("submit", () => { kaydediliyor = true; });
    window.addEventListener("beforeunload", olay => {
        if (form.dataset.degisiklik && !kaydediliyor) { olay.preventDefault(); olay.returnValue = ""; }
    });
})();
