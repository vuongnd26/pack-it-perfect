// Stages hand-authored environment vector art (original work for this project).
// Usage: node tools/asset-pipeline/art-build.js <resvg runtime directory> <existing empty absolute staging directory> <static Fredoka .ttf>
// Use the existing Assets/Fonts/Fredoka-SemiBold.ttf static face (SIL OFL 1.1); no font download is needed.
const fs=require('fs'),path=require('path');
const [,, resvgDir, outDir, fontFile]=process.argv;
if (!resvgDir || !outDir || !fontFile || !path.isAbsolute(outDir) || !fs.existsSync(outDir) || !fs.statSync(outDir).isDirectory())
  throw Error('Usage: node art-build.js <resvg runtime directory> <existing empty absolute staging directory> <static Fredoka .ttf>');
const destination=fs.realpathSync(outDir), project=fs.realpathSync(path.join(__dirname,'../..'));
const relative=path.relative(project,destination);
if (!relative || (relative !== '..' && !relative.startsWith('..'+path.sep) && !path.isAbsolute(relative)))
  throw Error('Output must be scratch staging outside the project; promote reviewed PNGs separately.');
if (fs.readdirSync(destination).length) throw Error('Output staging directory must be empty.');
const {Resvg}=require(path.join(resvgDir,'node_modules/@resvg/resvg-js'));
const INK='#2A2436';
const svg=(w,h,body,defs='')=>`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs>${body}</svg>`;
const A={};

// ---- container frame / lid (9-slice, tintable greyscale) ----
{
 const guard=(cx,cy)=>`<circle cx="${cx}" cy="${cy}" r="50" fill="#000" fill-opacity=".14"/>`;
 A.frame=svg(192,192,`
 <rect x="4.5" y="4.5" width="183" height="183" rx="36" fill="#DEDEDE" stroke="${INK}" stroke-width="9"/>
 <g clip-path="url(#in)">
  <rect x="0" y="9" width="192" height="13" fill="#fff"/>
  <rect x="0" y="166" width="192" height="20" fill="#000" fill-opacity=".2"/>
  ${guard(9,9)}${guard(183,9)}${guard(9,183)}${guard(183,183)}
 </g>`,
 `<clipPath id="in"><rect x="9" y="9" width="174" height="174" rx="31.5"/></clipPath>`);
}
// ---- inner shadow overlay (9-slice) ----
A.inset=svg(128,128,`<rect x="0" y="0" width="128" height="34" fill="url(#t)"/><rect x="0" y="112" width="128" height="16" fill="url(#b)"/>
 <rect x="0" y="0" width="18" height="128" fill="url(#l)"/><rect x="110" y="0" width="18" height="128" fill="url(#r)"/>`,
 `<linearGradient id="t" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#1A1020" stop-opacity=".42"/><stop offset="1" stop-color="#1A1020" stop-opacity="0"/></linearGradient>
  <linearGradient id="b" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#1A1020" stop-opacity=".14"/><stop offset="1" stop-color="#1A1020" stop-opacity="0"/></linearGradient>
  <linearGradient id="l" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#1A1020" stop-opacity=".22"/><stop offset="1" stop-color="#1A1020" stop-opacity="0"/></linearGradient>
  <linearGradient id="r" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="#1A1020" stop-opacity=".22"/><stop offset="1" stop-color="#1A1020" stop-opacity="0"/></linearGradient>`);
// ---- handle ----
{
 const p='M52,112 L52,54 Q52,26 80,26 L176,26 Q204,26 204,54 L204,112';
 A.handle=svg(256,112,`
 <path d="${p}" fill="none" stroke="${INK}" stroke-width="38" stroke-linejoin="round"/>
 <path d="${p}" fill="none" stroke="#6B7386" stroke-width="20" stroke-linejoin="round"/>
 <path d="M60,104 L60,54 Q60,34 80,34 L176,34 Q190,34 195,44" fill="none" stroke="#fff" stroke-opacity=".45" stroke-width="5" stroke-linecap="round"/>
 <rect x="26" y="84" width="52" height="34" rx="8" fill="#8A93A6" stroke="${INK}" stroke-width="8"/>
 <rect x="178" y="84" width="52" height="34" rx="8" fill="#8A93A6" stroke="${INK}" stroke-width="8"/>`);
}
// ---- wheel ----
A.wheel=svg(96,96,`<circle cx="48" cy="48" r="40" fill="#3B3F4E" stroke="${INK}" stroke-width="9"/>
 <circle cx="48" cy="48" r="17" fill="#C9CEDA" stroke="${INK}" stroke-width="6"/>
 <path d="M22,36 A30,30 0 0 1 48,18" fill="none" stroke="#fff" stroke-opacity=".35" stroke-width="6" stroke-linecap="round"/>`);
