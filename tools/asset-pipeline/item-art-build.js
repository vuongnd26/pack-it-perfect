// Original whole-object illustrations. No external images or generated icons are embedded.
// node item-art-build.js <resvg dependency directory> <output Resources/Art directory> <levels.json>
const fs = require('fs');
const path = require('path');
const [,, runtime, output, levelsPath] = process.argv;
const { Resvg } = require(path.join(runtime, 'node_modules/@resvg/resvg-js'));
const INK = '#49394D', SW = 5, CELL = 100, PPC = 128;
const objects = [];
const rect = (x,y,w,h,r,fill,extra='') => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${r}" fill="${fill}" ${extra}/>`;
const line = (d,color=INK,width=SW,extra='') => `<path d="${d}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round" ${extra}/>`;
const circle = (x,y,r,fill,extra='') => `<circle cx="${x}" cy="${y}" r="${r}" fill="${fill}" ${extra}/>`;
const ellipse = (x,y,rx,ry,fill,extra='') => `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="${fill}" ${extra}/>`;
const outline = `stroke="${INK}" stroke-width="${SW}" stroke-linejoin="round"`;
const star = (x,y,s,c) => `<path transform="translate(${x} ${y}) scale(${s})" d="M0,-12 L3.6,-4 L12,-3 L6,3 L8,12 L0,7 L-8,12 L-6,3 L-12,-3 L-3.6,-4Z" fill="${c}"/>`;
function contour(rows) {
  const cells = new Set(); rows.forEach((row,y)=>[...row].forEach((v,x)=>{if(v==='X')cells.add(`${x},${y}`)}));
  const edges = new Map();
  for(const key of cells){const [x,y]=key.split(',').map(Number), p=[[x,y],[x+1,y],[x+1,y+1],[x,y+1]];
    for(let i=0;i<4;i++){const a=p[i].join(','),b=p[(i+1)%4].join(','),reverse=`${b}:${a}`; if(edges.has(reverse))edges.delete(reverse);else edges.set(`${a}:${b}`,[p[i],p[(i+1)%4]]);}}
  const all=[...edges.values()], vertices=[all[0][0]];let current=vertices[0];
  while(all.length){let i=all.findIndex(e=>e[0][0]===current[0]&&e[0][1]===current[1]);
    if(i<0)throw Error('Unclosed contour');const e=all.splice(i,1)[0];current=e[1];if(current.join(',')===vertices[0].join(','))break;vertices.push(current);}
  // Remove intermediate collinear grid vertices before rounding the silhouette.
  let points=vertices.filter((p,i)=>{const a=vertices[(i+vertices.length-1)%vertices.length],b=vertices[(i+1)%vertices.length];return (p[0]-a[0])*(b[1]-p[1])!==(p[1]-a[1])*(b[0]-p[0]);}).map(p=>p.map(v=>v*CELL));
  const radius=14;let d='';
  points.forEach((p,i)=>{const a=points[(i+points.length-1)%points.length],b=points[(i+1)%points.length];let l=Math.hypot(p[0]-a[0],p[1]-a[1]),n=Math.hypot(b[0]-p[0],b[1]-p[1]);const pre=[p[0]+(a[0]-p[0])*radius/l,p[1]+(a[1]-p[1])*radius/l],post=[p[0]+(b[0]-p[0])*radius/n,p[1]+(b[1]-p[1])*radius/n];d+=`${i?'L':'M'}${pre} Q${p} ${post} `;});return d+'Z';
}
function add(id,name,rows,base,details,customPath=null){
  const w=rows[0].length*CELL,h=rows.length*CELL,d=customPath||contour(rows);
  // Outer white rim + soft contact shade are baked into the object, not a puzzle block beneath it.
  const svg=`<svg xmlns="http://www.w3.org/2000/svg" width="${w*PPC/CELL}" height="${h*PPC/CELL}" viewBox="0 0 ${w} ${h}"><defs>
  <linearGradient id="base" x1="0" y1="0" x2=".75" y2="1"><stop stop-color="${base[0]}"/><stop offset=".55" stop-color="${base[1]}"/><stop offset="1" stop-color="${base[2]}"/></linearGradient>
  <linearGradient id="glass" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#8698C7"/><stop offset="1" stop-color="#44364F"/></linearGradient>
  <clipPath id="clip"><path d="${d}"/></clipPath></defs>
  <g transform="translate(5 5) scale(${(w-10)/w} ${(h-10)/h})"><path d="${d}" fill="url(#base)" ${outline}/><g clip-path="url(#clip)">
  ${line(d,'#fff',8,'opacity=".32" transform="translate(4 5)"')}${line(d,INK,12,'opacity=".12" transform="translate(-2 -7)"')}${details(w,h)}</g><path d="${d}" fill="none" ${outline}/></g></svg>`;
  fs.mkdirSync(path.join(output,'Items'),{recursive:true});
  fs.writeFileSync(path.join(output,'Items',id+'.png'),new Resvg(svg).render().asPng());
  objects.push({id,display_name:name,sprite:'Items/'+id,cells:rows});
}

