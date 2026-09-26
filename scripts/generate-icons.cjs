const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const root = path.resolve(__dirname, '..');

function ico(frames) {
  const header = Buffer.alloc(6 + 16 * frames.length);
  header.writeUInt16LE(1, 2); header.writeUInt16LE(frames.length, 4);
  let offset = header.length;
  frames.forEach(({ size, png }, i) => {
    const p = 6 + i * 16;
    header[p] = header[p + 1] = size === 256 ? 0 : size;
    header.writeUInt16LE(1, p + 4); header.writeUInt16LE(32, p + 6);
    header.writeUInt32LE(png.length, p + 8); header.writeUInt32LE(offset, p + 12);
    offset += png.length;
  });
  return Buffer.concat([header, ...frames.map(frame => frame.png)]);
}

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  try {
    fs.mkdirSync(path.join(root, 'chrome-extension/icons'), { recursive: true });
    // Render the wordmark PNG directly from its outlined SVG, without a font.
    const wordmark = fs.readFileSync(path.join(root, 'assets/logo.svg'), 'utf8');
    const [, width, height] = wordmark.match(/width="(\d+)" height="(\d+)"/);
    const logoPage = await browser.newPage({ viewport: { width: Number(width) * 2, height: Number(height) * 2 }, deviceScaleFactor: 1 });
    await logoPage.setContent(`<style>html,body{margin:0;background:transparent}svg{display:block;width:100vw;height:100vh}</style>${wordmark}`);
    await logoPage.screenshot({ omitBackground: true, path: path.join(root, 'assets/logo.png') });
    await logoPage.close();
    for (const [sourceName, outputName] of [['icon', 'logo'], ['icon-paused', 'logo-paused']]) {
      const svg = fs.readFileSync(path.join(root, 'assets', sourceName + '.svg'), 'utf8');
      const frames = [];
      for (const size of [16, 20, 24, 32, 40, 48, 64, 128, 256]) {
        const page = await browser.newPage({ viewport: { width: size, height: size }, deviceScaleFactor: 1 });
        await page.setContent(`<style>html,body{margin:0;background:transparent}svg{display:block;width:100vw;height:100vh}</style>${svg}`);
        const png = await page.screenshot({ omitBackground: true });
        frames.push({ size, png });
        if (sourceName === 'icon') {
          if ([16, 32, 48, 128].includes(size)) fs.writeFileSync(path.join(root, 'chrome-extension/icons', `icon-${size}.png`), png);
          if (size === 256) fs.writeFileSync(path.join(root, 'assets/icon.png'), png);
        }
        await page.close();
      }
      fs.writeFileSync(path.join(root, 'assets', outputName + '.ico'), ico(frames));
    }
    console.log('Generated Windows multi-size icons and Chrome icons from SVG.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
