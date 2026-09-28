(() => {
  "use strict";

  if (!["chatgpt.com", "chat.openai.com"].includes(location.hostname)) return;

  let enabled = true;
  let sending = false;
  const composerSelector = '#prompt-textarea[contenteditable="true"], #prompt-textarea[contenteditable="plaintext-only"], textarea#prompt-textarea';
  const sendButtonSelector = [
    'button[data-testid="send-button"]',
    'button[data-testid="composer-submit-button"]',
    'button[aria-label="Send prompt"]',
    'button[aria-label="Send message"]',
    'button[aria-label="プロンプトを送信"]',
    'button[aria-label="メッセージを送信"]'
  ].join(",");

  function composerOf(event) {
    for (const node of event.composedPath()) {
      if (node instanceof Element) {
        const editor = node.closest(composerSelector);
        if (editor) return editor;
      }
    }
    return null;
  }

  function insertLineBreak(editor) {
    editor.focus();
    if (document.execCommand("insertLineBreak", false)) return;
    if (document.execCommand("insertText", false, "\n")) return;

    if (editor instanceof HTMLTextAreaElement) {
      const start = editor.selectionStart;
      const end = editor.selectionEnd;
      editor.setRangeText("\n", start, end, "end");
      editor.dispatchEvent(new InputEvent("input", {
        bubbles: true,
        composed: true,
        inputType: "insertLineBreak",
        data: null
      }));
    }
  }

  function send(editor) {
    const form = editor.closest("form");
    const scope = form || document;
    const button = scope.querySelector(sendButtonSelector);
    if (button instanceof HTMLButtonElement && !button.disabled && button.getAttribute("aria-disabled") !== "true") {
      button.click();
      return;
    }
    if (form && typeof form.requestSubmit === "function") form.requestSubmit();
  }

  window.addEventListener("beterenter:settings", event => {
    enabled = event.detail !== false;
    sending = false;
  });
  window.dispatchEvent(new Event("beterenter:request-settings"));

  // Stop ChatGPT's Enter handler before it can submit. A normal Enter is
  // applied as an edit; Ctrl+Enter invokes only the composer's send action.
  window.addEventListener("keydown", event => {
    if (!enabled || event.key !== "Enter" || event.altKey || event.metaKey) return;
    const editor = composerOf(event);
    if (!editor || editor.getAttribute("aria-disabled") === "true" || editor.disabled || editor.readOnly) return;

    // Keep Enter available to confirm an active IME conversion.
    if (event.isComposing || event.keyCode === 229) return;

    event.preventDefault();
    event.stopImmediatePropagation();

    if (!event.ctrlKey) {
      insertLineBreak(editor);
      return;
    }
    if (event.repeat || sending) return;

    sending = true;
    queueMicrotask(() => {
      try { send(editor); }
      finally { sending = false; }
    });
  }, true);
})();
