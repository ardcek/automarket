// Çalışan uygulamadan (localhost) önemli sayfaların sunucusuz kopyasını üretir → Masaüstü\hafta3
// Kullanım: node hafta3.mjs <adres> <wwwroot> <arackiralama.db> <VarsayilanMetinler.cs> <hedef klasör>
import fs from "node:fs";
import path from "node:path";
import initSqlJs from "sql.js";

const [, , ADRES, WWWROOT, DB_YOLU, METIN_CS, HEDEF] = process.argv;
const BURASI = path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1"));

// ---------------------------------------------------------------- veriler (SQLite, salt okunur)
const SQL = await initSqlJs();
const db = new SQL.Database(fs.readFileSync(DB_YOLU));
const sorgu = s => { const r = db.exec(s); if (!r.length) return []; const { columns, values } = r[0]; return values.map(v => Object.fromEntries(columns.map((c, i) => [c, v[i]]))); };
const araclar = sorgu(`SELECT a.Id, a.KategoriId, a.MarkaId, a.VitesTuruId, a.YakitTuruId, a.KoltukSayisi, a.GunlukFiyat, a.Yil,
                              a.Kilometre, a.SubeId, a.Depozito, m.Ad AS Marka, a.Model
                       FROM Araclar a JOIN Markalar m ON m.Id = a.MarkaId WHERE a.Durum <> 'Pasif'`);
const ekstralar = sorgu("SELECT Id, Ad, GunlukFiyat FROM Ekstralar WHERE Aktif = 1");
const ayar = sorgu("SELECT * FROM SiteAyarlari LIMIT 1")[0];
const metinOzel = Object.fromEntries(sorgu("SELECT Anahtar, Deger FROM Metinler").map(m => [m.Anahtar, m.Deger]));
const metinCs = fs.readFileSync(METIN_CS, "utf8");
const metin = anahtar => metinOzel[anahtar] ?? metinCs.match(new RegExp(`\\["${anahtar.replace(".", "\\.")}"\\]\\s*=\\s*"([^"]*)"`))?.[1] ?? anahtar;

const veri = {
    araclar: Object.fromEntries(araclar.map(a => [a.Id, {
        kategori: a.KategoriId, markaId: a.MarkaId, vites: a.VitesTuruId, yakit: a.YakitTuruId, koltuk: a.KoltukSayisi,
        fiyat: a.GunlukFiyat, yil: a.Yil, km: a.Kilometre, sube: a.SubeId, depozito: a.Depozito, marka: a.Marka, model: a.Model,
    }])),
    ekstralar: Object.fromEntries(ekstralar.map(e => [e.Id, { ad: e.Ad, fiyat: e.GunlukFiyat }])),
    ayarlar: {
        haftalikGun: ayar.HaftalikIndirimGunu, haftalikOran: ayar.HaftalikIndirimOrani, aylikGun: ayar.AylikIndirimGunu,
        aylikOran: ayar.AylikIndirimOrani, tekYon: ayar.TekYonUcreti, enUzun: ayar.EnUzunKiralama,
        sayfaBasina: ayar.SayfaBasinaArac, siralama: ayar.VarsayilanSiralama,
    },
    metinler: { girisKapali: metin("hesap.giris-kapali"), kayitKapali: metin("hesap.kayit-kapali") },
};

// ---------------------------------------------------------------- sayfaları çek
const getir = async adres => { const c = await fetch(ADRES + adres); if (!c.ok) throw new Error(`${adres} → ${c.status}`); return c.text(); };
const sayfalar = {};
sayfalar["index.html"] = await getir("/");
sayfalar["giris.html"] = await getir("/Hesap/Giris");
sayfalar["kayit.html"] = await getir("/Hesap/Kayit");
for (const a of araclar) sayfalar[`arac-${a.Id}.html`] = await getir(`/Araclar/Detay/${a.Id}`);

// Araç listesi: tüm sayfalardaki kartlar tek sayfada toplanır, filtre/sıralama/sayfalamayı statik-site.js yapar
const ilkSayfa = await getir("/Araclar");
const toplamSayfa = Number(ilkSayfa.match(/Sayfa 1 \/ (\d+)/)?.[1] ?? 1);
const kartlar = [];
for (let s = 1; s <= toplamSayfa; s++) {
    const html = s === 1 ? ilkSayfa : await getir(`/Araclar?Siralama=${ayar.VarsayilanSiralama}&Sayfa=${s}`);
    kartlar.push(...(html.match(/<div class="col-sm-6 col-xl-4"><article class="arac-karti">[\s\S]*?<\/article>\s*<\/div>/g) ?? []));
}
if (kartlar.length !== araclar.length) throw new Error(`kart sayısı ${kartlar.length}, araç sayısı ${araclar.length}`);
const isaretliKartlar = kartlar.map((kart, sira) => {
    const id = kart.match(/\/Araclar\/Detay\/(\d+)/)[1];
    return kart.replace('<div class="col-sm-6 col-xl-4">', `<div class="col-sm-6 col-xl-4" data-arac="${id}" data-sira="${sira}">`);
});
const bosKutu = (await getir("/Araclar?Arama=zzzqqqxyz")).match(/<div class="card bos-durum">[\s\S]*?<\/div>/)[0]
    .replace('<div class="card bos-durum">', '<div class="card bos-durum" data-statik-bos hidden>');
