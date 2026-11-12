// Kolay panel testleri: Site Düzenleyici (canlı önizleme, kaydet, vazgeç) + Filtreler ve Özellikler
import puppeteer from "puppeteer-core";
const B = "http://localhost:5046";
const sonuclar = [];
const kontrol = (ad, kosul, ayrinti = "") => sonuclar.push(`${kosul ? "✓" : "✗"} ${ad}${ayrinti ? " — " + ayrinti : ""}`);
const bekle = ms => new Promise(r => setTimeout(r, ms));

const t = await puppeteer.launch({ executablePath: "C:/Program Files/Google/Chrome/Application/chrome.exe", headless: true });
const p = await t.newPage();
await p.setViewport({ width: 1500, height: 950 });
await p.emulateMediaFeatures([{ name: "prefers-color-scheme", value: "light" }]);
const hatalar = [];
p.on("pageerror", h => hatalar.push(`${p.url()} :: ${h.message}`));
p.on("console", m => { if (m.type() === "error") hatalar.push(`${p.url()} :: ${m.text()}`); });
p.on("dialog", d => d.accept());   // "emin misiniz?" sorularına evet
const git = async adres => p.goto(B + adres, { waitUntil: "networkidle0" });

// ---- Admin girişi ----
await git("/Hesap/Giris");
await p.type("#Eposta", "admin@rota.com"); await p.type("#Sifre", "Rota123!");
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.click("form button.btn-primary")]);
kontrol("Admin girişi panele götürüyor", p.url().includes("/Yonetim"));
const panel = await p.content();
kontrol("Ana ekranda 'Ne yapmak istiyorsun?' kartları", panel.includes("Ne yapmak istiyorsun?") && (await p.$$(".kolay-kart")).length === 8);
kontrol("Menüde Kolay bölümü ve katlı Gelişmiş", panel.includes("Site Düzenleyici") && panel.includes("Filtreler ve Özellikler") && (await p.$("#gelismisMenu:not(.show)")) !== null);

// ---- Site Düzenleyici: ana sayfa ----
await git("/Yonetim/Duzenleyici");
const cerceve = async () => (await p.$("#onizleme-cercevesi")).contentFrame();
let c = await cerceve();
await c.waitForSelector("h1");
const ilkBaslik = await c.$eval("h1", e => e.textContent.trim());
kontrol("Önizleme yüklendi", ilkBaslik.includes("Yolculuğun nereye olursa olsun"), ilkBaslik.slice(0, 40));
const kutuSayisi = await p.$$eval("#duzenleyici-formu [data-metin], #duzenleyici-formu [data-ayar]", k => k.length);
kontrol("Ana sayfa sekmesinde kutular", kutuSayisi > 30, `${kutuSayisi} kutu`);

// 1) Yazınca önizleme ANINDA değişsin (yeniden yükleme olmadan)
const yuklemeSayisi = await p.evaluate(() => { window.__yukleme = 0; document.getElementById("onizleme-cercevesi").addEventListener("load", () => window.__yukleme++); return 0; });
const kutu = '[data-metin="anasayfa.baslik"]';
await p.click(kutu, { clickCount: 3 });
await p.type(kutu, "Yola çıkmaya hazır mısın,");
await bekle(150);
c = await cerceve();
const yeniBaslik = await c.$eval("h1", e => e.textContent.trim());
kontrol("Yazdıkça önizleme anında değişiyor", yeniBaslik.startsWith("Yola çıkmaya hazır mısın,"), yeniBaslik.slice(0, 40));
await bekle(1200);
kontrol("Yazı değişikliğinde önizleme yeniden yüklenmedi", (await p.evaluate(() => window.__yukleme)) === 0);
kontrol("'değişti' işareti ve durum satırı", (await p.$eval(kutu, k => !k.closest(".duz-alan").querySelector(".duz-degisti").hidden)) && (await p.$eval("[data-duz-durum]", e => e.textContent)).includes("1 kaydedilmemiş"));

// 2) Gerçek site kaydetmeden değişmemeli (başka bir oturumdan bak)
const disaridan = await (await fetch(B + "/")).text();
kontrol("Kaydetmeden gerçek site değişmiyor", disaridan.includes("Yolculuğun nereye olursa olsun") && !disaridan.includes("Yola çıkmaya hazır"));

// 3) Önizlemede bir yazıya tıklayınca soldaki kutusu açılsın
c = await cerceve();
await c.evaluate(() => [...document.querySelectorAll("h2")].find(h => h.textContent.includes("Her bütçeye uygun"))?.click());
await bekle(300);
const odak = await p.evaluate(() => document.activeElement?.dataset?.metin);
kontrol("Önizlemede yazıya tıklayınca kutusu açılıyor", odak === "anasayfa.onecikan-baslik", odak);

