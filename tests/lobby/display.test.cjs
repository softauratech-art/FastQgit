const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
class Element {
    constructor(tag) { this.tag = tag; this.children = []; this.textContent = ''; this.dataset = {}; this.attributes = {}; this.scrollTop = 0; this.scrollHeight = 900; this.clientHeight = 300; }
    appendChild(child) { this.children.push(child); }
    replaceChildren() { this.children = []; }
    setAttribute(name, value) { this.attributes[name] = value; }
    get text() { return this.textContent + this.children.map(c => c.text).join(' '); }
}
const ids = Object.fromEntries(['queues','updated','clock','date','slideLabel','previous','next','pause','fullscreen','connectionLabel','rotationLabel'].map(id => [id,new Element(id)]));
const intervals = [], timeouts = new Map(); let timer=0;
let response = {ok:true,status:200,json:async()=>({queues:Array.from({length:4},(_,i)=>({id:i,name:'Queue '+i,serving:[],waiting:Array.from({length:7},(_,j)=>({name:j===0?'<img src=x onerror=alert(1)>':'Person '+j,time:'10:30 AM',kind:'Walk-in'}))}))})};
const goodResponse = response;
const body = {dataset:{feed:'/Lobby/Data'}};
const window = {innerWidth:1600,innerHeight:1200,scrollY:0,scrollTo(x,y){this.scrollY=y;},scrollBy(x,y){this.scrollY+=y;},addEventListener(){}};
vm.runInNewContext(fs.readFileSync('src/FastQ.Web/Scripts/lobby-display.js','utf8'), {
 document:{documentElement:{scrollHeight:1800},getElementById:id=>ids[id],createElement:tag=>new Element(tag),body:body},window,
 fetch:async()=>response,AbortController,Date,
 setInterval:(fn,delay)=>intervals.push({fn,delay}),setTimeout:(fn,delay)=>{timeouts.set(++timer,{fn,delay});return timer;},clearTimeout:id=>timeouts.delete(id)
});
async function settle(){await new Promise(resolve=>setImmediate(resolve));}
(async()=>{
 await settle(); assert.equal(ids.queues.children.length,3);
 assert.equal(body.dataset.state,'live'); assert.equal(ids.connectionLabel.textContent,'Live updates');
 assert.equal(ids.queues.attributes['aria-busy'],'false');
 assert.match(ids.queues.text,/Arrived/);
 assert.match(ids.queues.text,/<img src=x onerror=alert\(1\)>/); // Rendered as literal text, never HTML.
 assert.equal(ids.queues.children[0].children[2].children[1].children.filter(c=>c.className==='person').length,7);
 assert.match(ids.queues.text,/10:30 AM/); assert.match(ids.queues.text,/Person 6/);
 ids.next.onclick(); assert.match(ids.queues.text,/Queue 3/);
 ids.next.onclick(); assert.match(ids.queues.text,/Person 2/);
 ids.next.onclick(); ids.next.onclick(); assert.match(ids.queues.text,/Person 6/);
 ids.pause.onclick(); assert.equal(ids.rotationLabel.textContent,'Scrolling and rotation paused'); const before=ids.slideLabel.textContent;
 const tick=intervals.find(x=>x.delay===100).fn;
 for(let i=0;i<400;i++) tick(); assert.equal(ids.slideLabel.textContent,before); assert.equal(window.scrollY,0);
 ids.pause.onclick(); for(let i=0;i<100;i++) tick(); assert.equal(ids.slideLabel.textContent,before); assert.equal(window.scrollY,0); assert.ok(ids.queues.children[0].children[2].children[1].scrollTop>0);
 const reviewedList = ids.queues.children[0].children[2].children[1];
 for(let i=0;i<160;i++) tick();
 assert.equal(ids.slideLabel.textContent,before);
 assert.equal(reviewedList.scrollTop,0); // Return to top after the bottom pause.
 assert.equal(window.scrollY,0);
 for(let i=0;i<90;i++) tick(); assert.notEqual(ids.slideLabel.textContent,before);
 window.innerWidth=600;ids.next.onclick();assert.equal(ids.queues.children.length,1);
 // Short kiosk screens also render the full waiting list.
 window.innerWidth=1366;window.innerHeight=768;ids.next.onclick();
 assert.match(ids.queues.text,/Person 6/);
 assert.equal(ids.queues.children[0].children[2].children[1].children.filter(c=>c.className==='person').length,7);
 const retry=()=>[...timeouts.values()].filter(t=>t.delay===15000).at(-1).fn();
 response={ok:false,status:503};retry();await settle();
 assert.equal(body.dataset.state,'offline');assert.doesNotMatch(ids.queues.text,/Person|img/);
 assert.match(ids.queues.text,/temporarily unavailable/);
 response=goodResponse;retry();await settle();assert.equal(body.dataset.state,'live');
 response={ok:true,status:200,json:async()=>({queues:[{id:1,name:'Test queue',serving:[],waiting:[{name:'Only person',kind:'Walk-in',time:'10:00 AM'}]}]})};retry();await settle();
 let waiting=ids.queues.children[0].children[2];
 assert.equal(waiting.children[1].children.filter(c=>c.className==='person').length,1);
 assert.equal(waiting.children[1].children.filter(c=>c.className==='person person-placeholder').length,0);
 response={ok:true,status:200,json:async()=>({queues:[{id:1,name:'Test queue',serving:[],waiting:[]}]})};retry();await settle();
 waiting=ids.queues.children[0].children[2];
 assert.match(waiting.text,/No one waiting/);
 response={ok:true,status:200,json:async()=>({queues:[]})};retry();await settle();
 assert.equal(body.dataset.state,'live');assert.match(ids.queues.text,/No queues available/);
 response={ok:false,status:403};retry();await settle();
 assert.equal(body.dataset.state,'denied');assert.equal(ids.connectionLabel.textContent,'Access ended');
 assert.doesNotMatch(ids.queues.text,/Person|img/);assert.match(ids.updated.textContent,/Access ended/);
 console.log('PASS: queue slides, full waiting lists, waiting-list scrolling without page movement, empty queues, pause/resume, responsive count, literal text, connection states, short screens, access revocation');
})().catch(e=>{console.error(e);process.exitCode=1;});