const sayiSatiri = ilkSayfa.match(/<div class="text-muted mb-3"><b class="text-body">\d+<\/b>[^<]*<\/div>/)[0]
    .replace('<b class="text-body">', '<b class="text-body" data-statik-sayi>');
const bas = ilkSayfa.indexOf('<div class="col-lg-9">'), son = ilkSayfa.indexOf("</main>", bas);
let liste = ilkSayfa.slice(0, bas) +
`<div class="col-lg-9">
            ${sayiSatiri}
            ${bosKutu}
            <div class="row g-4" data-statik-kartlar>
${isaretliKartlar.join("\n")}
            </div>
            <div data-statik-sayfalama></div>
        </div>
    </div>
</div>
` + ilkSayfa.slice(son);
liste = liste.replace(/(<h1 class="h3 mb-1">[\s\S]*?<\/h1>\s*<p class="text-muted")/, "$1 data-statik-tarih");
sayfalar["araclar.html"] = liste;

// ---------------------------------------------------------------- dönüştür
const kullanilan = new Set();
function linkCevir(deger) {
    if (!deger.startsWith("/") || deger.startsWith("//")) return deger;
    const [, yol, sorgu = "", kare = ""] = deger.match(/^([^?#]*)(\?[^#]*)?(#.*)?$/);
    if (yol === "/") return "index.html" + kare;
    if (yol === "/Araclar") return "araclar.html" + sorgu + kare;
    const detay = yol.match(/^\/Araclar\/Detay\/(\d+)$/);
    if (detay) return `arac-${detay[1]}.html` + sorgu;
    if (yol.startsWith("/Hesap/Kayit")) return "kayit.html";
    if (/^\/(Hesap|Profil|Rezervasyon)/.test(yol)) return "giris.html";
    return "#";
}
for (const [ad, html0] of Object.entries(sayfalar)) {
    let html = html0;
    html = html.replace(/\s*<script src="\/js\/fiyat-hesapla\.js[^"]*"><\/script>/g, "");                 // sunucuya sorar → statik-site.js hesaplar
    html = html.replace(/(src|href)="\/((?:css|js|lib|img)\/[^"?]+)(\?[^"]*)?"/g, (_, o, yol) => { kullanilan.add(yol); return `${o}="dosyalar/${yol}"`; });
    html = html.replace(/\b(href|action)="([^"]*)"/g, (_, o, d) => `${o}="${linkCevir(d)}"`);
    html = html.replace(/\s*<input name="__RequestVerificationToken" type="hidden" value="[^"]*" \/>/g, "");
    html = html.replace(/<form\b[^>]*>/g, etiket => {
        if (!/method="post"/i.test(etiket)) return etiket;
        if (etiket.includes('action="giris.html"')) return etiket.replace("<form", '<form data-statik-hesap="giris"');
        if (etiket.includes('action="kayit.html"')) return etiket.replace("<form", '<form data-statik-hesap="kayit"');
        return etiket;
    });
    html = html.replace("</body>", '    <script src="dosyalar/js/statik-veri.js"></script>\n    <script src="dosyalar/js/statik-site.js"></script>\n</body>');
    if (/<script(?![^>]*\bsrc=)[^>]*>/.test(html)) throw new Error(`${ad}: satır içi script var (ardcek.com CSP engeller)`);
    const kalan = html.match(/(?:href|src|action)="\/(?!\/)[^"]*"/);
    if (kalan) throw new Error(`${ad}: çevrilmemiş adres ${kalan[0]}`);
    sayfalar[ad] = html;
}

// ---------------------------------------------------------------- yaz
fs.mkdirSync(HEDEF, { recursive: true });
for (const [ad, html] of Object.entries(sayfalar)) fs.writeFileSync(path.join(HEDEF, ad), html);
const kopyala = goreli => {
    const varis = path.join(HEDEF, "dosyalar", goreli);
    fs.mkdirSync(path.dirname(varis), { recursive: true });
    fs.copyFileSync(path.join(WWWROOT, goreli), varis);
};
kullanilan.forEach(kopyala);
// CSS'in içinden çağrılan dosyalar
for (const css of [...kullanilan].filter(y => y.endsWith(".css"))) {
    const icerik = fs.readFileSync(path.join(WWWROOT, css), "utf8");
    for (const [, , u] of icerik.matchAll(/url\((['"]?)([^)'"]+)\1\)/g)) {
        if (u.startsWith("data:")) continue;
        const goreli = path.posix.normalize(path.posix.join(path.posix.dirname(css), u.split(/[?#]/)[0]));
        if (fs.existsSync(path.join(WWWROOT, goreli))) { kopyala(goreli); kullanilan.add(goreli); }
    }
}
fs.mkdirSync(path.join(HEDEF, "dosyalar", "js"), { recursive: true });
fs.writeFileSync(path.join(HEDEF, "dosyalar", "js", "statik-veri.js"),
    "// Statik kopyanın verileri (araçlar, ekstralar, fiyat kuralları). hafta3.mjs ile veritabanından üretildi.\nwindow.ROTA = " + JSON.stringify(veri, null, 1) + ";\n");
fs.copyFileSync(path.join(BURASI, "statik", "statik-site.js"), path.join(HEDEF, "dosyalar", "js", "statik-site.js"));
console.log(`${Object.keys(sayfalar).length} sayfa, ${kullanilan.size} dosya, ${araclar.length} araç`);
