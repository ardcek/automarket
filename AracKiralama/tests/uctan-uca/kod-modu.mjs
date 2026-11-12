// ============================================================================
// UÇTAN UCA TEST: Yapay Zekâ KOD MODU (gerçek modelle, BASLAT.bat ile açılmış uygulamada)
//   node AracKiralama/tests/uctan-uca/kod-modu.mjs ["istek"]
// Adımlar: dosya bul → değişiklik önerisi → geçici kopyada derle → uygula → yeniden başlat
//          → sitede kontrol → geri al → yeniden başlat → eski haline döndü mü?
// DİKKAT: Gerçek kod dosyalarını değiştirir ve sonunda geri alır. git ile çalışırken kullanın.
// ============================================================================
//   Başka istek için: node kod-modu.mjs "istek" "/kontrol/edilecek/sayfa" "aranacak düzenli ifade"
const B = process.env.ADRES ?? "http://localhost:5046";
const istekMetni = process.argv[2] ?? "Araç kartında aracın kilometresi de görünsün (örn. 45.000 km)";
const kontrolSayfasi = process.argv[3] ?? "/Araclar";
const desen = new RegExp(process.argv[4] ?? "\\d[\\d.]* km");
const cerezler = new Map();
const istek = async (adres, s = {}) => {
    const c = await fetch(B + adres, { redirect: "manual", ...s, headers: { ...(s.headers ?? {}), cookie: [...cerezler].map(([a, d]) => `${a}=${d}`).join("; ") } });
    for (const x of c.headers.getSetCookie()) { const [a, d] = x.split(";")[0].split("="); cerezler.set(a, d); }
    return c;
};
const anahtarAl = html => html.match(/name="__RequestVerificationToken" type="hidden" value="([^"]+)"/)[1];
const bekle = ms => new Promise(r => setTimeout(r, ms));
let anahtar;
async function girisYap() {
    cerezler.clear();
    let html = await (await istek("/Hesap/Giris")).text();
    await istek("/Hesap/Giris", { method: "POST", body: new URLSearchParams({ Eposta: "admin@rota.com", Sifre: "Rota123!", __RequestVerificationToken: anahtarAl(html) }) });
    html = await (await istek("/Yonetim/YapayZeka")).text();
    anahtar = anahtarAl(html);
}
const post = async (adres, govde) => (await istek(adres, { method: "POST", headers: { "Content-Type": "application/json", RequestVerificationToken: anahtar }, body: JSON.stringify(govde) })).json();
async function acilmasiniBekle() {
    await bekle(4000);
    for (let i = 0; i < 150; i++) { try { if ((await fetch(B + "/health")).ok) return true; } catch { } await bekle(2000); }
    return false;
}
async function motorHazir() {
    for (let i = 0; i < 300; i++) { const d = await (await istek("/Yonetim/YapayZeka/Durum")).json(); if (d.durum === "Hazir") return d; if (d.durum === "Hata") throw new Error(d.mesaj); await bekle(1000); }
}

await girisYap();
const durum = await motorHazir();
console.log(`Motor: ${durum.model} · ${durum.motor}\nİstek: ${istekMetni}\n`);

const secim = await post("/Yonetim/YapayZeka/KodDosyaSec", { istek: istekMetni });
if (secim.hata) { console.log("✗ Dosya seçimi:", secim.hata); process.exit(1); }
console.log(`1) Dosyalar (${secim.sure} sn): ${secim.dosyalar.join(", ")}\n   ${secim.anladigim}`);

let oneri = await post("/Yonetim/YapayZeka/KodOner", { istek: istekMetni, dosyalar: secim.dosyalar });
if (oneri.hata) { console.log("✗ Öneri:", oneri.hata); process.exit(1); }
console.log(`2) Öneri (${oneri.sure} sn): ${oneri.anladigim}`);
for (const d of oneri.degisiklikler) console.log(`   ${d.dosya}${d.hata ? "  ⚠ " + d.hata : ""}\n   - ${d.eski.replaceAll("\n", "\n   - ")}\n   + ${d.yeni.replaceAll("\n", "\n   + ")}`);

let deneme = await post("/Yonetim/YapayZeka/KodDene", { degisiklikler: oneri.degisiklikler });
for (let tur = 1; !deneme.basarili && tur <= 2; tur++) {
    console.log(`3) Deneme ${tur} başarısız: ${deneme.hatalar.join(" | ")}\n   → yapay zekâ düzeltiyor`);
    oneri = await post("/Yonetim/YapayZeka/KodOner", { istek: istekMetni, dosyalar: secim.dosyalar, degisiklikler: deneme.degisiklikler, oncekiHata: deneme.hatalar.join("\n") });
    if (oneri.hata) { console.log("✗", oneri.hata); process.exit(1); }
    deneme = await post("/Yonetim/YapayZeka/KodDene", { degisiklikler: oneri.degisiklikler });
}
console.log(`3) Deneme derlemesi: ${deneme.basarili ? "✓ başarılı" : "✗ " + deneme.hatalar.join(" | ")} (${deneme.sure} sn)`);
if (!deneme.basarili) process.exit(1);

const uygula = await post("/Yonetim/YapayZeka/KodUygula", { istek: istekMetni, degisiklikler: oneri.degisiklikler, sure: oneri.sure });
console.log(`4) Uygulandı: ${uygula.hata ?? uygula.ozet} · yeniden başlatılabilir: ${uygula.yenidenBaslatilabilir}`);
if (uygula.hata) process.exit(1);

let bas = Date.now();
await post("/Yonetim/YapayZeka/YenidenBaslat", {});
console.log(`5) Yeniden başlatma: ${await acilmasiniBekle() ? "✓" : "✗"} (${Math.round((Date.now() - bas) / 1000)} sn)`);
const liste = await (await fetch(B + kontrolSayfasi)).text();
console.log(`6) ${kontrolSayfasi} sayfasında ${desen} görünüyor mu: ${desen.test(liste) ? "✓ evet" : "✗ hayır"}`);

await girisYap();
const html = await (await istek("/Yonetim/YapayZeka")).text();
const geri = await istek(`/Yonetim/YapayZeka/GeriAl/${uygula.kayitId}`, { method: "POST", body: new URLSearchParams({ __RequestVerificationToken: anahtarAl(html) }) });
console.log(`7) Geri alındı: ${geri.status === 302 ? "✓" : "✗ " + geri.status}`);
bas = Date.now();
await post("/Yonetim/YapayZeka/YenidenBaslat", {});
console.log(`8) Yeniden başlatma: ${await acilmasiniBekle() ? "✓" : "✗"} (${Math.round((Date.now() - bas) / 1000)} sn)`);
const sonra = await (await fetch(B + kontrolSayfasi)).text();
console.log(`9) Eski haline döndü: ${!desen.test(sonra) ? "✓" : "✗"}`);
