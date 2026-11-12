// ardcek.com'un CSP başlığıyla bir klasörü /<önek>/ altında sunar (test için)
import http from "node:http";
import fs from "node:fs";
import path from "node:path";
const kok = path.resolve(process.argv[2]);
const onek = "/" + process.argv[3] + "/";
const port = Number(process.argv[4] || 5089);
const csp = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self' https://api.web3forms.com; frame-ancestors 'self'; base-uri 'self'; form-action 'self' https://api.web3forms.com";
const tur = { ".html": "text/html; charset=utf-8", ".css": "text/css", ".js": "text/javascript", ".jpg": "image/jpeg", ".png": "image/png", ".svg": "image/svg+xml", ".woff2": "font/woff2", ".woff": "font/woff", ".pdf": "application/pdf", ".txt": "text/plain; charset=utf-8" };
http.createServer((istek, yanit) => {
  let yol = decodeURIComponent(istek.url.split("?")[0]);
  if (!yol.startsWith(onek)) { yanit.writeHead(302, { Location: onek }); return yanit.end(); }
  yol = yol.slice(onek.length) || "index.html";
  const dosya = path.join(kok, yol);
  if (!dosya.startsWith(kok) || !fs.existsSync(dosya) || fs.statSync(dosya).isDirectory()) { yanit.writeHead(404); return yanit.end("404"); }
  yanit.writeHead(200, { "Content-Type": tur[path.extname(dosya)] || "application/octet-stream", "Content-Security-Policy": csp });
  fs.createReadStream(dosya).pipe(yanit);
}).listen(port, () => console.log("hazır " + port));