// ARTWORK — broad forms, object-specific materials, seams and highlights; no per-cell grid.
const pink=['#FFB4C6','#F27C9B','#D34D7A'], blue=['#9BD9FF','#56ADEF','#337CC5'], mint=['#A1EDCF','#57C7AA','#289B88'], yellow=['#FFE9A1','#F8CA62','#DFA04C'], purple=['#DAC5FF','#B297E4','#896BC4'], red=['#FFB29B','#F97B67','#D95554'];
function sock(id,name,rows,palette){add(id,name,rows,palette,(w,h)=>
 rect(6,6,88,34,8,'#FFF6DB',outline)+line('M18,15 V31 M34,15 V31 M50,15 V31 M66,15 V31 M82,15 V31','#D8C9B7',3)+
 `<path d="M${w-43},${h-96} Q${w-5},${h-87} ${w-5},${h-55} V${h-5} H${w-45} Q${w-76},${h-52} ${w-43},${h-96}Z" fill="#FFF6DB" stroke="${INK}" stroke-width="4"/>`+
 `<path d="M0,${h-103} Q57,${h-108} 65,${h-57} Q39,${h-28} 0,${h-36}Z" fill="#FFF6DB" stroke="${INK}" stroke-width="4"/>`+
 line(`M19,56 V${h-130}`,'#fff',10,'opacity=".45"')+star(51,h>200?108:74,.65,'#FFF8DE')+line(`M87,${h-60} Q111,${h-35} ${w-62},${h-38}`,INK,3,'stroke-dasharray="4 6" opacity=".45"'));}
sock('sock_long','Cozy sock',['X.','X.','XX'],red);
sock('sock_short','Ankle sock',['X.','XX'],pink);
sock('sock_sport','Sport sock',['X.','XX'],blue);
sock('sock_tall','Knee sock',['X.','X.','X.','XX'],mint);
add('makeup','Makeup compact',['XX','XX'],pink,(w,h)=>rect(14,14,172,83,17,'#FFF0DC',outline)+rect(25,23,150,61,12,'url(#glass)')+line('M42,69 L91,30 M86,73 L139,30','#fff',8,'opacity=".4"')+rect(17,108,166,73,14,'#E4A078',outline)+[0,1,2].map((v)=>circle(47+v*53,142,21,['#F9C3A7','#EF7E8B','#A86B83'][v],`stroke="#C17E72" stroke-width="3"`)).join('')+rect(85,183,30,12,5,'#FFE69D',outline));
add('lunchbox','Bento box',['XX','XX'],mint,()=>rect(15,15,170,170,22,'#FFF5D7',outline)+rect(25,26,66,144,12,'#F7C979')+rect(101,26,72,65,12,'#F2C2A4')+rect(101,101,72,69,12,'#E6D9AA')+line('M95,20 V180 M95,95 H181','#86B496',5)+[0,1,2,3].map(i=>rect(33,35+i*32,49,23,10,'#F9F0D7',`stroke="#DABC8A" stroke-width="2"`)).join('')+circle(136,58,23,'#F37062')+ellipse(136,54,11,6,'#FFB199')+circle(127,131,20,'#6AB966')+circle(150,144,20,'#8ED071')+rect(4,72,14,49,5,'#F8D184',outline)+rect(183,72,14,49,5,'#F8D184',outline));
add('camera_case','Instant camera',['XX','XX'],yellow,()=>rect(8,118,184,69,12,'#F7B35A')+circle(103,111,53,'#FFF4D4',outline)+circle(103,111,39,'#554A66',outline)+circle(103,111,26,'#7EABD0')+circle(95,102,11,'#D8F4FF')+rect(21,24,53,29,7,'#FFF8E4',outline)+rect(130,23,45,35,9,'#655875',outline)+rect(33,164,131,12,5,'#8A6572')+line('M22,74 V103','#fff',8,'opacity=".6"'));
add('gift','Gift box',['XX','XX'],purple,()=>rect(83,0,34,200,3,'#FFE6A0',outline)+rect(0,70,200,29,3,'#FFE6A0',outline)+`<path d="M98,68 C25,64 30,10 65,24 Q86,30 98,68 C161,4 190,58 111,72Z" fill="#FFDB79" ${outline}/>`+circle(102,70,15,'#FFECAA',outline)+line('M20,119 V166','#fff',8,'opacity=".5"'));
function bar(id,name,n,palette,detail){add(id,name,['X'.repeat(n)],palette,(w,h)=>detail(w,h));}
bar('book','Pocket journal',2,blue,(w,h)=>rect(11,12,w-22,h-24,12,'#F4E7CE',outline)+rect(8,5,w-22,h-22,12,'#77BDEF',outline)+line(`M30,9 V76 M44,84 H${w-23}`,'#3779AD',4)+star(w/2+12,43,1,'#FFF3C0')+line('M48,20 H77','#D5F1FF',6));
bar('thermos','Vacuum flask',3,mint,(w,h)=>rect(w-62,9,53,82,12,'#4F6868',outline)+rect(w-75,14,15,72,3,'#FFE2A0',outline)+line(`M22,22 H${w-92}`,'#E0FFF0',12)+line(`M30,73 H${w-100}`,INK,3,'opacity=".3"')+ellipse(55,48,23,26,'#FFF5D8')+star(55,48,.85,'#70C6B0'));
bar('toothpaste','Toothpaste tube',3,blue,(w,h)=>rect(w-58,10,50,80,10,'#FFF8DF',outline)+line(`M${w-45},23 V77 M${w-28},23 V77`,'#D9D7C9',4)+rect(20,18,w-102,64,13,'#F9FFF0')+line(`M38,63 Q83,13 132,48 T${w-93},48`,'#59BBA7',13));
bar('closed_umbrella','Folded umbrella',4,purple,(w,h)=>`<path d="M12,49 Q25,6 65,8 H${w-95} V91 H65 Q24,91 12,49Z" fill="#BFA4EA" ${outline}/>`+line(`M56,30 H${w-107} M56,70 H${w-107}`,'#8066B4',4)+rect(w-117,8,23,84,4,'#FFE2A0',outline)+line(`M${w-87},50 H${w-60} Q${w-12},50 ${w-12},26 Q${w-11},10 ${w-31},12`,INK,14)+line(`M${w-85},48 H${w-60} Q${w-18},48 ${w-18},27`,'#E8B77A',6));
bar('crayon','Jumbo crayon',4,red,(w,h)=>rect(38,8,w-90,84,5,'#FFE4B9',outline)+line(`M62,18 V82 M${w-77},18 V82`,'#E57861',9)+`<path d="M7,50 L38,11 V89Z" fill="#C95751" ${outline}/>`+star(w/2,50,1.5,'#EC816B')+line(`M90,25 H${w-115}`,'#fff',5,'opacity=".5"'));
bar('sushi_roll','Sushi roll',3,['#94C3A4','#537E70','#365B57'],(w,h)=>rect(14,12,w-28,h-24,26,'#FFF2D8',outline)+rect(31,27,w-62,h-54,17,'#EE947D')+line(`M56,40 H${w-57}`,'#CFE6A8',13)+line(`M83,51 H${w-83}`,'#F9CF68',12)+[45,95,145,195,245].map(x=>line(`M${x},19 L${x+10},25`,'#DBCDAA',3)).join(''));
bar('biscuit','Biscuit pack',2,yellow,(w,h)=>rect(12,13,w-24,h-26,18,'#F8D899',outline)+[45,100,155].map(x=>circle(x,50,24,'#DC9E60',`stroke="#BA7B4F" stroke-width="3"`)+[[-8,-8],[7,5],[-6,12]].map(([dx,dy])=>circle(x+dx,50+dy,3,'#925B43')).join('')).join(''));

