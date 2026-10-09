// TEMA: sayfa çizilmeden önce açık / koyu temayı uygular (<head> içinde, defer olmadan yüklenir).
// Öncelik: ziyaretçinin seçimi (localStorage) → admin'in varsayılan teması → cihazın ayarı
(function () {
    let tema = null;
    try { tema = localStorage.getItem("tema"); } catch { /* gizli sekme */ }
    const varsayilan = document.documentElement.dataset.varsayilanTema;   // _Head.cshtml'de admin ayarından yazılır
    if (!tema && varsayilan !== "sistem") tema = varsayilan === "koyu" ? "dark" : "light";
    if (!tema) tema = matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    document.documentElement.setAttribute("data-bs-theme", tema);
})();
