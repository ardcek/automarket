// =====================================================================
// YAPAY ZEKÂ ASİSTANI sayfası (Areas/Yonetim/Views/YapayZeka/Index.cshtml)
// Sunucu tarafı: Areas/Yonetim/Controllers/YapayZekaController.cs
//
//  • Durum çubuğu : motor hazır mı? (2 saniyede bir sorar)
//  • Panel modu   : istek → /Sor → işlem kartları → (düzenle, kontrol et, önizle) → /Uygula
//  • Kod modu     : istek → /KodDosyaSec → /KodOner → /KodDene → /KodUygula → /YenidenBaslat
// Sayfada <script> yazılamadığı (güvenlik kuralı) için bütün davranış bu dosyada.
// Ekrana yazılan her şey textContent ile yazılır (modelin cevabı HTML olarak çalıştırılmaz).
// =====================================================================
(function () {
    const $ = secici => document.querySelector(secici);
    if (!$("#yz-durum-karti")) return;

    // ---------------- Küçük yardımcılar ----------------
    /** Element oluşturur: el("div", { class: "x" }, "yazı", altEleman) */
    function el(etiket, ozellikler = {}, ...cocuklar) {
        const e = document.createElement(etiket);
        for (const [ad, deger] of Object.entries(ozellikler)) {
            if (ad === "class") e.className = deger;
            else if (ad.startsWith("on")) e.addEventListener(ad.slice(2), deger);
            else if (deger === true) e.setAttribute(ad, "");
            else if (deger !== false && deger != null) e.setAttribute(ad, deger);
        }
        for (const c of cocuklar.flat()) if (c != null) e.append(c instanceof Node ? c : document.createTextNode(String(c)));
        return e;
    }
    const goster = (e, acik = true) => e?.classList.toggle("d-none", !acik);
    const anahtar = () => document.querySelector('input[name="__RequestVerificationToken"]').value;

    /** JSON gönderir; sunucu { hata } döndürürse hata fırlatır. */
    async function gonder(adres, govde = {}, sinyal) {
        const cevap = await fetch(adres, {
            method: "POST", signal: sinyal,
            headers: { "Content-Type": "application/json", "RequestVerificationToken": anahtar() },
            body: JSON.stringify(govde),
        });
        const json = await cevap.json().catch(() => ({ hata: `Sunucu hatası (${cevap.status})` }));
        if (!cevap.ok || json.hata) throw new Error(json.hata || `Sunucu hatası (${cevap.status})`);
        return json;
    }

    /** "Düşünüyor… 12 sn" sayacı; durdurmak için dönen fonksiyonu çağır. */
    function sayac(yer, yazi = "Düşünüyor") {
        const bas = Date.now();
        const yaz = () => { yer.textContent = `${yazi}… ${Math.round((Date.now() - bas) / 1000)} sn`; };
        yaz();
        const id = setInterval(yaz, 1000);
        return () => { clearInterval(id); yer.textContent = ""; };
    }

    function hataGoster(yer, mesaj) {
        yer.replaceChildren(el("div", { class: "alert alert-danger small mb-0" }, el("i", { class: "bi bi-exclamation-triangle me-1" }), mesaj));
        goster(yer);
    }

    // Örnek isteklere tıklayınca yazı kutusuna kopyala
    document.querySelectorAll(".yz-ornek").forEach(b => b.addEventListener("click", () => {
        const hedef = $(b.dataset.hedef);
        hedef.value = b.textContent.trim();
        hedef.focus();
    }));

    // =====================================================================
    // DURUM ÇUBUĞU
    // =====================================================================
    const durumYazilari = { Yok: "Kurulu değil", Kapali: "Kapalı", Aciliyor: "Açılıyor", Hazir: "Hazır", Hata: "Hata" };
    let sonDurum = null;

    async function durumuYenile() {
        try {
            const d = await (await fetch($("#yz-durum-karti").dataset.durumAdresi)).json();
            sonDurum = d.durum;
            $(".yz-isik").dataset.durum = d.durum;
            $(".yz-isik").title = durumYazilari[d.durum] ?? d.durum;
            $("#yz-durum-yazi").textContent = d.mesaj;
            if (d.model) $("#yz-model").textContent = d.model;
            $("#yz-motor").textContent = d.motor ?? "";
            const cikti = $("#yz-son-cikti");
            cikti.textContent = d.sonCikti ?? "";
            goster(cikti, !!d.sonCikti);
        } catch { /* sunucu yeniden başlıyor olabilir */ }
        setTimeout(durumuYenile, sonDurum === "Aciliyor" ? 2000 : 10000);
    }
    durumuYenile();

    $("#yz-baslat")?.addEventListener("click", async () => {
        await gonder("/Yonetim/YapayZeka/Baslat", { model: $("#yz-model-sec").value }).catch(h => alert(h.message));
        sonDurum = "Aciliyor";
        setTimeout(durumuYenile, 500);
    });
    $("#yz-durdur")?.addEventListener("click", async () => {
        await gonder("/Yonetim/YapayZeka/Durdur").catch(h => alert(h.message));
        durumuYenile();
    });

    // =====================================================================
    // PANEL MODU
    // =====================================================================
    let sonIstek = "", sonAnladigim = "", sonSure = 0, iptalDenetcisi = null;

    $("#yz-gonder").addEventListener("click", async () => {
        const istek = $("#yz-istek").value.trim();
        if (!istek) { $("#yz-istek").focus(); return; }
        goster($("#yz-sonuc"), false); goster($("#yz-bitti"), false);
        $("#yz-gonder").disabled = true; goster($("#yz-iptal"));
        iptalDenetcisi = new AbortController();
        const dur = sayac($("#yz-sayac"), sonDurum === "Hazir" ? "Düşünüyor" : "Model yükleniyor, sonra düşünecek");
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/Sor", { istek }, iptalDenetcisi.signal);
            [sonIstek, sonAnladigim, sonSure] = [istek, cevap.anladigim ?? "", cevap.sure];
            $("#yz-anladigim").replaceChildren(
                el("b", {}, "Anladığım: "), sonAnladigim,
                el("div", { class: "text-muted mt-1" }, `${cevap.sure} sn · ${cevap.tokenHizi} token/sn · ${cevap.model ?? ""}${cevap.duzeltildi ? " · hatalar bir kez otomatik düzeltildi" : ""}`));
            kartlariCiz(cevap.kartlar);
            goster($("#yz-sonuc"));
        } catch (hata) {
            if (hata.name !== "AbortError") hataGoster($("#yz-bitti"), hata.message);
        } finally {
            dur(); $("#yz-gonder").disabled = false; goster($("#yz-iptal"), false);
        }
    });
    $("#yz-iptal").addEventListener("click", () => iptalDenetcisi?.abort());

    /** Her işlem için bir kart: işaret kutusu, başlık, özet, hatalar ve "değerleri düzenle". */
    function kartlariCiz(kartlar, oncekiSecimler) {
        const yer = $("#yz-kartlar");
        yer.replaceChildren();
        let uygulanacakVar = false, onizlemeVar = false, yapilamazVar = false;
        kartlar.forEach((k, sira) => {
            const hatali = k.hatalar.length > 0;
            if (!k.uygulanabilir) {
                // Cevap ya da "panelden yapılamıyor": seçilemez bilgi kutusu
                if (k.tur === "yapilamaz") yapilamazVar = true;
                yer.append(el("div", { class: `alert ${k.tur === "yapilamaz" ? "alert-warning" : "alert-info"} small mb-0`, "data-tur": k.tur },
                    el("i", { class: `bi ${k.ikon} me-1` }), el("b", {}, k.baslik + ": "), k.ozet));
                return;
            }
            uygulanacakVar = true;
            if (k.onizlemeVar) onizlemeVar = true;
            // İlk gelişte sunucunun önerisi (hatalı ya da "değişiklik yok" kartlar işaretsiz); kontrolden sonra kullanıcının seçimi korunur
            const secili = oncekiSecimler ? (oncekiSecimler[sira] ?? k.secili) : k.secili;
            const kutuId = `yz-kart-${sira}`;
            yer.append(el("div", { class: `card yz-kart ${hatali ? "border-danger" : ""}`, "data-tur": k.tur },
                el("div", { class: "card-body py-2" },
                    el("div", { class: "d-flex gap-2 align-items-start" },
                        el("input", { class: "form-check-input mt-1", type: "checkbox", id: kutuId, checked: secili }),
                        el("label", { class: "flex-grow-1", for: kutuId },
                            el("div", { class: "fw-semibold" }, el("i", { class: `bi ${k.ikon} me-1 metin-ana` }), k.baslik),
                            el("div", { class: "small" }, k.ozet),
                            k.hatalar.map(h => el("div", { class: "small text-danger" }, el("i", { class: "bi bi-x-circle me-1" }), h))),
                        el("button", { type: "button", class: "btn btn-sm btn-sade", "data-bs-toggle": "collapse", "data-bs-target": `#${kutuId}-ayrinti` }, "Değerleri düzenle")),
                    el("div", { class: "collapse mt-2", id: `${kutuId}-ayrinti` }, parametreKutulari(k.parametreler)))));
        });
        goster($("#yz-uygula"), uygulanacakVar);
        goster($("#yz-kontrol"), uygulanacakVar);
        goster($("#yz-onizle"), onizlemeVar);
        goster($("#yz-koda-gec"), yapilamazVar && !!$("#yz-kod-istek"));
        goster($("#yz-onizleme-karti"), false);
    }

    /** Parametreleri türüne göre düzenlenebilir kutulara çevirir (yazı, sayı, evet/hayır, liste). */
    function parametreKutulari(parametreler) {
        const satir = el("div", { class: "row g-2" });
        for (const [ad, deger] of Object.entries(parametreler)) {
            const etiket = el("label", { class: "form-label small mb-0 text-muted" }, ad.replaceAll("_", " "));
            let kutu;
            if (typeof deger === "boolean") {
                kutu = el("div", { class: "form-check form-switch" }, el("input", { class: "form-check-input", type: "checkbox", "data-ad": ad, "data-tur": "bool", checked: deger }));
            } else if (typeof deger === "number") {
                kutu = el("input", { class: "form-control form-control-sm", type: "number", step: "any", value: deger, "data-ad": ad, "data-tur": "sayi" });
            } else if (Array.isArray(deger)) {
                kutu = el("textarea", { class: "form-control form-control-sm", rows: Math.min(6, deger.length + 1), "data-ad": ad, "data-tur": "liste" });
                kutu.value = deger.join("\n");
            } else {
                const uzun = String(deger ?? "").length > 70 || ad === "icerik";
                kutu = el(uzun ? "textarea" : "input", { class: "form-control form-control-sm", rows: uzun ? 5 : null, "data-ad": ad, "data-tur": "yazi" });
                kutu.value = deger ?? "";
            }
            satir.append(el("div", { class: Array.isArray(deger) || typeof deger === "string" && String(deger).length > 70 ? "col-12" : "col-md-6" }, etiket, kutu));
        }
        return satir;
    }

    /** Kartlardaki (düzenlenmiş) değerlerden işlem listesini yeniden oluşturur. */
    function islemleriTopla(sadeceSecililer) {
        const islemler = [], secimler = [];
        document.querySelectorAll("#yz-kartlar > [data-tur]").forEach(kart => {
            const kutu = kart.querySelector(":scope > .card-body > div > input[type=checkbox]");
            if (!kutu) return;   // cevap / "yapılamaz" kutuları seçilemez, gönderilmez
            secimler.push(kutu.checked);
            if (sadeceSecililer && !kutu.checked) return;
            const islem = { tur: kart.dataset.tur };
            kart.querySelectorAll("[data-ad]").forEach(g => {
                const tur = g.dataset.tur;
                islem[g.dataset.ad] = tur === "bool" ? g.checked
                    : tur === "sayi" ? (g.value === "" ? null : Number(g.value))
                    : tur === "liste" ? g.value.split("\n").map(s => s.trim()).filter(Boolean)
                    : g.value;
            });
            islemler.push(islem);
        });
        return { islemler, secimler };
    }

    $("#yz-kontrol").addEventListener("click", async () => {
        const { islemler, secimler } = islemleriTopla(false);
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/Kontrol", { islemler });
            kartlariCiz(cevap.kartlar, secimler);
        } catch (hata) { alert(hata.message); }
    });

    $("#yz-onizle").addEventListener("click", async () => {
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/Onizle", { islemler: islemleriTopla(true).islemler });
            $("#yz-onizleme").src = cevap.adres;
            goster($("#yz-onizleme-karti"));
            $("#yz-onizleme-karti").scrollIntoView({ behavior: "smooth" });
        } catch (hata) { alert(hata.message); }
    });
    $("#yz-onizleme-kapat").addEventListener("click", () => goster($("#yz-onizleme-karti"), false));

    $("#yz-uygula").addEventListener("click", async () => {
        const islemler = islemleriTopla(true).islemler.filter(i => !["cevap_ver", "yapilamaz"].includes(i.tur));
        if (islemler.length === 0) { alert("Uygulanacak işlem seçilmedi."); return; }
        $("#yz-uygula").disabled = true;
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/Uygula", { istek: sonIstek, anladigim: sonAnladigim, islemler, sure: sonSure });
            goster($("#yz-sonuc"), false);
            $("#yz-bitti").replaceChildren(
                el("div", { class: "fw-semibold mb-1" }, el("i", { class: "bi bi-check-circle me-1" }), "Uygulandı. Değişiklikler sitede şimdi geçerli."),
                el("div", { class: "small yz-ozet mb-2" }, cevap.ozet),
                el("div", { class: "d-flex flex-wrap gap-2" },
                    cevap.linkler.map(l => el("a", { class: "btn btn-sm btn-sade", href: l, target: "_blank" }, el("i", { class: "bi bi-box-arrow-up-right me-1" }), l)),
                    geriAlButonu(cevap.kayitId)));
            goster($("#yz-bitti"));
        } catch (hata) {
            alert(hata.message);
        } finally {
            $("#yz-uygula").disabled = false;
        }
    });

    /** Geçmişteki "Geri al" ile aynı işi yapan küçük form (sayfa yenilenir). */
    function geriAlButonu(kayitId) {
        return el("form", { method: "post", action: `/Yonetim/YapayZeka/GeriAl/${kayitId}` },
            el("input", { type: "hidden", name: "__RequestVerificationToken", value: anahtar() }),
            el("button", { class: "btn btn-sm btn-sade text-danger" }, el("i", { class: "bi bi-arrow-counterclockwise me-1" }), "Geri al"));
    }

    $("#yz-vazgec").addEventListener("click", () => { goster($("#yz-sonuc"), false); $("#yz-istek").focus(); });

    $("#yz-koda-gec").addEventListener("click", () => {
        $("#yz-kod-istek").value = sonIstek;
        bootstrap.Tab.getOrCreateInstance(document.querySelector('[data-bs-target="#yz-kod"]')).show();
    });

    // Ctrl+Enter ile gönder
    $("#yz-istek").addEventListener("keydown", e => { if (e.key === "Enter" && e.ctrlKey) $("#yz-gonder").click(); });

    // =====================================================================
    // KOD MODU (sadece Admin ve uygulama kaynak koddan çalışıyorsa sayfada var)
    // =====================================================================
    if (!$("#yz-kod-istek")) return;
    let kodIstek = "", kodAnladigim = "", kodSure = 0, dosyalar = [], degisiklikler = [], sonHatalar = [];

    function dosyalariCiz() {
        $("#yz-kod-dosyalar").replaceChildren(...dosyalar.map(d => el("span", { class: "etiket" }, el("i", { class: "bi bi-file-earmark-code" }), d,
            el("button", { type: "button", class: "btn-close ms-1", style: "font-size:.6rem", "aria-label": "Çıkar", onclick: () => { dosyalar = dosyalar.filter(x => x !== d); dosyalariCiz(); } }))));
    }

    $("#yz-kod-dosya-sec").addEventListener("click", async () => {
        kodIstek = $("#yz-kod-istek").value.trim();
        if (!kodIstek) { $("#yz-kod-istek").focus(); return; }
        goster($("#yz-kod-oneri"), false); goster($("#yz-kod-bitti"), false);
        $("#yz-kod-dosya-sec").disabled = true;
        const dur = sayac($("#yz-kod-sayac"), "Dosyalar aranıyor");
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/KodDosyaSec", { istek: kodIstek });
            dosyalar = cevap.dosyalar;
            kodSure = cevap.sure;
            $("#yz-kod-anladigim").textContent = cevap.anladigim ?? "";
            $("#yz-kod-tum-dosyalar").replaceChildren(...cevap.tumDosyalar.map(d => el("option", { value: d })));
            dosyalariCiz();
            goster($("#yz-kod-dosyalar-karti"));
        } catch (hata) { hataGoster($("#yz-kod-bitti"), hata.message); }
        finally { dur(); $("#yz-kod-dosya-sec").disabled = false; }
    });

    $("#yz-kod-dosya-ekle-buton").addEventListener("click", () => {
        const d = $("#yz-kod-dosya-ekle").value.trim();
        if (d && !dosyalar.includes(d)) { dosyalar.push(d); dosyalariCiz(); }
        $("#yz-kod-dosya-ekle").value = "";
    });

    /** Değişiklik önerisini ister. duzeltme = true ise önceki öneri ve derleme hataları da gönderilir. */
    async function oneriIste(duzeltme) {
        const buton = duzeltme ? $("#yz-kod-duzelt") : $("#yz-kod-oner");
        buton.disabled = true;
        const dur = sayac($("#yz-kod-sayac"), duzeltme ? "Hatalar düzeltiliyor" : "Değişiklik yazılıyor");
        try {
            const govde = { istek: kodIstek, dosyalar };
            if (duzeltme) Object.assign(govde, { degisiklikler, oncekiHata: sonHatalar.join("\n") });
            const cevap = await gonder("/Yonetim/YapayZeka/KodOner", govde);
            kodAnladigim = cevap.anladigim ?? "";
            kodSure += cevap.sure;
            degisiklikler = cevap.degisiklikler;
            $("#yz-kod-oneri-anladigim").replaceChildren(el("b", {}, "Anladığım: "), kodAnladigim, el("div", { class: "text-muted" }, `${cevap.sure} sn`));
            farklariCiz();
            $("#yz-kod-deneme").replaceChildren();
            goster($("#yz-kod-uygula"), false); goster($("#yz-kod-duzelt"), false);
            goster($("#yz-kod-oneri"));
        } catch (hata) { hataGoster($("#yz-kod-bitti"), hata.message); }
        finally { dur(); buton.disabled = false; }
    }
    $("#yz-kod-oner").addEventListener("click", () => oneriIste(false));
    $("#yz-kod-duzelt").addEventListener("click", () => oneriIste(true));

    /** Her değişiklik: dosya adı, eski (kırmızı) ve yeni (yeşil) metin. */
    function farklariCiz() {
        $("#yz-kod-farklar").replaceChildren(...(degisiklikler.length === 0
            ? [el("div", { class: "alert alert-secondary small mb-0" }, "Model bir değişiklik önermedi. İsteği daha açık yazmayı ya da başka dosya eklemeyi dene.")]
            : degisiklikler.map(d => el("div", { class: `card ${d.hata ? "border-danger" : ""}` },
                el("div", { class: "card-header small d-flex gap-2 align-items-center" },
                    el("i", { class: "bi bi-file-earmark-code" }), el("b", {}, d.dosya), d.yeniDosya ? el("span", { class: "etiket" }, "yeni dosya") : null,
                    d.hata ? el("span", { class: "text-danger ms-auto" }, d.hata) : null),
                el("div", { class: "card-body p-0" },
                    d.yeniDosya ? null : el("pre", { class: "yz-fark yz-eski" }, d.eski),
                    el("pre", { class: "yz-fark yz-yeni" }, d.yeni))))));
    }

    $("#yz-kod-dene").addEventListener("click", async () => {
        $("#yz-kod-dene").disabled = true;
        const dur = sayac($("#yz-kod-sayac"), "Geçici kopyada derleniyor");
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/KodDene", { degisiklikler });
            degisiklikler = cevap.degisiklikler;   // "eski metin bulunamadı" gibi işaretlerle
            farklariCiz();
            sonHatalar = cevap.hatalar;
            $("#yz-kod-deneme").replaceChildren(cevap.basarili
                ? el("div", { class: "alert alert-success small mb-0" }, el("i", { class: "bi bi-check-circle me-1" }), `Derleme başarılı (${cevap.sure} sn). Gerçek dosyalar henüz değişmedi; farkı okuyup "Uygula"ya basabilirsin.`)
                : el("div", { class: "alert alert-danger small mb-0" }, el("b", {}, "Denemede hata çıktı (gerçek dosyalar değişmedi):"),
                    el("ul", { class: "mb-0 mt-1" }, cevap.hatalar.map(h => el("li", {}, h)))));
            goster($("#yz-kod-uygula"), cevap.basarili);
            goster($("#yz-kod-duzelt"), !cevap.basarili);
        } catch (hata) { hataGoster($("#yz-kod-deneme"), hata.message); }
        finally { dur(); $("#yz-kod-dene").disabled = false; }
    });

    $("#yz-kod-uygula").addEventListener("click", async () => {
        if (!confirm("Değişiklikler gerçek kod dosyalarına yazılsın mı? (Eski halleri yedeklenir, geçmişten geri alınabilir.)")) return;
        try {
            const cevap = await gonder("/Yonetim/YapayZeka/KodUygula", { istek: kodIstek, anladigim: kodAnladigim, degisiklikler, sure: kodSure });
            goster($("#yz-kod-oneri"), false); goster($("#yz-kod-dosyalar-karti"), false);
            const bitti = $("#yz-kod-bitti");
            bitti.replaceChildren(
                el("div", { class: "fw-semibold mb-1" }, el("i", { class: "bi bi-check-circle me-1" }), "Dosyalar değiştirildi ve yedeklendi."),
                el("div", { class: "small yz-ozet mb-2" }, cevap.ozet),
                cevap.veritabaniYenilenecek ? el("div", { class: "small mb-2" }, el("i", { class: "bi bi-database-exclamation me-1" }), "Bir model dosyası değişti: uygulama açılırken yerel veritabanı yedeklenip örnek verilerle yeniden oluşturulacak.") : null,
                el("div", { class: "d-flex gap-2" },
                    cevap.yenidenBaslatilabilir
                        ? el("button", { type: "button", class: "btn btn-sm btn-primary", onclick: yenidenBaslat }, el("i", { class: "bi bi-arrow-repeat me-1" }), "Şimdi yeniden başlat")
                        : el("span", { class: "small" }, "Değişikliğin görünmesi için uygulamayı kapatıp yeniden açın."),
                    geriAlButonu(cevap.kayitId)));
            goster(bitti);
        } catch (hata) { alert(hata.message); }
    });

    /** Uygulamayı yeniden başlatır ve açılana kadar bekleyip sayfayı yeniler. */
    async function yenidenBaslat() {
        try { await gonder("/Yonetim/YapayZeka/YenidenBaslat"); } catch (hata) { alert(hata.message); return; }
        const bitti = $("#yz-kod-bitti");
        bitti.replaceChildren(el("div", {}, el("span", { class: "spinner-border spinner-border-sm me-2" }), "Uygulama yeniden derlenip açılıyor (yarım dakika kadar)…"));
        await new Promise(r => setTimeout(r, 4000));
        for (let i = 0; i < 120; i++) {
            try { if ((await fetch("/health", { cache: "no-store" })).ok) { location.reload(); return; } } catch { /* henüz açılmadı */ }
            await new Promise(r => setTimeout(r, 2000));
        }
        bitti.textContent = "Uygulama açılmadı. BASLAT.bat penceresindeki hatayı okuyun; gerekirse GERI-AL.bat ile son değişikliği geri alın.";
    }
})();