// The T silhouette is the umbrella itself: broad canopy and a chunky wrapped handle.
add('umbrella','Open umbrella',['XXX','.X.'],blue,()=>`<path d="M4,88 Q150,-28 296,88 Q245,72 201,97 Q150,75 100,97 Q45,72 4,88Z" fill="#6EC7EB" ${outline}/>`+`<path d="M150,5 Q105,27 101,95 Q150,75 201,97 Q195,28 150,5Z" fill="#FFE09A" ${outline}/>`+line('M150,107 V153 Q150,180 172,180 Q190,180 190,163',INK,18)+line('M150,107 V153 Q150,180 172,180 Q185,180 185,163','#E1A378',9)+rect(112,122,26,56,12,'#EAF6FA',`opacity=".3"`));
add('cap','Baseball cap',['XXX','.X.'],yellow,()=>`<path d="M5,88 Q22,3 102,5 H195 Q277,4 295,89Z" fill="#F7BE66" ${outline}/>`+line('M150,9 V88','#D08D4E',4)+`<path d="M104,92 H196 V178 Q151,208 104,178Z" fill="#F18A69" ${outline}/>`+star(150,49,1.5,'#FFF2BC')+line('M38,57 Q60,21 99,21','#FFE6A3',9));
add('fried_egg','Broccoli omelette',['XXX','.X.'],['#FFF6D1','#FFE098','#E8B95F'],()=>ellipse(150,71,53,48,'#FFBB45',outline)+ellipse(138,57,23,13,'#FFE392')+[30,70,227,268].map((x,i)=>circle(x,52+i%2*24,18,'#71B978')+circle(x-4,46+i%2*24,11,'#A1D684')).join('')+line('M127,135 Q150,153 177,130','#E3B25A',5));

