// ============================================================================
// ARAYÜZ TESTİ: Yapay Zekâ Asistanı sayfası GERÇEK TARAYICIDA (Chrome, görünmez)
// Uygulama ve yapay zekâ motoru çalışırken:
//   node AracKiralama/tests/uctan-uca/yapay-zeka-arayuz.mjs
// Ek paket gerekmez: Chrome "uzaktan hata ayıklama" (DevTools) bağlantısıyla yönetilir.
// Adımlar: giriş → sayfa → istek yaz, Gönder → kartlar → Önizle → Uygula → Geri al
// Konsol hatalarını toplar, ekran görüntülerini %TEMP%\rota-arayuz\ altına kaydeder.
// ============================================================================
import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync, mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

const B = process.env.ADRES ?? "http://localhost:5046";
const CHROME = process.env.CHROME ?? "C:/Program Files/Google/Chrome/Application/chrome.exe";
const klasor = join(tmpdir(), "rota-arayuz"); mkdirSync(klasor, { recursive: true });
const sonuclar = [], hatalar = [];
const kontrol = (ad, kosul, ayrinti = "") => { sonuclar.push(`${kosul ? "✓" : "✗"} ${ad}${ayrinti ? " — " + ayrinti : ""}`); console.log(sonuclar.at(-1)); };
const bekle = ms => new Promise(r => setTimeout(r, ms));

// ---- Chrome'u aç ve DevTools'a bağlan ----
const chrome = spawn(CHROME, ["--headless=new", "--remote-debugging-port=9333", `--user-data-dir=${mkdtempSync(join(tmpdir(), "rota-chrome-"))}`, "--window-size=1400,1000", "about:blank"], { stdio: "ignore" });
let sayfaWs;
for (let i = 0; i < 40 && !sayfaWs; i++) { await bekle(250); try { sayfaWs = (await (await fetch("http://127.0.0.1:9333/json")).json()).find(h => h.type === "page")?.webSocketDebuggerUrl; } catch { } }
const ws = new WebSocket(sayfaWs);
await new Promise(r => ws.addEventListener("open", r));
let sira = 0; const bekleyenler = new Map();
ws.addEventListener("message", m => {
    const v = JSON.parse(m.data);
    if (v.id && bekleyenler.has(v.id)) { bekleyenler.get(v.id)(v); bekleyenler.delete(v.id); }
    if (v.method === "Runtime.exceptionThrown") hatalar.push(v.params.exceptionDetails.exception?.description ?? v.params.exceptionDetails.text);
    if (v.method === "Runtime.consoleAPICalled" && v.params.type === "error") hatalar.push(v.params.args.map(a => a.value ?? a.description).join(" "));
    if (v.method === "Log.entryAdded" && v.params.entry.level === "error") hatalar.push(v.params.entry.text + " " + (v.params.entry.url ?? ""));
});
const cdp = (method, params = {}) => new Promise(r => { const id = ++sira; bekleyenler.set(id, r); ws.send(JSON.stringify({ id, method, params })); });
const js = async ifade => (await cdp("Runtime.evaluate", { expression: ifade, awaitPromise: true, returnByValue: true })).result?.result?.value;
const git = async adres => { await cdp("Page.navigate", { url: B + adres }); await bekle(1500); for (let i = 0; i < 40 && await js("document.readyState") !== "complete"; i++) await bekle(250); };
const kosulBekle = async (ifade, saniye = 300) => { for (let i = 0; i < saniye * 2; i++) { if (await js(ifade)) return true; await bekle(500); } return false; };
const goruntu = async ad => writeFileSync(join(klasor, ad + ".png"), Buffer.from((await cdp("Page.captureScreenshot", { captureBeyondViewport: true })).result.data, "base64"));
await cdp("Page.enable"); await cdp("Runtime.enable"); await cdp("Log.enable");
// confirm() / alert() pencerelerini kendiliğinden onayla
ws.addEventListener("message", m => { const v = JSON.parse(m.data); if (v.method === "Page.javascriptDialogOpening") cdp("Page.handleJavaScriptDialog", { accept: true }); });

