// =====================================================================
// GENEL DAVRANIŞLAR — her sayfada yüklenir.
// View'larda <script> yazmak yerine HTML'e "data-..." özelliği eklenir,
// davranış buradan otomatik bağlanır. Mevcut özellikler:
//   data-tema-degistir          → açık / koyu tema butonu
//   data-onay="Emin misiniz?"   → tıklamadan önce onay sorar
//   data-otomatik-gonder        → form içindeki seçim değişince formu gönderir
//   data-tarih-cifti            → içindeki [data-tarih-baslangic] / [data-tarih-bitis] kutularını eşler
//   data-bs-toggle="tooltip"    → fareyle üstüne gelince açıklama
//   data-demo="e-posta"         → giriş sayfasında e-postayı doldurur
//   data-gorsel-onizle="#id"    → dosya seçilince resmi önizler
//   data-ikon-onizle="#id"      → ikon adı yazılınca ikonu gösterir
// =====================================================================

// ---- Açık / koyu tema ----
function temaIkonunuGuncelle() {
    const koyu = document.documentElement.getAttribute("data-bs-theme") === "dark";
    document.querySelectorAll("[data-tema-degistir] i").forEach(i => i.className = koyu ? "bi bi-sun" : "bi bi-moon-stars");
}
document.querySelectorAll("[data-tema-degistir]").forEach(buton => buton.addEventListener("click", () => {
    const yeni = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-bs-theme", yeni);
    try { localStorage.setItem("tema", yeni); } catch { }
    temaIkonunuGuncelle();
    document.dispatchEvent(new Event("temadegisti"));   // grafikler renklerini yeniler
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
