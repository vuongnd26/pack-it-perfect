// Original UI illustrations, kept separate from gameplay and item silhouettes.
// node ui-art-build.js <resvg runtime directory> <existing empty absolute staging directory>
const fs = require('fs'), path = require('path');
const [,, runtime, output] = process.argv;
// Generators only stage candidates. In-project or non-empty destinations must never be overwritten.
if (!runtime || !output || !path.isAbsolute(output) || !fs.existsSync(output) || !fs.statSync(output).isDirectory())
  throw Error('Usage: node ui-art-build.js <resvg runtime directory> <existing empty absolute staging directory>');
const destination = fs.realpathSync(output), project = fs.realpathSync(path.join(__dirname, '../..'));
const relative = path.relative(project, destination);
if (!relative || (relative !== '..' && !relative.startsWith('..' + path.sep) && !path.isAbsolute(relative)))
  throw Error('Output must be scratch staging outside the project; promote reviewed PNGs separately.');
if (fs.readdirSync(destination).length) throw Error('Output staging directory must be empty.');
const { Resvg } = require(path.join(runtime, 'node_modules/@resvg/resvg-js'));
const ink = '#49394D';
const svg = (w,h,body,defs='') => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${body}</svg>`;
const stroke = `stroke="${ink}" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"`;
const symbolStroke = `stroke="${ink}" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"`;
const rule = body => svg(128,128,`<circle cx="64" cy="64" r="58" fill="#FFF5DF"/>${body}`);
const icons = {};
icons.ui_home = svg(128,128,`<path d="M24,62 V111 H104 V62" fill="#FFE9B9" ${stroke}/><path d="M12,64 L64,17 L116,64 L102,78 L64,43 L26,78Z" fill="#F28B78" ${stroke}/><rect x="52" y="77" width="25" height="35" rx="5" fill="#83C6BF" ${stroke}/><path d="M29,53 L64,23 L100,54" fill="none" stroke="#FFCAB0" stroke-width="5"/>`);
// Large bulb, one filament and one base; no tiny cap stripes or decorative rays.
icons.ui_hint = svg(128,128,`<path d="M43,86 C43,72 24,65 24,44 C24,4 104,4 104,44 C104,65 85,73 85,86Z" fill="url(#gold)" ${stroke}/><path d="M44,88 H84 V101 Q64,120 44,101Z" fill="${ink}"/><path d="M57,81 V61 L48,49 M57,61 H71 L80,49 M71,61 V81" fill="none" stroke="${ink}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/><path d="M39,43 Q40,26 55,25" fill="none" stroke="#FFF8D0" stroke-width="8" stroke-linecap="round"/>`, '<linearGradient id="gold" x2=".6" y2="1"><stop stop-color="#FFF3AA"/><stop offset="1" stop-color="#FFBC42"/></linearGradient>');
icons.ui_restart = svg(128,128,`<path d="M92,35 A43,43 0 1 0 105,85" fill="none" stroke="${ink}" stroke-width="24" stroke-linecap="round"/><path d="M92,35 A43,43 0 1 0 105,85" fill="none" stroke="#74BECA" stroke-width="13" stroke-linecap="round"/><path d="M77,13 L114,46 L72,53Z" fill="#A8DFDF" ${stroke}/>`);
icons.ui_star = svg(160,160,`<path d="M80,10 L101,53 L149,60 L114,94 L122,143 L80,120 L38,143 L46,94 L11,60 L59,53Z" fill="url(#gold)" ${stroke}/><path d="M39,63 L64,59 L80,27 L94,59 L127,65" fill="none" stroke="#FFF5BD" stroke-width="7" stroke-linejoin="round"/>`, '<linearGradient id="gold" x2=".3" y2="1"><stop stop-color="#FFE991"/><stop offset="1" stop-color="#F8B94B"/></linearGradient>');
icons.ui_lock = svg(128,128,`<path d="M35,57 V35 C35,2 94,2 94,35 V57" fill="none" stroke="${ink}" stroke-width="18"/><path d="M35,57 V35 C35,2 94,2 94,35 V57" fill="none" stroke="#C3CDD8" stroke-width="9"/><rect x="19" y="52" width="90" height="66" rx="17" fill="#D7C5A5" ${stroke}/><circle cx="64" cy="79" r="11" fill="${ink}"/><path d="M64,85 V98" stroke="${ink}" stroke-width="10" stroke-linecap="round"/>`);
icons.ui_clock = svg(160,160,`<path d="M61,12 H99 M80,12 V29" fill="none" ${stroke}/><circle cx="80" cy="91" r="60" fill="#F3B18A" ${stroke}/><circle cx="80" cy="91" r="46" fill="#FFF7DC"/><path d="M80,58 V91 L103,105" fill="none" ${stroke}/><path d="M80,47 V53 M80,128 V135 M36,91 H42 M119,91 H125" ${stroke}/>`);
icons.ui_ribbon = svg(800,160,`<path d="M88,38 L8,29 L32,84 L8,140 L140,123 L129,49 M712,38 L792,29 L768,84 L792,140 L660,123 L671,49" fill="#278B88" ${stroke}/><path d="M106,18 Q400,0 694,18 L709,118 Q400,137 91,118Z" fill="#59C4B1" ${stroke}/><path d="M121,32 Q400,16 680,32" fill="none" stroke="#BBEEDA" stroke-width="7" stroke-linecap="round"/>`);
// Keep dimensions/corners compatible with the existing 84-pixel ui_card slicing border.
icons.ui_card = svg(192,192,`<rect x="6" y="12" width="180" height="174" rx="33" fill="#CEAC85"/><rect x="6" y="6" width="180" height="170" rx="33" fill="#FFF5DF" ${stroke}/><path d="M42,16 H150" fill="none" stroke="#FFFCF1" stroke-width="5" stroke-linecap="round"/>`);
// Cozy Travel: keep the existing palette, but give each booster one dominant, recognizable silhouette.
icons.ui_undo = svg(128,128,`<path d="M53,20 L12,55 L53,89 V70 H75 Q102,70 98,108 Q124,72 104,51 Q94,40 74,40 H53Z" fill="#83C6BF" ${stroke}/>`);
icons.ui_autopack = svg(128,128,`<path d="M33,48 V33 Q33,26 40,26 H61 V48" fill="none" stroke="${ink}" stroke-width="9" stroke-linejoin="round"/><rect x="14" y="48" width="100" height="66" rx="15" fill="#F3B18A" ${stroke}/><path d="M26,61 H65" stroke="#FFD6B0" stroke-width="7" stroke-linecap="round"/><path d="M91,7 L98,25 L116,32 L98,39 L91,57 L84,39 L66,32 L84,25Z" fill="#FFE991" ${stroke}/>`);
icons.ui_freeze = svg(128,128,`<circle cx="64" cy="64" r="58" fill="#D5F1F5"/><path d="M64,22 V106 M28,43 L100,85 M28,85 L100,43 M52,29 L64,41 L76,29 M52,99 L64,87 L76,99 M29,57 L45,53 L43,37 M99,71 L83,75 L85,91 M29,71 L45,75 L43,91 M99,57 L83,53 L85,37" fill="none" ${symbolStroke}/>`);
// Count backing: deliberately blank, opaque and quiet so a runtime dark number is the only central mark.
icons.ui_badge = svg(128,128,`<rect x="10" y="18" width="108" height="104" rx="28" fill="#E9D5B6"/><rect x="10" y="8" width="108" height="104" rx="28" fill="#FFF5DF" stroke="${ink}" stroke-width="6"/>`);
// One chunky cream face and a short lower edge; no dashed inset or doubled inner border.
icons.ui_slot = svg(128,128,`<rect x="10" y="16" width="108" height="102" rx="22" fill="#E9D5B6"/><rect x="10" y="10" width="108" height="102" rx="22" fill="#FFF5DF" stroke="${ink}" stroke-width="6"/>`);
icons.ui_pause = svg(128,128,`<circle cx="64" cy="64" r="53" fill="#FFF5DF" ${stroke}/><rect x="38" y="32" width="19" height="64" rx="6" fill="${ink}"/><rect x="71" y="32" width="19" height="64" rx="6" fill="${ink}"/>`);
// Opaque cream backing and a single dark symbol make rules readable without a colour-only cue.
icons.ui_heavy = rule(`<path d="M44,40 V28 C44,1 84,1 84,28 V40 H92 L110,108 H18 L36,40Z M55,40 H73 V28 C73,15 55,15 55,28Z" fill="${ink}" fill-rule="evenodd"/>`);
icons.ui_upright = rule(`<path d="M64,16 L31,51 H51 V86 H77 V51 H97Z" fill="${ink}"/><path d="M26,105 H102" ${symbolStroke}/>`);
icons.ui_pair = rule(`<rect x="17" y="29" width="56" height="43" rx="21" transform="rotate(-35 45 50)" fill="none" ${symbolStroke}/><rect x="55" y="56" width="56" height="43" rx="21" transform="rotate(-35 83 78)" fill="none" ${symbolStroke}/><path d="M49,74 L79,53" ${symbolStroke}/>`);
icons.ui_fragile = rule(`<path d="M33,19 H95 L89,55 Q84,77 64,77 Q44,77 39,55Z" fill="${ink}"/><path d="M64,73 V106 M43,109 H85" fill="none" ${symbolStroke}/><path d="M67,24 L55,41 L70,47 L59,63" fill="none" stroke="#FFF5DF" stroke-width="7" stroke-linecap="round" stroke-linejoin="round"/>`);
icons.ui_chest = svg(128,128,`<path d="M14,59 Q14,19 43,19 H85 Q114,19 114,59 V110 H14Z" fill="#C98E6B" ${stroke}/><path d="M34,23 V109 M94,23 V109" stroke="#FFE991" stroke-width="10"/><path d="M14,59 H114" ${stroke}/><rect x="51" y="51" width="26" height="31" rx="5" fill="#FFE991" ${stroke}/><circle cx="64" cy="65" r="4" fill="${ink}"/>`);
for (const [id, source] of Object.entries(icons)) fs.writeFileSync(path.join(destination,id+'.png'), new Resvg(source,{fitTo:{mode:'zoom',value:2}}).render().asPng());
console.log(`Staged ${Object.keys(icons).length} original UI assets; no project assets imported or changed.`);
