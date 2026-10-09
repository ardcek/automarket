// =====================================================================
// 3 ADIMLI REZERVASYON FORMU (Views/Rezervasyon/Yeni.cshtml)
// Aynı formdaki [data-adim="1|2|3"] bölümlerini sırayla gösterir.
// [data-adima-git="2"] butonları adım değiştirir.
// =====================================================================
(function () {
    const form = document.getElementById("rezervasyon-formu");
    if (!form) return;

    function adimaGit(numara) {
        form.querySelectorAll("[data-adim]").forEach(p => p.classList.toggle("aktif", Number(p.dataset.adim) === numara));
        document.querySelectorAll("[data-adim-gostergesi]").forEach(g => {
            const n = Number(g.dataset.adimGostergesi);
            g.classList.toggle("aktif", n === numara);
            g.classList.toggle("bitti", n < numara);
        });
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    form.querySelectorAll("[data-adima-git]").forEach(b => b.addEventListener("click", () => adimaGit(Number(b.dataset.adimaGit))));
    adimaGit(Number(form.dataset.ilkAdim || 1));   // hata varsa doğrudan 3. adım açılır
})();
