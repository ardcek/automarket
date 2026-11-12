// ============================================================================
// UÇTAN UCA TEST: Kampanyalar ve Müşteri Bilgileri (meslek indirimi senaryosu)
// Uygulama çalışırken (http://localhost:5046) ve veritabanı TAZEYKEN çalıştırın:
//   node AracKiralama/tests/uctan-uca/kampanya.mjs
// Ek paket gerekmez: Node'un kendi fetch'i kullanılır, çerezler elle taşınır.
// Senaryo: "Meslek gruplarına göre indirim yap, filtrede de indirimli araçlar görünsün"
// ============================================================================
const B = process.env.ADRES ?? "http://localhost:5046";
const sonuclar = [];
const kontrol = (ad, kosul, ayrinti = "") => sonuclar.push(`${kosul ? "✓" : "✗"} ${ad}${ayrinti ? " — " + ayrinti : ""}`);

// ---- Küçük yardımcılar: çerezli istek, form gönderme ----
function oturum() {
    const cerezler = new Map();
    const istek = async (adres, secenek = {}) => {
        const cevap = await fetch(B + adres, { redirect: "manual", ...secenek, headers: { ...(secenek.headers ?? {}), cookie: [...cerezler].map(([a, d]) => `${a}=${d}`).join("; ") } });
        for (const c of cevap.headers.getSetCookie()) { const [ad, deger] = c.split(";")[0].split("="); cerezler.set(ad, deger); }
        return cevap;
    };
    const sayfa = async adres => (await istek(adres)).text();
    // Formun bulunduğu sayfayı açar, güvenlik anahtarını alır, alanları gönderir
    const gonder = async (formAdresi, alanlar, hedef = formAdresi) => {
        const html = await sayfa(formAdresi);
        const anahtar = html.match(/name="__RequestVerificationToken" type="hidden" value="([^"]+)"/)?.[1];
        const govde = new URLSearchParams();
        for (const [ad, deger] of Object.entries(alanlar)) for (const d of [deger].flat()) govde.append(ad, d);
        govde.append("__RequestVerificationToken", anahtar);
        return istek(hedef, { method: "POST", body: govde });
    };
    return { istek, sayfa, gonder };
}
const adet = html => Number(html.match(/<b class="text-body">(\d+)<\/b>/)?.[1] ?? -1);   // "N araç bulundu"
const kodla = encodeURIComponent;

// ---------------- 1) Admin: "Meslek" bilgisi ve iki kampanya ----------------
const admin = oturum();
const giris = await admin.gonder("/Hesap/Giris", { Eposta: "admin@rota.com", Sifre: "Rota123!" });
kontrol("Admin girişi", giris.status === 302 && giris.headers.get("location")?.includes("/Yonetim"));

const panel = await admin.sayfa("/Yonetim");
kontrol("Menüde Kampanyalar ve Müşteri Bilgileri", panel.includes("Kampanyalar ve İndirimler") && panel.includes("Müşteri Bilgileri"));

let c = await admin.gonder("/Yonetim/MusteriAlanlari/Ekle", {
    Ad: "Meslek", Tur: "Secim", SeceneklerMetni: "Öğrenci\nÖğretmen\nDoktor\nMühendis\nDiğer",
    KayittaGoster: ["true", "false"], Zorunlu: "false", Aktif: ["true", "false"], Sira: "0",
});
kontrol("Müşteri bilgisi eklendi (Meslek)", c.status === 302);
const alanId = (await admin.sayfa("/Yonetim/MusteriAlanlari")).match(/MusteriAlanlari\/Duzenle\/(\d+)/)?.[1];

const kampanyaFormu = await admin.sayfa("/Yonetim/Kampanyalar/Ekle");
kontrol("Kampanya formunda 'Meslek: Öğretmen' seçeneği", kampanyaFormu.includes(`value="${alanId}|Öğretmen"`));
c = await admin.gonder("/Yonetim/Kampanyalar/Ekle", { Ad: "Öğretmen indirimi", RozetYazisi: "Öğretmenlere %15", IndirimYuzdesi: "15", MusteriKosulu: `${alanId}|Öğretmen`, Aktif: ["true", "false"], Sira: "0" });
kontrol("Kampanya eklendi (Öğretmen %15)", c.status === 302);
c = await admin.gonder("/Yonetim/Kampanyalar/Ekle", { Ad: "Doktor indirimi", IndirimYuzdesi: "20", MusteriKosulu: `${alanId}|Doktor`, Aktif: ["true", "false"], Sira: "1" });
kontrol("Kampanya eklendi (Doktor %20)", c.status === 302);
const liste = await admin.sayfa("/Yonetim/Kampanyalar");
kontrol("Kampanya listesinde koşul özeti", liste.includes("Meslek: Öğretmen") && liste.includes("%15") && liste.includes("%20"));

// Hatalı form: bitiş başlangıçtan önce → kaydedilmemeli
c = await admin.gonder("/Yonetim/Kampanyalar/Ekle", { Ad: "Bozuk", IndirimYuzdesi: "10", BaslangicTarihi: "2026-12-10", BitisTarihi: "2026-12-01", Aktif: ["true", "false"], Sira: "0" });
kontrol("Bitiş < başlangıç reddedilir", c.status === 200 && (await c.text()).includes("Bitiş tarihi başlangıçtan önce olamaz"));

