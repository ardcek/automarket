// .docx içindeki word/document.xml'i DOM olarak düzenlemek için küçük yardımcılar.
const { DOMParser, XMLSerializer } = require('@xmldom/xmldom');
const W = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main';

function yukle(xml) { return new DOMParser().parseFromString(xml, 'text/xml'); }
function yaz(dom) { return new XMLSerializer().serializeToString(dom); }

// Doğrudan çocuklar (isim alanı W)
function cocuklar(el, ad) {
  const s = [];
  for (let c = el.firstChild; c; c = c.nextSibling) if (c.nodeType === 1 && c.namespaceURI === W && c.localName === ad) s.push(c);
  return s;
}
function ilkCocuk(el, ad) { return cocuklar(el, ad)[0] || null; }
function hepsi(el, ad) { return Array.from(el.getElementsByTagNameNS(W, ad)); }
function metin(el) { return hepsi(el, 't').map(t => t.textContent).join(''); }

// Gövdedeki üst düzey öğeler (p / tbl) sırasıyla
function govde(dom) { return hepsi(dom.documentElement, 'body')[0]; }
function ustOgeler(dom) {
  const s = [];
  for (let c = govde(dom).firstChild; c; c = c.nextSibling) if (c.nodeType === 1 && (c.localName === 'p' || c.localName === 'tbl')) s.push(c);
  return s;
}

function yeni(dom, ad, nitelikler = {}) {
  const el = dom.createElementNS(W, 'w:' + ad);
  for (const [k, v] of Object.entries(nitelikler)) el.setAttributeNS(W, 'w:' + k, String(v));
  return el;
}

// Yazı özellikleri: Arial 9 (sz=18), isteğe bağlı kalın / italik / boyut
function rPr(dom, { kalin = false, italik = false, boyut = 18 } = {}) {
  const r = yeni(dom, 'rPr');
  r.appendChild(yeni(dom, 'rFonts', { ascii: 'Arial', hAnsi: 'Arial', cs: 'Arial', eastAsia: 'Arial' }));
  if (kalin) { r.appendChild(yeni(dom, 'b')); r.appendChild(yeni(dom, 'bCs')); }
  if (italik) { r.appendChild(yeni(dom, 'i')); r.appendChild(yeni(dom, 'iCs')); }
  r.appendChild(yeni(dom, 'sz', { val: boyut }));
  r.appendChild(yeni(dom, 'szCs', { val: boyut }));
  r.appendChild(yeni(dom, 'lang', { val: 'tr-TR', eastAsia: 'tr-TR', bidi: 'ar-SA' }));
  return r;
}

function run(dom, yazi, ozellik) {
  const r = yeni(dom, 'r');
  r.appendChild(rPr(dom, ozellik));
  const t = yeni(dom, 't');
  t.setAttribute('xml:space', 'preserve');
  t.appendChild(dom.createTextNode(yazi));
  r.appendChild(t);
  return r;
}

// "**kalın** normal".split('**') → run dizisi (tek sıradaki parçalar kalın)
function parcaRunlari(dom, parcalar, temel) {
  const s = [];
  parcalar.forEach((p, i) => { if (p.length) s.push(run(dom, p, { ...temel, kalin: temel.kalin || i % 2 === 1 })); });
  return s;
}

// Paragraf özellikleri (öğe sırası OOXML şemasına uygun: keepNext, pageBreakBefore, numPr, spacing, ind, jc, rPr)
function pPr(dom, { hiza = 'both', once = 0, sonra = 60, satir = 240, sol = 0, sag = 0, asili = 0, numId = null, sayfaBolme = false, birlikte = false, butun = false, boyut = 18 } = {}) {
  const p = yeni(dom, 'pPr');
  if (birlikte) p.appendChild(yeni(dom, 'keepNext'));
  if (butun) p.appendChild(yeni(dom, 'keepLines'));   // paragrafın satırları sayfa arasında bölünmesin
  if (sayfaBolme) p.appendChild(yeni(dom, 'pageBreakBefore'));
  p.appendChild(yeni(dom, 'widowControl'));   // sayfa sonunda / başında tek satır kalmasın
  if (numId != null) {
    const np = yeni(dom, 'numPr');
    np.appendChild(yeni(dom, 'ilvl', { val: 0 }));
    np.appendChild(yeni(dom, 'numId', { val: numId }));
    p.appendChild(np);
  }
  p.appendChild(yeni(dom, 'spacing', { before: once, after: sonra, line: satir, lineRule: 'auto' }));
  if (sol || sag || asili) p.appendChild(yeni(dom, 'ind', { left: sol, right: sag, hanging: asili }));
  p.appendChild(yeni(dom, 'jc', { val: hiza }));
  p.appendChild(rPr(dom, { boyut }));   // paragraf işareti de Arial 9 olsun (son satırın yüksekliği eşit kalsın)
  return p;
}

// Blok türü: 'p' (paragraf / başı kalın paragraf), 'm' (madde), 'b' (kalın başlık), 'k' (küçük not)
function blokTuru(blok) {
  if (typeof blok === 'string') return 'p';
  if (blok.r != null) return 'p';
  if (blok.m != null) return 'm';
  if (blok.b != null) return 'b';
  if (blok.k != null) return 'k';
  throw new Error('Bilinmeyen blok: ' + JSON.stringify(blok));
}

// Bir içerik bloğunu (bkz. icerik.js) paragrafa çevirir
function paragraf(dom, blok, { maddeNumId = null, hiza = 'both', once = 0, sonra = 60, sol = 0, sag = 0, maddeSol = 284, butun = false } = {}) {
  const p = yeni(dom, 'p');
  const tur = blokTuru(blok);
  const yazi = typeof blok === 'string' ? blok : (blok.r ?? blok.m ?? blok.b ?? blok.k);
  let temel = {}, ozellik = { hiza, once, sonra, sol, sag, butun };
  if (tur === 'm') ozellik = { ...ozellik, numId: maddeNumId, sol: sol + maddeSol, asili: maddeNumId != null ? 284 : 0 };
  if (tur === 'b') { temel = { kalin: true }; ozellik = { ...ozellik, hiza: 'left', birlikte: true }; }
  if (tur === 'k') { temel = { italik: true, boyut: 16 }; ozellik = { ...ozellik, hiza: 'left', boyut: 16 }; }
  p.appendChild(pPr(dom, ozellik));
  parcaRunlari(dom, String(yazi).split('**'), temel).forEach(r => p.appendChild(r));
  return p;
}

module.exports = { W, yukle, yaz, cocuklar, ilkCocuk, hepsi, metin, govde, ustOgeler, yeni, rPr, run, pPr, paragraf, parcaRunlari, blokTuru };
