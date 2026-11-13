"""ART-03 / ART-17: original fictional, faceted SVG portraits; no reference photographs."""
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

# angle, horizontal head offset, shoulder stance, collar, expression, gaze.
# Authored staging keeps the established palette, hair, glasses and beard identities.
STAGING = {
    "ruiz": (-3, -3, "level", "open", "focused", 1),
    "cole": (6, 5, "right", "round", "kind", -2),
    "whitfield": (-7, -5, "left", "stand", "serious", 2),
    "mills": (4, 0, "right", "rolled", "smile", -1),
    "hale": (0, 3, "level", "knit", "steady", 1),
    "bell": (-4, 2, "left", "zip", "smile", 3),
    "park": (7, -3, "right", "open", "curious", 0),
    "nair": (-6, 5, "level", "round", "kind", -2),
    "delgado": (5, -4, "left", "stand", "smile", 2),
    "mensah": (-2, 1, "right", "zip", "serious", -2),
    "okafor": (8, 0, "level", "rolled", "kind", 1),
    "brandt": (-8, 0, "left", "knit", "focused", 0),
    "lin": (2, -2, "right", "stand", "curious", -2),
    "ortiz": (-5, -3, "level", "open", "kind", 2),
    "dunn": (3, 4, "left", "zip", "steady", -1),
    "ward": (0, -4, "right", "round", "smile", 3),
    "reyna": (-1, 4, "level", "rolled", "curious", -2),
}
COLLARS = {
    "open": '<path d="M93 232L125 259L108 281L76 246M163 232L131 259L151 280L185 246" fill="#eee6ce"/><path d="M128 262V320" stroke="#ded6bf" stroke-width="3"/>',
    "round": '<path d="M94 235Q128 274 163 235L177 246Q128 292 80 247Z" fill="#ece2c9"/>',
    "stand": '<path d="M100 231L125 242V265L94 252ZM156 231L131 242V265L162 252Z" fill="#eee6ce"/><path d="M128 250V320" stroke="#ded6bf" stroke-width="3"/>',
    "rolled": '<path d="M89 233L121 258L109 268L76 245ZM167 233L135 258L148 268L183 245Z" fill="#eee6ce"/><path d="M80 244L111 267M177 244L147 267" fill="none" stroke="#c5bca4" stroke-width="4"/>',
    "knit": '<path d="M92 235Q128 268 164 235L170 246Q128 281 86 246Z" fill="#c6c6ad"/><path d="M95 245Q128 270 161 245" fill="none" stroke="#eee6ce" stroke-width="3"/>',
    "zip": '<path d="M94 233L128 257L163 233L171 246L128 277L85 246Z" fill="#eee6ce"/><path d="M128 268V320" stroke="#d5cbb6" stroke-width="4"/><path d="M127 279V292" stroke="#505c53" stroke-width="5"/>',
}
SHOULDERS = {
    "level": "M12 320L29 260L91 224H165L228 260L247 320Z",
    "left": "M7 320L24 273L86 239L108 226L163 229L226 253L252 320Z",
    "right": "M6 320L30 253L93 229L149 226L170 239L232 273L251 320Z",
}
EXPRESSIONS = {
    "focused": ("M84 118L110 121M147 121L173 117", "M110 191Q128 194 146 190"),
    "kind": ("M84 119Q97 111 110 118M147 118Q160 112 173 120", "M108 188Q128 205 149 187"),
    "serious": ("M84 121L110 119M147 119L173 121", "M110 194Q128 190 146 194"),
    "smile": ("M84 117Q97 111 110 117M147 117Q160 111 173 117", "M105 187Q128 211 151 186"),
    "curious": ("M84 115Q97 108 110 114M147 121L173 119", "M110 192Q130 202 149 187"),
    "steady": ("M84 119L110 119M147 119L173 119", "M110 192Q128 199 146 192"),
}


