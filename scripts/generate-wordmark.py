"""Generate font-independent Better-return SVGs from the bundled Inter font."""
from math import ceil
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen

root = Path(__file__).resolve().parent.parent
font = instantiateVariableFont(TTFont(root / 'assets/fonts/InterVariable.ttf'), {'wght': 600, 'opsz': 32}, inplace=True)
glyphs = font.getGlyphSet()
cmap = font.getBestCmap()
upm = font['head'].unitsPerEm

def outline(character, size, x, baseline):
    pen = SVGPathPen(glyphs)
    glyph = cmap[ord(character)]
    glyphs[glyph].draw(TransformPen(pen, (size / upm, 0, 0, -size / upm, x, baseline)))
    return pen.getCommands(), font['hmtx'][glyph][0] * size / upm

paths = []
x = 10
for character in 'Better':
    data, advance = outline(character, 92, x, 85)
    paths.append(f'  <path d="{data}" fill="#15191e"/>')
    x += advance - 1.1
x += 10
data, advance = outline('\u21a9', 92, x, 85)
paths.append(f'  <path d="{data}" fill="#0e9f85"/>')
width = ceil(x + advance + 10)
svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="108" viewBox="0 0 {width} 108" role="img" aria-label="Better\u21a9 — BeterEnter">\n' + '\n'.join(paths) + '\n</svg>\n'
(root / 'assets/logo.svg').write_text(svg, encoding='utf-8')
(root / 'assets/logo-dark.svg').write_text(svg.replace('#15191e', '#f6f7f9'), encoding='utf-8')

letter, _ = outline('B', 34, 7, 44)
arrow, _ = outline('\u21a9', 27, 29, 42)
for name, background in [('icon', '#0e9f85'), ('icon-paused', '#717b87')]:
    icon = f'''<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64" role="img" aria-label="BeterEnter{(' paused' if name.endswith('paused') else '')}">
  <rect x="2" y="2" width="60" height="60" rx="16" fill="{background}"/>
  <path d="{letter}" fill="#fff"/>
  <path d="{arrow}" fill="#fff"/>
</svg>
'''
    (root / f'assets/{name}.svg').write_text(icon, encoding='utf-8')
(root / 'chrome-extension/wordmark.svg').write_text(svg, encoding='utf-8')
(root / 'chrome-extension/wordmark-dark.svg').write_text(svg.replace('#15191e', '#f6f7f9'), encoding='utf-8')
print(f'Generated outlined wordmark ({width} x 108) and compact tray SVGs from Inter SemiBold.')
