// ============================================================================
// UÇTAN UCA TEST: Yapay Zekâ Asistanı GERÇEK modelle (panel modu)
// Uygulama ve yapay zekâ motoru çalışırken:
//   node AracKiralama/tests/uctan-uca/yapay-zeka.mjs            → 10 örnek isteği sorar, kartları ve süreleri yazar
//   node AracKiralama/tests/uctan-uca/yapay-zeka.mjs --uygula   → meslek isteğini ayrıca uygular, sitede kontrol eder, geri alır
// Ek paket gerekmez (Node'un fetch'i). Modelin cevabı her seferinde biraz farklı olabilir;
// "✓" = kartlar hatasız ve beklenen işlem türleri var.
// ============================================================================
const B = process.env.ADRES ?? "http://localhost:5046";
const uygula = process.argv.includes("--uygula");
const cerezler = new Map();
const istek = async (adres, s = {}) => {
    const c = await fetch(B + adres, { redirect: "manual", ...s, headers: { ...(s.headers ?? {}), cookie: [...cerezler].map(([a, d]) => `${a}=${d}`).join("; ") } });
    for (const x of c.headers.getSetCookie()) { const [a, d] = x.split(";")[0].split("="); cerezler.set(a, d); }
    return c;
};
const anahtarAl = html => html.match(/name="__RequestVerificationToken" type="hidden" value="([^"]+)"/)[1];

