// Original whole-object illustrations. No external images or generated icons are embedded.
// node item-art-build.js <resvg dependency directory> <output Resources/Art directory> <levels.json>
//
// Every object is drawn as the thing itself inside its polyomino footprint: the silhouette is the footprint, the
// interior shows the object's construction (cuffs, seams, caps, labels, handles), and a shared lighting model gives
// each one a top-left key light, a soft bottom contact shade, a rim highlight and a material finish.
const fs = require('fs');
const path = require('path');
const [,, runtime, output, levelsPath] = process.argv;
const { Resvg } = require(path.join(runtime, 'node_modules/@resvg/resvg-js'));
const INK = '#3E3246', SW = 5, CELL = 100, PPC = 128;
const objects = [];
let uid = 0;
const rect = (x,y,w,h,r,fill,extra='') => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" ${extra}/>`;
const line = (d,color=INK,width=SW,extra='') => `<path d="${d}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
const shape = (d,fill,extra='') => `<path d="${d}" fill="${fill}" ${extra}/>`;
const circle = (x,y,r,fill,extra='') => `<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" ${extra}/>`;
const ellipse = (x,y,rx,ry,fill,extra='') => `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="${fill}" ${extra}/>`;
const outline = `stroke="${INK}" stroke-width="${SW}" stroke-linejoin="round" stroke-linecap="round"`;
const thin = `stroke="${INK}" stroke-width="3.5" stroke-linejoin="round" stroke-linecap="round"`;
const star = (x,y,s,c,extra='') => `<path transform="translate(${x} ${y}) scale(${s})" d="M0,-12 L3.6,-4 L12,-3 L6,3 L8,12 L0,7 L-8,12 L-6,3 L-12,-3 L-3.6,-4Z" fill="${c}" ${extra}/>`;
const sparkle = (x,y,s,c='#fff') => `<path transform="translate(${x} ${y}) scale(${s})" d="M0,-10 Q1.5,-1.5 10,0 Q1.5,1.5 0,10 Q-1.5,1.5 -10,0 Q-1.5,-1.5 0,-10Z" fill="${c}" opacity=".9"/>`;
// Soft specular: a white blob with a blurred edge (resvg renders feGaussianBlur).
const gloss = (x,y,rx,ry,op=.5,rot=0) => { const id='g'+(uid++); return `<defs><filter id="${id}" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="4"/></filter></defs><ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="#fff" opacity="${op}" filter="url(#${id})" transform="rotate(${rot} ${x} ${y})"/>`; };
// Repeated vertical ribs (knit cuffs, bottle grips, corrugation).
const ribs = (x,y,w,h,n,color,width=3,op=.55) => Array.from({length:n},(_,i)=>line(`M${x+(i+.5)*w/n},${y} V${y+h}`,color,width,`opacity="${op}"`)).join('');
const stitches = (d,color='#fff',op=.6) => line(d,color,3,`stroke-dasharray="6 7" opacity="${op}"`);

function contour(rows) {
  const cells = new Set(); rows.forEach((row,y)=>[...row].forEach((v,x)=>{if(v==='X')cells.add(`${x},${y}`)}));
  const edges = new Map();
  for(const key of cells){const [x,y]=key.split(',').map(Number), p=[[x,y],[x+1,y],[x+1,y+1],[x,y+1]];
    for(let i=0;i<4;i++){const a=p[i].join(','),b=p[(i+1)%4].join(','),reverse=`${b}:${a}`; if(edges.has(reverse))edges.delete(reverse);else edges.set(`${a}:${b}`,[p[i],p[(i+1)%4]]);}}
  const all=[...edges.values()], vertices=[all[0][0]];let current=vertices[0];
  while(all.length){let i=all.findIndex(e=>e[0][0]===current[0]&&e[0][1]===current[1]);
    if(i<0)throw Error('Unclosed contour');const e=all.splice(i,1)[0];current=e[1];if(current.join(',')===vertices[0].join(','))break;vertices.push(current);}
  let points=vertices.filter((p,i)=>{const a=vertices[(i+vertices.length-1)%vertices.length],b=vertices[(i+1)%vertices.length];return (p[0]-a[0])*(b[1]-p[1])!==(p[1]-a[1])*(b[0]-p[0]);}).map(p=>p.map(v=>v*CELL));
  const radius=16;let d='';
  points.forEach((p,i)=>{const a=points[(i+points.length-1)%points.length],b=points[(i+1)%points.length];let l=Math.hypot(p[0]-a[0],p[1]-a[1]),n=Math.hypot(b[0]-p[0],b[1]-p[1]);const pre=[p[0]+(a[0]-p[0])*radius/l,p[1]+(a[1]-p[1])*radius/l],post=[p[0]+(b[0]-p[0])*radius/n,p[1]+(b[1]-p[1])*radius/n];d+=`${i?'L':'M'}${pre} Q${p} ${post} `;});return d+'Z';
}

// Material finishes layered inside the silhouette. 'cloth' adds a fine weave, 'metal' a hard diagonal reflection,
// 'glass' a tall soft reflection, 'paper' a faint grain; 'matte' adds nothing beyond the shared light.
function finish(kind,w,h){
  switch(kind){
    case 'cloth': return `<rect width="${w}" height="${h}" fill="#000" opacity=".07" filter="url(#weave)"/>`;
    case 'paper': return `<rect width="${w}" height="${h}" fill="#000" opacity=".05" filter="url(#grain)"/>`;
    case 'metal': return `<path d="M${w*.08},${h*.9} L${w*.4},${h*.06} L${w*.52},${h*.06} L${w*.2},${h*.9}Z" fill="#fff" opacity=".1" filter="url(#soft)"/><path d="M${w*.56},${h*.95} L${w*.8},${h*.1} L${w*.84},${h*.1} L${w*.6},${h*.95}Z" fill="#fff" opacity=".07" filter="url(#soft)"/>`;
    case 'glass': return `<path d="M${w*.1},${h*.14} Q${w*.16},${h*.5} ${w*.1},${h*.82}" fill="none" stroke="#fff" stroke-width="${Math.min(w,h)*.045}" stroke-linecap="round" opacity=".32" filter="url(#soft)"/>`;
    default: return '';
  }
}