// Bent objects occupy an L naturally, without a backing polyomino.
add('sandwich','Sandwich',['X.','XX'],['#FFE6AE','#E8BC7B','#D29C62'],()=>line('M13,19 H80 V119 H185 V184 H18Z','#FFF0C7',12)+line('M22,88 H83 V125 H173','#81BE70',12)+line('M27,102 H80 V140 H173','#ED8D79',9)+circle(47,44,4,'#CF9A67')+circle(54,65,4,'#CF9A67')+circle(123,165,4,'#CF9A67'));
add('toy_drill','Toy drill',['XX','X.'],yellow,()=>rect(14,16,133,65,15,'#66BFC6',outline)+rect(150,23,43,50,7,'#788497',outline)+line('M166,26 V70 M179,26 V70','#AFBBC6',4)+rect(20,108,66,77,13,'#F5A463',outline)+rect(71,104,19,34,5,'#5E5262',outline)+line('M28,130 H68 M28,148 H68','#CA7754',4)+circle(48,49,17,'#FFE7A3',outline));
add('hairdryer','Hair dryer',['XX','XX','X.'],pink,()=>rect(14,16,172,167,36,'#F49AAE',outline)+circle(101,91,64,'#FFF1D8',outline)+circle(101,91,49,'#7B6279',outline)+Array.from({length:7},(_,i)=>line(`M${64+i*12},58 V124`,'#B39BAF',4)).join('')+rect(17,202,65,83,16,'#B395DB',outline)+rect(58,219,15,27,5,'#FFEAA9',outline));
add('water_bottle','Sports bottle',['X','X','X'],mint,(w,h)=>rect(12,8,76,44,11,'#557E89',outline)+line('M29,17 V40 M47,17 V40 M65,17 V40','#A5CBCC',4)+rect(14,109,72,109,10,'#EFFFF1',outline)+ellipse(50,161,23,30,'#8ED5B5')+line('M26,65 V93 M25,238 V267','#D6FFF1',9));
add('toothbrush','Travel toothbrush',['X','X','X','X'],mint,(w,h)=>rect(12,10,76,136,20,'#FFF8E5',outline)+Array.from({length:7},(_,i)=>line(`M24,26 ${'L'}76,${26+i*16}`,'#8DCED8',7)).join('')+rect(28,181,44,171,22,'#41A995')+line('M43,203 V323','#B1F3D4',10)+circle(50,373,12,'#E7F9DB',outline));
add('perfume','Perfume atomizer',['X.','XX'],purple,()=>rect(14,10,71,65,12,'#FCE5A3',outline)+rect(24,83,51,21,5,'#EFC778',outline)+rect(17,123,164,62,14,'#DBC5F5',outline)+ellipse(118,150,39,24,'#FFF1CD')+star(118,150,.85,'#B68BC9')+line('M29,121 V159','#fff',10,'opacity=".6"'));
add('milk_carton','Milk carton',['XX','X.','X.'],blue,()=>`<path d="M4,87 L29,9 H176 L195,88Z" fill="#FFF3D7" ${outline}/>`+line('M29,12 L84,83 M88,10 L104,86','#A0D4E9',5)+rect(9,117,80,117,8,'#FFF5DA',outline)+ellipse(49,173,30,37,'#73BCD2')+`<path d="M45,145 Q18,172 43,192 Q75,200 66,170Z" fill="#FFF6DC"/>`+line('M23,254 H76 M23,270 H76','#397CBB',5));
add('sneaker','High-top sneaker',['X.','X.','XX'],blue,()=>rect(9,9,83,44,11,'#FFF4DE',outline)+`<path d="M12,240 Q55,213 105,232 L185,249 V281 H12Z" fill="#FFDD83" ${outline}/>`+rect(7,281,187,15,5,'#FFF7E4',outline)+line('M70,92 L25,106 L72,123 L27,141 L73,158','#FFF6E4',8)+line('M12,205 Q55,192 84,219','#285D98',4)+circle(44,176,17,'#E6F4F0',outline));
// Unusual shapes correspond to soft/folded items and moulded toys, not distorted everyday rigid icons.
add('scarf_s','Folded scarf',['XX.','.XX'],purple,()=>line('M18,27 H173 Q180,69 218,75 H278','#FCE4AD',22)+line('M26,57 H148 Q159,112 211,130 H278','#EFE0FF',9)+[0,1,2,3,4].map(i=>line(`M${216+i*14},162 V190`,'#FFE9B7',5)).join(''));
add('headphones','Travel headphones',['X.X','XXX'],purple,()=>line('M45,63 V147 Q150,180 255,147 V63','#E8DAFF',24)+rect(15,13,66,99,22,'#FFCE87',outline)+rect(218,13,66,99,22,'#FFCE87',outline)+line('M34,31 V88 M237,31 V88','#FFF1C5',9)+line('M89,155 H213','#8065A9',5));
add('scarf_w','Zigzag scarf',['X..','XX.','.XX'],red,()=>line('M23,16 V109 Q24,145 108,146 Q156,144 153,216 Q153,257 276,252','#FFE3AE',27)+line('M66,19 V83 Q68,105 143,105 Q195,102 195,176 Q198,215 279,210','#F4B3C2',12)+[0,1,2,3].map(i=>line(`M${229+i*16},267 V290`,'#FFE3AE',5)).join(''));
add('scarf_z','Folded wool scarf',['XX.','.X.','.XX'],mint,()=>line('M19,29 H149 V244 H279','#FFE3A2',26)+line('M22,62 H117 V275 H278','#CBF4DB',10)+[0,1,2,3,4].map(i=>line(`M${216+i*15},280 V293`,'#FFE3A2',4)).join(''));
add('plant_f','Travel plant',['.XX','XX.','.X.'],mint,()=>ellipse(155,46,42,37,'#A0D88D',outline)+ellipse(252,46,39,38,'#7FC878',outline)+ellipse(51,145,41,36,'#8DCF80',outline)+line('M153,225 V119 Q152,66 236,48 M151,151 L57,144','#417D64',10)+rect(111,211,77,82,13,'#EEAA78',outline)+rect(105,203,89,22,8,'#FFC38F',outline)+line('M129,239 V269','#FFDCB0',7));
add('pillow_v','Neck pillow',['XXX','X..','X..'],pink,()=>line('M29,260 V66 Q29,31 62,31 H275','#FFD9DF',18)+line('M73,270 V88 Q78,73 88,74 H272','#C75984',4,'stroke-dasharray="5 7"')+circle(54,175,8,'#FFE3D6')+circle(54,228,8,'#FFE3D6')+star(185,49,1,'#FFECC7'));
add('toy_dinosaur','Toy dinosaur',['XX.','.XX'],mint,()=>circle(47,40,13,'#FFF9E7',outline)+circle(50,42,6,INK)+line('M21,65 Q49,80 78,64',INK,4)+`<path d="M112,101 L126,72 L143,102 L159,75 L178,105Z" fill="#FFE08D" ${outline}/>`+rect(125,149,39,44,8,'#E7E8AC',outline)+rect(234,150,41,42,8,'#E7E8AC',outline)+ellipse(204,140,40,24,'#A1E4B6'));
// Context variants — the silhouette is explained by the object's own material and construction.
function ovenMitt(id,rows,palette){add(id,'Oven mitt',rows,palette,(w,h)=>rect(10,h-47,79,37,9,'#FFF0D4',outline)+line(`M20,20 L${w-20},${h-70} M20,65 L${w-65},${h-25} M65,18 L18,65 M${w-22},${h-100} L75,${h-5}`,'#B96765',3,'opacity=".45"')+line(`M22,${h-60} V${h-100}`,'#FFE5D4',8));}
ovenMitt('oven_mitt',['X.','XX'],red);
function ankleShoe(id,rows,palette){add(id,'Trainer',rows,palette,()=>rect(14,11,69,30,8,'#FFF4DF',outline)+line('M19,165 H183 M20,179 H185','#FFF5DC',10)+line('M71,62 L25,77 L76,91 L29,108','#FFF7E7',8)+line('M96,121 Q144,111 179,142','#285F99',5)+star(51,135,.9,'#FFE29B'));}
ankleShoe('trainer',['X.','XX'],blue);
function plush(id,rows){add(id,'Cuddly bear',rows,yellow,(w,h)=>circle(25,23,20,'#D69061',outline)+circle(75,23,20,'#D69061',outline)+ellipse(50,58,39,37,'#E9B47B',outline)+ellipse(50,72,23,16,'#FFE1B1')+circle(35,55,4,INK)+circle(66,55,4,INK)+ellipse(50,66,7,5,INK)+line('M50,71 V79 M43,80 Q50,85 57,80',INK,3)+ellipse(53,h-55,34,42,'#FFE5B3',outline)+ellipse(w-44,h-38,34,26,'#D6956B',outline)+rect(14,98,75,19,7,'#79BCB4',outline));}
plush('plush',['X.','XX']);
plush('plush_long',['X.','X.','XX']);
add('croissant','Butter croissant',['X.','XX'],yellow,()=>line('M30,19 Q64,30 67,75 Q68,130 113,144 Q150,154 184,164','#FFEDB9',12)+line('M20,75 Q49,69 85,88 M29,112 Q56,95 93,117 M86,162 Q105,127 128,128 M138,184 L158,144','#BB7D48',5)+line('M20,27 Q10,45 16,61','#FFF4CD',7));
add('prawn','Cooked prawn',['X.','XX'],red,()=>line('M17,38 H83 M15,71 H84 M16,108 H87 M34,146 L84,122 M83,177 L104,138 M127,187 L139,146','#FFE7C0',9)+`<path d="M168,122 L196,107 L193,163 L176,191 L159,159Z" fill="#E65E58" ${outline}/>`+line('M27,22 L67,22','#FFF5DF',9));
add('broccoli','Broccoli floret',['X.','XX'],mint,()=>rect(94,126,84,56,19,'#DFE8A5',outline)+line('M110,146 H164','#B8CD83',5)+[[42,38,33],[65,79,32],[35,113,29],[65,143,28]].map(([x,y,r])=>circle(x,y,r,'#6DB77B',outline)+ellipse(x-8,y-8,r*.5,r*.3,'#A5D88F')).join(''));
add('sausage','Smoked sausage',['X.','X.','XX'],red,()=>line('M25,19 V221 Q25,264 174,266','#FFBEA0',15)+line('M10,98 L86,75 M10,167 L86,144 M115,206 L101,289','#AD514A',5)+line('M31,28 V59','#FFE8C9',8));
add('baguette_v','Baked loaf',['XXX','X..','X..'],yellow,()=>line('M20,266 V62 Q20,23 72,23 H275','#FFF0BD',15)+['M32,89 L77,61','M26,151 L75,125','M25,222 L73,196','M112,13 L99,78','M184,13 L172,78','M253,13 L241,78'].map(d=>line(d,'#BC834E',7)).join(''));
bar('baguette','Baguette',3,yellow,(w,h)=>line('M18,23 H280','#FFF0BF',10)+[60,120,180,240].map(x=>line(`M${x},14 L${x-20},84`,'#BC834E',6)).join(''));
bar('carrot','Garden carrot',4,['#FFDC93','#FFAC65','#E57C4A'],(w,h)=>rect(8,9,69,82,16,'#86BE7A',outline)+line('M28,20 V78 M47,20 V78 M65,20 V78','#C5DB91',5)+[120,185,250,315].map(x=>line(`M${x},18 V40 M${x+20},62 V83`,'#CB753F',5)).join('')+line(`M100,20 H${w-45}`,'#FFE2A1',8));
add('pepper','Sweet pepper',['XX','XX','X.'],red,()=>line('M100,18 Q119,56 160,61','#80AD68',18)+line('M26,74 Q5,142 33,235 M80,77 Q107,132 75,255 M147,88 Q175,125 153,179','#C95F56',5)+line('M28,82 Q17,122 24,148','#FFE0B6',12)+ellipse(65,64,28,19,'#91BC73',outline));
add('cheese_u','Hard cheese',['XX','X.','XX'],yellow,()=>line('M12,17 H182 M15,276 H183','#FFEBA6',10)+[[52,50,18],[136,46,13],[47,145,17],[42,248,13],[139,251,20]].map(([x,y,r])=>circle(x,y,r,'#D79A4D',`stroke="#BE8644" stroke-width="3"`)+ellipse(x+3,y+4,r*.65,r*.5,'#E7B05B')).join(''));
add('potato_sack','Potato sack',['X..','XX.','.XX'],['#DDC392','#BC9E76','#9F805F'],()=>[[48,47],[48,147],[146,145],[149,247],[249,247]].map(([x,y],i)=>ellipse(x,y,40,43,i%2?'#DFB97C':'#E9CA8E',outline)+circle(x-9,y-10,3,'#A7855A')+circle(x+13,y+14,3,'#A7855A')).join('')+['M0,13 L289,294','M0,70 L230,294','M15,0 L300,285','M95,0 L0,95','M194,40 L40,194','M293,140 L140,293'].map(d=>line(d,'#8E795A',3,'opacity=".6"')).join(''));
add('leek','Bent leek',['X.','X.','X.','XX'],['#CCE3AA','#A0C68D','#72AA80'],()=>line('M26,20 V295 Q25,368 177,369','#F2E9C5',26)+line('M67,17 V299 Q63,334 179,334','#D6E3B4',12)+line('M13,22 V174 M46,20 V192 M78,19 V161','#5B9470',5)+line('M122,383 L126,397 M145,382 L151,395 M169,381 L179,391','#B5A576',3));
add('jar_t','Glass food jar',['XXX','.X.'],['#FADEAA','#EBC689','#D5A86B'],()=>rect(8,9,284,72,15,'#9FC7B0',outline)+line('M25,25 V64 M52,25 V64 M80,25 V64 M109,25 V64 M138,25 V64 M167,25 V64 M196,25 V64 M225,25 V64 M254,25 V64 M279,25 V64','#D8E8BE',5)+rect(111,116,78,68,15,'#FFF2D0',outline)+ellipse(150,147,25,20,'#DDA56E')+line('M122,131 V154','#fff',8,'opacity=".5"'));
add('pump_t','Pump dispenser',['XXX','.X.'],mint,()=>rect(7,9,286,77,14,'#FFF1D4',outline)+line('M20,61 H278','#B1D6CA',7)+rect(117,111,66,76,12,'#89D1C4',outline)+ellipse(150,149,22,26,'#FFF8DC')+line('M133,127 V155','#fff',8,'opacity=".6"'));
add('bath_brush','Long bath brush',['...X','XXXX'],yellow,()=>rect(12,116,284,67,24,'#E6B882',outline)+line('M27,135 H271','#FFE0A8',9)+rect(310,14,79,170,26,'#FFF0D3',outline)+Array.from({length:6},(_,i)=>line(`M321,${34+i*25} H378`,'#D6BB98',6)).join(''));
add('atomizer_f','Perfume atomizer',['.XX','XX.','.X.'],purple,()=>rect(117,12,170,73,15,'#FFE0A0',outline)+rect(178,21,30,55,7,'#D2AA74',outline)+ellipse(50,150,41,35,'#E6A2BD',outline)+line('M90,148 H127',INK,10)+rect(115,113,71,174,17,'#B2BCE8',outline)+rect(126,174,49,61,10,'#FFF0D4',outline)+star(150,203,1,'#B297D9')+line('M127,127 V154','#E8F1FF',9));
add('shampoo_w','Shampoo bottle',['X..','XX.','.XX'],blue,()=>rect(12,8,76,57,10,'#FFDE9F',outline)+line('M30,20 V51 M50,20 V51 M70,20 V51','#CC9F6E',4)+line('M22,89 V158 H148 V244 H274','#CEE8F0',13)+rect(116,214,154,63,12,'#FFF0D4',outline)+line('M136,242 Q175,214 211,245 T254,240','#72B8BE',9));
add('pump_long','Tall pump bottle',['XX','.X','.X'],pink,()=>rect(9,9,181,62,12,'#FFF1D6',outline)+rect(116,75,67,21,5,'#E8C48F',outline)+rect(114,131,70,105,11,'#FFE7D4',outline)+ellipse(149,179,25,32,'#C79FCE')+line('M126,251 V275','#FFCFD4',9));
add('bottle_tall','Travel bottle',['X.','X.','X.','XX'],mint,()=>rect(13,9,74,48,11,'#FFE7B8',outline)+line('M28,20 V45 M48,20 V45 M68,20 V45','#D6B58A',4)+line('M25,80 V227','#DAF8DC',12)+rect(17,239,69,122,11,'#FFF0D8',outline)+star(51,289,1.4,'#73B6A2')+line('M111,368 H173','#A5E5BF',9));
add('towel_p','Folded towel',['XXX','.XX'],purple,()=>line('M16,24 H280 M118,124 H279','#F1DEFF',12)+line('M17,66 H281 M121,164 H280','#8B6EAD',4,'stroke-dasharray="5 7"')+line('M133,88 H274','#DFC8F3',7));
add('hoodie_p','Folded hoodie',['XXX','.XX'],blue,()=>`<path d="M132,17 Q185,4 191,69 Q158,94 123,69Z" fill="#8BC8ED" ${outline}/>`+line('M147,72 V123 M176,72 V123','#FFF1D6',5)+rect(134,136,131,47,13,'#4891C1',outline)+line('M20,25 H92 M30,48 H92','#C4E7F6',8));
function foldedSock(id,rows,palette){add(id,'Cozy sock',rows,palette,(w,h)=>rect(w-88,9,76,38,9,'#FFF3D6',outline)+line(`M${w-74},17 V37 M${w-57},17 V37 M${w-40},17 V37 M${w-23},17 V37`,'#D8BFA8',3)+line(`M20,${h-34} Q${w/2},${h-4} ${w-24},${h-70}`,INK,3,'stroke-dasharray="5 6" opacity=".5"')+star(w/2,h*.6,1,'#FFF1D4')+line(`M24,${h/2} V${h-65}`,'#FFCCD6',9));}
foldedSock('sock_folded',['XX','XX'],pink);
foldedSock('sock_scrunched',['.X.','XXX','..X'],pink);
foldedSock('sock_t',['X.','XX','X.'],pink);
bar('sock_straight','Cozy sock',3,pink,(w,h)=>rect(w-56,9,45,80,12,'#FFF0D5',outline)+line(`M${w-43},24 V75 M${w-27},24 V75`,'#DBC2AC',4)+ellipse(24,50,22,34,'#FFF0D5',outline)+star(w/2,50,1.1,'#FFF1D4')+line(`M59,24 H${w-91}`,'#FFD1DB',10));
function slipper(id,rows){add(id,'Packable slipper',rows,blue,(w,h)=>rect(12,12,w-24,h-24,18,'#FFE4B3',outline)+rect(20,19,w-40,h-40,14,'#7BBFE9',outline)+line(`M25,${h*.4} Q${w/2},${h*.25} ${w-25},${h*.4}`,'#397BA8',6)+star(w/2,h*.65,1.3,'#FFF0C2')+line(`M35,28 H${w-40}`,'#CFEFFC',8));}
slipper('slipper_folded',['XX','XX']);slipper('slipper_straight',['XXX']);
bar('telescope','Toy telescope',3,purple,(w,h)=>rect(10,9,40,82,10,'#FFE7AB',outline)+rect(w-67,9,57,82,10,'#FFE7AB',outline)+line(`M68,23 H${w-88}`,'#EBD8FF',12)+star(w/2,54,1.3,'#FFF0D5'));
bar('powerbank','Power bank',4,blue,(w,h)=>rect(11,9,56,82,12,'#D4DDE6',outline)+rect(22,31,31,38,5,'#4A5968',outline)+rect(w-85,23,56,54,8,'#D5F4D4',outline)+[0,1,2].map(i=>rect(w-77+i*14,35,9,30,2,'#73BD90')).join('')+line(`M86,25 H${w-110}`,'#C3E7FD',9));
add('wrench_z','Cranked wrench',['XX.','.X.','.XX'],['#D9DCE2','#A8ADBD','#7D8399'],()=>rect(14,12,170,69,14,'#6F778C',outline)+rect(38,23,121,44,10,'#D4D7E0',outline)+rect(116,117,65,70,12,'#E8BC7D',outline)+line('M127,137 H170 M127,152 H170 M127,167 H170','#BB8B59',4)+rect(115,219,174,68,14,'#7D8598',outline)+rect(142,232,121,40,10,'#D4D7E0',outline)+circle(147,100,7,'#EDF1EC',outline)+circle(197,251,10,'#EDF1EC',outline));