// ---- blocked cell (tinted by the theme block colour) ----
{
 let lines='';for(let i=-128;i<256;i+=52)lines+=`<line x1="${i}" y1="128" x2="${i+128}" y2="0" stroke="#fff" stroke-width="13"/>`;
 A.blocked=svg(128,128,`<rect x="4" y="4" width="120" height="120" rx="20" fill="#fff" fill-opacity=".62"/>
 <g clip-path="url(#c)" opacity=".4">${lines}</g>`,
 `<clipPath id="c"><rect x="4" y="4" width="120" height="120" rx="20"/></clipPath>`);
}
// ---- fill bar (9-slice) ----
A.bar_back=svg(96,48,`<rect x="3" y="3" width="90" height="42" rx="21" fill="#fff" stroke="${INK}" stroke-width="6"/>
 <rect x="8" y="8" width="80" height="12" rx="6" fill="#2A2436" fill-opacity=".1"/>`);
A.bar_fill=svg(96,48,`<rect x="7" y="7" width="82" height="34" rx="17" fill="url(#g)"/>
 <rect x="16" y="11" width="64" height="7" rx="3.5" fill="#fff" fill-opacity=".55"/>`,
 `<linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff"/><stop offset="1" stop-color="#C8C8C8"/></linearGradient>`);
