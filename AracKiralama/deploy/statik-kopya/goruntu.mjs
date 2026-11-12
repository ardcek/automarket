// hafta3 kopyasından rapor için ekran görüntüleri alır
import puppeteer from "puppeteer-core";
const S = "http://localhost:5089/hafta3/";
const H = process.argv[2];
const t = await puppeteer.launch({ executablePath: "C:/Program Files/Google/Chrome/Application/chrome.exe", headless: true });
const p = await t.newPage();
await p.emulateMediaFeatures([{ name: "prefers-color-scheme", value: "light" }]);
const al = async (ad, adres, { w = 1440, h = 900, tam = false, once } = {}) => {
  await p.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
  await p.goto(S + adres, { waitUntil: "networkidle0" });
    if (once) await once();
  await p.screenshot({ path: `${H}/${ad}.png`, fullPage: tam, type: "png" });
};
const yarin = new Date(); yarin.setDate(yarin.getDate() + 1); yarin.setHours(10, 0, 0, 0);
const kutu = d => new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
const bit = new Date(yarin); bit.setDate(bit.getDate() + 8);
await al("ana", "", { h: 1000 });
await al("liste", "araclar.html", { h: 1250 });
await al("liste-suzulmus", "araclar.html?KategoriId=3&Siralama=fiyat-azalan", { h: 1100 });
await al("liste-tarihli", `araclar.html?AlisSubeId=&IadeSubeId=&Baslangic=${kutu(yarin)}&Bitis=${kutu(bit)}`, { h: 1000 });
await al("detay", `arac-3.html?Baslangic=${kutu(yarin)}&Bitis=${kutu(bit)}&AlisSubeId=1&IadeSubeId=2`, { h: 1150, once: async () => {
  for (const e of [6, 4]) await p.$eval(`input[name="EkstraIdleri"][value="${e}"]`, k => k.click());
}});
await al("giris", "giris.html", { h: 900, once: async () => {
  await p.type("#Eposta", "musteri@rota.com"); await p.type("#Sifre", "Rota123!"); await p.click("form[data-statik-hesap] button");
}});
await al("kayit", "kayit.html", { h: 1000, once: async () => {
  for (const [s, d] of [["#Ad", "Ayşe"], ["#Soyad", "Yılmaz"], ["#Eposta", "ayse@ornek.com"], ["#Telefon", "05551112233"], ["#Sifre", "abc12345"], ["#SifreTekrar", "abc12345"]]) await p.type(s, d);
  await p.click("#KvkkOnay"); await p.click("form[data-statik-hesap] button");
}});
await al("mobil", "araclar.html", { w: 390, h: 844 });
await t.close();
console.log("tamam");
