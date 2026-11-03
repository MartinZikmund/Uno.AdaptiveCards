// Runs the visualizer's interaction self-tests (?selftest) in headless Chromium and exits non-zero on failure.
// Usage: node wasm-selftest.js <url> [screenshot.png]
const { chromium } = require('playwright');
(async () => {
  const url = process.argv[2];
  // Media self-tests start playback programmatically; allow it without a user gesture.
  // PW_CHANNEL=chrome uses Google Chrome: the open-source Chromium build has no H.264 decoder for the mp4 test clip.
  const browser = await chromium.launch({
    channel: process.env.PW_CHANNEL || undefined,
    args: ['--autoplay-policy=no-user-gesture-required'],
  });
  const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });
  let done = null;
  page.on('console', m => {
    const t = m.text();
    if (t.includes('SELFTEST')) { console.log(t); if (t.includes('SELFTEST DONE')) done = t; }
  });
  page.on('pageerror', e => console.log('PAGE ERROR: ' + e.message));
  await page.goto(url + (url.includes('?') ? '&' : '?') + 'selftest', { timeout: 180000 });
  const start = Date.now();
  while (!done && Date.now() - start < 600000) await page.waitForTimeout(1000);
  if (process.argv[3]) await page.screenshot({ path: process.argv[3] });
  await browser.close();
  console.log(done || 'SELFTEST TIMEOUT');
  process.exit(done && done.includes('SELFTEST DONE OK') ? 0 : 1);
})();
