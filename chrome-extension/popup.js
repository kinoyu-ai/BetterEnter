"use strict";
const toggle = document.getElementById("enabled");
const status = document.getElementById("status");
chrome.storage.local.get({ enabled: true }, settings => {
  toggle.checked = settings.enabled !== false;
  toggle.disabled = false;
});
toggle.addEventListener("change", () => {
  chrome.storage.local.set({ enabled: toggle.checked }, () => {
    status.textContent = chrome.runtime.lastError ? "保存できませんでした。もう一度お試しください。" : toggle.checked ? "有効です。開いている ChatGPT にも反映されます。" : "一時停止しました。";
  });
});