def portrait(key, spec):
    skin, hair, shirt, cut, glasses, beard = spec
    angle, offset, stance, collar, expression, gaze = STAGING[key]
    brows, mouth = EXPRESSIONS[expression]
    shapes = f"""<rect width="256" height="320" rx="20" fill="#ded3bd"/>
<path d="M0 245L256 55V320H0Z" fill="#c9c7b1"/>
<path d="{SHOULDERS[stance]}" fill="#{shirt}"/>
<path d="M126 232L150 320H247L228 260L165 224Z" fill="#343d4b" opacity=".16"/>
<path d="M103 182H153V244L128 264L103 244Z" fill="#{skin}"/>
{COLLARS[collar]}
<g transform="translate({offset} 0) rotate({angle} 128 212)">
<path d="M69 108L86 68L128 53L171 72L188 112L177 175L153 211L125 224L91 204L74 170Z" fill="#{skin}"/>
<path d="M128 77L178 109L175 172L151 208L128 220Z" fill="#714f3b" opacity=".18"/>
<path d="{brows}" fill="none" stroke="#3b403a" stroke-width="4" stroke-linecap="round"/>
"""
    # Broad whites and dark pupils remain open at small desk portrait sizes.
    for x in (98, 160):
        shapes += f'<ellipse cx="{x}" cy="135" rx="14" ry="9" fill="#eeeede" stroke="#3b403a" stroke-width="2"/><circle cx="{x + gaze}" cy="135" r="5" fill="#303a34"/>'
    shapes += '<path d="M128 144L120 170L133 172" fill="none" stroke="#805e49" stroke-width="4"/>'
    if beard:
        shapes += f'<path d="M80 169L100 192L126 203L152 190L177 167L167 200L128 226L91 209Z" fill="#{hair}" opacity=".72"/>'
    shapes += f'<path d="{mouth}" fill="none" stroke="#674a3c" stroke-width="4" stroke-linecap="round"/>'
    if cut == "crop":
        shapes += f'<path d="M68 117L70 88L91 64L129 56L166 65L186 88L188 117L172 90L91 91Z" fill="#{hair}"/>'
    elif cut == "side":
        shapes += f'<path d="M65 132L66 87L95 57L149 53L183 80L190 133L165 91L105 111L86 101Z" fill="#{hair}"/>'
    elif cut == "bun":
        shapes += f'<circle cx="166" cy="66" r="35" fill="#{hair}"/><path d="M65 136L67 90L92 59L134 50L176 74L189 131L164 94L136 91L89 111L80 140Z" fill="#{hair}"/>'
    elif cut == "curl":
        shapes += ''.join(f'<circle cx="{x}" cy="{y}" r="23" fill="#{hair}"/>' for x, y in [(76, 100), (86, 77), (112, 63), (139, 61), (164, 74), (178, 98)])
    else:
        shapes += f'<path d="M62 174L63 101Q60 48 125 48Q193 48 194 107L197 178L175 161L170 100L143 91L105 108L84 113L80 178Z" fill="#{hair}"/>'
    if glasses:
        shapes += '<g fill="none" stroke="#444d47" stroke-width="4"><rect x="77" y="122" width="41" height="28" rx="7"/><rect x="139" y="122" width="41" height="28" rx="7"/><path d="M118 135H139"/></g>'
    shapes += '</g>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="320" viewBox="0 0 256 320"><title>Original fictional tabletop cast: {key}</title>{shapes}</svg>\n'


def fallback_portrait():
    """An unassigned silhouette, never a shared fictional character's face."""
    shapes = f"""<rect width="256" height="320" rx="20" fill="#ded3bd"/>
<path d="M0 245L256 55V320H0Z" fill="#c9c7b1"/>
<path d="{SHOULDERS['level']}" fill="#6f8071"/>
<path d="M126 232L150 320H247L228 260L165 224Z" fill="#343d37" opacity=".12"/>
<path d="M103 182H153V244L128 264L103 244Z" fill="#737d71"/>
{COLLARS['knit']}
<path d="M69 108L86 68L128 53L171 72L188 112L177 175L153 211L125 224L91 204L74 170Z" fill="#737d71"/>"""
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="320" viewBox="0 0 256 320"><title>Unassigned tabletop portrait silhouette</title>{shapes}</svg>\n'


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for key, spec in CAST.items():
        (OUT / f"{key}.svg").write_text(portrait(key, spec), encoding="utf-8")
    (OUT / "fallback.svg").write_text(fallback_portrait(), encoding="utf-8")