try {
    // ---- Giriş ----
    await git("/Hesap/Giris");
    await js(`document.querySelector("#Eposta").value="admin@rota.com"; document.querySelector("#Sifre").value="Rota123!"; document.querySelector("form button.btn-primary").click(); true`);
    await bekle(2500);
    await git("/Yonetim/YapayZeka");
    kontrol("Sayfa açıldı, menüde Yapay Zekâ Asistanı", await js(`document.title.includes("Yapay Zekâ") && !!document.querySelector('a.aktif[href="/Yonetim/YapayZeka"]')`));
    kontrol("Motor hazır (durum ışığı yeşil)", await kosulBekle(`document.querySelector(".yz-isik").dataset.durum === "Hazir"`, 300));

    // ---- Örnek isteğe tıkla, Gönder ----
    await js(`[...document.querySelectorAll(".yz-ornek")].find(b => b.textContent.includes("Meslek")).click(); true`);
    kontrol("Örnek istek kutuya yazıldı", (await js(`document.querySelector("#yz-istek").value`))?.includes("Meslek"));
    await js(`document.querySelector("#yz-gonder").click(); true`);
    kontrol("Düşünürken sayaç görünüyor", await kosulBekle(`document.querySelector("#yz-sayac").textContent.includes("sn")`, 10));
    kontrol("Kartlar geldi", await kosulBekle(`!document.querySelector("#yz-sonuc").classList.contains("d-none")`, 300));
    const kartlar = await js(`[...document.querySelectorAll("#yz-kartlar .yz-kart")].map(k => k.querySelector(".fw-semibold").textContent.trim() + " | " + k.querySelector("label .small").textContent.trim())`);
    kontrol("Kampanya kartları var", kartlar?.some(k => k.startsWith("Kampanya ekle")), `${kartlar?.length} kart`);
    console.log("   " + (kartlar ?? []).join("\n   "));
    await goruntu("1-kartlar");

    // ---- Değerleri düzenle: ilk kampanyanın oranını 25 yap, kontrol et ----
    await js(`const k = [...document.querySelectorAll("#yz-kartlar .yz-kart")].find(k => k.dataset.tur === "kampanya_ekle"); k.querySelector("[data-bs-toggle=collapse]").click(); k.querySelector('[data-ad="indirim_yuzdesi"]').value = 25; true`);
    await js(`document.querySelector("#yz-kontrol").click(); true`);
    kontrol("Düzenlenen oran kontrol sonrası özette", await kosulBekle(`[...document.querySelectorAll("#yz-kartlar .yz-kart label .small")].some(s => s.textContent.includes("%25"))`, 20));

    // ---- Uygula ----
    await js(`document.querySelector("#yz-uygula").click(); true`);
    kontrol("Uygulandı mesajı", await kosulBekle(`!document.querySelector("#yz-bitti").classList.contains("d-none") && document.querySelector("#yz-bitti").textContent.includes("Uygulandı")`, 30));
    await goruntu("2-uygulandi");

    // ---- Önizleme: başlık isteği ----
    await js(`document.querySelector("#yz-istek").value = "Ana sayfadaki büyük başlık Yolculuk burada başlar olsun"; document.querySelector("#yz-gonder").click(); true`);
    await kosulBekle(`!document.querySelector("#yz-sonuc").classList.contains("d-none") && document.querySelectorAll("#yz-kartlar .yz-kart").length > 0`, 300);
    await js(`document.querySelector("#yz-onizle").click(); true`);
    kontrol("Önizleme çerçevesi açıldı", await kosulBekle(`!document.querySelector("#yz-onizleme-karti").classList.contains("d-none") && document.querySelector("#yz-onizleme").src.includes("onizleme=1")`, 15));
    await bekle(3000);
    kontrol("Önizlemede yeni başlık görünüyor", await js(`document.querySelector("#yz-onizleme").contentDocument?.querySelector("h1")?.textContent.includes("burada")`));
    await goruntu("3-onizleme");
    const gercek = await (await fetch(B + "/")).text();
    kontrol("Gerçek site değişmedi (önizleme taslaktır)", !gercek.includes("Yolculuk <span"));
    await js(`document.querySelector("#yz-vazgec").click(); true`);

    // ---- Geçmiş ve geri al ----
    await git("/Yonetim/YapayZeka");
    kontrol("Geçmişte kayıt var", await js(`document.querySelectorAll("form[action*='GeriAl']").length >= 1`));
    await js(`document.querySelector("form[action*='GeriAl'] button").click(); true`);
    await bekle(2500);
    kontrol("Geri alındı bildirimi", await js(`document.body.textContent.includes("geri alındı")`));
    await goruntu("4-gecmis");

    // ---- Kod modu sekmesi görünüyor mu ----
    await js(`document.querySelector('[data-bs-target="#yz-kod"]').click(); true`);
    await bekle(500);
    kontrol("Kod modu sekmesi açılıyor", await js(`document.querySelector("#yz-kod").classList.contains("active")`));
} finally {
    kontrol("Konsolda JavaScript hatası yok", hatalar.length === 0, hatalar.slice(0, 3).join(" | "));
    console.log(`\n${sonuclar.filter(s => s.startsWith("✓")).length}/${sonuclar.length} kontrol geçti · ekran görüntüleri: ${klasor}`);
    ws.close(); chrome.kill();
}