// ---------------- 2) Ziyaretçi: araç listesi ----------------
const ziyaretci = oturum();
const tumu = await ziyaretci.sayfa("/Araclar");
const toplam = adet(tumu);
kontrol("Araç listesinde İndirimler bölümü", tumu.includes("İndirimler") && tumu.includes(`name="Mu[${alanId}]"`) && tumu.includes('name="SadeceIndirimli"'));
kontrol("Meslek seçilmeden kartlarda olası rozet", tumu.includes("kampanya-rozeti olasi") && !tumu.includes("fiyat-indirimli"));

const ogretmen = await ziyaretci.sayfa(`/Araclar?Mu%5B${alanId}%5D=${kodla("Öğretmen")}`);
kontrol("Öğretmen seçilince indirimli fiyat ve rozet", ogretmen.includes("fiyat-indirimli") && ogretmen.includes("Öğretmenlere %15") && ogretmen.includes("<del>"));
kontrol("Seçim sayfalama / detay linklerine taşınıyor", new RegExp(`Mu%5B${alanId}%5D=`).test(ogretmen.match(/href="\/Araclar\/Detay\/[^"]+"/)?.[0] ?? ""));

const indirimli = adet(await ziyaretci.sayfa("/Araclar?SadeceIndirimli=true"));
kontrol("Sadece indirimli (meslek seçilmeden) → bütün araçlar adaydır", indirimli === toplam, `${indirimli}/${toplam}`);
const muhendis = adet(await ziyaretci.sayfa(`/Araclar?SadeceIndirimli=true&Mu%5B${alanId}%5D=${kodla("Mühendis")}`));
kontrol("Mühendise kampanya yok → 0 araç", muhendis === 0, `${muhendis}`);

// ---------------- 3) Canlı fiyat (/api/fiyat) ----------------
const aracId = Number(tumu.match(/\/Araclar\/Detay\/(\d+)/)[1]);
const yarin = new Date(Date.now() + 86400000); yarin.setHours(10, 0, 0, 0);
const tarih = g => { const d = new Date(yarin.getTime() + g * 86400000); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16); };
const fiyat = async mu => (await (await fetch(B + "/api/fiyat", { method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ aracId, baslangic: tarih(0), bitis: tarih(3), alisSubeId: 1, iadeSubeId: 1, ekstraIdleri: [], mu }) })).json());
const f1 = await fiyat({ [alanId]: "Öğretmen" });
kontrol("API: Öğretmen için %15 ve kampanya adı", f1.teklif?.indirimOrani === 0.15 && f1.teklif?.indirimAciklamasi === "Öğretmen indirimi", JSON.stringify(f1.teklif?.indirimAciklamasi));
const f2 = await fiyat({});
kontrol("API: meslek yoksa indirim yok", f2.teklif?.indirim === 0);

const detay = await ziyaretci.sayfa(`/Araclar/Detay/${aracId}?Mu%5B${alanId}%5D=${kodla("Doktor")}`);
kontrol("Detayda 'Meslek' seçimi, Doktor seçili", detay.includes(`name="Mu[${alanId}]"`) && /<option[^>]*value="Doktor"[^>]*selected|<option[^>]*selected[^>]*value="Doktor"/.test(detay));

// ---------------- 4) Ana sayfa, kayıt formu ----------------
kontrol("Ana sayfadaki kartlarda rozet", (await ziyaretci.sayfa("/")).includes("kampanya-rozeti"));
kontrol("Kayıt formunda Meslek sorusu", (await ziyaretci.sayfa("/Hesap/Kayit")).includes(`name="Alan[${alanId}]"`));

// ---------------- 5) Koruma: kampanyada kullanılan bilgi silinemez ----------------
c = await admin.gonder("/Yonetim/MusteriAlanlari", {}, `/Yonetim/MusteriAlanlari/Sil/${alanId}`);
kontrol("Kampanyada kullanılan Meslek bilgisi silinmiyor", (await admin.sayfa("/Yonetim/MusteriAlanlari")).includes(">Meslek<"));

// ---------------- 6) Kampanyayı kapat → rozetler kalkar ----------------
const kampanyaIdleri = [...liste.matchAll(/Kampanyalar\/AcKapat\/(\d+)/g)].map(m => m[1]);
for (const id of kampanyaIdleri) await admin.gonder("/Yonetim/Kampanyalar", {}, `/Yonetim/Kampanyalar/AcKapat/${id}`);
const kapali = await ziyaretci.sayfa("/Araclar");
kontrol("Kampanyalar kapanınca İndirimler bölümü ve rozetler gizlenir", !kapali.includes("kampanya-rozeti") && !kapali.includes('name="SadeceIndirimli"'));
for (const id of kampanyaIdleri) await admin.gonder("/Yonetim/Kampanyalar", {}, `/Yonetim/Kampanyalar/AcKapat/${id}`);   // geri aç

console.log(sonuclar.join("\n"));
console.log(`\n${sonuclar.filter(s => s.startsWith("✓")).length}/${sonuclar.length} kontrol geçti`);
