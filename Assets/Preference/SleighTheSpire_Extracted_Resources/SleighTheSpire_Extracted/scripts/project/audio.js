import { E } from "./engine.js";

let ctx = null, master = null, sfxBus = null, musBus = null, noiseBuf = null;
const last = {};

export function audioInit() {
	try {
		const AC = globalThis.AudioContext || globalThis.webkitAudioContext;
		if (!AC) { console.warn("[Sleigh] WebAudio unavailable (set 'Use worker' = No)."); return; }
		ctx = new AC();
		const comp = ctx.createDynamicsCompressor();
		comp.threshold.value = -14; comp.ratio.value = 4;
		comp.connect(ctx.destination);
		master = ctx.createGain(); master.connect(comp);
		sfxBus = ctx.createGain(); sfxBus.connect(master);
		musBus = ctx.createGain(); musBus.connect(master);
		const len = ctx.sampleRate;
		noiseBuf = ctx.createBuffer(1, len, ctx.sampleRate);
		const d = noiseBuf.getChannelData(0);
		for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
		applyVolumes();
		music.preload();
	} catch (e) { ctx = null; console.warn(e); }
}

export function audioUnlock() { if (ctx && ctx.state === "suspended") ctx.resume(); }

export function applyVolumes() {
	if (!ctx) return;
	const s = E.settings;
	master.gain.value = s.mute ? 0 : s.master;
	sfxBus.gain.value = s.sfx;
	musBus.gain.value = s.music;
}

const mtof = m => 440 * Math.pow(2, (m - 69) / 12);
const vr = (v = 0.06) => 1 + (Math.random() * 2 - 1) * v;
function gate(name, ms = 30) {
	const now = performance.now();
	if (last[name] && now - last[name] < ms) return false;
	last[name] = now; return true;
}
function env(g, t, a, peak, dur) {
	g.gain.setValueAtTime(0.0001, t);
	g.gain.exponentialRampToValueAtTime(Math.max(0.0002, peak), t + a);
	g.gain.exponentialRampToValueAtTime(0.0001, t + Math.max(a + 0.01, dur));
}
function tone(f, o = {}) {
	if (!ctx || ctx.state !== "running") return;
	const t = ctx.currentTime + (o.delay || 0), dur = o.dur ?? 0.1;
	const osc = ctx.createOscillator(), g = ctx.createGain();
	osc.type = o.type || "square";
	osc.frequency.setValueAtTime(Math.max(20, f), t);
	if (o.f2) osc.frequency.exponentialRampToValueAtTime(Math.max(20, o.f2), t + dur);
	env(g, t, o.at ?? 0.004, o.vol ?? 0.15, dur);
	osc.connect(g); g.connect(o.bus || sfxBus);
	osc.start(t); osc.stop(t + dur + 0.05);
}
function noise(o = {}) {
	if (!ctx || ctx.state !== "running") return;
	const t = ctx.currentTime + (o.delay || 0), dur = o.dur ?? 0.1;
	const s = ctx.createBufferSource(); s.buffer = noiseBuf;
	const fl = ctx.createBiquadFilter(); fl.type = o.type || "lowpass";
	fl.frequency.setValueAtTime(o.f || 2000, t);
	if (o.f2) fl.frequency.exponentialRampToValueAtTime(Math.max(20, o.f2), t + dur);
	fl.Q.value = o.q ?? 1;
	const g = ctx.createGain(); env(g, t, 0.003, o.vol ?? 0.15, dur);
	s.connect(fl); fl.connect(g); g.connect(o.bus || sfxBus);
	s.start(t, Math.random() * 0.5); s.stop(t + dur + 0.05);
}