// 4) Arama kutusu
await p.type("[data-duz-ara]", "buton");
const gorunen = await p.$$eval(".duz-alan", a => a.filter(x => !x.hidden).length);
kontrol("Arama kutuları süzüyor", gorunen > 0 && gorunen < 10, `${gorunen} kutu`);
await p.$eval("[data-duz-ara]", e => { e.value = ""; e.dispatchEvent(new Event("input", { bubbles: true })); });

// 5) Kaydet → gerçek sitede görünsün
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.click(".duz-alt-cubuk .btn-primary")]);
const kayitli = await (await fetch(B + "/")).text();
kontrol("Kaydet → gerçek site değişti", kayitli.includes("Yola çıkmaya hazır mısın,"));
kontrol("Kaydettikten sonra başarı mesajı", (await p.content()).includes("Kaydedildi"));

// 6) Ayar: renk değişince önizleme yeniden yüklenip yeni rengi göstersin
await git("/Yonetim/Duzenleyici?sayfa=gorunum");
c = await cerceve(); await c.waitForSelector("h1");
await p.$eval('[data-ayar="AnaRenk"]', k => { k.value = "#e8590c"; k.dispatchEvent(new Event("input", { bubbles: true })); });
await p.waitForFunction(() => {
    try {
        const d = document.getElementById("onizleme-cercevesi").contentDocument;
        return d.defaultView.getComputedStyle(d.documentElement).getPropertyValue("--ana").trim() === "#e8590c";
    } catch { return false; }   // önizleme yeniden yüklenirken belge bir anlığına boştur
}, { timeout: 8000 }).then(() => kontrol("Renk değişince önizleme yeni renkte", true)).catch(() => kontrol("Renk değişince önizleme yeni renkte", false));
const disRenk = await (await fetch(B + "/")).text();
kontrol("Renk kaydedilmeden gerçek site eski renkte", disRenk.includes("--ana: #2747d6"));

// 7) Vazgeç → taslak silinsin, önizleme eski renge dönsün
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.click(".duz-alt-cubuk form .btn-sade")]);
c = await cerceve(); await c.waitForSelector("h1");
const renkSonra = await c.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue("--ana").trim());
kontrol("Vazgeç → önizleme eski renkte", renkSonra === "#2747d6", renkSonra);

// 8) Sunum sekmesi: kurumsal sayfa aç → kaydet → /Sayfa/hakkimizda açılsın, sonra geri kapat
await git("/Yonetim/Duzenleyici?sayfa=sunum");
const hakkimizdaId = await p.$$eval("[data-sayfa]", k => k.find(x => x.closest(".duz-alan").textContent.includes("Hakkımızda"))?.dataset.sayfa);
await p.click(`[data-sayfa="${hakkimizdaId}"]`);
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.click(".duz-alt-cubuk .btn-primary")]);
kontrol("Kurumsal sayfa panelden açılıyor", (await fetch(B + "/Sayfa/hakkimizda")).status === 200);
await p.click(`[data-sayfa="${hakkimizdaId}"]`);
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.click(".duz-alt-cubuk .btn-primary")]);
kontrol("Kurumsal sayfa panelden kapanıyor", (await fetch(B + "/Sayfa/hakkimizda")).status === 404);

// ---- Filtreler ve Özellikler ----
// 9) "Bagaj hacmi" (Sayı, lt, kartta göster)
await git("/Yonetim/Ozellikler/Ekle");
await p.type("#Ad", "Bagaj hacmi");
await p.select("#Tur", "1");   // Sayı (enum sırası: Secim=0, Sayi=1, EvetHayir=2, Yazi=3)
await p.type("#Birim", "lt");
await p.$eval("#Ikon", k => { k.value = "bi-suitcase"; });
await p.click("#KarttaGoster");
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form .btn-primary.btn-lg", b => b.click())]);
kontrol("Özellik eklenince değer tablosu açılıyor", p.url().includes("/Yonetim/Ozellikler/Degerler/"));
const bagajId = p.url().split("/").pop();
// Her araca 300 + (sıra × 20) lt yaz
const satirlar = await p.$$eval('input[name^="deger["]', k => k.map(x => x.name));
for (let i = 0; i < satirlar.length; i++) await p.$eval(`input[name="${satirlar[i]}"]`, (k, v) => { k.value = v; }, String(300 + i * 20));
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form .btn-primary.btn-lg", b => b.click())]);
kontrol("Değerler kaydedildi", (await p.content()).includes(`${satirlar.length} aracın`));

// 10) "Klima" (Var / Yok), ilk 10 araçta var
await git("/Yonetim/Ozellikler/Ekle");
await p.type("#Ad", "Klima"); await p.select("#Tur", "2");
await p.$eval("#Ikon", k => { k.value = "bi-snow"; });
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form .btn-primary.btn-lg", b => b.click())]);
const klimaId = p.url().split("/").pop();
const klimaSatirlari = await p.$$eval('select[name^="deger["]', k => k.map(x => x.name));
for (const ad of klimaSatirlari.slice(0, 10)) await p.select(`select[name="${ad}"]`, "true");
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form .btn-primary.btn-lg", b => b.click())]);

