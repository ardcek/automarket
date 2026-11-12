// Kullanım: node doldur.js <girdi.docx> <cikti.docx>
// PROJE BİLGİ FORMU'nu icerik.js'teki metinlerle doldurur. Formun kendi biçimi (tablolar, başlıklar,
// açıklamalar, üst/alt bilgi) korunur; sadece boş hücreler ve boş metin kutuları doldurulur.
const fs = require('fs');
const JSZip = require('jszip');
const Y = require('./docx-yardimci');
const icerik = require('./icerik');
const { W } = Y;

const [, , girdi, cikti] = process.argv;

// { b: 'Başlık' } + 'paragraf'  →  { r: '**Başlık.** paragraf' }  (başlık satırı sayfa sonunda yalnız kalmasın)
function birlestir(bloklar) {
  const s = [];
  for (let i = 0; i < bloklar.length; i++) {
    const b = bloklar[i], n = bloklar[i + 1];
    if (b && typeof b === 'object' && b.b != null && typeof n === 'string') {
      const nokta = /[.:]$/.test(b.b) ? '' : '.';
      s.push({ r: '**' + b.b + nokta + '** ' + n }); i++;
    } else s.push(b);
  }
  return s;
}

(async () => {
  const zip = await JSZip.loadAsync(fs.readFileSync(girdi));
  const dom = Y.yukle(await zip.file('word/document.xml').async('string'));
  const maddeNumId = await maddeNumaralandirmasi(zip);
  const ust = Y.ustOgeler(dom);
  const rapor = [];

  // ── yardımcılar ──────────────────────────────────────────────────────────
  const duz = (el) => Y.metin(el).replace(/\s+/g, ' ').trim();
  const metinIle = (aranan) => { const i = ust.findIndex(o => o.localName === 'p' && duz(o).startsWith(aranan)); if (i < 0) throw new Error('bulunamadı: ' + aranan); return i; };
  const sonrakiTablo = (i) => { for (let j = i + 1; j < ust.length; j++) if (ust[j].localName === 'tbl') return ust[j]; throw new Error('tablo yok: ' + i); };
  const tabloBasligiIle = (aranan) => ust.find(o => o.localName === 'tbl' && duz(Y.cocuklar(o, 'tr')[0]).includes(aranan));
  const satirlar = (tbl) => Y.cocuklar(tbl, 'tr');
  const hucreler = (tr) => Y.cocuklar(tr, 'tc');

  function trPrAl(tr) { let trPr = Y.ilkCocuk(tr, 'trPr'); if (!trPr) { trPr = Y.yeni(dom, 'trPr'); tr.insertBefore(trPr, tr.firstChild); } return trPr; }
  function yukseklikSerbest(tr) { for (const h of Y.cocuklar(trPrAl(tr), 'trHeight')) if (h.getAttributeNS(W, 'hRule') === 'exact') h.setAttributeNS(W, 'w:hRule', 'atLeast'); }
  function bolunmesin(tr) { const trPr = trPrAl(tr); if (!Y.ilkCocuk(trPr, 'cantSplit')) trPr.insertBefore(Y.yeni(dom, 'cantSplit'), trPr.firstChild); }
  function keepNextEkle(p) {
    let ppr = Y.ilkCocuk(p, 'pPr');
    if (!ppr) { ppr = Y.yeni(dom, 'pPr'); p.insertBefore(ppr, p.firstChild); }
    if (Y.ilkCocuk(ppr, 'keepNext') || Y.ilkCocuk(ppr, 'sectPr')) return;
    const stil = Y.ilkCocuk(ppr, 'pStyle');
    ppr.insertBefore(Y.yeni(dom, 'keepNext'), stil ? stil.nextSibling : ppr.firstChild);
  }
  // Başlık satırı: her sayfada tekrarlansın ve ilk veri satırından ayrılmasın
  function baslikSatiri(tbl) {
    const tr = satirlar(tbl)[0];
    const trPr = trPrAl(tr);
    if (!Y.ilkCocuk(trPr, 'tblHeader')) trPr.appendChild(Y.yeni(dom, 'tblHeader'));
    bolunmesin(tr);
    Y.hepsi(tr, 'p').forEach(keepNextEkle);
  }
  // Başlık + açıklama paragrafları, ardından gelen kutu / tabloyla aynı sayfada kalsın
  function birlikteTut(baslangicMetni) {
    const i = metinIle(baslangicMetni);
    const tbl = sonrakiTablo(i);
    for (let k = i; k < ust.indexOf(tbl); k++) if (ust[k].localName === 'p') keepNextEkle(ust[k]);
  }
  // Tablonun hücre iç boşluğu (sol) küçükse paragraflara girinti verilir
  function ekGirinti(tc) {
    const solBoslugu = (mar) => { const s = mar && (Y.ilkCocuk(mar, 'start') || Y.ilkCocuk(mar, 'left')); return s ? +s.getAttributeNS(W, 'w') : null; };
    const hucre = solBoslugu(Y.ilkCocuk(Y.ilkCocuk(tc, 'tcPr') || tc, 'tcMar'));            // önce hücrenin kendi ayarı
    const tablo = solBoslugu(Y.ilkCocuk(Y.ilkCocuk(tc.parentNode.parentNode, 'tblPr'), 'tblCellMar'));
    const deger = hucre ?? tablo ?? 0;
    return deger >= 80 ? 0 : 100 - deger;
  }

  // Hücrenin içeriğini (tcPr hariç) silip verilen bloklarla doldurur.
  // kip: 'kutu' (büyük metin kutusu) | 'hucre' (tablo hücresi) | 'tek' (tek satırlık kimlik hücresi)
  function hucreDoldur(tc, bloklar, { kip = 'hucre', hiza = 'left', dikey = 'top', maddeSol } = {}) {
    for (let c = tc.firstChild; c;) { const s = c.nextSibling; if (!(c.nodeType === 1 && c.localName === 'tcPr')) tc.removeChild(c); c = s; }
    const dizi = birlestir(Array.isArray(bloklar) ? bloklar : [bloklar]);
    const K = kip === 'kutu', T = kip === 'tek';
    const girinti = K || T ? 0 : ekGirinti(tc);
    dizi.forEach((b, i) => {
      const tur = Y.blokTuru(b), sonraki = i + 1 < dizi.length ? Y.blokTuru(dizi[i + 1]) : null;
      let once = 0, sonra;
      if (tur === 'm') sonra = sonraki === 'm' ? (K ? 40 : 20) : (K ? 100 : 40);
      else sonra = K ? 100 : 40;
      if (typeof b === 'object' && (b.r != null || b.b != null) && i > 0) once = K ? 60 : 20;
      if (i === 0) once = K ? 60 : 30;
      if (i === dizi.length - 1) sonra = K ? 60 : 30;
      if (T) { once = 0; sonra = 0; }
      tc.appendChild(Y.paragraf(dom, b, { maddeNumId, hiza, once, sonra, sol: girinti, sag: girinti, maddeSol: maddeSol ?? (K ? 284 : 227), butun: K && i === 0 }));
    });
    const tcPr = Y.ilkCocuk(tc, 'tcPr');
    if (tcPr) { const v = Y.ilkCocuk(tcPr, 'vAlign'); if (v) v.setAttributeNS(W, 'w:val', dikey); else tcPr.appendChild(Y.yeni(dom, 'vAlign', { val: dikey })); }
    yukseklikSerbest(tc.parentNode);
    return girinti;
  }
  // Tablonun sütun genişliklerini (tblGrid + her hücrenin tcW) değiştirir
  function sutunGenislikleri(tbl, genislikler) {
    Y.hepsi(Y.ilkCocuk(tbl, 'tblGrid'), 'gridCol').forEach((g, i) => g.setAttributeNS(W, 'w:w', String(genislikler[i])));
    for (const tr of satirlar(tbl)) hucreler(tr).forEach((tc, i) => { const w = Y.hepsi(tc, 'tcW')[0]; if (w) w.setAttributeNS(W, 'w:w', String(genislikler[i])); });
  }
  function satirKlonla(tr) { const k = tr.cloneNode(true); tr.parentNode.insertBefore(k, tr.nextSibling); return k; }
  // Veri satırı sayısını n'ye tamamlar (son satırı kopyalayarak)
  function satirSayisi(tbl, baslikSayisi, n) {
    let s = satirlar(tbl);
    while (s.length - baslikSayisi < n) { satirKlonla(s[s.length - 1]); s = satirlar(tbl); }
    return s.slice(baslikSayisi, baslikSayisi + n);
  }

  // ── 1) GRUP NO ───────────────────────────────────────────────────────────
  {
    const p = ust[metinIle('GRUP NO')];
    const r = Y.hepsi(p, 'r').pop().cloneNode(true);            // etiketin biçimini (kalın, 10 pt) aynen kullan
    for (const t of Y.hepsi(r, 't')) t.parentNode.removeChild(t);
    const t = Y.yeni(dom, 't'); t.setAttribute('xml:space', 'preserve'); t.appendChild(dom.createTextNode(' ' + icerik.grupNo));
    r.appendChild(t); p.appendChild(r);
    rapor.push('GRUP NO: ' + icerik.grupNo);
  }

  // ── 2) Proje başlığı, lider, katılımcılar ────────────────────────────────
  {
    const tbl = tabloBasligiIle('Proje Başlığı');
    const s = satirlar(tbl);
    const bul = (aranan) => s.find(tr => Y.metin(tr).includes(aranan));
    const hucreYaz = (tc, yazi) => hucreDoldur(tc, [yazi], { kip: 'tek', hiza: 'center', dikey: 'center' });
    Y.hepsi(s[0], 'p')[0].appendChild(Y.run(dom, icerik.baslik, {}));   // "Proje Başlığı:" etiketinin yanına
    const lider = bul('Proje Lideri');
    hucreYaz(hucreler(lider).slice(-2)[0], icerik.lider.no);
    hucreYaz(hucreler(lider).slice(-1)[0], icerik.lider.ad);
    const ilk = s.indexOf(bul('Katılımcılar'));
    icerik.katilimcilar.forEach((k, j) => { const tr = s[ilk + j]; hucreYaz(hucreler(tr).slice(-2)[0], k.no); hucreYaz(hucreler(tr).slice(-1)[0], k.ad); });
    // Kullanılmayan (boş) katılımcı satırları silinir; her hücrenin kendi alt çizgisi olduğu için tablo kapalı kalır
    const bos = s.slice(ilk + icerik.katilimcilar.length).filter(tr => !duz(tr));
    bos.forEach(tr => tr.parentNode.removeChild(tr));
    rapor.push('Kimlik tablosu: lider + ' + icerik.katilimcilar.length + ' katılımcı (' + bos.length + ' boş satır silindi)');
  }

  // ── 3) Metin kutuları (başlığın ardından gelen tek hücreli tablo) ─────────
  const kutu = (baslik, bloklar) => {
    const tc = Y.hepsi(sonrakiTablo(metinIle(baslik)), 'tc')[0];
    hucreDoldur(tc, bloklar, { kip: 'kutu', hiza: 'both' });
    rapor.push('Kutu: ' + baslik + ' → ' + bloklar.length + ' blok');
  };
  kutu('ULUSAL KAZANIM', icerik.ulusalKazanim);
  kutu('AMAÇ VE HEDEFLER', icerik.amac);
  kutu('YÖNTEM', icerik.yontem);
  kutu('Sanayi İşbirliğine Yönelik', icerik.yolHaritasi);
  kutu('BELİRTMEK İSTEDİĞİNİZ DİĞER KONULAR', icerik.digerKonular);

  // ── 4) İş-Zaman Çizelgesi ────────────────────────────────────────────────
  {
    const tbl = sonrakiTablo(metinIle('İŞ-ZAMAN ÇİZELGESİ'));
    // Çizelge dikey sayfada (bkz. 12. adım): toplam 9556 = yazı alanının %98'i, diğer tablolarla aynı.
    // Formdaki not: "satırlar ve sütunlar gerektiği kadar genişletilebilir". Zaman sütunu "(Aralık 2026 –" tek satıra sığacak kadar geniş.
    sutunGenislikleri(tbl, [558, 2480, 2700, 1330, 2488]);
    const ind = Y.ilkCocuk(Y.ilkCocuk(tbl, 'tblPr'), 'tblInd');
    if (ind) ind.setAttributeNS(W, 'w:w', '108');   // sol kenar diğer tablolarla hizalı (212 ile sağdan taşıyordu)
    baslikSatiri(tbl);
    const veri = satirSayisi(tbl, 1, icerik.isPaketleri.length);
    veri.forEach((tr, j) => {
      const [no, ad, kim, zaman, olcut] = icerik.isPaketleri[j];
      const h = hucreler(tr);
      hucreDoldur(h[0], ['**' + no + '**'], { kip: 'tek', hiza: 'center', dikey: 'center' });
      hucreDoldur(h[1], ad); hucreDoldur(h[2], kim); hucreDoldur(h[3], zaman, { hiza: 'center' }); hucreDoldur(h[4], olcut);
      bolunmesin(tr);
    });
    rapor.push('İş-zaman: ' + veri.length + ' İP');
  }

  // ── 5) Risk yönetimi ─────────────────────────────────────────────────────
  {
    const tbl = sonrakiTablo(metinIle('RİSK YÖNETİMİ TABLOSU'));
    sutunGenislikleri(tbl, [820, 4080, 4656]);   // "2, 4, 7" tek satıra sığsın (toplam aynı: 9556)
    baslikSatiri(tbl);
    const veri = satirSayisi(tbl, 1, icerik.riskler.length);
    veri.forEach((tr, j) => {
      const [no, risk, plan] = icerik.riskler[j];
      const h = hucreler(tr);
      hucreDoldur(h[0], [no], { kip: 'tek', hiza: 'center', dikey: 'center' }); hucreDoldur(h[1], [risk]); hucreDoldur(h[2], [plan]);
      bolunmesin(tr);
    });
    rapor.push('Risk: ' + veri.length + ' satır');
  }

  // ── 6) Araştırma olanakları ──────────────────────────────────────────────
  {
    const tbl = sonrakiTablo(metinIle('ARAŞTIRMA OLANAKLARI TABLOSU'));
    baslikSatiri(tbl);
    const veri = satirSayisi(tbl, 1, icerik.olanaklar.length);
    veri.forEach((tr, j) => { const h = hucreler(tr); hucreDoldur(h[0], [icerik.olanaklar[j][0]]); hucreDoldur(h[1], [icerik.olanaklar[j][1]]); bolunmesin(tr); });
    rapor.push('Olanaklar: ' + veri.length + ' satır');
  }

  // ── 7) Çıktılar ve etkiler (etiket sütunu formda hazır; 2. ve 3. sütun doldurulur) ──
  const ikiSutun = (baslikMetni, liste, ad) => {
    const tbl = tabloBasligiIle(baslikMetni);
    baslikSatiri(tbl);
    const veri = satirlar(tbl).slice(1);
    if (veri.length !== liste.length) throw new Error(ad + ': satır sayısı ' + veri.length + ' ≠ ' + liste.length);
    let g = 0;
    veri.forEach((tr, j) => { const h = hucreler(tr); g = hucreDoldur(h[h.length - 2], liste[j][0]); hucreDoldur(h[h.length - 1], liste[j][1], { hiza: 'center' }); bolunmesin(tr); });
    rapor.push(ad + ': ' + veri.length + ' satır (ek girinti ' + g + ')');
  };
  ikiSutun('Çıktı Türü', icerik.ciktilar, 'Çıktılar');
  ikiSutun('Etki Türü', icerik.etkiler, 'Etkiler');

  // ── 8) Paylaşım ve yayılım ───────────────────────────────────────────────
  {
    const tbl = sonrakiTablo(metinIle('PROJE ÇIKTILARININ PAYLAŞIMI VE YAYILIMI TABLOSU'));
    baslikSatiri(tbl);
    const veri = satirSayisi(tbl, 1, icerik.paylasim.length);
    veri.forEach((tr, j) => { const h = hucreler(tr); icerik.paylasim[j].forEach((y, k) => hucreDoldur(h[k], [y], k === 2 ? { hiza: 'center' } : {})); bolunmesin(tr); });
    rapor.push('Paylaşım: ' + veri.length + ' satır');
  }

  // ── 9) Başlıklar, kutu / tablolarından ayrı sayfaya düşmesin ─────────────
  ['AMAÇ VE HEDEFLER', 'YÖNTEM', 'Risk Yönetimi', 'Araştırma Olanakları', 'YAYGIN ETKİ', 'Projeden Elde Edilmesi Öngörülen Etkilere',
    'Sanayi İşbirliğine Yönelik', '6.4. Proje Çıktılarının', 'BELİRTMEK İSTEDİĞİNİZ DİĞER KONULAR'].forEach(birlikteTut);
  // "3. YÖNTEM" yeni sayfadan başlasın (yoksa kutunun ilk satırı önceki sayfanın dibinde tek kalıyor)
  {
    const ppr = Y.ilkCocuk(ust[metinIle('YÖNTEM')], 'pPr');
    const once = Y.ilkCocuk(ppr, 'keepLines') || Y.ilkCocuk(ppr, 'keepNext') || Y.ilkCocuk(ppr, 'pStyle');
    if (!Y.ilkCocuk(ppr, 'pageBreakBefore')) ppr.insertBefore(Y.yeni(dom, 'pageBreakBefore'), once ? once.nextSibling : ppr.firstChild);
  }

  // ── 10) İş-zaman çizelgesinin altındaki (*) notunun ardına açıklama ──────
  {
    const bas = metinIle('İŞ-ZAMAN ÇİZELGESİ');
    const i = ust.findIndex((o, k) => k > bas && o.localName === 'p' && Y.metin(o).startsWith('(*)'));
    ust[i].parentNode.insertBefore(Y.paragraf(dom, { k: icerik.isPaketiNotu }, { sonra: 0 }), ust[i].nextSibling);
  }

  // ── 11) EK-1: KAYNAKLAR (son başlığın biçimiyle, yeni sayfada) ───────────
  {
    const baslik = ust[metinIle('BELİRTMEK İSTEDİĞİNİZ DİĞER KONULAR')].cloneNode(true);
    const runs = Y.hepsi(baslik, 'r');
    runs.slice(1).forEach(r => r.parentNode.removeChild(r));
    Y.hepsi(runs[0], 't').forEach((t, k) => { if (k) t.parentNode.removeChild(t); else t.textContent = 'EK-1: KAYNAKLAR'; });
    const ppr = Y.ilkCocuk(baslik, 'pPr');
    const numPr = Y.ilkCocuk(ppr, 'numPr'); if (numPr) ppr.removeChild(numPr);  // başlık numarası almasın
    const kn = Y.ilkCocuk(ppr, 'keepNext');
    ppr.insertBefore(Y.yeni(dom, 'pageBreakBefore'), kn ? kn.nextSibling : (Y.ilkCocuk(ppr, 'pStyle') || { nextSibling: ppr.firstChild }).nextSibling);
    const body = Y.govde(dom), sectPr = Y.ilkCocuk(body, 'sectPr');
    body.insertBefore(baslik, sectPr);
    body.insertBefore(Y.paragraf(dom, { k: 'Ekler, formun 22 sayfalık sınırına dahil değildir.' }, { sonra: 120 }), sectPr);
    icerik.kaynaklar.forEach(k => body.insertBefore(Y.paragraf(dom, k, { hiza: 'left', sonra: 60, sol: 340, sag: 0 }), sectPr));
    // asılı girinti (ikinci satırlar içeride)
    Y.hepsi(body, 'p').slice(-icerik.kaynaklar.length).forEach(p => { const ind = Y.hepsi(p, 'ind')[0]; ind.setAttributeNS(W, 'w:hanging', '340'); });
    rapor.push('Kaynaklar: ' + icerik.kaynaklar.length);
  }

  // ── 12) Bütün sayfalar dikey: şablondaki yatay bölüm (yalnız iş-zaman çizelgesi) kaldırılır ──
  // Bölüm sonu, bölümün son paragrafındaki sectPr'dir. Yatay bölümün sectPr'si (boş paragrafıyla) silinince
  // çizelge sonraki dikey bölüme katılır: "4. PROJE YÖNETİMİ" yine yeni sayfadan başlar, Risk Yönetimi
  // çizelgenin hemen ardından gelir. Üst / alt bilgiler (ortalı logo, sayfa no) iki bölümde de aynıdır.
  {
    const yatay = Y.hepsi(dom.documentElement, 'sectPr').filter(s => (Y.ilkCocuk(s, 'pgSz') || { getAttributeNS: () => '' }).getAttributeNS(W, 'orient') === 'landscape');
    for (const s of yatay) {
      const p = s.parentNode.parentNode;   // sectPr → pPr → p
      if (s.parentNode.localName !== 'pPr' || p.localName !== 'p') throw new Error('yatay bölüm gövde sonunda, beklenmiyordu');
      if (duz(p)) s.parentNode.removeChild(s); else p.parentNode.removeChild(p);
    }
    rapor.push('Yatay bölüm kaldırıldı: ' + yatay.length);
  }

  zip.file('word/document.xml', Y.yaz(dom));
  // Klasör kayıtlarını çıkar (Word bazı ortamlarda klasör kaydı olan .docx'i reddedebiliyor)
  for (const ad of Object.keys(zip.files)) if (zip.files[ad].dir) delete zip.files[ad];
  const cikan = await zip.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE' });
  fs.writeFileSync(cikti, cikan);
  console.log(rapor.join('\n'));
  console.log('Yazıldı:', cikti, cikan.length, 'bayt');
})().catch(e => { console.error('HATA:', e); process.exit(1); });

