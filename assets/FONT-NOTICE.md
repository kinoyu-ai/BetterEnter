# Wordmark typography

The product is named **BeterEnter**. Its wordmark reads **Better↩**.

The wordmark and compact B↩ icons use Inter, weight 600 (SemiBold), optical size 32. All glyphs are converted to SVG paths; rendered SVGs and PNGs do not require an installed font.

Source: https://github.com/rsms/inter

Font snapshot: commit `353b61b9f4430d5f420d56605a6e7993e0941470`, `docs/font-files/InterVariable.ttf`.

The font file is distributed in the source package under SIL Open Font License 1.1, with the full notice in `fonts/OFL.txt`. The project's MIT license does not replace the font's license. SF Pro is not used or bundled.

To regenerate SVG paths, install Python FontTools and run `python scripts/generate-wordmark.py`. Then run `npm run icons` to convert the SVGs into transparent PNGs and Windows ICO files. Normal builds use committed assets and do not require FontTools.