function add(id,name,rows,base,details,opts={}){
  const w=rows[0].length*CELL,h=rows.length*CELL,d=opts.path||contour(rows);
  const [light,mid,dark]=base;
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="${w*PPC/CELL}" height="${h*PPC/CELL}" viewBox="0 0 ${w} ${h}"><defs>
  <linearGradient id="base" x1="0" y1="0" x2=".7" y2="1"><stop stop-color="${light}"/><stop offset=".5" stop-color="${mid}"/><stop offset="1" stop-color="${dark}"/></linearGradient>
  <radialGradient id="key" cx="0.22" cy="0.14" r="0.75"><stop stop-color="#fff" stop-opacity=".42"/><stop offset=".55" stop-color="#fff" stop-opacity="0"/></radialGradient>
  <linearGradient id="contact" x1="0" y1="0" x2="0" y2="1"><stop offset=".55" stop-color="${INK}" stop-opacity="0"/><stop offset="1" stop-color="${INK}" stop-opacity=".28"/></linearGradient>
  <linearGradient id="glassfill" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#9FB3DC"/><stop offset="1" stop-color="#4C405B"/></linearGradient>
  <filter id="weave" x="0" y="0" width="100%" height="100%"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" seed="7"/><feColorMatrix type="saturate" values="0"/></filter>
  <filter id="soft" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="3"/></filter>
  <filter id="grain" x="0" y="0" width="100%" height="100%"><feTurbulence type="fractalNoise" baseFrequency="0.6" numOctaves="1" seed="3"/><feColorMatrix type="saturate" values="0"/></filter>
  <clipPath id="clip"><path d="${d}"/></clipPath></defs>
  <g transform="translate(5 5) scale(${(w-10)/w} ${(h-10)/h})">
    <path d="${d}" fill="url(#base)" ${outline}/>
    <g clip-path="url(#clip)">
      <path d="${d}" fill="url(#key)"/>
      ${details(w,h)}
      ${finish(opts.finish||'matte',w,h)}
      <path d="${d}" fill="url(#contact)"/>
      ${line(d,'#fff',9,'opacity=".38" transform="translate(4 5)"')}${line(d,INK,13,'opacity=".14" transform="translate(-3 -7)"')}
    </g>
    <path d="${d}" fill="none" ${outline}/>
  </g></svg>`;
  fs.mkdirSync(path.join(output,'Items'),{recursive:true});
  fs.writeFileSync(path.join(output,'Items',id+'.png'),new Resvg(svg).render().asPng());
  objects.push({id,display_name:name,sprite:'Items/'+id,cells:rows});
}

// ------------------------------------------------------------------ palettes
const pink=['#FFC3D1','#F68BA7','#D9557F'], blue=['#AEE3FF','#5FB2F0','#3579C2'], mint=['#B9F1D9','#5FCDB0','#2C9E8A'],
 yellow=['#FFF0B8','#FBD070','#E2A64E'], purple=['#E4D2FF','#B79EE8','#8B6BC6'], red=['#FFC0AA','#FA8470','#D8584F'],
 cream=['#FFF8E6','#F6E6C6','#DDC79C'], navy=['#5A6B8C','#3B4A68','#273350'], wood=['#F0C98C','#D5A061','#A66E3B'],
 steel=['#E6E9EF','#B4BBCA','#7E8799'], olive=['#CFE3A8','#9BBF72','#6C9152'], orange=['#FFD4A3','#FFAB5E','#E27E36'],
 teal=['#B6ECE8','#63C6C3','#2E8F93'], brown=['#D9B08C','#A97A54','#6F4D35'], sky=['#E9F6FF','#C5E6FA','#8FC2E8'];

// ------------------------------------------------------------------ knitwear
// A real sock drawn in an L: ribbed cuff on top, knit leg, a heel in contrasting yarn at the corner, a toe cap at the
// far end of the foot, and a tiny pattern. Works for every L that has a vertical leg and a horizontal foot.
function sock(id,name,rows,palette,pattern='stripe'){
  add(id,name,rows,palette,(w,h)=>{
    const foot=h-CELL, legW=CELL; let s='';
    s+=rect(8,8,legW-16,36,8,'#FFF8E7',outline)+ribs(14,12,legW-28,28,6,'#D9C6AC',3,.8);            // cuff
    s+=line(`M${legW-70},56 V${foot-14}`,'#fff',9,'opacity=".35"');                                   // leg highlight
    if(pattern==='stripe') s+=line(`M14,74 H${legW-14} M14,96 H${legW-14}`,'#fff',8,'opacity=".5"');
    if(pattern==='dots') for(let y=72;y<foot-30;y+=26) for(let x=24;x<legW-16;x+=28) s+=circle(x+(y/26%2)*14,y,4.5,'#fff','opacity=".7"');
    if(pattern==='heart') s+=shape(`M${legW/2},104 C${legW/2-18},86 ${legW/2-34},110 ${legW/2},126 C${legW/2+34},110 ${legW/2+18},86 ${legW/2},104Z`,'#fff','opacity=".8"');
    s+=shape(`M4,${foot-34} Q6,${h-6} 60,${h-6} Q30,${h-30} 46,${foot-10}Z`,'#FFF4DF',outline);       // heel
    s+=shape(`M${w-54},${foot+8} Q${w-8},${foot+10} ${w-8},${h/2+foot/2} Q${w-10},${h-8} ${w-54},${h-8} Q${w-28},${h/2+foot/2} ${w-54},${foot+8}Z`,'#FFF4DF',outline); // toe
    s+=stitches(`M${legW+10},${foot+54} H${w-62}`,'#fff',.45);
    return s;
  },{finish:'cloth'});
}
sock('sock_long','Cozy sock',['X.','X.','XX'],red,'stripe');
sock('sock_short','Ankle sock',['X.','XX'],pink,'dots');
sock('sock_sport','Sport sock',['X.','XX'],blue,'stripe');
sock('sock_travel','Travel sock',['X.','X.','XX'],pink,'heart');
// Rolled into a ball: a sphere of yarn with the cuff wrapped around it.
add('sock_folded','Rolled socks',['XX','XX'],pink,(w,h)=>
 circle(100,100,80,'#F9A2BB','opacity=".9"')+line('M34,70 Q100,20 166,70','#fff',10,'opacity=".45"')+
 shape('M20,118 Q100,80 180,118 Q182,150 170,160 Q100,120 30,160 Q18,150 20,118Z','#FFF8E7',outline)+ribs(34,118,132,36,10,'#D9C6AC',3,.6)+
 line('M60,52 Q100,38 140,52 M52,78 Q100,60 148,78','#fff',7,'opacity=".5"')+star(100,56,.9,'#fff','opacity=".85"'),{finish:'cloth'});
// Scrunched: two socks pushed into each other with the cuffs both showing.
add('sock_scrunched','Scrunched socks',['.X.','XXX','..X'],pink,(w,h)=>
 rect(112,10,76,38,9,'#FFF8E7',outline)+ribs(120,14,60,30,5,'#D9C6AC',3,.8)+
 rect(8,112,78,36,9,'#FFF8E7',outline)+ribs(16,116,62,28,5,'#D9C6AC',3,.8)+
 line('M150,62 V130 M44,160 H120','#fff',9,'opacity=".4"')+
 shape('M206,222 Q250,210 290,236 Q292,282 250,290 Q216,276 206,222Z','#FFF4DF',outline)+
 stitches('M112,186 Q180,170 240,200','#fff',.5)+shape('M140,214 C128,200 112,218 140,236 C168,218 152,200 140,214Z','#fff','opacity=".8"'),{finish:'cloth'});
add('sock_t','Knee sock',['X.','XX','X.'],pink,(w,h)=>
 rect(8,8,84,38,9,'#FFF8E7',outline)+ribs(16,12,68,30,6,'#D9C6AC',3,.8)+
 line('M14,74 H86 M14,96 H86 M14,118 H86','#fff',8,'opacity=".5"')+line('M30,56 V250','#fff',9,'opacity=".3"')+
 shape('M118,108 Q190,104 190,150 Q190,194 118,190 Q150,150 118,108Z','#FFF4DF',outline)+
 shape('M10,262 Q10,294 54,294 Q86,294 90,266 Q60,250 10,262Z','#FFF4DF',outline),{finish:'cloth'});
add('sock_straight','Rolled-flat sock',['XXX'],pink,(w,h)=>
 rect(8,8,44,84,10,'#FFF8E7',outline)+ribs(14,14,32,72,4,'#D9C6AC',3,.8)+
 line('M70,30 H230 M70,70 H230','#fff',8,'opacity=".45"')+star(150,50,1.1,'#fff','opacity=".85"')+
 shape('M244,10 Q294,12 294,50 Q294,88 244,90 Q262,50 244,10Z','#FFF4DF',outline),{finish:'cloth'});

// ------------------------------------------------------------------ bottles and jars
// Vertical bottle in a column: cap, neck, shoulder, body with a label; the pump/atomizer sits in the side cell.
function capAt(x,y,w,h,color){ return rect(x,y,w,h,8,color,outline)+ribs(x+6,y+5,w-12,h-10,4,'#fff',3,.5); }
function label(x,y,w,h,color,mark){ return rect(x,y,w,h,10,color,outline)+line(`M${x+12},${y+h*.3} H${x+w-12} M${x+12},${y+h*.5} H${x+w-12} M${x+12},${y+h*.7} H${x+w*.6}`,'#C9B99A',4)+(mark||''); }
add('perfume','Perfume atomizer',['X.','XX'],purple,(w,h)=>
 capAt(22,8,56,44,'#FFE5A8')+rect(30,56,40,22,5,'#E3C58C',outline)+                                    // cap and neck
 label(22,112,60,64,'#FFF3D9',star(52,144,.9,'#B48BD8'))+line('M16,92 V104','#fff',8,'opacity=".5"')+
 line('M86,150 H122',INK,10)+line('M86,150 H122','#E3C58C',5)+circle(150,150,30,'#F9A3C0',outline)+line('M134,136 Q150,122 166,136','#fff',5,'opacity=".7"')+ // tube and bulb in the side cell
 rect(142,112,16,12,3,'#E3C58C',outline),{finish:'glass'});
add('perfume_upright','Perfume bottle',['.X','XX'],purple,(w,h)=>
 capAt(120,8,60,46,'#FFE5A8')+rect(128,58,44,22,5,'#E3C58C',outline)+
 circle(48,150,34,'#F9A3C0',outline)+line('M82,150 H112',INK,9)+line('M34,134 Q48,120 62,134','#fff',5,'opacity=".7"')+
 label(106,118,78,56,'#FFF3D9',star(145,146,.9,'#B48BD8'))+line('M118,96 V110','#fff',8,'opacity=".5"'),{finish:'glass'});
add('atomizer_short','Travel atomizer',['XX','X.'],purple,(w,h)=>
 rect(112,24,76,52,10,'#FFE5A8',outline)+ribs(120,30,60,40,5,'#fff',3,.5)+rect(100,40,16,20,4,'#E3C58C',outline)+
 line('M26,120 V180','#fff',10,'opacity=".5"')+label(30,114,54,60,'#FFF3D9',star(57,144,.75,'#B48BD8'))+
 rect(6,96,88,16,6,'#D9BBEF',outline),{finish:'glass'});
add('atomizer_f','Perfume atomizer',['.XX','XX.','.X.'],purple,(w,h)=>
 rect(116,14,172,70,16,'#FFE5A8',outline)+ribs(130,24,140,50,8,'#fff',3,.45)+rect(176,84,48,20,5,'#E3C58C',outline)+
 ellipse(50,150,42,36,'#F9A3C0',outline)+line('M92,150 H128',INK,10)+line('M30,136 Q50,116 72,136','#fff',6,'opacity=".7"')+
 label(124,160,54,84,'#FFF3D9','')+star(151,202,1,'#B48BD8')+line('M128,124 V148 M130,254 V282','#fff',9,'opacity=".5"'),{finish:'glass'});
add('shampoo_pump','Shampoo dispenser',['XX','X.','X.'],mint,(w,h)=>
 rect(10,8,120,52,14,'#FFF3D9',outline)+line('M130,34 H184 Q192,34 192,44 V62','#FFF3D9',16)+line('M130,34 H184 Q192,34 192,44 V62',INK,4)+
 rect(24,64,52,20,6,'#D5B992',outline)+rect(10,90,80,30,8,'#B9F1D9',outline)+
 label(16,140,68,106,'#F2FFF6',ellipse(50,192,22,30,'#7BD0B3'))+line('M26,110 V116 M26,256 V284','#fff',9,'opacity=".55"'),{finish:'glass'});
add('shampoo_w','Shampoo bottle',['X..','XX.','.XX'],blue,(w,h)=>
 capAt(12,10,76,52,'#FFE5A8')+line('M24,90 V160 H150 V246 H274','#D6EEF6',14)+line('M24,90 V160 H150 V246 H274',INK,3,'opacity=".25"')+
 label(118,214,154,66,'#FFF3D9',line('M138,246 Q176,218 212,248 T256,244','#6BB9C0',9))+line('M38,96 V150','#fff',9,'opacity=".5"'),{finish:'glass'});
add('pump_long','Tall pump bottle',['XX','.X','.X'],pink,(w,h)=>
 rect(8,22,118,40,12,'#FFF3D9',outline)+line('M126,42 H180 Q190,42 190,52 V70',INK,14)+line('M126,42 H180 Q190,42 190,52 V70','#FFF3D9',7)+
 rect(118,76,66,22,5,'#E8C48F',outline)+rect(112,104,80,24,7,'#F9C2D4',outline)+
 label(116,136,68,100,'#FFEAF0',ellipse(150,186,24,30,'#C79FCE'))+line('M126,250 V276','#fff',9,'opacity=".55"'),{finish:'glass'});
add('pump_t','Pump dispenser',['XXX','.X.'],mint,(w,h)=>
 rect(8,12,284,70,16,'#FFF3D9',outline)+line('M22,60 H278','#A8DACB',8)+rect(132,24,36,38,8,'#D5B992',outline)+
 rect(112,96,76,18,6,'#8ED6C6',outline)+label(116,120,68,70,'#FFF9EA',ellipse(150,156,22,24,'#7BD0B3'))+line('M130,130 V170','#fff',8,'opacity=".6"'),{finish:'glass'});
add('bottle_tall','Travel bottle',['X.','X.','X.','XX'],mint,(w,h)=>
 capAt(14,8,72,46,'#FFE5A8')+rect(22,58,56,18,5,'#D6B58A',outline)+line('M26,90 V230','#DFFCE8',14)+
 label(16,244,70,124,'#FFF6E2',star(51,300,1.4,'#73B6A2'))+line('M108,370 H176','#A5E5BF',10)+rect(104,356,86,30,9,'#B9F1D9',outline),{finish:'glass'});
add('thermos','Vacuum flask',['XXX'],steel,(w,h)=>
 rect(w-64,8,56,84,14,'#4F6868',outline)+ribs(w-58,14,44,72,5,'#8EA7A7',3,.6)+rect(w-78,18,16,64,4,'#FFE2A0',outline)+
 line('M20,22 H200','#fff',14,'opacity=".55"')+line('M20,78 H200',INK,4,'opacity=".25"')+ellipse(64,50,26,30,'#FFF5D8',outline)+star(64,50,.9,'#70C6B0'),{finish:'metal'});
add('toothpaste','Toothpaste tube',['XXX'],blue,(w,h)=>
 rect(w-56,12,46,76,10,'#FFF8DF',outline)+ribs(w-50,18,34,64,4,'#D9D7C9',4,.9)+
 rect(14,20,w-82,60,20,'#F3FBFF',outline)+line('M12,22 L22,78 M26,20 L34,80','#8AB7D4',4,'opacity=".6"')+
 line(`M44,62 Q86,16 134,50 T${w-96},50`,'#59BBA7',12)+circle(w-120,50,10,'#E65C6E'),{finish:'glass'});
add('toothbrush','Travel toothbrush',['X','X','X','X'],mint,(w,h)=>
 rect(14,10,72,132,22,'#FFF8E5',outline)+Array.from({length:8},(_,i)=>line(`M24,${22+i*15} H76`,'#8DCED8',6)).join('')+line('M24,52 Q50,36 76,52','#E65C6E',6)+
 rect(28,148,44,232,22,'#3FA893',outline)+line('M42,176 V340','#B1F3D4',9,'opacity=".8"')+circle(50,372,12,'#E7F9DB',outline),{finish:'glass'});
add('jar_t','Glass food jar',['XXX','.X.'],cream,(w,h)=>
 rect(8,10,284,74,16,'#8FC4A9',outline)+ribs(18,16,264,60,11,'#DDEFD0',5,.7)+rect(8,70,284,14,4,'#6FA98C','opacity=".9"')+
 rect(104,94,92,30,8,'#D7F2E6','opacity=".9"')+rect(112,118,76,70,16,'#FFF4D8',outline)+ellipse(150,152,26,22,'#DDA56E')+line('M122,134 V158','#fff',8,'opacity=".6"'),{finish:'glass'});
add('milk_carton','Milk carton',['XX','X.','X.'],sky,(w,h)=>
 shape('M6,90 L30,8 H176 L194,90Z','#FFF6DE',outline)+line('M30,12 L84,86 M90,10 L104,88','#B6DCEC',5)+rect(92,24,60,20,6,'#4D8FC6',outline)+
 rect(10,118,80,170,10,'#FFF8E3',outline)+rect(10,118,80,36,6,'#4D8FC6','opacity=".9"')+ellipse(50,208,30,36,'#73BCD2')+shape('M46,182 Q20,210 44,230 Q74,236 68,206Z','#FFF8E3')+
 line('M22,262 H78 M22,276 H78','#4D8FC6',5),{finish:'paper'});
add('wine_glass','Crystal goblet',['X','X'],sky,(w,h)=>
 shape('M14,14 H86 L80,76 Q74,106 50,108 Q26,106 20,76Z','#DDF2F6',outline)+shape('M24,52 H76 L72,78 Q64,98 50,98 Q34,96 28,78Z','#E98CA8','opacity=".9"')+
 line('M50,110 V172',INK,9)+ellipse(50,180,34,9,'#DDF2F6',outline)+line('M28,24 V48','#fff',8)+line('M64,22 V38','#fff',4),{finish:'glass'});
add('vase','Ceramic vase',['X','X','X'],purple,(w,h)=>
 shape('M24,13 H76 L70,88 Q96,132 90,210 L82,284 H18 L10,210 Q4,132 30,88Z','#DCC9F0',outline)+ellipse(50,16,27,9,'#7E6693',outline)+
 line('M24,160 Q50,136 77,160 M21,188 Q50,164 80,188','#FFF2D2',9)+star(50,228,1.35,'#F0BE73')+line('M32,104 Q18,130 24,150','#fff',8,'opacity=".7"'),{finish:'glass'});
add('snow_globe','Travel snow globe',['XX','XX'],sky,(w,h)=>
 circle(100,84,76,'#CFEEF4',outline)+circle(100,84,64,'#A6D9E6')+shape('M38,128 L76,66 L98,104 L126,48 L166,128Z','#F4FBEA',outline)+
 [66,142,112,84,128].map((x,i)=>circle(x,[44,90,33,72,110][i],4.5,'#fff')).join('')+line('M48,54 Q32,76 38,94','#fff',10,'opacity=".9"')+
 rect(30,144,140,46,14,'#E7B979',outline)+ribs(40,150,120,34,7,'#fff',3,.35)+star(100,167,1,'#FFF2CF'),{finish:'glass'});
add('tea_set','Cup and saucer',['X.','XX'],pink,(w,h)=>
 rect(12,12,76,100,18,'#FFF5DF',outline)+ellipse(50,20,32,10,'#B97E68',outline)+star(50,66,1,'#D794B5')+line('M22,40 V70','#fff',7)+
 ellipse(104,152,88,34,'#F7C5CD',outline)+ellipse(104,152,62,20,'#FFEBD9',outline)+ellipse(136,146,28,13,'#B47A63',outline)+line('M74,152 Q104,138 134,152','#fff',4,'opacity=".6"'),{finish:'glass'});
add('teapot','Porcelain teapot',['XX','XX'],mint,(w,h)=>
 ellipse(166,100,30,38,'#FFF5DF',outline)+ellipse(166,100,15,22,'#6FBBA6',outline)+shape('M50,88 L10,62 Q6,112 56,130','#FFF5DF',outline)+
 ellipse(98,114,66,64,'#FFF5DF',outline)+ellipse(98,62,48,14,'#C6E9D6',outline)+circle(98,43,10,'#E9BC6D',outline)+
 star(98,120,1.5,'#EBA383')+line('M62,90 Q50,114 64,136','#fff',9),{finish:'glass'});

// ------------------------------------------------------------------ soft goods
add('pillow_v','Neck pillow',['XXX','X..','X..'],pink,(w,h)=>
 line('M30,262 V70 Q30,34 64,34 H272','#FFD6DD',18)+stitches('M72,270 V92 Q76,76 90,76 H272','#C75984',.8)+
 line('M20,100 Q20,70 60,66 M120,52 Q150,46 180,52','#fff',8,'opacity=".55"')+circle(54,176,8,'#FFEADF')+circle(54,230,8,'#FFEADF')+star(190,50,1,'#FFF1D0'),{finish:'cloth'});
add('towel_p','Folded towel',['XXX','.XX'],purple,(w,h)=>
 rect(8,8,284,90,16,'#F1E2FF','opacity=".85"')+line('M16,26 H284 M118,126 H284','#fff',12,'opacity=".7"')+stitches('M18,68 H282 M122,166 H280','#8B6EAD',.8)+
 line('M134,90 H276','#DFC8F3',8)+line('M8,100 H292','#B49AD6',4,'opacity=".9"')+ribs(220,112,60,24,6,'#B49AD6',3,.7),{finish:'cloth'});
add('hoodie_p','Folded hoodie',['XXX','.XX'],blue,(w,h)=>
 shape('M130,14 Q188,4 194,70 Q160,96 122,70Z','#8EC9EC',outline)+line('M146,74 V124 M178,74 V124','#FFF1D6',5)+
 rect(118,130,160,54,16,'#4891C1',outline)+line('M134,150 H262','#fff',6,'opacity=".3"')+rect(10,34,98,56,14,'#5FA6DA','opacity=".9"')+
 line('M22,46 H96 M30,72 H96','#C4E7F6',8)+stitches('M14,98 H284','#fff',.5),{finish:'cloth'});
add('scarf_s','Folded scarf',['XX.','.XX'],purple,(w,h)=>
 line('M18,28 H172 Q182,70 220,76 H280','#FCE4AD',24)+line('M26,58 H150 Q160,112 212,130 H280','#EFE0FF',10)+
 line('M30,90 H120 Q130,150 190,160 H280','#D7C2F2',8,'opacity=".8"')+ribs(216,160,60,32,5,'#FFE9B7',5,.9),{finish:'cloth'});
add('scarf_w','Zigzag scarf',['X..','XX.','.XX'],red,(w,h)=>
 line('M24,16 V110 Q24,146 108,146 Q156,146 154,216 Q154,258 276,254','#FFE3AE',28)+line('M66,20 V84 Q68,106 143,106 Q195,104 195,176 Q198,214 279,210','#F4B3C2',12)+
 stitches('M44,30 V100','#fff',.5)+ribs(228,266,52,24,4,'#FFE3AE',5,.9),{finish:'cloth'});
add('oven_mitt','Oven mitt',['X.','XX'],red,(w,h)=>
 rect(10,h-48,80,38,10,'#FFF2D8',outline)+ribs(18,h-42,64,26,6,'#D9C6AC',3,.8)+
 ['M20,20 L160,160','M20,66 L120,166','M60,16 L180,136','M20,112 L80,172','M106,14 L186,94'].map(d=>line(d,'#B96765',3,'opacity=".4"')).join('')+
 ['M166,14 L20,160','M186,54 L80,160','M126,12 L16,122'].map(d=>line(d,'#B96765',3,'opacity=".4"')).join('')+
 circle(146,46,14,'#FFF2D8',outline)+line('M160,46 Q176,30 184,46',INK,4),{finish:'cloth'});
function plush(id,rows){add(id,'Cuddly bear',rows,yellow,(w,h)=>
 circle(25,23,20,'#D69061',outline)+circle(75,23,20,'#D69061',outline)+circle(25,23,9,'#F2B98C')+circle(75,23,9,'#F2B98C')+
 ellipse(50,58,40,38,'#E9B47B',outline)+ellipse(50,72,24,16,'#FFE1B1')+circle(36,54,4.5,INK)+circle(66,54,4.5,INK)+circle(38,52,1.5,'#fff')+circle(68,52,1.5,'#fff')+
 ellipse(50,66,7,5,INK)+line('M50,71 V79 M43,80 Q50,85 57,80',INK,3)+circle(28,66,6,'#F7A3A3','opacity=".6"')+circle(72,66,6,'#F7A3A3','opacity=".6"')+
 ellipse(53,h-55,34,42,'#FFE5B3',outline)+ellipse(w-44,h-38,34,26,'#D6956B',outline)+rect(14,98,75,19,7,'#79BCB4',outline)+star(51,107,.6,'#fff'),{finish:'cloth'});}
plush('plush',['X.','XX']); plush('plush_long',['X.','X.','XX']);
function slipper(id,rows){add(id,'Packable slipper',rows,blue,(w,h)=>
 rect(12,12,w-24,h-24,20,'#FFE4B3',outline)+rect(22,20,w-44,h-40,16,'#7BBFE9',outline)+
 shape(`M${w*.3},${h*.42} Q${w/2},${h*.2} ${w*.7},${h*.42} Q${w/2},${h*.56} ${w*.3},${h*.42}Z`,'#3A7CAA','opacity=".9"')+
 star(w/2,h*.7,1.3,'#FFF0C2')+line(`M34,30 H${w-40}`,'#CFEFFC',8,'opacity=".8"')+stitches(`M26,${h-28} H${w-26}`,'#fff',.5),{finish:'cloth'});}
slipper('slipper_folded',['XX','XX']); slipper('slipper_straight',['XXX']);
function ankleShoe(id,rows,palette){add(id,'Trainer',rows,palette,()=>
 rect(14,10,70,32,9,'#FFF4DF',outline)+line('M20,164 H184 M20,178 H186','#FFF5DC',10)+line('M72,62 L26,78 L76,92 L30,110','#FFF7E7',8)+
 line('M96,122 Q144,112 180,142','#285F99',5)+star(52,136,.9,'#FFE29B')+line('M24,196 H186','#4A4F5C',6,'opacity=".6"'),{finish:'cloth'});}
ankleShoe('trainer',['X.','XX'],blue);
add('sneaker','High-top sneaker',['X.','X.','XX'],blue,()=>
 rect(10,10,82,44,12,'#FFF4DF',outline)+shape('M12,240 Q56,214 106,232 L186,250 V282 H12Z','#FFDD83',outline)+rect(8,282,188,14,5,'#FFF7E4',outline)+
 line('M70,92 L26,106 L72,124 L28,142 L74,160','#FFF6E4',8)+line('M12,206 Q56,192 84,220','#285D98',4)+circle(44,178,17,'#E6F4F0',outline)+star(44,178,.8,'#3579C2'),{finish:'cloth'});
add('hiking_boot','Hiking boot',['X.','X.','XX'],wood,()=>
 rect(8,10,84,172,14,'#D9A66A',outline)+rect(18,18,66,30,8,'#FFF0D1',outline)+ribs(24,22,54,22,5,'#D9C6AC',3,.8)+
 shape('M8,176 H90 L110,220 Q182,212 192,254 V280 H10Z','#B78358',outline)+rect(8,274,186,18,6,'#5F505A',outline)+
 line('M30,68 L68,88 M68,68 L30,88 M30,106 L68,126 M68,106 L30,126 M30,144 L68,164 M68,144 L30,164','#FFF0CF',7)+
 [78,116,154].map(y=>circle(26,y,4,'#E9BC6D')+circle(74,y,4,'#E9BC6D')).join('')+line('M118,238 Q160,228 176,248','#E7B980',7),{finish:'cloth'});
add('cap','Baseball cap',['XXX','.X.'],yellow,()=>
 shape('M6,88 Q22,4 102,6 H196 Q278,4 294,90Z','#F7BE66',outline)+line('M150,10 V88 M70,24 Q60,56 44,88 M230,24 Q240,56 256,88','#D08D4E',4)+
 shape('M104,92 H196 V178 Q150,208 104,178Z','#F18A69',outline)+line('M114,100 H186','#fff',5,'opacity=".3"')+star(150,50,1.5,'#FFF2BC')+circle(150,14,8,'#F7BE66',outline),{finish:'cloth'});
add('headphones','Travel headphones',['X.X','XXX'],purple,()=>
 line('M46,64 V146 Q150,180 254,146 V64','#E8DAFF',26)+line('M46,64 V146 Q150,180 254,146 V64',INK,4,'opacity=".2"')+
 rect(14,14,68,100,24,'#FFCE87',outline)+rect(218,14,68,100,24,'#FFCE87',outline)+ellipse(48,64,18,32,'#5E4D80','opacity=".9"')+ellipse(252,64,18,32,'#5E4D80','opacity=".9"')+
 line('M34,30 V88 M238,30 V88','#FFF1C5',8,'opacity=".8"')+line('M90,156 H212','#8065A9',5),{finish:'matte'});
add('umbrella','Open umbrella',['XXX','.X.'],blue,()=>
 shape('M4,88 Q150,-28 296,88 Q245,72 201,97 Q150,75 100,97 Q45,72 4,88Z','#6EC7EB',outline)+shape('M150,5 Q105,27 101,95 Q150,75 201,97 Q195,28 150,5Z','#FFE09A',outline)+
 line('M30,76 Q90,20 150,8','#fff',7,'opacity=".5"')+line('M150,107 V153 Q150,180 172,180 Q190,180 190,163',INK,18)+line('M150,107 V153 Q150,180 172,180 Q185,180 185,163','#E1A378',9)+circle(150,4,6,INK),{finish:'matte'});

// ------------------------------------------------------------------ food
add('sandwich','Sandwich',['X.','XX'],cream,()=>
 line('M14,20 H80 V118 H184 V184 H18Z','#FFF1C9',12)+line('M22,88 H84 V126 H174','#81BE70',12)+line('M28,102 H82 V140 H174','#ED8D79',9)+line('M30,112 H80 V150 H170','#FFE066',6)+
 [[46,44],[56,66],[122,164],[150,160]].map(([x,y])=>circle(x,y,4,'#CF9A67')).join(''),{finish:'paper'});
add('croissant','Butter croissant',['X.','XX'],yellow,()=>
 line('M30,20 Q64,30 68,76 Q70,132 114,146 Q152,156 184,166','#FFEFBD',14)+
 line('M20,76 Q50,70 86,88 M30,112 Q58,96 94,118 M88,162 Q106,128 128,130 M138,186 L158,146','#BB7D48',5)+line('M22,30 Q12,46 18,62','#FFF4CD',8)+
 [[40,40],[60,100],[150,150]].map(([x,y])=>circle(x,y,3,'#fff','opacity=".7"')).join(''),{finish:'paper'});
add('prawn','Cooked prawn',['X.','XX'],red,()=>
 line('M18,36 H84 M16,70 H86 M16,106 H88 M36,146 L86,122 M84,178 L106,138 M128,188 L140,146','#FFE7C0',9)+
 shape('M168,122 L198,106 L194,164 L176,192 L158,160Z','#E65E58',outline)+line('M28,22 L68,22','#FFF5DF',9)+circle(30,54,5,INK)+circle(32,52,1.8,'#fff')+
 line('M40,56 Q80,40 120,60 M44,96 Q90,80 118,98','#F9A89A',4,'opacity=".8"'),{finish:'matte'});
add('broccoli','Broccoli floret',['X.','XX'],mint,()=>
 rect(94,126,86,56,20,'#DFE8A5',outline)+line('M110,146 H164 M112,164 H152','#B8CD83',5)+
 [[42,38,33],[65,79,32],[35,113,29],[65,143,28]].map(([x,y,r])=>circle(x,y,r,'#6DB77B',outline)+ellipse(x-8,y-8,r*.5,r*.3,'#A5D88F')+circle(x+r*.3,y+r*.2,r*.22,'#4F9A5F','opacity=".6"')).join(''),{finish:'matte'});
add('sausage','Smoked sausage',['X.','X.','XX'],red,()=>
 line('M26,20 V220 Q26,264 176,266','#FFBEA0',16)+line('M10,98 L86,76 M10,166 L86,144 M116,206 L102,290','#AD514A',5)+
 line('M32,30 V60','#FFE8C9',8)+line('M60,230 Q120,250 170,250','#fff',6,'opacity=".4"')+ellipse(32,16,10,8,'#AD514A',outline),{finish:'matte'});
add('baguette_v','Baked loaf',['XXX','X..','X..'],yellow,()=>
 line('M22,266 V62 Q22,24 74,24 H274','#FFF0BD',16)+['M34,90 L78,62','M28,152 L76,126','M26,222 L74,196','M112,14 L100,78','M184,14 L172,78','M254,14 L242,78'].map(d=>line(d,'#BC834E',7)).join('')+
 line('M10,40 Q40,12 80,12','#fff',8,'opacity=".5"')+[[50,110],[50,180],[150,40],[220,40]].map(([x,y])=>circle(x,y,2.5,'#fff','opacity=".7"')).join(''),{finish:'paper'});
add('baguette','Baguette',['XXX'],yellow,(w,h)=>
 line('M18,24 H282','#FFF0BF',12)+[60,120,180,240].map(x=>line(`M${x},14 L${x-22},86`,'#BC834E',7)).join('')+line('M14,50 H286','#E2A64E',3,'opacity=".5"')+
 ellipse(10,50,8,22,'#E2A64E','opacity=".8"')+ellipse(290,50,8,22,'#E2A64E','opacity=".8"'),{finish:'paper'});
add('carrot','Garden carrot',['XXXX'],orange,(w,h)=>
 rect(8,10,70,80,16,'#86BE7A',outline)+line('M28,20 V78 M48,20 V78 M66,20 V78','#C5DB91',5)+
 [120,185,250,315].map(x=>line(`M${x},18 V40 M${x+20},62 V83`,'#CB753F',5)).join('')+line('M100,22 H356','#FFE2A1',8)+shape('M356,14 L394,50 L356,86Z','#E27E36',outline),{finish:'matte'});
add('pepper','Sweet pepper',['XX','XX','X.'],red,()=>
 line('M100,18 Q119,56 160,61','#80AD68',18)+line('M26,74 Q5,142 33,236 M80,78 Q107,132 76,256 M148,88 Q176,126 154,180','#C95F56',5)+
 line('M30,84 Q18,124 26,150','#FFE0B6',12)+ellipse(66,64,30,20,'#91BC73',outline)+line('M50,60 Q66,52 82,60','#fff',4,'opacity=".6"'),{finish:'matte'});
add('cheese_u','Hard cheese',['XX','X.','XX'],yellow,()=>
 line('M12,18 H184 M14,278 H184','#FFEBA6',10)+[[52,50,18],[136,46,13],[48,146,17],[42,248,13],[140,252,20],[90,100,9],[92,196,8]].map(([x,y,r])=>circle(x,y,r,'#D79A4D',`stroke="#BE8644" stroke-width="3"`)+ellipse(x+3,y+4,r*.65,r*.5,'#E7B05B')).join('')+line('M20,30 H176','#fff',6,'opacity=".4"'),{finish:'matte'});
add('leek','Bent leek',['X.','X.','X.','XX'],olive,()=>
 line('M26,20 V295 Q25,368 177,369','#F2E9C5',26)+line('M67,17 V299 Q63,334 179,334','#D6E3B4',12)+line('M13,22 V174 M46,20 V192 M78,19 V161','#5B9470',5)+
 line('M122,383 L126,397 M145,382 L151,395 M169,381 L179,391','#B5A576',3)+line('M30,30 V120','#fff',6,'opacity=".4"'),{finish:'matte'});
add('potato_sack','Potato sack',['X..','XX.','.XX'],['#E3C99A','#C3A47A','#9F805F'],()=>
 [[48,47],[48,147],[146,145],[149,247],[249,247]].map(([x,y],i)=>ellipse(x,y,40,43,i%2?'#DFB97C':'#E9CA8E',outline)+circle(x-9,y-10,3,'#A7855A')+circle(x+13,y+14,3,'#A7855A')+ellipse(x-12,y-16,12,7,'#fff','opacity=".35"')).join('')+
 ['M0,13 L289,294','M0,70 L230,294','M15,0 L300,285','M95,0 L0,95','M194,40 L40,194','M293,140 L140,293'].map(d=>line(d,'#8E795A',3,'opacity=".55"')).join(''),{finish:'cloth'});
add('sushi_roll','Sushi roll',['XXX'],['#9ED0AE','#5A8A78','#365B57'],(w,h)=>
 rect(14,12,w-28,h-24,28,'#FFF4DC',outline)+rect(32,28,w-64,h-56,18,'#EE947D')+line(`M58,42 H${w-58}`,'#CFE6A8',13)+line(`M84,54 H${w-84}`,'#F9CF68',12)+
 [45,95,145,195,245].map(x=>line(`M${x},20 L${x+10},26`,'#DBCDAA',3)).join('')+ellipse(w/2,h/2,18,10,'#F26B57'),{finish:'matte'});
add('biscuit','Biscuit pack',['XX'],yellow,(w,h)=>
 rect(12,13,w-24,h-26,20,'#F8D899',outline)+[46,100,154].map(x=>circle(x,50,24,'#DC9E60',`stroke="#BA7B4F" stroke-width="3"`)+[[-8,-8],[7,5],[-6,12]].map(([dx,dy])=>circle(x+dx,50+dy,3,'#925B43')).join('')+ellipse(x-8,40,8,4,'#fff','opacity=".45"')).join(''),{finish:'paper'});
add('fried_egg','Broccoli omelette',['XXX','.X.'],['#FFF8DC','#FFE39F','#E8B95F'],()=>
 ellipse(150,72,56,50,'#FFBB45',outline)+ellipse(136,56,24,14,'#FFE392')+[30,70,228,268].map((x,i)=>circle(x,52+i%2*24,18,'#71B978')+circle(x-4,46+i%2*24,11,'#A1D684')).join('')+
 line('M127,136 Q150,154 177,132','#E3B25A',5)+ellipse(150,160,26,14,'#FFF0C8','opacity=".6"'),{finish:'matte'});
add('lunchbox','Bento box',['XX','XX'],mint,()=>
 rect(15,15,170,170,22,'#FFF6DA',outline)+rect(25,26,66,144,12,'#F7C979')+rect(101,26,72,65,12,'#F2C2A4')+rect(101,101,72,69,12,'#E6D9AA')+line('M95,20 V180 M95,95 H181','#86B496',5)+
 [0,1,2,3].map(i=>rect(33,35+i*32,49,23,10,'#F9F0D7',`stroke="#DABC8A" stroke-width="2"`)).join('')+circle(136,58,23,'#F37062')+ellipse(136,54,11,6,'#FFB199')+circle(127,131,20,'#6AB966')+circle(150,144,20,'#8ED071')+
 rect(4,72,14,49,5,'#F8D184',outline)+rect(183,72,14,49,5,'#F8D184',outline),{finish:'matte'});

// ------------------------------------------------------------------ gear, tools, books
add('book','Pocket journal',['XX'],blue,(w,h)=>
 rect(11,12,w-22,h-24,12,'#F4E7CE',outline)+rect(8,5,w-22,h-22,12,'#77BDEF',outline)+line(`M30,9 V76 M44,84 H${w-23}`,'#3779AD',4)+
 rect(44,18,w-82,34,6,'#FFF8E6','opacity=".9"')+line('M56,30 H120 M56,42 H104','#C9B99A',4)+star(w-44,62,.8,'#FFF3C0')+line(`M14,80 H${w-26}`,'#fff',3,'opacity=".6"'),{finish:'paper'});
add('book_stack','Stack of journals',['XX','XX'],blue,()=>
 rect(12,14,176,50,10,'#F29C8E',outline)+rect(23,24,149,26,4,'#FFF2D4')+rect(8,76,180,50,10,'#A5D8BE',outline)+rect(20,87,148,24,4,'#FFF2D4')+
 rect(15,139,174,48,10,'#93BFE4',outline)+rect(27,149,148,24,4,'#FFF2D4')+line('M43,30 H159 M37,93 H151 M45,155 H158','#DBC3A0',3)+
 ribs(30,26,30,22,4,'#fff',2,.6)+ribs(28,89,30,20,4,'#fff',2,.6)+ribs(34,151,30,20,4,'#fff',2,.6),{finish:'paper'});
add('telescope','Toy telescope',['XXX'],purple,(w,h)=>
 rect(10,12,56,76,12,'#FFE7AB',outline)+ribs(16,18,44,64,4,'#DAB46A',4,.8)+rect(66,22,70,56,8,'#B79EE8',outline)+rect(136,28,70,44,8,'#9A80D6',outline)+rect(206,34,46,32,8,'#7E64C0',outline)+
 rect(252,28,40,44,10,'#FFE7AB',outline)+circle(272,50,12,'#8FC9FF',outline)+line('M22,30 H200','#fff',8,'opacity=".45"')+star(102,50,1.1,'#FFF0D5'),{finish:'metal'});
add('powerbank','Power bank',['XXXX'],steel,(w,h)=>
 rect(10,12,w-20,h-24,18,'#D2DAE4',outline)+rect(24,24,w-48,18,6,'#fff','opacity=".5"')+rect(26,34,48,36,6,'#4A5968',outline)+rect(84,34,48,36,6,'#4A5968',outline)+
 [0,1,2,3].map(i=>circle(w-160+i*26,52,7,i<3?'#73E09A':'#CFD6DE',`stroke="#2F3C4E" stroke-width="2.5"`)).join('')+rect(w-48,36,30,30,8,'#2F3C4E',outline)+line(`M${w-40},51 H${w-26}`,'#AEB8C4',4)+
 line('M150,78 H260','#9AA6B4',4),{finish:'metal'});
add('dumbbell','Travel dumbbell',['XXX'],steel,()=>
 rect(56,34,189,34,10,'#AAB3BF',outline)+ribs(96,38,110,26,12,'#8E99A8',3,.7)+rect(14,10,58,82,14,'#71839A',outline)+rect(228,10,58,82,14,'#71839A',outline)+
 line('M30,26 V70 M244,26 V70','#BDCFD9',8)+line('M86,42 H216','#E3EBE5',6,'opacity=".9"'),{finish:'metal'});
add('wrench_z','Cranked wrench',['XX.','.X.','.XX'],steel,()=>
 rect(14,12,172,70,16,'#6F778C',outline)+rect(38,24,122,44,10,'#D4D7E0',outline)+rect(116,116,66,72,14,'#E8BC7D',outline)+line('M128,136 H170 M128,152 H170 M128,168 H170','#BB8B59',4)+
 rect(114,218,176,70,16,'#7D8598',outline)+rect(142,232,122,40,10,'#D4D7E0',outline)+circle(148,100,8,'#EDF1EC',outline)+circle(198,252,10,'#EDF1EC',outline)+line('M46,36 H120','#fff',6,'opacity=".6"'),{finish:'metal'});
add('toolbox','Travel toolbox',['XXX','XXX'],red,()=>
 rect(96,10,108,36,12,'#5E5466',outline)+rect(113,20,74,19,5,'#F9E3BD')+rect(10,48,280,140,20,'#E99376',outline)+rect(10,48,280,50,16,'#F5BC8C',outline)+line('M15,100 H285','#9C6664',6)+
 rect(62,80,30,46,6,'#FDE0A0',outline)+rect(209,80,30,46,6,'#FDE0A0',outline)+line('M31,138 V162 M268,138 V162','#FFD2AD',8)+circle(150,138,14,'#FFF2CF',outline)+line('M24,58 H276','#fff',6,'opacity=".35"'),{finish:'metal'});
add('iron_l','Travel iron',['XX','X.'],mint,()=>
 rect(17,12,162,70,26,'#CCE8D7',outline)+rect(48,28,104,30,12,'#638F92',outline)+line('M60,43 H140','#fff',5,'opacity=".3"')+shape('M11,90 H88 V178 H11Z','#A9C5CD',outline)+
 line('M24,170 H78','#E8F3EA',9)+circle(34,70,9,'#F5C781',outline)+line('M36,126 H70 M36,146 H70','#63858E',4)+ellipse(50,112,24,6,'#fff','opacity=".5"'),{finish:'metal'});
add('camera_case','Instant camera',['XX','XX'],yellow,()=>
 rect(8,118,184,70,12,'#F7B35A')+circle(103,111,54,'#FFF4D4',outline)+circle(103,111,40,'#554A66',outline)+circle(103,111,27,'#7EABD0')+circle(95,102,11,'#D8F4FF')+
 rect(21,24,54,30,8,'#FFF8E4',outline)+rect(130,23,46,36,9,'#655875',outline)+circle(153,41,9,'#FFE066')+rect(33,164,132,12,5,'#8A6572')+line('M22,74 V104','#fff',8,'opacity=".6"'),{finish:'metal'});
add('toy_drill','Toy drill',['XX','X.'],yellow,()=>
 rect(14,16,134,66,16,'#66BFC6',outline)+rect(150,24,44,50,8,'#788497',outline)+ribs(156,30,32,38,3,'#AFBBC6',4,.9)+rect(20,108,66,78,14,'#F5A463',outline)+rect(72,104,20,34,5,'#5E5262',outline)+
 line('M28,130 H68 M28,148 H68','#CA7754',4)+circle(48,50,18,'#FFE7A3',outline)+line('M30,24 H120','#fff',6,'opacity=".4"'),{finish:'matte'});
add('toy_dinosaur','Toy dinosaur',['XX.','.XX'],mint,()=>
 circle(48,40,14,'#FFF9E7',outline)+circle(51,42,6,INK)+circle(53,40,2,'#fff')+line('M22,66 Q50,82 80,64',INK,4)+shape('M112,101 L126,72 L144,102 L160,75 L180,106Z','#FFE08D',outline)+
 rect(126,150,40,44,10,'#E7E8AC',outline)+rect(234,150,42,42,10,'#E7E8AC',outline)+ellipse(204,140,42,26,'#A1E4B6')+[[196,130],[214,146],[232,132]].map(([x,y])=>circle(x,y,4,'#6FB98A')).join(''),{finish:'matte'});
add('hand_mirror','Hand mirror',['XXX','.X.'],purple,()=>
 ellipse(150,50,138,42,'#F3CA85',outline)+ellipse(150,50,120,30,'url(#glassfill)',outline)+line('M70,66 L114,28 M130,72 L182,25','#DCEFF3',8)+
 rect(120,104,60,84,18,'#DAB5E8',outline)+star(150,144,1,'#FFE8BA')+circle(150,190,8,'#F3CA85',outline),{finish:'glass'});
add('makeup','Makeup compact',['XX','XX'],pink,()=>
 rect(14,14,172,84,18,'#FFF0DC',outline)+rect(25,23,150,62,12,'url(#glassfill)')+line('M42,69 L91,30 M86,73 L139,30','#fff',8,'opacity=".4"')+
 rect(17,108,166,74,14,'#E4A078',outline)+[0,1,2].map(v=>circle(47+v*53,142,21,['#F9C3A7','#EF7E8B','#A86B83'][v],`stroke="#C17E72" stroke-width="3"`)+ellipse(42+v*53,136,8,5,'#fff','opacity=".45"')).join('')+rect(85,184,30,12,5,'#FFE69D',outline),{finish:'glass'});
add('plant_f','Travel plant',['.XX','XX.','.X.'],mint,()=>
 ellipse(155,46,42,37,'#A0D88D',outline)+ellipse(252,46,39,38,'#7FC878',outline)+ellipse(51,145,41,36,'#8DCF80',outline)+line('M153,225 V119 Q152,66 236,48 M151,151 L57,144','#417D64',10)+
 line('M140,60 Q156,40 176,36 M236,60 Q252,40 272,38 M36,150 Q50,130 70,128','#fff',4,'opacity=".5"')+rect(111,211,78,82,14,'#EEAA78',outline)+rect(105,203,90,22,8,'#FFC38F',outline)+line('M129,239 V269','#FFDCB0',7),{finish:'matte'});
add('bath_brush','Long bath brush',['...X','XXXX'],wood,()=>
 rect(12,116,286,68,26,'#E6B882',outline)+line('M28,136 H270','#FFE0A8',9)+line('M40,160 H260','#B98650',4,'opacity=".6"')+rect(310,14,80,170,28,'#FFF0D3',outline)+
 Array.from({length:6},(_,i)=>line(`M322,${34+i*25} H378`,'#D6BB98',6)).join('')+circle(32,150,8,'#6F4D35','opacity=".6"'),{finish:'matte'});

// ------------------------------------------------------------------ context replacements (same footprints as the pieces they replace)
add('shaker','Protein shaker',['XX','XX'],teal,()=>
 rect(40,10,120,40,12,'#3E3246',outline)+ribs(50,16,100,28,8,'#6B5E78',3,.8)+rect(48,54,104,18,6,'#2E8F93',outline)+
 rect(30,78,140,108,18,'#DDF7F5','opacity=".92"')+[100,124,148].map(y=>line(`M48,${y} H152`,'#2E8F93',3,'opacity=".5"')).join('')+rect(60,112,80,50,10,'#63C6C3',outline)+star(100,137,1,'#fff')+line('M46,92 V170','#fff',8,'opacity=".6"'),{finish:'glass'});
add('snorkel','Snorkel',['X.','X.','XX'],orange,()=>
 rect(20,8,60,36,14,'#FFF0C8',outline)+line('M50,48 V210','#FFD4A3',22)+line('M50,48 V210',INK,3,'opacity=".2"')+
 rect(18,96,64,26,8,'#E27E36',outline)+line('M50,220 Q50,268 100,268 H170','#FFD4A3',22)+line('M50,220 Q50,268 100,268 H170',INK,3,'opacity=".2"')+
 rect(160,236,34,64,12,'#3E3246',outline)+line('M34,60 V190','#fff',7,'opacity=".5"'),{finish:'matte'});
add('desk_lamp','Desk lamp',['X.','XX'],yellow,()=>
 shape('M8,14 H92 L80,74 H20Z','#FBD070',outline)+line('M22,24 H78','#fff',6,'opacity=".5"')+line('M50,76 V140 Q50,160 70,160 H140',INK,12)+line('M50,76 V140 Q50,160 70,160 H140','#B4BBCA',6)+
 rect(120,124,70,64,16,'#7E8799',outline)+ellipse(155,176,26,6,'#fff','opacity=".4"')+circle(46,60,6,'#FFF8E6')+circle(155,150,9,'#E27E36',outline),{finish:'metal'});
add('watering_can','Watering can',['X.','XX'],teal,()=>
 line('M50,20 L50,96',INK,20)+line('M50,20 L50,96','#63C6C3',10)+ellipse(50,18,30,14,'#2E8F93',outline)+[[30,16],[50,10],[70,16]].map(([x,y])=>circle(x,y,3,'#fff')).join('')+
 rect(22,104,164,82,22,'#63C6C3',outline)+line('M104,96 Q150,82 182,104',INK,14)+line('M104,96 Q150,82 182,104','#B6ECE8',6)+line('M40,124 H170','#fff',7,'opacity=".45"')+star(104,146,1,'#FFF8E6'),{finish:'metal'});
add('poster_tube','Rolled poster',['XXX'],cream,(w,h)=>
 line('M22,50 H278','#F6E6C6',58)+rect(8,10,30,80,10,'#B4BBCA',outline)+rect(262,10,30,80,10,'#B4BBCA',outline)+line('M50,26 H250','#fff',10,'opacity=".6"')+
 line('M60,70 H240','#DDC79C',5)+rect(110,38,80,24,8,'#F68BA7',outline)+line('M124,50 H176','#fff',4),{finish:'paper'});
add('camp_lantern','Camp lantern',['.XX','XX.','.X.'],orange,()=>
 line('M150,20 Q240,10 272,50',INK,16)+line('M150,20 Q240,10 272,50','#FFD4A3',8)+rect(112,46,76,30,8,'#3E3246',outline)+
 rect(118,80,64,120,14,'#FFF2C6','opacity=".95"')+shape('M150,100 Q134,150 150,186 Q166,150 150,100Z','#FFAB5E','opacity=".9"')+circle(150,142,14,'#FFF8E6','opacity=".95"')+
 line('M24,150 H112',INK,16)+line('M24,150 H112','#B4BBCA',8)+rect(108,206,84,52,12,'#3E3246',outline)+ribs(118,214,64,36,5,'#6B5E78',4,.8)+line('M126,90 V170','#fff',7,'opacity=".6"'),{finish:'metal'});

// ------------------------------------------------------------------ assignments: context, zone and pair decisions are content, never guessed at runtime.
const assignments = [
 ['sock_long','sneaker','makeup'],
 ['snorkel','trainer','perfume','makeup','book'],
 ['sausage','croissant','broccoli','sushi_roll','biscuit','prawn'],
 ['milk_carton','sausage','broccoli','croissant','prawn','broccoli','sandwich','biscuit'],
 ['toy_dinosaur','plush_long','thermos','umbrella','cap','trainer','book'],
 ['sock_long','trainer','shaker','sock_short','book'],
 ['sandwich','croissant','oven_mitt','umbrella','prawn','fried_egg'],
 ['sock_straight','trainer','sock_sport','cap','trainer','sock_short','sock_short','perfume'],
 ['oven_mitt','plush_long','telescope','desk_lamp','toy_drill','watering_can','plush','plush','poster_tube'],
 ['sock_short','sock_long','perfume','cap','perfume','trainer','pump_t','sock_short','sock_straight'],
 ['pillow_v','camp_lantern','thermos','scarf_w','sneaker','lunchbox','carrot'],
 ['baguette_v','baguette','pepper','leek','lunchbox','potato_sack','cheese_u','jar_t'],
 ['towel_p','shampoo_pump','toothpaste','atomizer_f','shampoo_w','toothbrush','perfume','bath_brush'],
 ['hoodie_p','thermos','perfume_upright','umbrella','sock_folded','scarf_w','pump_long','sock_scrunched','wrench_z','powerbank'],
 ['atomizer_short','sock_t','headphones','book','sock_travel','slipper_folded','bottle_tall','thermos','slipper_straight','sock_short','scarf_s','wrench_z','powerbank']
];
function normalize(cells){const minX=Math.min(...cells.map(c=>c[0])),minY=Math.min(...cells.map(c=>c[1]));return cells.map(([x,y])=>[x-minX,y-minY]);}
const key=cells=>normalize(cells).map(c=>c.join(',')).sort().join(';');
const cellsOf=rows=>rows.flatMap((row,y)=>[...row].flatMap((s,x)=>s==='X'?[[x,y]]:[]));
const rotate=cells=>normalize(cells.map(([x,y])=>[-y,x]));
function transformFor(art,solution){for(let mirrored of [false,true]){let cells=normalize(cellsOf(art.cells).map(([x,y])=>[mirrored?-x:x,y]));for(let turns=0;turns<4;turns++){if(key(cells)===key(solution))return {solution_turns:turns,mirrored};cells=rotate(cells);}}throw Error(`No shape match for ${art.id}`);}
const cozyAssignments=JSON.parse(fs.readFileSync(path.join(__dirname,'../level-design/cozy-assignments.json'),'utf8'));
const levels=JSON.parse(fs.readFileSync(levelsPath,'utf8')).levels, bindings=[];
for (const level of levels) if (level.number > 15) assignments[level.number-1] = cozyAssignments[String(level.number)];
for(let li=0;li<levels.length;li++){
 const level=levels[li];if(assignments[li].length!==level.pieces.length)throw Error('Assignment count '+level.number);
 level.pieces.forEach((piece,pi)=>{const id=assignments[li][pi], art=objects.find(a=>a.id===id);if(!art)throw Error(`Level ${level.number} ${piece.letter}: unknown art ${id}`);const cells=[];
  level.map.forEach((row,y)=>[...row].forEach((letter,x)=>{if(letter===piece.letter)cells.push([x,y]);}));
  try{bindings.push({level:level.number,piece:piece.letter,art_id:id,...transformFor(art,cells)});}catch(e){throw Error(`Level ${level.number} ${piece.letter}: ${e.message}`);}
 });
}
// Omit unused work-in-progress illustrations from the runtime catalog (no orphan entries).
const used=new Set(bindings.map(b=>b.art_id));
for(const object of objects)if(!used.has(object.id))fs.unlinkSync(path.join(output,object.sprite+'.png'));
fs.writeFileSync(path.join(output,'item_catalog.json'),JSON.stringify({schema_version:1,objects:objects.filter(o=>used.has(o.id)),bindings},null,2)+'\n');
console.log(`${bindings.length} pieces bound to ${used.size} whole-object illustrations; geometry checked for every binding.`);