// all the sfx are still synthesised, no sound files needed for these
export const sfx = {
	hover() { if (gate("hover", 45)) tone(1500 * vr(), { dur: 0.025, vol: 0.03 }); },
	cardHover(i = 0) { if (gate("chov", 40)) tone(mtof(79 + (i % 7) * 2), { type: "triangle", dur: 0.04, vol: 0.035 }); },
	click() { if (!gate("click")) return; tone(660 * vr(), { f2: 990, dur: 0.06, vol: 0.1 }); tone(1320, { type: "triangle", dur: 0.05, vol: 0.05, delay: 0.03 }); },
	toggle(on) { tone(on ? 700 : 500, { f2: on ? 1100 : 350, type: "triangle", dur: 0.07, vol: 0.1 }); noise({ dur: 0.03, vol: 0.05, type: "highpass", f: 5000 }); },
	tick() { if (gate("tick", 35)) tone(1800 * vr(0.02), { dur: 0.015, vol: 0.035 }); },
	count() { if (gate("count", 55)) tone(1400 * vr(0.04), { type: "triangle", dur: 0.02, vol: 0.03 }); },
	pick() { tone(520 * vr(), { f2: 780, type: "triangle", dur: 0.07, vol: 0.14 }); noise({ dur: 0.05, vol: 0.05, type: "highpass", f: 4000 }); },
	error() { if (!gate("err", 120)) return; tone(140, { dur: 0.08, vol: 0.12 }); tone(110, { dur: 0.1, vol: 0.12, delay: 0.09 }); },
	draw() { if (gate("draw", 35)) noise({ dur: 0.07, vol: 0.09, type: "bandpass", f: 3500 * vr(0.2), f2: 6000, q: 0.8 }); },
	shuffle() { for (let i = 0; i < 6; i++) noise({ dur: 0.05, vol: 0.07, type: "bandpass", f: 3000 + i * 300, delay: i * 0.04 }); },
	play(streak = 0) {
		const p = Math.pow(2, Math.min(streak, 10) / 12);
		noise({ dur: 0.12, vol: 0.1, type: "bandpass", f: 2000, f2: 6000, q: 0.7 });
		tone(440 * p * vr(0.02), { f2: 880 * p, type: "triangle", dur: 0.1, vol: 0.11 });
	},
	unwrap() { for (let i = 0; i < 4; i++) noise({ dur: 0.04, vol: 0.09, type: "highpass", f: 2500 + Math.random() * 3000, delay: i * 0.03 }); tone(1046, { type: "sine", dur: 0.2, vol: 0.07, delay: 0.08 }); },
	hit(n = 6) {
		if (!gate("hit", 25)) return;
		const p = Math.min(1, n / 25);
		noise({ dur: 0.09 + p * 0.12, vol: 0.22 + p * 0.2, f: 4000, f2: 300 });
		tone(200 * vr() - p * 60, { f2: 50, dur: 0.1 + p * 0.12, vol: 0.16 + p * 0.1 });
		if (p > 0.6) tone(60, { f2: 30, type: "sine", dur: 0.35, vol: 0.35 });
	},
	crit() { tone(90, { f2: 30, type: "sine", dur: 0.45, vol: 0.4 }); noise({ dur: 0.35, vol: 0.25, f: 6000, f2: 200 }); tone(1600, { type: "triangle", f2: 400, dur: 0.25, vol: 0.06 }); },
	whiff() { noise({ dur: 0.12, vol: 0.06, type: "bandpass", f: 1200, f2: 4000, q: 2 }); },
	blockHit() { if (!gate("bh")) return; tone(1800 * vr(), { f2: 1500, dur: 0.06, vol: 0.06 }); tone(2700 * vr(), { type: "triangle", dur: 0.12, vol: 0.05 }); },
	block() { tone(700, { f2: 900, type: "triangle", dur: 0.08, vol: 0.13 }); tone(1050, { type: "triangle", dur: 0.14, vol: 0.09, delay: 0.05 }); },
	buff() { [0, 4, 7, 12].forEach((s, i) => tone(mtof(72 + s), { type: "triangle", dur: 0.1, vol: 0.09, delay: i * 0.045 })); },
	debuff() { [0, -3, -6].forEach((s, i) => tone(mtof(60 + s), { dur: 0.12, vol: 0.07, delay: i * 0.06 })); },
	heal() { [0, 7, 12, 16, 19].forEach((s, i) => tone(mtof(79 + s), { type: "sine", dur: 0.25, vol: 0.06, delay: i * 0.05 })); },
	coin() { tone(988, { dur: 0.06, vol: 0.09 }); tone(1319, { dur: 0.18, vol: 0.09, delay: 0.06 }); },
	jingle() { for (let i = 0; i < 7; i++) noise({ dur: 0.06, vol: 0.06, type: "bandpass", f: 7000 + Math.random() * 3000, q: 6, delay: i * 0.05 + Math.random() * 0.02 }); },
	bell(m = 84) { const f = mtof(m); tone(f, { type: "sine", dur: 1.2, vol: 0.12 }); tone(f * 2.76, { type: "sine", dur: 0.5, vol: 0.035 }); tone(f * 5.4, { type: "sine", dur: 0.25, vol: 0.02 }); },
	death() { noise({ dur: 0.5, vol: 0.22, f: 3000, f2: 100 }); tone(400, { f2: 60, type: "sawtooth", dur: 0.5, vol: 0.1 }); },
	win() { [0, 4, 7, 12, 16, 19, 24].forEach((s, i) => tone(mtof(67 + s), { dur: 0.16, vol: 0.07, delay: i * 0.07 })); sfx.jingle(); },
	lose() { [0, -2, -5, -9, -12].forEach((s, i) => tone(mtof(64 + s), { type: "triangle", dur: 0.4, vol: 0.11, delay: i * 0.22 })); },
	turn() { sfx.bell(79); tone(mtof(86), { type: "sine", dur: 0.5, vol: 0.05, delay: 0.1 }); },
	enemyTurn() { tone(mtof(50), { type: "sawtooth", dur: 0.25, vol: 0.05 }); tone(mtof(49), { type: "sawtooth", dur: 0.3, vol: 0.05, delay: 0.15 }); },
	windup() { tone(180, { f2: 420, type: "sawtooth", dur: 0.18, vol: 0.05 }); },
	whoosh() { noise({ dur: 0.3, vol: 0.1, type: "bandpass", f: 400, f2: 3000, q: 1.2 }); },
	spirit() { [0, 4, 7, 11, 14].forEach((s, i) => tone(mtof(76 + s), { type: "sine", dur: 0.4, vol: 0.06, delay: i * 0.07 })); sfx.jingle(); },
	transform() { noise({ dur: 1.6, vol: 0.3, f: 200, f2: 2000, q: 3 }); tone(110, { f2: 40, type: "sawtooth", dur: 1.6, vol: 0.15 }); tone(55, { dur: 1.6, vol: 0.1 }); },
	potion() { for (let i = 0; i < 5; i++) tone(300 + Math.random() * 400, { f2: 800, type: "sine", dur: 0.06, vol: 0.07, delay: i * 0.05 }); },
	relic() { [0, 7, 12, 16].forEach((s, i) => tone(mtof(72 + s), { type: "triangle", dur: 0.6, vol: 0.05, delay: i * 0.03 })); sfx.jingle(); },
	type() { if (gate("type", 45)) tone(700 + Math.random() * 300, { dur: 0.02, vol: 0.02 }); },
	line() { tone(mtof(84 + Math.floor(Math.random() * 5)), { type: "sine", dur: 0.6, vol: 0.03 }); },
	pop() { tone(800 * vr(), { f2: 1300, type: "sine", dur: 0.06, vol: 0.09 }); },
	energy() { tone(880, { f2: 1760, type: "triangle", dur: 0.12, vol: 0.07 }); },
	thud() { tone(90, { f2: 40, type: "sine", dur: 0.25, vol: 0.28 }); noise({ dur: 0.15, vol: 0.12, f: 600 }); },
	fire() { if (gate("fire", 90)) noise({ dur: 0.08, vol: 0.03, type: "bandpass", f: 900 + Math.random() * 1500, q: 2 }); },
	heartbeat() { tone(60, { f2: 45, type: "sine", dur: 0.12, vol: 0.25 }); tone(55, { f2: 40, type: "sine", dur: 0.14, vol: 0.2, delay: 0.16 }); },
	holdTick(p) { if (gate("hold", 60)) tone(400 + p * 600, { type: "triangle", dur: 0.03, vol: 0.05 }); },
	page() { noise({ dur: 0.1, vol: 0.06, type: "bandpass", f: 2500, f2: 5000, q: 1 }); },
	give() { tone(660 * vr(), { f2: 1320, type: "sine", dur: 0.12, vol: 0.08 }); tone(990, { type: "triangle", dur: 0.18, vol: 0.05, delay: 0.06 }); },
	love() { [0, 4, 7].forEach((st, i) => tone(mtof(81 + st), { type: "sine", dur: 0.18, vol: 0.07, delay: i * 0.05 })); },
	hate() { tone(220, { f2: 140, type: "sawtooth", dur: 0.18, vol: 0.06 }); },
	charm() { [0, 4, 7, 12, 16, 19, 24].forEach((st, i) => tone(mtof(72 + st), { type: "sine", dur: 0.35, vol: 0.07, delay: i * 0.06 })); sfx.bell(96); sfx.jingle(); },
	clink() { tone(2200 * vr(0.08), { type: "sine", dur: 0.12, vol: 0.06 }); tone(3300 * vr(0.08), { type: "sine", dur: 0.08, vol: 0.03, delay: 0.02 }); },
	grab() { tone(900 * vr(), { f2: 1400, type: "triangle", dur: 0.05, vol: 0.06 }); },
	spot() { noise({ dur: 0.25, vol: 0.06, type: "bandpass", f: 1200, f2: 300, q: 2 }); tone(110, { f2: 80, type: "sine", dur: 0.2, vol: 0.12 }); },
	warn() { tone(mtof(62), { type: "triangle", dur: 0.3, vol: 0.08 }); tone(mtof(57), { type: "triangle", dur: 0.4, vol: 0.08, delay: 0.18 }); },
};