// ---- tutorial hand (fingertip at the top centre-left) ----
{
 const shapes=`
  <rect x="62" y="12" width="40" height="130" rx="20"/>
  <rect x="58" y="96" width="112" height="96" rx="34"/>
  <rect x="100" y="84" width="27" height="60" rx="13.5"/>
  <rect x="123" y="88" width="27" height="60" rx="13.5"/>
  <rect x="146" y="96" width="26" height="56" rx="13"/>
  <ellipse cx="54" cy="140" rx="19" ry="36" transform="rotate(-28 54 140)"/>`;
 A.hand=svg(220,260,`<g transform="rotate(-14 110 130)">
  <rect x="74" y="182" width="84" height="56" rx="12" fill="#4DA3FF" stroke="${INK}" stroke-width="11"/>
  <rect x="74" y="184" width="84" height="14" fill="#fff" fill-opacity=".35"/>
  <g fill="#fff" stroke="${INK}" stroke-width="22" stroke-linejoin="round">${shapes}</g>
  <g fill="#fff">${shapes}</g>
  <path d="M168,150 Q172,186 138,192 L76,192 Q64,190 60,176 Q110,186 150,170 Q162,164 168,150Z" fill="#C9D3E8" fill-opacity=".8"/>
  <g fill="none" stroke="${INK}" stroke-width="5" stroke-linecap="round" stroke-opacity=".8">
   <path d="M102,112 L102,140"/><path d="M125,116 L125,144"/><path d="M148,120 L148,146"/></g>
  <rect x="70" y="24" width="9" height="46" rx="4.5" fill="#E8EEFA"/>
 </g>`);
}
// ---- backgrounds ----
{
 // Quiet play field: no repeating dots behind labels/markers. Broad warm dunes retain travel depth.
 A.bg_game=svg(540,1170,`<rect width="540" height="1170" fill="url(#g)"/>
 <ellipse cx="90" cy="180" rx="410" ry="390" fill="#FFFBF0" fill-opacity=".4"/>
 <path d="M0,930 Q135,880 270,925 T540,905 V1170 H0Z" fill="#FFD9AE" fill-opacity=".42"/>
 <path d="M0,1010 Q160,960 300,1005 T540,985 V1170 H0Z" fill="#FFC98F" fill-opacity=".32"/>`,
 `<linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFFBF0"/><stop offset="1" stop-color="#FFEAD0"/></linearGradient>`);
 // Menu keeps its soft radial glow/clouds/hills, not a high-frequency sunburst.
 A.bg_menu=svg(540,1170,`<rect width="540" height="1170" fill="url(#r)"/>
 <circle cx="70" cy="1010" r="150" fill="#FF9B54" fill-opacity=".38"/><circle cx="300" cy="1090" r="190" fill="#FF8F4A" fill-opacity=".36"/><circle cx="520" cy="1000" r="150" fill="#FF9B54" fill-opacity=".38"/>
 <g fill="#fff" fill-opacity=".5"><path d="M30,150 C30,132 65,126 89,134 C98,103 151,111 154,136 C192,143 180,170 125,170 H86 C49,170 30,166 30,150Z"/><path d="M388,250 C388,232 401,227 415,230 C429,209 456,218 456,235 C500,239 506,265 443,267 C410,268 388,263 388,250Z"/></g>`,
 `<radialGradient id="r" cx="270" cy="400" r="900" gradientUnits="userSpaceOnUse"><stop offset="0" stop-color="#FFF0B8"/><stop offset=".45" stop-color="#FFD27A"/><stop offset="1" stop-color="#FFA95C"/></radialGradient>`);
}
// ---- logo ----
{
 const star=(x,y,s,f)=>`<path transform="translate(${x} ${y}) scale(${s})" d="M0,-20 Q3,-3 20,0 Q3,3 0,20 Q-3,3 -20,0 Q-3,-3 0,-20Z" fill="${f}" stroke="${INK}" stroke-width="${5/s}" stroke-linejoin="round"/>`;
 const text=(t,y,size,fill,sw)=>`
  <text x="450" y="${y+12}" font-family="Fredoka" font-weight="600" font-size="${size}" text-anchor="middle" fill="${INK}" stroke="${INK}" stroke-width="${sw}" stroke-linejoin="round">${t}</text>
  <text x="450" y="${y}" font-family="Fredoka" font-weight="600" font-size="${size}" text-anchor="middle" fill="${INK}" stroke="${INK}" stroke-width="${sw}" stroke-linejoin="round">${t}</text>
  <text x="450" y="${y}" font-family="Fredoka" font-weight="600" font-size="${size}" text-anchor="middle" fill="${fill}">${t}</text>`;
 A.logo=svg(900,640,`
 <g transform="rotate(-7 450 170)">
  <path d="M380,78 L380,48 Q380,26 402,26 L498,26 Q520,26 520,48 L520,78" fill="none" stroke="${INK}" stroke-width="34" stroke-linejoin="round"/>
  <path d="M380,78 L380,48 Q380,26 402,26 L498,26 Q520,26 520,48 L520,78" fill="none" stroke="#6B7386" stroke-width="16" stroke-linejoin="round"/>
  <circle cx="345" cy="298" r="20" fill="#3B3F4E" stroke="${INK}" stroke-width="9"/><circle cx="555" cy="298" r="20" fill="#3B3F4E" stroke="${INK}" stroke-width="9"/>
  <path d="M352,96 L318,52 L376,62 Z" fill="#F4675C" stroke="${INK}" stroke-width="10" stroke-linejoin="round"/>
  <path d="M520,96 L548,40 Q572,36 580,58 L560,98 Z" fill="#5AA9F5" stroke="${INK}" stroke-width="10" stroke-linejoin="round"/>
  <rect x="290" y="76" width="320" height="216" rx="38" fill="#35B8A8" stroke="${INK}" stroke-width="13"/>
  <g clip-path="url(#body)">
   <rect x="290" y="82" width="320" height="16" fill="#fff" fill-opacity=".4"/>
   <rect x="290" y="232" width="320" height="60" fill="#000" fill-opacity=".14"/>
   <rect x="296" y="150" width="308" height="12" fill="${INK}" fill-opacity=".55"/>
   <rect x="348" y="76" width="34" height="216" fill="#FFD166" stroke="${INK}" stroke-width="7"/>
   <rect x="518" y="76" width="34" height="216" fill="#FFD166" stroke="${INK}" stroke-width="7"/>
   <circle cx="296" cy="82" r="44" fill="#1E8F82" stroke="${INK}" stroke-width="6"/><circle cx="604" cy="82" r="44" fill="#1E8F82" stroke="${INK}" stroke-width="6"/>
   <circle cx="296" cy="286" r="44" fill="#1E8F82" stroke="${INK}" stroke-width="6"/><circle cx="604" cy="286" r="44" fill="#1E8F82" stroke="${INK}" stroke-width="6"/>
  </g>
  <rect x="290" y="76" width="320" height="216" rx="38" fill="none" stroke="${INK}" stroke-width="13"/>
  <rect x="352" y="182" width="26" height="22" rx="5" fill="#fff" stroke="${INK}" stroke-width="6"/><rect x="522" y="182" width="26" height="22" rx="5" fill="#fff" stroke="${INK}" stroke-width="6"/>
  <path transform="translate(450 214) scale(1.9)" d="M0,-20 L5.9,-6.2 L20.6,-5.3 L9.3,4.2 L12.9,18.6 L0,10.8 L-12.9,18.6 L-9.3,4.2 L-20.6,-5.3 L-5.9,-6.2Z" fill="#FFF3C4" stroke="${INK}" stroke-width="3.6" stroke-linejoin="round"/>
 </g>
 ${star(210,120,1.6,'#FFE066')}${star(700,90,1.2,'#fff')}${star(735,250,1.9,'#FFE066')}${star(170,280,1.1,'#fff')}
 ${text('Pack It',452,150,'#FFFFFF',30)}
 ${text('Perfect',600,176,'url(#gold)',32)}`,
 `<clipPath id="body"><rect x="290" y="76" width="320" height="216" rx="38"/></clipPath>
  <linearGradient id="gold" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE873"/><stop offset="1" stop-color="#FFB02E"/></linearGradient>`);
}
for(const k of Object.keys(A)){
  const png=new Resvg(A[k],{fitTo:{mode:'zoom',value:2},font:{fontFiles:[fontFile],loadSystemFonts:false,defaultFontFamily:'Fredoka'}}).render().asPng();
  fs.writeFileSync(path.join(destination,k+'.png'),png);
}
console.log(`Staged ${Object.keys(A).length} original environment assets: ${Object.keys(A).join(',')}; no project assets imported or changed.`);
