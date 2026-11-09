"""ART-03: original fictional, faceted SVG portraits; no reference photographs."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "assets/portraits"
# Deliberate character designs, not appearance inferred from a surname or nationality.
CAST = {
    "ruiz": ("b98d6c", "383a32", "556e65", "side", False, False),
    "cole": ("dfb79a", "895d45", "657783", "bun", True, False),
    "whitfield": ("956f56", "b5b1a2", "68796d", "crop", False, True),
    "mills": ("c99f7c", "4c4339", "7e8b71", "curl", False, False),
    "hale": ("c3977c", "5e5a50", "6b7864", "crop", True, True),
    "bell": ("8d644e", "2e3431", "727d69", "crop", False, False),
    "park": ("bfa084", "494a43", "536c71", "side", True, False),
    "nair": ("b2876a", "3e3733", "73847c", "bun", False, False),
    "delgado": ("d3ab8d", "875844", "9d7759", "wave", True, False),
    "mensah": ("76543f", "303632", "718995", "crop", True, True),
    "okafor": ("996e53", "343c35", "8b9173", "curl", False, False),
    "brandt": ("e0ba9f", "b7a179", "687f6d", "side", False, False),
    "lin": ("c3a283", "484237", "66717d", "wave", True, False),
    "ortiz": ("a98065", "685245", "8b8f74", "bun", True, False),
    "dunn": ("d0a28a", "a7a18f", "596e79", "crop", False, True),
    "ward": ("c7ae98", "b0afa6", "72836e", "wave", True, False),
    "reyna": ("c29371", "584837", "956d56", "side", False, True),
}

def portrait(key, spec):
    skin, hair, shirt, cut, glasses, beard = spec
    shapes = f'''<rect width="256" height="320" rx="20" fill="#ded3bd"/>
<path d="M0 245L256 55V320H0Z" fill="#c9c7b1"/>
<path d="M12 320L29 260L91 224H165L228 260L247 320Z" fill="#{shirt}"/>
<path d="M126 232L150 320H247L228 260L165 224Z" fill="#343d4b" opacity=".16"/>
<path d="M103 182H153V244L128 264L103 244Z" fill="#{skin}"/>
<path d="M69 108L86 68L128 53L171 72L188 112L177 175L153 211L125 224L91 204L74 170Z" fill="#{skin}"/>
<path d="M128 77L178 109L175 172L151 208L128 220Z" fill="#714f3b" opacity=".18"/>
<path d="M80 131L105 127M151 127L174 131" stroke="#3b403a" stroke-width="5" stroke-linecap="round"/>
<path d="M128 139L120 170L133 172" fill="none" stroke="#805e49" stroke-width="4"/>
<path d="M110 191Q128 199 146 190" fill="none" stroke="#785343" stroke-width="4"/>
<path d="M90 238L122 269L108 286L75 247M166 238L134 269L149 286L181 247" fill="#eee6ce"/>
<path d="M128 269V320" stroke="#e1dcc7" stroke-width="3"/>
<path d="M45 287H85M172 287H212" stroke="#374b47" stroke-opacity=".35" stroke-width="4"/>'''
    if cut == "crop": shapes += f'<path d="M68 117L70 88L91 64L129 56L166 65L186 88L188 117L172 90L91 91Z" fill="#{hair}"/>'
    elif cut == "side": shapes += f'<path d="M65 132L66 87L95 57L149 53L183 80L190 133L165 91L105 111L86 101Z" fill="#{hair}"/>'
    elif cut == "bun": shapes += f'<circle cx="166" cy="66" r="35" fill="#{hair}"/><path d="M65 136L67 90L92 59L134 50L176 74L189 131L164 94L136 91L89 111L80 140Z" fill="#{hair}"/>'
    elif cut == "curl":
        shapes += ''.join(f'<circle cx="{x}" cy="{y}" r="23" fill="#{hair}"/>' for x, y in [(76, 100), (86, 77), (112, 63), (139, 61), (164, 74), (178, 98)])
    else: shapes += f'<path d="M62 174L63 101Q60 48 125 48Q193 48 194 107L197 178L175 161L170 100L143 91L105 108L84 113L80 178Z" fill="#{hair}"/>'
    if glasses: shapes += '<g fill="none" stroke="#444d47" stroke-width="4"><rect x="79" y="119" width="35" height="24" rx="7"/><rect x="143" y="119" width="35" height="24" rx="7"/><path d="M114 127H143"/></g>'
    if beard: shapes += f'<path d="M80 169L100 192L126 203L152 190L177 167L167 200L128 226L91 209Z" fill="#{hair}" opacity=".72"/>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="320" viewBox="0 0 256 320"><title>Original fictional cast study: {key}</title>{shapes}</svg>\n'

if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for key, spec in CAST.items(): (OUT / f"{key}.svg").write_text(portrait(key, spec), encoding="utf-8")
    (OUT / "fallback.svg").write_text('<svg xmlns="http://www.w3.org/2000/svg" width="256" height="320"><rect width="256" height="320" rx="20" fill="#ded3bd"/><circle cx="128" cy="118" r="52" fill="#a69e8d"/><path d="M28 320L47 245L100 215H156L209 245L228 320Z" fill="#6f8071"/></svg>\n', encoding="utf-8")