// loop lengths are in samples @44.1k, taken from the render. each mp3 holds two loops,
// so we can loop the middle and dodge the encoder padding at both ends
const SONGS = {
	calm: { file: "calm", loop: 920348 / 44100 },
	battle: { file: "battle", loop: 641455 / 44100 },
	boss: { file: "boss", loop: 579945 / 44100 },
	end: { file: "end", loop: 504000 / 44100 },
};
const bufs = {}, loading = {};
let cur = null, playing = null, duck = 1;

async function fetchSong(name) {
	const file = SONGS[name].file, assets = E.rt?.assets;
	if (!assets || !ctx) return null;
	for (const ext of [".mp3", ".webm", ".ogg", ".m4a"]) {
		try {
			const url = await assets.getMediaFileUrl(file + ext);
			const data = await assets.fetchArrayBuffer(url);
			return await ctx.decodeAudioData(data);
		} catch (_) {}
	}
	console.warn(`[Sleigh] Music "${file}.mp3" not found. Import it into the project's Music folder.`);
	return null;
}
function loadSong(name) {
	if (!SONGS[name]) return Promise.resolve(null);
	if (!loading[name]) loading[name] = fetchSong(name).then(b => (bufs[name] = b));
	return loading[name];
}

function fadeOut(p, t = 0.6) {
	if (!p) return;
	const now = ctx.currentTime;
	p.g.gain.cancelScheduledValues(now);
	p.g.gain.setValueAtTime(p.g.gain.value, now);
	p.g.gain.linearRampToValueAtTime(0, now + t);
	try { p.src.stop(now + t + 0.05); } catch (_) {}
}
// crossfade: the old track ramps out while the new one comes in
function start(name) {
	const buf = bufs[name]; if (!buf || cur !== name || !ctx) return;
	fadeOut(playing);
	const src = ctx.createBufferSource(), g = ctx.createGain(), L = SONGS[name].loop;
	src.buffer = buf; src.loop = true;
	if (buf.duration >= L * 1.5) { src.loopStart = L * 0.5; src.loopEnd = L * 1.5; }
	g.gain.setValueAtTime(0, ctx.currentTime);
	g.gain.linearRampToValueAtTime(duck, ctx.currentTime + 0.5);
	src.connect(g); g.connect(musBus); src.start();
	playing = { name, src, g };
}

export const music = {
	play(name) {
		if (cur === name) return;
		cur = name;
		if (!ctx || !SONGS[name]) return;
		if (bufs[name]) start(name); else loadSong(name).then(() => start(name));
	},
	stop() { cur = null; if (ctx) fadeOut(playing, 0.4); playing = null; },
	duck(v) { duck = v; if (playing && ctx) playing.g.gain.setTargetAtTime(v, ctx.currentTime, 0.1); },
	get current() { return cur; },
	preload() { for (const k in SONGS) loadSong(k); },
};
