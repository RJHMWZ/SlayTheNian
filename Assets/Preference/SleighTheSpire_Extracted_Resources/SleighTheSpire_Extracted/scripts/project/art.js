import { W, H, hasAnim } from "./engine.js";
import { POTIONS } from "./data.js";

export function icon(g, name, cx, cy, o = {}) {
	if (!name || !hasAnim("Icon", name)) return null;
	const k = g.sp("Icon", cx, cy, { anim: name, sc: o.sc ?? 1, col: o.col });
	if (o.size) { const f = o.size / Math.max(k.nw, k.nh); k.w = k.nw * f; k.h = k.nh * f; }
	return k;
}

export function potionIcon(g, id, x, y, o = {}) {
	return icon(g, "potion_" + id, x, y, o) || icon(g, "potion", x, y, { ...o, col: o.tint ? POTIONS[id].col : undefined });
}

// fake size when the sprite's missing
export function creature(g, type, anim) {
	if (!hasAnim(type, anim)) return { w: 32, h: 48, k: null };
	const k = g.sp(type, 0, 0, { anim });
	return { w: k.w, h: k.h, k };
}

// dy is for combat (pulls the backdrop up a bit)
export function backdrop(sc, key, dy = 0) {
	if (!hasAnim("Background", key)) return false;
	sc.g("BG", 0, 0).sp("Background", 0, dy, { anim: key });
	return true;
}

export function tiledBg(sc, w = W + 200, x0 = -100, y0 = 0, h = H, type = "BG") {
	if (!hasAnim(type)) return null;
	const g = sc.g("BG", 0, 0);
	g.sp(type, x0, y0, { w, h });
	return g;
}

export function sceneSprite(sc, type) {
	if (!hasAnim(type)) return null;
	const g = sc.g("BG", 0, 0);
	g.sp(type, 0, 0);
	return g;
}

export function bgimg(g, name, x, y, o = {}) {
	if (!hasAnim("BGImage", name)) return null;
	return g.sp("BGImage", x, y, { anim: name, sc: o.sc ?? 1 });
}

export const partIcon = (g, id, x, y) => icon(g, "part_" + id, x, y);