sock('sock_travel','Cozy sock',['X.','X.','XX'],pink);
add('atomizer_short','Travel atomizer',['XX','X.'],purple,()=>rect(10,9,182,69,13,'#FFE1A4',outline)+rect(113,22,64,25,6,'#C4A2D2',outline)+rect(14,116,72,71,13,'#CFB3EB',outline)+ellipse(50,151,25,24,'#FFF0D4')+star(50,151,.85,'#B58CD1')+line('M25,124 V146','#F2E6FD',8));

add('perfume_upright','Perfume atomizer',['.X','XX'],purple,()=>rect(113,9,74,57,10,'#FFE4A5',outline)+rect(124,75,53,24,6,'#E5C18C',outline)+rect(13,115,172,70,14,'#D7B7EB',outline)+ellipse(104,149,38,23,'#FFF0D3')+star(104,149,1,'#AA87C8')+line('M27,128 V157','#F8E6FD',9));
add('shampoo_pump','Shampoo dispenser',['XX','X.','X.'],mint,()=>rect(10,9,179,58,12,'#FFF0D4',outline)+rect(17,78,67,22,6,'#D5B992',outline)+rect(16,135,69,106,12,'#E9F5D6',outline)+ellipse(50,180,23,30,'#82CBB0')+line('M27,110 V126 M27,254 V278','#D8FFEA',9)+line('M113,49 H177','#D5D4BD',6));

