// =====================================================================
// CANLI FİYAT HESAPLAMA (araç detayı ve rezervasyon formu)
// Formdaki tarih / şube / ekstra değiştikçe /api/fiyat adresine sorar ve
// sonucu kalem kalem gösterir. Hesabın kendisi sunucuda yapılır
// (Servisler/FiyatServisi.cs) → fiyat kuralı tek yerde kalır.
//
// HTML tarafı:  <form data-fiyat-formu data-arac-id="5"> ... </form>
//   içinde: name="Baslangic" / "Bitis" / "AlisSubeId" / "IadeSubeId" / "EkstraIdleri"
//           <ul data-fiyat-dokumu>  <div data-fiyat-hatalari>  <button data-fiyat-gonder>
// =====================================================================
(function () {
    const form = document.querySelector("[data-fiyat-formu]");
    if (!form) return;

    const dokum = form.querySelector("[data-fiyat-dokumu]");
    const hatalar = form.querySelector("[data-fiyat-hatalari]");
    const gonderButonu = form.querySelector("[data-fiyat-gonder]");
    const deger = ad => form.querySelector(`[name="${ad}"]`)?.value;
    const para = sayi => new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 }).format(sayi) + " ₺";

    async function hesapla() {
        const istek = {
            aracId: Number(form.dataset.aracId),
            baslangic: deger("Baslangic"),
            bitis: deger("Bitis"),
            alisSubeId: Number(deger("AlisSubeId")),
            iadeSubeId: Number(deger("IadeSubeId")),
            ekstraIdleri: [...form.querySelectorAll('[name="EkstraIdleri"]:checked')].map(k => Number(k.value)),
        };
        if (!istek.baslangic || !istek.bitis) return;

        dokum?.classList.add("yukleniyor");
        const cevap = await fetch("/api/fiyat", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(istek),
        }).then(c => c.json());
        dokum?.classList.remove("yukleniyor");
        goster(cevap);
    }

    function goster(cevap) {
        // Hatalar (ör. "araç bu tarihlerde dolu")
        hatalar.replaceChildren(...cevap.hatalar.map(mesaj => {
            const kutu = document.createElement("div");
            kutu.className = "alert alert-warning py-2 px-3 small mb-2";
            kutu.textContent = mesaj;
            return kutu;
        }));
        // Müsait değilse veya sürücü şartı tutmuyorsa (data-kilitli) buton pasif
        if (gonderButonu) gonderButonu.disabled = !cevap.musait || gonderButonu.dataset.kilitli === "true";
        if (!cevap.teklif || !dokum) return;

        const t = cevap.teklif;
        const satirlar = [[`${t.gunSayisi} gün × ${para(t.gunlukFiyat)}`, para(t.kiraTutari)]];
        if (t.indirim > 0) satirlar.push([`Uzun dönem indirimi (%${Math.round(t.indirimOrani * 100)})`, "− " + para(t.indirim), "indirim"]);
        t.ekstralar.forEach(e => satirlar.push([e.ad, para(e.tutar)]));
        if (t.tekYonUcreti > 0) satirlar.push(["Farklı şubeye iade", para(t.tekYonUcreti)]);
        satirlar.push(["Toplam", cevap.toplamYazi, "toplam"]);

        dokum.replaceChildren(...satirlar.map(([yazi, tutar, sinif]) => {
            const li = document.createElement("li");
            if (sinif) li.className = sinif;
            li.innerHTML = "<span></span><span class='rakam'></span>";
            li.children[0].textContent = yazi;
            li.children[1].textContent = tutar;
            return li;
        }));
        const depozito = form.querySelector("[data-fiyat-depozito]");
        if (depozito) depozito.textContent = cevap.depozitoYazi;
    }

    form.addEventListener("change", hesapla);
    hesapla();
})();