// 11) "Kasa tipi" (Seçim listesi)
await git("/Yonetim/Ozellikler/Ekle");
await p.type("#Ad", "Kasa tipi"); await p.select("#Tur", "0");
await p.type("#SeceneklerMetni", "Sedan\nHatchback\nSUV");
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form .btn-primary.btn-lg", b => b.click())]);
const kasaId = p.url().split("/").pop();
const kasaSatirlari = await p.$$eval('select[name^="deger["]', k => k.map(x => x.name));
for (const [i, ad] of kasaSatirlari.entries()) await p.select(`select[name="${ad}"]`, ["Sedan", "Hatchback", "SUV"][i % 3]);
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form .btn-primary.btn-lg", b => b.click())]);

// 12) Sitede filtreler çalışıyor mu?
const sayi = async q => Number((await (await fetch(`${B}/Araclar?${q}`)).text()).match(/<b class="text-body">(\d+)<\/b>/)[1]);
const liste = await (await fetch(`${B}/Araclar`)).text();
kontrol("Araç listesinde yeni filtreler görünüyor", liste.includes("Bagaj hacmi") && liste.includes("Klima") && liste.includes("Kasa tipi"));
kontrol("Kartta bagaj hacmi görünüyor", /bi-suitcase"><\/i>\d+ lt/.test(liste));
const toplam = await sayi("");
const bagaj = await sayi(`OzEnAz%5B${bagajId}%5D=500&OzEnCok%5B${bagajId}%5D=700`);
kontrol("Sayı filtresi (500–700 lt)", bagaj === 11, `${bagaj} araç`);
const klimaVar = await sayi(`Oz%5B${klimaId}%5D=true`), klimaYok = await sayi(`Oz%5B${klimaId}%5D=false`);
kontrol("Var / Yok filtresi", klimaVar === 10 && klimaVar + klimaYok === toplam, `var ${klimaVar}, yok ${klimaYok}, toplam ${toplam}`);
const sedan = await sayi(`Oz%5B${kasaId}%5D=Sedan`), hb = await sayi(`Oz%5B${kasaId}%5D=Hatchback`), suv = await sayi(`Oz%5B${kasaId}%5D=SUV`);
kontrol("Seçim listesi filtresi", sedan + hb + suv === toplam && sedan === 16, `${sedan}+${hb}+${suv}`);
const birlikte = await sayi(`Oz%5B${kasaId}%5D=Sedan&Oz%5B${klimaId}%5D=true`);
kontrol("İki özel filtre birlikte", birlikte > 0 && birlikte <= Math.min(sedan, klimaVar), `${birlikte} araç`);
// Sayfalama linkleri filtreyi taşıyor mu?
const sayfalama = await (await fetch(`${B}/Araclar?Oz%5B${kasaId}%5D=Sedan`)).text();
kontrol("Sayfalama linkleri özel filtreyi taşıyor", /Oz%5B\d+%5D=Sedan[^"]*Sayfa=2/.test(sayfalama) || /Sayfa=2[^"]*Oz%5B\d+%5D=Sedan/.test(sayfalama));
// Tıklayarak: filtre panelinde seçim yapınca liste süzülsün
await git("/Araclar");
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.select(`select[name="Oz[${kasaId}]"]`, "SUV")]);
kontrol("Filtre panelinde seçince liste süzülüyor", (await p.$eval("b.text-body", e => e.textContent)) === String(suv));
// Detay
const ilkAracId = (await (await fetch(`${B}/Araclar`)).text()).match(/\/Araclar\/Detay\/(\d+)/)[1];
const detay = await (await fetch(`${B}/Araclar/Detay/${ilkAracId}`)).text();
kontrol("Detayda yeni özellik kutuları", detay.includes("Bagaj hacmi") && detay.includes("Kasa tipi") && detay.includes("Klima"));

// 13) Araç düzenleme formunda ek özellikler
await git(`/Yonetim/Araclar/Duzenle/${ilkAracId}`);
kontrol("Araç formunda Ek özellikler kartı", (await p.content()).includes("Ek özellikler") && (await p.$(`input[name="Ozellikler[${bagajId}]"]`)) !== null);
await p.$eval(`input[name="Ozellikler[${bagajId}]"]`, k => { k.value = "999"; });
await Promise.all([p.waitForNavigation({ waitUntil: "networkidle0" }), p.$eval("form button.btn-primary.btn-lg", b => b.click())]);
kontrol("Araç formundan özellik kaydediliyor", (await (await fetch(`${B}/Araclar/Detay/${ilkAracId}`)).text()).includes("999 lt"));

kontrol("Konsolda hata yok", hatalar.length === 0, hatalar.slice(0, 4).join(" | "));
await t.close();
console.log(sonuclar.join("\n"));
console.log(`\n${sonuclar.filter(s => s.startsWith("✓")).length}/${sonuclar.length} kontrol geçti`);