// Madde işareti (•) için numaralandırma tanımı ekler; numId döner
async function maddeNumaralandirmasi(zip) {
  const NUM = 'word/numbering.xml';
  const soyutXml = (id) => `<w:abstractNum xmlns:w="${W}" w:abstractNumId="${id}"><w:multiLevelType w:val="singleLevel"/><w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="bullet"/><w:lvlText w:val="•"/><w:lvlJc w:val="left"/><w:pPr><w:ind w:left="284" w:hanging="284"/></w:pPr><w:rPr><w:rFonts w:ascii="Arial" w:hAnsi="Arial" w:cs="Arial"/></w:rPr></w:lvl></w:abstractNum>`;
  const numXml = (id, soyut) => `<w:num xmlns:w="${W}" w:numId="${id}"><w:abstractNumId w:val="${soyut}"/></w:num>`;
  if (zip.file(NUM)) {
    const dom = Y.yukle(await zip.file(NUM).async('string'));
    const kok = dom.documentElement;
    const soyutlar = Y.cocuklar(kok, 'abstractNum'), numlar = Y.cocuklar(kok, 'num');
    const soyutId = Math.max(0, ...soyutlar.map(a => +a.getAttributeNS(W, 'abstractNumId'))) + 1;
    const numId = Math.max(0, ...numlar.map(a => +a.getAttributeNS(W, 'numId'))) + 1;
    kok.insertBefore(dom.importNode(Y.yukle(soyutXml(soyutId)).documentElement, true), numlar[0] || null);   // abstractNum'lar num'lardan önce
    const sonNum = numlar[numlar.length - 1];
    kok.insertBefore(dom.importNode(Y.yukle(numXml(numId, soyutId)).documentElement, true), sonNum ? sonNum.nextSibling : null);
    zip.file(NUM, Y.yaz(dom));
    return numId;
  }
  zip.file(NUM, `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:numbering xmlns:w="${W}">${soyutXml(1).replace(` xmlns:w="${W}"`, '')}${numXml(1, 1).replace(` xmlns:w="${W}"`, '')}</w:numbering>`);
  const tur = await zip.file('[Content_Types].xml').async('string');
  zip.file('[Content_Types].xml', tur.replace('</Types>', '<Override PartName="/word/numbering.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml"/></Types>'));
  const ilisk = await zip.file('word/_rels/document.xml.rels').async('string');
  zip.file('word/_rels/document.xml.rels', ilisk.replace('</Relationships>', '<Relationship Id="rIdMadde1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering" Target="numbering.xml"/></Relationships>'));
  return 1;
}
