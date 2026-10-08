const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
class Element {
    constructor(tag) { this.tag = tag; this.children = []; this.textContent = ''; this.dataset = {}; }
    appendChild(child) { this.children.push(child); }
    replaceChildren() { this.children = []; }
    setAttribute() {}
    get text() { return this.textContent + this.children.map(c => c.text).join(' '); }
}
const ids = Object.fromEntries(['queues','updated','clock','date','slideLabel','previous','next','pause','fullscreen'].map(id => [id,new Element(id)]));
const intervals = [], timeouts = new Map(); let timer=0;
let response = {ok:true,status:200,json:async()=>({queues:Array.from({length:4},(_,i)=>({id:i,name:'Queue '+i,serving:[],waiting:Array.from({length:7},(_,j)=>({name:j===0?'<img src=x onerror=alert(1)>':'Person '+j,time:'10:30 AM',kind:'Walk-in'}))}))})};
const window = {innerWidth:1600,innerHeight:1200,addEventListener(){}};
vm.runInNewContext(fs.readFileSync('src/FastQ.Web/Scripts/lobby-display.js','utf8'), {
 document:{getElementById:id=>ids[id],createElement:tag=>new Element(tag),body:{dataset:{feed:'/Lobby/Data'}}},window,
 fetch:async()=>response,AbortController,Date,
 setInterval:(fn,delay)=>intervals.push({fn,delay}),setTimeout:(fn,delay)=>{timeouts.set(++timer,{fn,delay});return timer;},clearTimeout:id=>timeouts.delete(id)
});
async function settle(){await new Promise(resolve=>setImmediate(resolve));}
(async()=>{
 await settle(); assert.equal(ids.queues.children.length,3);
 assert.match(ids.queues.text,/<img src=x onerror=alert\(1\)>/); // Rendered as literal text, never HTML.
 assert.match(ids.queues.text,/10:30 AM/); assert.doesNotMatch(ids.queues.text,/Person 6/);
 ids.next.onclick(); assert.match(ids.queues.text,/Queue 3/);
 ids.next.onclick(); assert.match(ids.queues.text,/Person 6/); // Overflow customers eventually appear.
 ids.pause.onclick(); const before=ids.slideLabel.textContent;
 intervals.find(x=>x.delay===10000).fn(); assert.equal(ids.slideLabel.textContent,before);
 ids.pause.onclick(); intervals.find(x=>x.delay===10000).fn(); assert.notEqual(ids.slideLabel.textContent,before);
 window.innerWidth=600;ids.next.onclick();assert.equal(ids.queues.children.length,1);
 response={ok:false,status:403}; const retry=[...timeouts.values()].find(t=>t.delay===15000);retry.fn();await settle();
 assert.doesNotMatch(ids.queues.text,/Person|img/);assert.match(ids.updated.textContent,/Access ended/);
 console.log('PASS: queue rotation, overflow names, pause/resume, responsive count, literal text, access revocation');
})().catch(e=>{console.error(e);process.exitCode=1;});