// ---- Giriş ----
let html = await (await istek("/Hesap/Giris")).text();
await istek("/Hesap/Giris", { method: "POST", body: new URLSearchParams({ Eposta: "admin@rota.com", Sifre: "Rota123!", __RequestVerificationToken: anahtarAl(html) }) });
html = await (await istek("/Yonetim/YapayZeka")).text();   // sayfa açılınca motor da açılır
const anahtar = anahtarAl(html);
const post = async (adres, govde) => {
    const c = await istek(adres, { method: "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: anahtar }, body: JSON.stringify(govde) });
    return c.json();
};

// ---- Motor hazır olana kadar bekle ----
let durum;
for (let i = 0; i < 300; i++) {
    durum = await (await istek("/Yonetim/YapayZeka/Durum")).json();
    if (durum.durum === "Hazir" || durum.durum === "Hata" || durum.durum === "Yok") break;
    await new Promise(r => setTimeout(r, 1000));
}
console.log(`Motor: ${durum.durum} · ${durum.model} · ${durum.motor}${durum.sonCikti ? "\n" + durum.sonCikti : ""}\n`);
if (durum.durum !== "Hazir") process.exit(1);

// ---- Örnek istekler: [istek, olması gereken işlem türleri, OLMAMASI gereken türler, en az kaç "metin_degistir"] ----
// (Meslek isteğinde "ayar_degistir" beklenmez: "İndirimli araçlar" filtresi zaten açıksa model eklemez.)
const ornekler = [
    ["Meslek gruplarına göre indirim yap, filtrede de indirimli araçlar görünsün", ["musteri_bilgisi_ekle", "kampanya_ekle"], []],
    ["Araçlara klima bilgisi ekle, bütün araçlarda olsun, filtrede seçilebilsin", ["arac_ozelligi_ekle", "arac_ozellik_degeri_ver"], []],
    ["SUV kategorisindeki araçların fiyatını %10 artır", ["arac_fiyati_degistir"], []],
    ["SUV'larda 7 gün ve üzeri kiralamalara %12 indirim yap", ["kampanya_ekle"], ["musteri_bilgisi_ekle"]],
    ["Ana sayfadaki büyük başlık \"Yolculuk burada başlar\" olsun", ["ana_baslik_degistir"], []],
    ["Butonların rengi turuncu olsun", ["ayar_degistir"], []],
    ["Siteye Sıkça Sorulan Sorular sayfası ekle, üst menüde görünsün", ["sayfa_ekle"], ["metin_degistir"]],
    ["Kar zinciri ek hizmetinin günlük fiyatı 150 lira olsun", ["ekstra_guncelle"], ["ekstra_ekle"]],
    ["Çocuk koltuğu ek hizmeti ekle, günlüğü 100 lira", ["ekstra_ekle"], []],
    ["Ekonomi kategorisini öğrencilere %20 indirimli yap", ["kampanya_ekle"], []],
    ["Kaç tane kampanya var?", ["cevap_ver"], []],
];

const sonuclar = [];
let toplamSure = 0;
for (const [metin, beklenen, yasak, enAzMetin = 0] of ornekler) {
    const cevap = await post("/Yonetim/YapayZeka/Sor", { istek: metin });
    if (cevap.hata) { sonuclar.push(`✗ ${metin}\n    HATA: ${cevap.hata}`); console.log(sonuclar.at(-1)); continue; }
    toplamSure += cevap.sure;
    const turler = cevap.kartlar.map(k => k.tur);
    const hatalar = cevap.kartlar.flatMap(k => k.hatalar.map(h => `${k.tur}: ${h}`));
    const tamam = hatalar.length === 0 && beklenen.every(t => turler.includes(t)) && !yasak.some(t => turler.includes(t))
        && turler.filter(t => t === "metin_degistir").length >= enAzMetin;
    sonuclar.push(`${tamam ? "✓" : "✗"} ${metin}  (${cevap.sure} sn, ${cevap.tokenHizi} tok/sn${cevap.duzeltildi ? ", düzeltildi" : ""})\n`
        + `    Anladığım: ${cevap.anladigim}\n`
        + cevap.kartlar.map(k => `    - ${k.baslik}: ${k.ozet}${k.hatalar.length ? "  ⚠ " + k.hatalar.join(" | ") : ""}`).join("\n"));
    console.log(sonuclar.at(-1));
}
const basarili = sonuclar.filter(s => s.startsWith("✓")).length;
console.log(`\n${basarili}/${ornekler.length} istek beklendiği gibi · ortalama ${(toplamSure / ornekler.length).toFixed(1)} sn`);

// ---- --uygula: meslek isteğini uygula, sitede kontrol et, geri al ----
if (uygula) {
    const cevap = await post("/Yonetim/YapayZeka/Sor", { istek: ornekler[0][0] });
    const islemler = cevap.kartlar.filter(k => k.uygulanabilir && k.hatalar.length === 0).map(k => ({ tur: k.tur, ...k.parametreler }));
    const sonuc = await post("/Yonetim/YapayZeka/Uygula", { istek: ornekler[0][0], anladigim: cevap.anladigim, islemler, sure: cevap.sure });
    console.log("\nUygulandı:", sonuc.hata ?? sonuc.ozet);
    const liste = await (await fetch(B + "/Araclar")).text();
    console.log("Araç listesinde İndirimler bölümü:", liste.includes('name="SadeceIndirimli"'), "· rozet:", liste.includes("kampanya-rozeti"));

    // Var olan kampanyayı güncelleme: "Öğretmen indirimini %20 yap"
    const guncelle = await post("/Yonetim/YapayZeka/Sor", { istek: "Öğretmen indirimini %20 yap" });
    const kart = guncelle.kartlar?.[0];
    console.log("Güncelleme isteği:", kart ? `${kart.tur} → ${kart.ozet} ${kart.hatalar.join(" ")}` : guncelle.hata);
    let ikinci = null;
    if (kart?.tur === "kampanya_guncelle" && kart.hatalar.length === 0)
        ikinci = await post("/Yonetim/YapayZeka/Uygula", { istek: "Öğretmen indirimini %20 yap", islemler: [{ tur: kart.tur, ...kart.parametreler }] });

    // Geri al: önce son kayıt, sonra ilki
    for (const id of [ikinci?.kayitId, sonuc.kayitId].filter(Boolean)) {
        html = await (await istek(`/Yonetim/YapayZeka`)).text();
        await istek(`/Yonetim/YapayZeka/GeriAl/${id}`, { method: "POST", body: new URLSearchParams({ __RequestVerificationToken: anahtarAl(html) }) });
    }
    const sonra = await (await fetch(B + "/Araclar")).text();
    console.log("Geri alındı · İndirimler bölümü kalktı:", !sonra.includes("kampanya-rozeti"));
}
