// =====================================================================
// EFEKTLER — müşteri sitesindeki fareyle / görünürlükle çalışan hareketler (Views/Shared/_Layout.cshtml yükler).
// Görünümü: wwwroot/css/vitrin.css. Kaydırmaya bağlı paralaks orada, CSS ile yapılır (bu dosyada değil).
// View'larda <script> yazılmaz; HTML'e şunlar eklenir:
//   data-goster            → kaydırınca yumuşakça belirir (aynı anda görünenler sırayla gelir)
//   data-sayac="1481"      → görününce 0'dan sayarak gelir (data-sonek="+" sona eklenir)
//   data-egim              → fareyle kart 3B eğilir, ışık fareyi izler ( data-egim="isik" → sadece ışık )
//   data-sahne             → koyu bölüm: ekranda değilken içindeki sürekli hareketler durur. İçinde:
//       data-derinlik="24" → fareyle en fazla 24 px kayar (eksi değer ters yöne; büyük sayı = öne yakın)
//       data-dondur="8"    → fareyle en fazla 8 derece 3B döner
//   data-duraklat          → ekranda değilken sürekli hareketi durur (örn. marka şeridi)
//   data-miknatis          → buton fareye doğru hafifçe çekilir
//   data-mobil-cubuk="#id" → telefondaki alt çubuk, #id ekranda görününce gizlenir
//   data-gizle-gorunce="#a, .b" → listelenen öğelerden BİRİ ekrandayken gizlenir (ana sayfadaki hızlı kirala çubuğu)
//   data-imlec-yazi="İncele"    → özel imleç bu öğenin üstünde büyür, halkasında bu söz yazar
//   data-perde             → başlığın kelimeleri kaydırınca perde arkasından sırayla yükselir (bölme burada, hareket vitrin.css'te)
//   data-gaz               → (son çağrı bandı) fare butona yaklaşınca arkadaki yol ızgarası ve far izleri hızlanır
//   data-araba             → (buton; sadece CSS) üstüne gelince alt kenarından küçük bir araba geçer
//   <body data-imlec="acik"> → özel fare imleci (Yönetim → Ayarlar → Görünüm → "Özel fare imleci")
//   .ust-menu.saydam-baslar → üst menü sayfanın en üstünde saydam, kaydırınca buzlu cam (view'da ViewData["SaydamMenu"] = true)
// "Hareketi azalt" açıksa (tema.js <html>'e "animasyon" eklemez) sadece menü ve alt çubuk çalışır.
// PERFORMANS: fare hareketinde sadece ilgili öğelerin translate / transform değeri yazılır (kare başına bir kez).
// =====================================================================
(function () {
    window.efektlerHazir = true;   // tema.js: bu dosya yüklenemezse gizli bölümleri geri açar
    const kok = document.documentElement;
    const hareket = kok.classList.contains("animasyon");
    const fareVar = matchMedia("(hover: hover) and (pointer: fine)").matches;   // dokunmatik ekranda eğme yok

    // ---- 1) Üst menü: yüksekliği ölçülür, kaydırınca gölge ("saydam-baslar" varsa en üstte saydamdır) ----
    const menu = document.querySelector(".ust-menu");
    if (menu) {
        const acilirMenu = document.getElementById("menu");
        let menuAcik = false, kaydi = null;
        const olc = () => { if (!menuAcik) kok.style.setProperty("--ust-yukseklik", menu.offsetHeight + "px"); };
        const guncelle = () => {
            const simdi = scrollY > 24;
            if (simdi !== kaydi) { kaydi = simdi; menu.classList.toggle("kaydi", simdi); }   // sadece değişince yaz
            menu.classList.toggle("menu-acik", menuAcik);                                    // telefonda menü açıkken saydam olmasın
        };
        acilirMenu?.addEventListener("show.bs.collapse", () => { menuAcik = true; guncelle(); });
        acilirMenu?.addEventListener("hidden.bs.collapse", () => { menuAcik = false; guncelle(); });
        addEventListener("scroll", guncelle, { passive: true });
        addEventListener("resize", olc);
        olc();
        guncelle();
    }

    // ---- 2) Telefondaki alt çubuk (araç detayı): hedef kutu görünürken gizlenir ----
    document.querySelectorAll("[data-mobil-cubuk]").forEach(cubuk => {
        const hedef = document.querySelector(cubuk.dataset.mobilCubuk);
        if (!hedef) return;
        new IntersectionObserver(([g]) => cubuk.classList.toggle("gizli", g.isIntersecting)).observe(hedef);
    });

    // ---- 2b) Hızlı kirala çubuğu (ana sayfa): listelenen öğelerden BİRİ bile ekrandayken gizlenir ----
    //      data-gizle-gorunce="#arama, .garaj, .alt-bilgi" → arama kutusu, garaj ya da alt bilgi görünürken saklanır.
    //      HTML'de "gizli" ile başlar (sayfa açılırken bir an görünüp kaybolmasın); gözcü ilk ölçümde doğru hâle getirir.
    document.querySelectorAll("[data-gizle-gorunce]").forEach(cubuk => {
        const hedefler = [...document.querySelectorAll(cubuk.dataset.gizleGorunce)];
        const gorunen = new Set();
        const gozcu = new IntersectionObserver(girdiler => {
            girdiler.forEach(g => g.isIntersecting ? gorunen.add(g.target) : gorunen.delete(g.target));
            cubuk.classList.toggle("gizli", gorunen.size > 0);
        });
        hedefler.forEach(h => gozcu.observe(h));
        if (!hedefler.length) cubuk.classList.remove("gizli");
    });

    // ---- 2c) Ana sayfadayken logoya tıklamak: sayfa baştan yüklenmez, en üste çıkılır; ekranın altından küçük bir araba geçer ----
    if (location.pathname === "/") document.querySelector(".ust-menu .logo")?.addEventListener("click", olay => {
        if (olay.button !== 0 || olay.ctrlKey || olay.metaKey || olay.shiftKey || olay.altKey) return;   // yeni sekmede açmak isteyene dokunma
        olay.preventDefault();
        scrollTo({ top: 0, behavior: hareket ? "smooth" : "instant" });
        if (!hareket || document.querySelector(".korna-araba")) return;
        const araba = document.createElement("span");
        araba.className = "korna-araba";
        araba.setAttribute("aria-hidden", "true");
        araba.innerHTML = '<span class="araba-ikon"></span>';
        araba.addEventListener("animationend", () => araba.remove());
        document.body.append(araba);
    });

    if (!hareket) {   // hareket kapalı: her şey olduğu gibi görünür
        document.querySelectorAll("[data-goster]").forEach(e => e.classList.add("gorundu"));
        return;
    }

    // ---- 3) Kaydırınca belirme: aynı anda ekrana girenlere sırayla 0, 1, 2... gecikme verilir ----
    // Sayfa geçiş efektiyle gelindiyse ilk ekrandakiler hemen görünür (yoksa geçiş bitince kartlar bir an kaybolur)
    let ilkTur = true;
    const belirt = new IntersectionObserver(girdiler => {
        const hemen = ilkTur && kok.classList.contains("gecisle-geldi");
        ilkTur = false;
        girdiler.filter(g => g.isIntersecting).forEach((g, sira) => {
            g.target.style.setProperty("--sira", Math.min(sira, 8));
            g.target.classList.toggle("hemen", hemen);
            g.target.classList.add("gorundu");
            belirt.unobserve(g.target);
        });
    }, { rootMargin: "0px 0px -8% 0px" });
    document.querySelectorAll("[data-goster]").forEach(e => belirt.observe(e));

    // ---- 4) Sayaçlar: 0'dan hedefe, sona doğru yavaşlayarak ----
    const bicim = new Intl.NumberFormat("tr-TR");
    const say = e => {
        const hedef = Number(e.dataset.sayac), sonek = e.dataset.sonek ?? "", baslangic = performance.now(), sure = 1800;
        if (!Number.isFinite(hedef)) return;
        const adim = simdi => {
            const t = Math.min(1, (simdi - baslangic) / sure);
            e.textContent = bicim.format(Math.round(hedef * (1 - Math.pow(1 - t, 4)))) + sonek;
            if (t < 1) requestAnimationFrame(adim);
        };
        requestAnimationFrame(adim);
    };
    const sayacGozcusu = new IntersectionObserver(girdiler => girdiler.forEach(g => {
        if (g.isIntersecting) { say(g.target); sayacGozcusu.unobserve(g.target); }
    }), { threshold: .6 });
    document.querySelectorAll("[data-sayac]").forEach(e => sayacGozcusu.observe(e));

    // ---- 5) Ekranda olmayan bölümlerin sürekli hareketlerini durdur (pil ve işlemci dostu) ----
    const duraklatici = new IntersectionObserver(girdiler => girdiler.forEach(g => g.target.classList.toggle("duraklat", !g.isIntersecting)));
    document.querySelectorAll("[data-sahne], [data-duraklat]").forEach(e => duraklatici.observe(e));

    // ---- 5b) Garaj (ana sayfa): geniş ekranda adımlar üst üste durur, sadece sırası gelen görünür.
    //      Klavyeyle (Tab) görünmeyen bir aracın butonuna gelinirse sayfa o aracın dilimine kaydırılır, araç sahneye gelir.
    //      Dilim hesabı vitrin.css'teki "Garaj" ile aynı: yapışkan yol = bölüm yüksekliği − ekran, her araç 1/adet'lik pay.
    //      Fareyle tıklama: üst üste duran adımlardan sadece sıradaki ("aktif") tıklanır; sırayı kaydırırken burası belirler
    //      (garaj ekrandayken, karede en çok bir kez). Kart düzeninde (telefon vb.) hiçbir şey yapılmaz.
    document.querySelectorAll(".garaj").forEach(garaj => {
        const sahne = garaj.querySelector(".garaj-sabit");
        const adimlar = [...garaj.querySelectorAll("[data-garaj-adim]")];
        if (!sahne || !adimlar.length) return;
        let yapiskan = false, gorunur = false, aktif = -1, kare = 0;
        const olc = () => { yapiskan = getComputedStyle(sahne).position === "sticky"; garaj.classList.toggle("garaj-izli", yapiskan); };
        // Sıradaki adım "aktif" (tıklanır); o ve iki komşusu "canli" (animasyonları çalışır, vitrin.css). Garaj ekrandan
        // uzaktayken (gorunur = false) hiçbir adım canlı değildir: kaydırma sayfanın geri kalanında hafif kalır.
        const sira = () => {
            kare = 0;
            if (!yapiskan) return;
            const ilerleme = -garaj.getBoundingClientRect().top / Math.max(1, garaj.offsetHeight - innerHeight);
            const i = gorunur ? Math.min(adimlar.length - 1, Math.max(0, Math.floor(ilerleme * adimlar.length))) : -9;
            if (i === aktif) return;
            aktif = i;
            garaj.classList.toggle("garaj-canli", gorunur);
            adimlar.forEach((adim, k) => { adim.classList.toggle("aktif", k === i); adim.classList.toggle("canli", Math.abs(k - i) <= 1); });
        };
        olc();
        sira();
        addEventListener("resize", () => { olc(); sira(); });
        new IntersectionObserver(([g]) => { gorunur = g.isIntersecting; sira(); }, { rootMargin: "40% 0px" }).observe(garaj);   // biraz önceden canlan
        addEventListener("scroll", () => { if (gorunur && !kare) kare = requestAnimationFrame(sira); }, { passive: true });
        garaj.addEventListener("focusin", olay => {
            const adim = olay.target.closest("[data-garaj-adim]");
            if (!adim || !yapiskan) return;
            const yol = garaj.offsetHeight - innerHeight;
            const hedef = scrollY + garaj.getBoundingClientRect().top + yol * (adimlar.indexOf(adim) + .45) / adimlar.length;
            scrollTo({ top: hedef, behavior: "instant" });
        });
    });

    // ---- 5c) Perdeli başlıklar: data-perde başlığın kelimeleri ayrı kutulara bölünür (--k = sıra);
    //      vitrin.css kaydırdıkça her kelimeyi kendi kutusunun arkasından yükseltir. Yazı aynı kalır, ekran okuyucu aynen okur.
    document.querySelectorAll("[data-perde]").forEach(baslik => {
        if (baslik.children.length) return;   // içinde başka etiket varsa (vurgu vb.) dokunma
        const kelimeler = baslik.textContent.trim().split(/\s+/);
        baslik.replaceChildren(...kelimeler.flatMap((kelime, sira) => {
            const kutu = document.createElement("span"), ic = document.createElement("span");
            kutu.className = "perde-kelime";
            kutu.style.setProperty("--k", sira);
            ic.textContent = kelime;
            kutu.append(ic);
            return sira ? [" ", kutu] : [kutu];
        }));
    });

    if (!fareVar) return;

    // ---- 6) Koyu bölümlerde fareyle derinlik: her katman kendi derinliğinde kayar ----
    //      Bölümün doğrudan içinde .fener varsa (üst bölüm, garaj) fareyi yumuşakça izleyen far ışığı olur.
    document.querySelectorAll("[data-sahne]").forEach(sahne => {
        const katmanlar = [...sahne.querySelectorAll("[data-derinlik]")].map(e => [e, Number(e.dataset.derinlik) || 0]);
        const donenler = [...sahne.querySelectorAll("[data-dondur]")].map(e => [e, Number(e.dataset.dondur) || 6]);
        const fener = sahne.querySelector(":scope > .fener");
        if (!katmanlar.length && !donenler.length && !fener) return;
        let kare = 0, x = 0, y = 0, fx = 0, fy = 0;
        const uygula = () => {
            kare = 0;
            for (const [e, d] of katmanlar) e.style.translate = `${(x * d).toFixed(1)}px ${(y * d).toFixed(1)}px`;
            for (const [e, a] of donenler) e.style.transform = `rotateY(${(x * -a).toFixed(2)}deg) rotateX(${(y * a * .75).toFixed(2)}deg)`;
            if (fener) fener.style.translate = `${fx.toFixed(0)}px ${fy.toFixed(0)}px`;
        };
        sahne.addEventListener("pointermove", olay => {
            const r = sahne.getBoundingClientRect();
            x = (olay.clientX - r.left) / r.width * 2 - 1;   // -1 (sol) … 1 (sağ)
            y = (olay.clientY - r.top) / r.height * 2 - 1;   // -1 (üst) … 1 (alt)
            fx = olay.clientX - r.left;
            fy = olay.clientY - r.top;
            fener?.classList.add("acik");
            if (!kare) kare = requestAnimationFrame(uygula);
        });
        sahne.addEventListener("pointerleave", () => { x = 0; y = 0; fener?.classList.remove("acik"); if (!kare) kare = requestAnimationFrame(uygula); });
    });

    // ---- 7) Kartlar: fareyle 3B eğilme + fareyi izleyen ışık ----
    document.querySelectorAll("[data-egim]").forEach(kart => {
        const egilsin = kart.dataset.egim !== "isik";
        let kare = 0, kutu = null, x = .5, y = .5;
        const uygula = () => {
            kare = 0;
            kart.style.setProperty("--mx", (x * 100).toFixed(1) + "%");
            kart.style.setProperty("--my", (y * 100).toFixed(1) + "%");
            if (egilsin) {
                kart.style.setProperty("--ry", ((x - .5) * 12).toFixed(2) + "deg");
                kart.style.setProperty("--rx", ((.5 - y) * 10).toFixed(2) + "deg");
            }
        };
        kart.addEventListener("pointerenter", () => { kutu = kart.getBoundingClientRect(); kart.classList.add("egiliyor"); });
        kart.addEventListener("pointermove", olay => {
            kutu ??= kart.getBoundingClientRect();
            x = (olay.clientX - kutu.left) / kutu.width;
            y = (olay.clientY - kutu.top) / kutu.height;
            if (!kare) kare = requestAnimationFrame(uygula);
        });
        kart.addEventListener("pointerleave", () => {
            cancelAnimationFrame(kare); kare = 0; kutu = null;
            kart.classList.remove("egiliyor");
            ["--rx", "--ry", "--mx", "--my"].forEach(p => kart.style.removeProperty(p));
        });
    });

    // ---- 8) Mıknatıslı butonlar ----
    document.querySelectorAll("[data-miknatis]").forEach(buton => {
        buton.addEventListener("pointermove", olay => {
            const r = buton.getBoundingClientRect();
            buton.style.translate = `${((olay.clientX - r.left - r.width / 2) * .22).toFixed(1)}px ${((olay.clientY - r.top - r.height / 2) * .3).toFixed(1)}px`;
        });
        buton.addEventListener("pointerleave", () => buton.style.translate = "");
    });

    // ---- 9) Özel fare imleci: nokta fareyi anında, halka yumuşakça izler; tıklanabilir şeyin üstünde büyür ----
    // Aç / kapa: Yönetim → Ayarlar → Görünüm → "Özel fare imleci" (_Layout <body data-imlec="acik">).
    // Dokunmatik ekranda ve "hareketi azalt" açıkken hiç çalışmaz. Görünüm: vitrin.css → "Özel fare imleci".
    // PERFORMANS: sadece iki küçük öğenin transform değeri yazılır; halka durunca döngü de durur.
    if (fareVar && hareket && document.body.dataset.imlec === "acik") {
        const nokta = document.createElement("div"), halka = document.createElement("div");
        nokta.className = "imlec-nokta";
        halka.className = "imlec-halka";
        const etiket = document.createElement("span");   // data-imlec-yazi="İncele" olan öğenin üstünde halkada yazan söz
        etiket.className = "imlec-etiket";
        halka.append(etiket);
        nokta.setAttribute("aria-hidden", "true");
        halka.setAttribute("aria-hidden", "true");
        document.body.append(nokta, halka);
        kok.classList.add("ozel-imlec");

        let x = -100, y = -100, hx = -100, hy = -100, donuyor = false;
        const adim = () => {
            hx += (x - hx) * .2;
            hy += (y - hy) * .2;
            halka.style.transform = `translate(${hx.toFixed(1)}px, ${hy.toFixed(1)}px)`;
            if (Math.abs(x - hx) + Math.abs(y - hy) > .4) requestAnimationFrame(adim); else donuyor = false;
        };
        addEventListener("pointermove", olay => {
            if (olay.pointerType !== "mouse") return;
            x = olay.clientX;
            y = olay.clientY;
            nokta.style.transform = `translate(${x}px, ${y}px)`;
            kok.classList.add("imlec-gorunur");
            if (!donuyor) { donuyor = true; requestAnimationFrame(adim); }
        }, { passive: true });

        const tiklanabilir = "a, button, [role=button], label, select, summary, [data-egim], .kategori-karti";
        const yaziKutusu = "input:not([type=checkbox]):not([type=radio]):not([type=submit]), textarea, [contenteditable]";
        document.addEventListener("pointerover", olay => {
            kok.classList.toggle("imlec-buyuk", !!olay.target.closest(tiklanabilir));
            kok.classList.toggle("imlec-yazi", !!olay.target.closest(yaziKutusu));   // yazı kutusunda normal imleç
            const yazili = olay.target.closest("[data-imlec-yazi]");
            if (yazili && etiket.textContent !== yazili.dataset.imlecYazi) {
                etiket.textContent = yazili.dataset.imlecYazi;
                // uzun söz ("Araçları gör") halkaya sığsın: halka, yazının genişliğine göre büyür (en az 2,35 kat)
                kok.style.setProperty("--imlec-olcek", Math.max(2.35, (etiket.textContent.length * 7.4 + 24) / 38).toFixed(2));
            }
            kok.classList.toggle("imlec-etiketli", !!yazili);
        });
        document.addEventListener("pointerdown", () => kok.classList.add("imlec-bas"));
        document.addEventListener("pointerup", () => kok.classList.remove("imlec-bas"));
        document.documentElement.addEventListener("mouseleave", () => kok.classList.remove("imlec-gorunur"));
    }

    // ---- 10) "Gaza basınca" (data-gaz, son çağrı bandı): fare butona yaklaşınca yol ızgarası ve far izleri hızlanır ----
    //      Hız, çalışan CSS animasyonlarının oynatma hızıyla değişir (updatePlaybackRate): sıçrama olmadan yumuşakça hızlanır.
    document.querySelectorAll("[data-gaz]").forEach(bant => {
        const buton = bant.querySelector(".btn");
        if (!buton) return;
        let gaz = false;
        const ayarla = simdi => {
            if (simdi === gaz) return;
            gaz = simdi;
            bant.classList.toggle("gaz", gaz);
            bant.querySelectorAll(".yol-izgara, .isik-izi").forEach(e => e.getAnimations().forEach(a => a.updatePlaybackRate(gaz ? 3.2 : 1)));
        };
        bant.addEventListener("pointermove", olay => {
            const r = buton.getBoundingClientRect();
            ayarla(Math.hypot(olay.clientX - (r.left + r.width / 2), olay.clientY - (r.top + r.height / 2)) < 240);
        });
        bant.addEventListener("pointerleave", () => ayarla(false));
    });

    // ---- 11) Alt bilgideki dev firma adı: harflere bölünür; fareye yakın harf "far vurmuş" gibi parlar (--yakin 0…1) ----
    const devYazi = document.querySelector(".dev-yazi");
    if (devYazi && !devYazi.children.length) {
        const harfler = [...devYazi.textContent].map(h => { const s = document.createElement("span"); s.className = "harf"; s.textContent = h; return s; });
        devYazi.replaceChildren(...harfler);
        devYazi.classList.add("dalgali");
        const alan = devYazi.closest(".alt-bilgi") ?? devYazi;
        let merkezler = [], kare = 0, fx = -1e5;
        const uygula = () => {
            kare = 0;
            const sol = devYazi.getBoundingClientRect().left;
            harfler.forEach((h, i) => h.style.setProperty("--yakin", Math.max(0, 1 - Math.abs(fx - sol - merkezler[i]) / 260).toFixed(2)));
        };
        alan.addEventListener("pointerenter", () => {   // harflerin ortası, yazının sol kenarına göre (bir kez ölçülür)
            const sol = devYazi.getBoundingClientRect().left;
            merkezler = harfler.map(h => { const r = h.getBoundingClientRect(); return r.left - sol + r.width / 2; });
        });
        alan.addEventListener("pointermove", olay => { fx = olay.clientX; if (!kare) kare = requestAnimationFrame(uygula); });
        alan.addEventListener("pointerleave", () => { fx = -1e5; if (!kare) kare = requestAnimationFrame(uygula); });
    }
})();
