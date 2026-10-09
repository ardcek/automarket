// =====================================================================
// YÖNETİM PANELİ GRAFİKLERİ (Chart.js kütüphanesi)
// Veriler sayfadaki <script type="application/json" id="grafik-verisi"> bloğundan okunur
// (bu blok çalıştırılmaz, sadece veri taşır → güvenlik politikasına takılmaz).
//   Gösterge paneli: { aylikGelir: [...], kategoriler: [...] }   Raporlar: { subeler: [...] }
// Her nokta: { Etiket: "Eki 26", Deger: 125000 }
// =====================================================================
(function () {
    const veriBlogu = document.getElementById("grafik-verisi");
    if (!veriBlogu || typeof Chart === "undefined") return;
    const veri = JSON.parse(veriBlogu.textContent);

    const renk = ad => getComputedStyle(document.documentElement).getPropertyValue(ad).trim();
    const para = v => new Intl.NumberFormat("tr-TR").format(v) + " ₺";
    let grafikler = [];

    function ciz() {
        grafikler.forEach(g => g.destroy());
        grafikler = [];
        const ana = renk("--ana"), cizgi = renk("--cizgi");
        Chart.defaults.font.family = "Inter, sans-serif";
        Chart.defaults.color = renk("--soluk");

        // Çubuk grafik oluşturan küçük yardımcı (yatay = true → çubuklar yan yana)
        const cubukGrafik = (kanvasId, noktalar, yatay) => {
            const kanvas = document.getElementById(kanvasId);
            if (!kanvas || !noktalar) return;
            grafikler.push(new Chart(kanvas, {
                type: "bar",
                data: { labels: noktalar.map(n => n.Etiket), datasets: [{ data: noktalar.map(n => n.Deger), backgroundColor: ana, borderRadius: 6, maxBarThickness: 36 }] },
                options: {
                    indexAxis: yatay ? "y" : "x", maintainAspectRatio: false,
                    plugins: { legend: { display: false }, tooltip: { callbacks: { label: c => para(c.raw) } } },
                    scales: { x: { grid: { color: yatay ? cizgi : "transparent" } }, y: { grid: { color: yatay ? "transparent" : cizgi }, ticks: { callback: v => yatay ? v : v / 1000 + "K" } } },
                },
            }));
        };

        cubukGrafik("aylik-gelir-grafigi", veri.aylikGelir, false);
        cubukGrafik("sube-grafigi", veri.subeler, true);

        const kategoriKanvasi = document.getElementById("kategori-grafigi");
        if (kategoriKanvasi && veri.kategoriler) {
            grafikler.push(new Chart(kategoriKanvasi, {
                type: "doughnut",
                data: { labels: veri.kategoriler.map(n => n.Etiket), datasets: [{ data: veri.kategoriler.map(n => n.Deger), backgroundColor: [ana, "#0891b2", "#f59e0b", "#8b5cf6", "#94a3b8", "#16a34a"], borderWidth: 0 }] },
                options: { maintainAspectRatio: false, cutout: "68%", plugins: { legend: { position: "bottom" } } },
            }));
        }
    }

    ciz();
    document.addEventListener("temadegisti", ciz);
})();
