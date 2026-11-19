// TEMA: sayfa çizilmeden önce açık / koyu temayı uygular (<head> içinde, defer olmadan yüklenir).
// Öncelik: ziyaretçinin seçimi (localStorage) → admin'in varsayılan teması → cihazın ayarı
(function () {
    const kok = document.documentElement;
    let tema = null;
    try { tema = localStorage.getItem("tema"); } catch { /* gizli sekme */ }
    const varsayilan = kok.dataset.varsayilanTema;   // _Head.cshtml'de admin ayarından yazılır
    if (!tema && varsayilan !== "sistem") tema = varsayilan === "koyu" ? "dark" : "light";
    if (!tema) tema = matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    kok.setAttribute("data-bs-theme", tema);

    // Hareketli efektler (wwwroot/css/vitrin.css + js/efektler.js):
    //   "js"        → JavaScript çalışıyor (örn. üst menü en üstte saydam olabilir)
    //   "animasyon" → cihazda "hareketi azalt" kapalı; kaydırınca belirme, kayan şerit vb. açık
    kok.classList.add("js");
    if (!matchMedia("(prefers-reduced-motion: reduce)").matches) kok.classList.add("animasyon");
    // Site içinden (bir önceki sayfa da bu siteyse) gelindiyse geçiş efekti olacak: ilk çizim, ana içerik (#icerik-sonu)
    // okunana kadar bekletilir ki uçan araç fotoğrafı yeni sayfada yerini bulsun. Dışarıdan ilk girişte beklenmez (sayfa hızlı açılır).
    try {
        if (kok.hasAttribute("data-vitrin") && "onpagereveal" in window && document.referrer && new URL(document.referrer).origin === location.origin) {
            const bekle = document.createElement("link");
            bekle.rel = "expect"; bekle.href = "#icerik-sonu"; bekle.setAttribute("blocking", "render");
            document.head.appendChild(bekle);
        }
    } catch { /* eski tarayıcı: geçiş efekti zaten yok */ }
    // Sayfaya geçiş efektiyle gelindiyse (vitrin.css → @view-transition) ekrandakiler beklemeden görünsün (efektler.js)
    // Yönetim panelinde geçiş efekti yok; siteden panele giderken ve form gönderilince (POST) geçiş baştan atlanır,
    // yoksa tarayıcı geçişi yarıda bırakıp konsola zararsız ama gereksiz bir hata yazar.
    const sessizce = gecis => { if (gecis) [gecis.ready, gecis.finished, gecis.updateCallbackDone].forEach(p => p?.catch(() => { })); };
    let formGonderildi = false;
    addEventListener("submit", olay => { if (olay.target.method === "post") formGonderildi = true; });
    addEventListener("pageswap", olay => {
        sessizce(olay.viewTransition);
        let hedef = "";
        try { hedef = new URL(olay.activation?.entry?.url ?? "", location.href).pathname.toLowerCase(); } catch { }
        if (formGonderildi || hedef.startsWith("/yonetim")) olay.viewTransition?.skipTransition();
    });
    addEventListener("pagereveal", olay => {
        sessizce(olay.viewTransition);
        if (olay.viewTransition) kok.classList.add("gecisle-geldi");
    });
    // Güvenlik ağı: efektler.js yüklenemezse gizli bekleyen bölümler sonsuza dek gizli kalmasın
    addEventListener("load", () => {
        if (document.body.classList.contains("vitrin") && !window.efektlerHazir) kok.classList.remove("animasyon", "js");
    });
})();
