(() => {
  "use strict";
  let enabled = true;
  const publish = () => window.dispatchEvent(new CustomEvent("beterenter:settings", { detail: enabled }));
  window.addEventListener("beterenter:request-settings", publish);
  chrome.storage.local.get({ enabled: true }, settings => { enabled = settings.enabled !== false; publish(); });
  chrome.storage.onChanged.addListener((changes, area) => {
    if (area === "local" && changes.enabled) { enabled = changes.enabled.newValue !== false; publish(); }
  });
})();
