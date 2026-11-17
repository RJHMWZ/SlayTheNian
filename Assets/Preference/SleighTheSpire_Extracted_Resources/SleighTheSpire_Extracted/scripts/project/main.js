import { initEngine, resetEngine, engineTick, go } from "./engine.js";
import { audioInit, music } from "./audio.js";
import "./data.js";
import "./enemies.js";
import "./events.js";
import "./friends.js";
import "./run.js";
import "./ui.js";
import "./combat.js";
import { TitleScene } from "./scenes.js";

const GAME_LAYOUT = "Game";
const ART_TYPES = ["Text", "Claus", "Gift", "Icon", "Background", "BG", "IntroBG", "ShopBG", "ChimneyBG", "StockingBG", "PowerFill", "Board", "ButtonPanel", "Enemy",
	"GameLogo", "GameOver", "TutorialImage", "BGImage", "Particle", "HeartEye", "SliderBar", "Toggle", "EnemyList"];

let running = false, audioReady = false;

function startGame(runtime) {
	const need = ["Px", "Txt"].filter(n => !runtime.objects[n]);
	if (need.length) { console.error(`[Sleigh] Missing required object types: ${need.join(", ")}`); return; }
	const missing = ART_TYPES.filter(n => !runtime.objects[n]);
	if (missing.length) console.warn(`[Sleigh] Missing object types (nothing is drawn for these): ${missing.join(", ")}`);
	resetEngine();
	initEngine(runtime);
	if (!audioReady) { audioInit(); audioReady = true; }
	go(() => new TitleScene(), true);
	running = true;
}
function stopGame() {
	running = false;
	try { music.stop(); } catch (_) {}
	resetEngine();
}

runOnStartup(async runtime => {
	runtime.addEventListener("beforeprojectstart", () => {
		runtime.addEventListener("tick", () => {
			if (!running) return;
			try { engineTick(); }
			catch (err) { console.error("[Sleigh]", err); }
		});
		let game = null;
		try { game = runtime.getLayout(GAME_LAYOUT); } catch (_) {}
		if (game) {
			game.addEventListener("afterlayoutstart", () => startGame(runtime));
			game.addEventListener("beforelayoutend", () => stopGame());
		} else {
			console.warn(`[Sleigh] No layout named "${GAME_LAYOUT}"; starting on the first layout.`);
			startGame(runtime);
		}
	});
});