// Explicit assignments: context/zone/pair decisions are content, never guessed at runtime.
const assignments = [
 ['sock_long','sneaker','makeup'],
 ['sock_long','trainer','perfume','makeup','book'],
 ['sausage','croissant','broccoli','sushi_roll','biscuit','prawn'],
 ['milk_carton','sausage','broccoli','croissant','prawn','broccoli','sandwich','biscuit'],
 ['toy_dinosaur','plush_long','thermos','umbrella','cap','trainer','book'],
 ['sock_long','trainer','camera_case','sock_short','book'],
 ['sandwich','croissant','oven_mitt','umbrella','prawn','fried_egg'],
 ['sock_straight','trainer','sock_sport','cap','trainer','sock_short','sock_short','perfume'],
 ['oven_mitt','plush_long','telescope','croissant','toy_drill','sandwich','plush','plush','baguette'],
 ['sock_short','sock_long','perfume','cap','perfume','trainer','pump_t','sock_short','sock_straight'],
 ['pillow_v','plant_f','thermos','scarf_w','sneaker','lunchbox','carrot'],
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
const levels=JSON.parse(fs.readFileSync(levelsPath,'utf8')).levels, bindings=[];
for(let li=0;li<levels.length;li++){
 const level=levels[li];if(assignments[li].length!==level.pieces.length)throw Error('Assignment count '+level.number);
 level.pieces.forEach((piece,pi)=>{const id=assignments[li][pi], art=objects.find(a=>a.id===id),cells=[];
  level.map.forEach((row,y)=>[...row].forEach((letter,x)=>{if(letter===piece.letter)cells.push([x,y]);}));
  try{bindings.push({level:level.number,piece:piece.letter,art_id:id,...transformFor(art,cells)});}catch(e){throw Error(`Level ${level.number} ${piece.letter}: ${e.message}`);}
 });
}
// Omit unused work-in-progress illustrations from the runtime catalog (no orphan entries).
const used=new Set(bindings.map(b=>b.art_id));
for(const object of objects)if(!used.has(object.id))fs.unlinkSync(path.join(output,object.sprite+'.png'));
fs.writeFileSync(path.join(output,'item_catalog.json'),JSON.stringify({schema_version:1,objects:objects.filter(o=>used.has(o.id)),bindings},null,2)+'\n');
console.log(`${bindings.length} pieces bound to ${used.size} whole-object illustrations; geometry checked for every binding.`);
