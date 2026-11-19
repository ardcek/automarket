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

    if (!fareVar) return;

    // ---- 6) Koyu bölümlerde fareyle derinlik: her katman kendi derinliğinde kayar ----
    document.querySelectorAll("[data-sahne]").forEach(sahne => {
        const katmanlar = [...sahne.querySelectorAll("[data-derinlik]")].map(e => [e, Number(e.dataset.derinlik) || 0]);
        const donenler = [...sahne.querySelectorAll("[data-dondur]")].map(e => [e, Number(e.dataset.dondur) || 6]);
        if (!katmanlar.length && !donenler.length) return;
        let kare = 0, x = 0, y = 0;
        const uygula = () => {
            kare = 0;
            for (const [e, d] of katmanlar) e.style.translate = `${(x * d).toFixed(1)}px ${(y * d).toFixed(1)}px`;
            for (const [e, a] of donenler) e.style.transform = `rotateY(${(x * -a).toFixed(2)}deg) rotateX(${(y * a * .75).toFixed(2)}deg)`;
        };
        sahne.addEventListener("pointermove", olay => {
            const r = sahne.getBoundingClientRect();
            x = (olay.clientX - r.left) / r.width * 2 - 1;   // -1 (sol) … 1 (sağ)
            y = (olay.clientY - r.top) / r.height * 2 - 1;   // -1 (üst) … 1 (alt)
            if (!kare) kare = requestAnimationFrame(uygula);
        });
        sahne.addEventListener("pointerleave", () => { x = 0; y = 0; if (!kare) kare = requestAnimationFrame(uygula); });
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
})();
