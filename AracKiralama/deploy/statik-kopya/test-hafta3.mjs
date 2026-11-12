// hafta3 statik kopyasını gerçek sistemle karşılaştırarak test eder (Chrome, CSP açık)
import puppeteer from "puppeteer-core";
import fs from "node:fs";
import path from "node:path";

const STATIK = "http://localhost:5089/hafta3/";
const GERCEK = "http://localhost:5046";
const KLASOR = "C:/Users/ardce/OneDrive/Desktop/hafta3";
const sonuclar = [];
const kontrol = (ad, kosul, ayrinti = "") => sonuclar.push(`${kosul ? "✓" : "✗"} ${ad}${ayrinti ? " — " + ayrinti : ""}`);

// ---- 0) Bütün sayfalardaki linkler var olan dosyalara mı gidiyor?
const dosyalar = new Set(fs.readdirSync(KLASOR));
let kirik = [];
for (const ad of [...dosyalar].filter(d => d.endsWith(".html"))) {
    const html = fs.readFileSync(path.join(KLASOR, ad), "utf8");
    for (const [, d] of html.matchAll(/(?:href|src|action)="([^"#?]+)/g)) {
        if (/^(https?:|data:|mailto:|tel:)/.test(d) || d === "#") continue;
        if (!fs.existsSync(path.join(KLASOR, d))) kirik.push(`${ad} → ${d}`);
    }
}
kontrol("Kırık link/dosya yok", kirik.length === 0, kirik.slice(0, 5).join(", "));

const tarayici = await puppeteer.launch({ executablePath: "C:/Program Files/Google/Chrome/Application/chrome.exe", headless: true, args: ["--no-first-run"] });
const sayfa = await tarayici.newPage();
await sayfa.setViewport({ width: 1366, height: 900 });
const hatalar = [];
sayfa.on("console", m => { if (m.type() === "error") hatalar.push(`${sayfa.url()} :: ${m.text()}`); });
sayfa.on("pageerror", h => hatalar.push(`${sayfa.url()} :: ${h.message}`));
sayfa.on("requestfailed", i => hatalar.push(`${i.url()} :: ${i.failure()?.errorText}`));
const git = async adres => { await sayfa.goto(adres, { waitUntil: "networkidle0" }); };
const kartIdleri = () => sayfa.$$eval("[data-statik-kartlar] [data-arac]", d => d.map(x => x.dataset.arac));

// ---- 1) Ana sayfa
await git(STATIK);
const ana = await sayfa.content();
kontrol("Ana sayfa açılıyor", ana.includes("arama-kutusu") || ana.includes("Araç Bul"));
kontrol("Footer'da Kurumsal yok", !ana.includes(">Kurumsal<"));
kontrol("Footer'da API linki yok", !ana.includes("swagger"));
kontrol("Ana sayfa yazı tipi yüklendi", await sayfa.evaluate(() => document.fonts.check("16px Inter")));
kontrol("İkonlar yüklendi", await sayfa.evaluate(() => getComputedStyle(document.querySelector(".bi"), "::before").fontFamily.includes("bootstrap-icons")));

// ---- 2) Gerçek sistemle aynı liste mi? (tarih seçmeden)
const ornekler = ["", "KategoriId=1", "KategoriId=3", "KategoriId=5", "MarkaId=2", "VitesTuruId=2", "YakitTuruId=1", "EnAzKoltuk=7",
    "EnDusukFiyat=1000&EnYuksekFiyat=2000", "EnAzYil=2024", "Arama=audi", "Siralama=fiyat-azalan", "Siralama=yeni", "Siralama=km",
    "Siralama=ad", "Siralama=fiyat-artan&Sayfa=2", "Siralama=fiyat-artan&Sayfa=4", "AlisSubeId=2", "KategoriId=2&Siralama=yeni&Sayfa=2", "Arama=zzz"];
for (const q of ornekler) {
    const gercekHtml = await (await fetch(`${GERCEK}/Araclar?${q}`)).text();
    const sutun = gercekHtml.slice(gercekHtml.indexOf('<div class="col-lg-9">'));
    const gercekIdler = [...new Set([...sutun.matchAll(/<article class="arac-karti">\s*<a class="gorsel" href="\/Araclar\/Detay\/(\d+)/g)].map(m => m[1]))];
    const gercekSayi = gercekHtml.match(/<b class="text-body">(\d+)<\/b>/)[1];
    await git(`${STATIK}araclar.html?${q}`);
    const statikIdler = await kartIdleri();
    const statikSayi = await sayfa.$eval("[data-statik-sayi]", e => e.textContent);
    kontrol(`Liste "${q || "(hepsi)"}"`, statikSayi === gercekSayi && statikIdler.join() === gercekIdler.join(), `sayı ${statikSayi}/${gercekSayi}`);
}

// ---- 3) Tıklayarak filtre (otomatik gönderim) ve sayfalama
await git(`${STATIK}araclar.html`);
kontrol("İlk sayfada 12 kart", (await kartIdleri()).length === 12);
kontrol("Sayfalama: Sayfa 1 / 4", (await sayfa.content()).includes("Sayfa 1 / 4"));
await Promise.all([sayfa.waitForNavigation({ waitUntil: "networkidle0" }), sayfa.click('label[for="k3"]')]);
kontrol("Kategoriye tıklayınca liste süzülüyor", sayfa.url().includes("KategoriId=3") && (await sayfa.$eval('input[id="k3"]', e => e.checked)));
await Promise.all([sayfa.waitForNavigation({ waitUntil: "networkidle0" }), sayfa.select('select[name="Siralama"]', "fiyat-azalan")]);
const fiyatlar = await sayfa.$$eval("[data-statik-kartlar] [data-arac]", d => d.map(x => window.ROTA.araclar[x.dataset.arac].fiyat));
kontrol("Sıralama değişince fiyatlar azalan", fiyatlar.every((f, i) => i === 0 || fiyatlar[i - 1] >= f) && sayfa.url().includes("KategoriId=3"));

// ---- 4) Ana sayfadaki arama formu → tarihli liste
await git(STATIK);
await Promise.all([sayfa.waitForNavigation({ waitUntil: "networkidle0" }), sayfa.click(".arama-kutusu button, form[action='araclar.html'] button")]);
const tarihli = await sayfa.content();
kontrol("Arama formu araç listesine gidiyor", sayfa.url().includes("araclar.html?") && sayfa.url().includes("Baslangic="));
kontrol("Tarih aralığı ve gün sayısı başlıkta", /gün<\/b>/.test(tarihli));
kontrol("Kartlarda toplam tutar (≈)", tarihli.includes("≈"));
kontrol("Kart linkleri tarihleri taşıyor", (await sayfa.$eval("[data-statik-kartlar] a.gorsel", a => a.getAttribute("href"))).includes("Baslangic="));
const tarihKutusu = await sayfa.$eval("[data-tarih-baslangic]", e => e.value);
kontrol("Tarih kutuları eskimiyor (yarın veya sonrası)", new Date(tarihKutusu) > new Date());

// ---- 5) Araç detayı: fiyat dökümü gerçek API ile aynı mı?
const yarin = new Date(); yarin.setDate(yarin.getDate() + 1); yarin.setHours(10, 0, 0, 0);
const kutu = t => new Date(t.getTime() - t.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
const senaryolar = [
    { arac: 3, gun: 3, ekstralar: [], alis: 1, iade: 1 },
    { arac: 3, gun: 8, ekstralar: [6, 4], alis: 1, iade: 2 },
    { arac: 20, gun: 31, ekstralar: [3, 5], alis: 2, iade: 2 },
];
for (const s of senaryolar) {
    const bitis = new Date(yarin); bitis.setDate(bitis.getDate() + s.gun);
    const api = await (await fetch(`${GERCEK}/api/fiyat`, { method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ aracId: s.arac, baslangic: kutu(yarin), bitis: kutu(bitis), alisSubeId: s.alis, iadeSubeId: s.iade, ekstraIdleri: s.ekstralar }) })).json();
    await git(`${STATIK}arac-${s.arac}.html?Baslangic=${kutu(yarin)}&Bitis=${kutu(bitis)}&AlisSubeId=${s.alis}&IadeSubeId=${s.iade}`);
    for (const e of s.ekstralar) await sayfa.$eval(`input[name="EkstraIdleri"][value="${e}"]`, k => k.click());
    const toplam = await sayfa.$eval("[data-fiyat-dokumu] li.toplam .rakam", e => e.textContent);
    const satirSayisi = await sayfa.$$eval("[data-fiyat-dokumu] li", l => l.length);
    const apiSatir = 1 + (api.teklif.indirim > 0) + api.teklif.ekstralar.length + (api.teklif.tekYonUcreti > 0) + 1;
    kontrol(`Fiyat araç ${s.arac}, ${s.gun} gün, ekstra [${s.ekstralar}], şube ${s.alis}→${s.iade}`, toplam === api.toplamYazi && satirSayisi === apiSatir, `${toplam} / ${api.toplamYazi}`);
}
await git(`${STATIK}arac-3.html`);
await Promise.all([sayfa.waitForNavigation({ waitUntil: "networkidle0" }), sayfa.click("[data-fiyat-gonder]")]);
kontrol("'Rezervasyon yap' giriş sayfasına götürüyor", sayfa.url().includes("giris.html"));

// ---- 6) Giriş ve kayıt: görünür ama çalışmaz
await git(`${STATIK}giris.html`);
kontrol("Demo hesap butonları yok", !(await sayfa.content()).includes("data-demo"));
await sayfa.click("form[data-statik-hesap] button");
kontrol("Boş girişte form uyarıları çıkıyor", await sayfa.$$eval(".field-validation-error", e => e.length > 0));
await sayfa.type("#Eposta", "musteri@rota.com");
await sayfa.type("#Sifre", "Rota123!");
await sayfa.click("form[data-statik-hesap] button");
kontrol("Girişte 'yakında açılacak' uyarısı", (await sayfa.content()).includes("Üye girişi çok yakında açılacak") && sayfa.url().includes("giris.html"));
await git(`${STATIK}kayit.html`);
for (const [s, d] of [["#Ad", "Deneme"], ["#Soyad", "Kişi"], ["#Eposta", "deneme@ornek.com"], ["#Telefon", "05551112233"], ["#Sifre", "abc12345"], ["#SifreTekrar", "abc12345"]]) await sayfa.type(s, d);
await sayfa.click("#KvkkOnay");
await sayfa.click("form[data-statik-hesap] button");
kontrol("Kayıtta 'yakında açılacak' uyarısı", (await sayfa.content()).includes("Üyelik sistemi çok yakında açılacak"));

// ---- 7) Menü linkleri ve tema
await git(STATIK);
const rezLink = await sayfa.$$eval("a", l => l.filter(a => a.textContent.includes("Rezervasyonlarım")).map(a => a.getAttribute("href")));
kontrol("Rezervasyonlarım → giriş", rezLink.length > 0 && rezLink.every(h => h === "giris.html"));
const once = await sayfa.evaluate(() => document.documentElement.getAttribute("data-bs-theme"));
await sayfa.click("[data-tema-degistir]");
const sonra = await sayfa.evaluate(() => document.documentElement.getAttribute("data-bs-theme"));
kontrol("Tema butonu çalışıyor", once !== sonra);

// ---- 8) Telefonda menü açılıyor mu?
await sayfa.setViewport({ width: 390, height: 844, isMobile: true });
await git(STATIK);
await sayfa.click(".navbar-toggler");
await sayfa.waitForSelector("#menu.show", { timeout: 3000 }).catch(() => null);
kontrol("Telefonda menü açılıyor", await sayfa.$("#menu.show") !== null);

kontrol("Konsolda hata / CSP ihlali yok", hatalar.length === 0, hatalar.slice(0, 5).join(" | "));
await tarayici.close();
console.log(sonuclar.join("\n"));
console.log(`\n${sonuclar.filter(s => s.startsWith("✓")).length}/${sonuclar.length} kontrol geçti`);
