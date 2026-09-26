(() => {
  "use strict";
  if (!["chatgpt.com", "chat.openai.com"].includes(location.hostname)) return;
  let enabled = true;
  let pendingLineBreak = null;
  const composerSelector = '#prompt-textarea[contenteditable="true"], #prompt-textarea[contenteditable="plaintext-only"], textarea#prompt-textarea';

  function composerOf(event) {
    for (const node of event.composedPath()) {
      if (node instanceof Element) {
        const editor = node.closest(composerSelector);
        if (editor) return editor;
      }
    }
    return null;
  }

  window.addEventListener("beterenter:settings", event => {
    enabled = event.detail !== false;
    pendingLineBreak = null;
  });
  window.dispatchEvent(new Event("beterenter:request-settings"));

  function remap(event) {
    if (!enabled || event.key !== "Enter" || event.altKey || event.metaKey) return;
    const editor = composerOf(event);
    if (!editor || editor.getAttribute("aria-disabled") === "true" || editor.disabled || editor.readOnly) return;
    const wasCtrl = event.ctrlKey;
    if (event.type === "keydown") {
      pendingLineBreak = wasCtrl ? null : editor;
      if (wasCtrl && event.repeat) {
        event.preventDefault(); event.stopImmediatePropagation(); return;
      }
    }
    // MAIN world: change modifiers on the original trusted event before ChatGPT
    // receives it. Leave keyCode/isComposing and confirmation behavior alone.
    const nativeModifierState = event.getModifierState.bind(event);
    Object.defineProperties(event, {
      ctrlKey: { configurable: true, value: false },
      shiftKey: { configurable: true, value: !wasCtrl },
      getModifierState: { configurable: true, value: key => key === "Control" ? false : key === "Shift" ? !wasCtrl : nativeModifierState(key) }
    });
    if (event.type === "keyup") pendingLineBreak = null;
  }
  for (const type of ["keydown", "keypress", "keyup"]) window.addEventListener(type, remap, true);

  // DOM modifier changes do not change native browser default editing.
  // Normalize paragraph insertion to a real line break, preserving undo.
  // Composition input types are untouched; there is no IME state detector.
  window.addEventListener("beforeinput", event => {
    if (!enabled || event.inputType !== "insertParagraph" || !event.cancelable) return;
    const editor = composerOf(event);
    if (!editor || editor !== pendingLineBreak || editor instanceof HTMLTextAreaElement) return;
    pendingLineBreak = null;
    event.preventDefault(); event.stopImmediatePropagation();
    if (!document.execCommand("insertLineBreak", false)) document.execCommand("insertText", false, "\n");
  }, true);
  window.addEventListener("focusout", () => { pendingLineBreak = null; }, true);
})();
