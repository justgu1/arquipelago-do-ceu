// Gera o APK de debug, instala em todos os celulares ligados ao adb (USB ou Wi-Fi) e, com --firebase, envia aos testadores.
// Uso: node tools/android/implantar.mjs [--firebase] [--sem-instalar] [--args="--driver=benchmark --perfis=todos"]
import { execFileSync, spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const raiz = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const jogo = path.join(raiz, "game");
const preset = path.join(jogo, "export_presets.cfg");
const apk = path.join(raiz, "build", "arquipelago-debug.apk");
const pacote = "org.arquipelagodoceu.jogo";
const atividade = `${pacote}/com.godot.game.GodotAppLauncher`;
const argumentos = process.argv.slice(2);
const flag = (nome) => argumentos.includes(`--${nome}`);
const valor = (nome) => argumentos.find((a) => a.startsWith(`--${nome}=`))?.slice(nome.length + 3);

const godot = process.env.GODOT;
if (!godot) {
	console.error("A variável GODOT precisa apontar para o executável de console do Godot.");
	process.exit(1);
}
const androidHome = process.env.ANDROID_HOME ?? "C:\dev\tools\android";
const ambiente = { ...process.env, JAVA_HOME: process.env.JAVA_HOME ?? "C:\dev\tools\jdk17", ANDROID_HOME: androidHome };
const adb = path.join(androidHome, "platform-tools", process.platform === "win32" ? "adb.exe" : "adb");

// O Android só atualiza o app por cima se o número da versão crescer; o número de commits só cresce.
const codigo = execFileSync("git", ["rev-list", "--count", "HEAD"], { cwd: raiz, encoding: "utf8" }).trim();
const original = fs.readFileSync(preset, "utf8");
fs.writeFileSync(preset, original.replace(/^version\/code=\d+$/m, `version/code=${codigo}`));
fs.mkdirSync(path.dirname(apk), { recursive: true });
try {
	console.log(`Compilando o APK (versão ${codigo})...`);
	execFileSync(godot, ["--headless", "--path", jogo, "--export-debug", "Android", apk], { env: ambiente, stdio: "inherit" });
} finally {
	fs.writeFileSync(preset, original);
}
console.log(`APK pronto: ${apk} (${(fs.statSync(apk).size / 1048576).toFixed(0)} MB)`);

const adbCom = (serial, ...resto) => execFileSync(adb, ["-s", serial, ...resto], { encoding: "utf8" });
const aparelhos = execFileSync(adb, ["devices"], { encoding: "utf8" })
	.split("\n").slice(1).map((l) => l.trim().split(/\s+/)).filter(([, estado]) => estado === "device").map(([serial]) => serial);

if (!flag("sem-instalar")) {
	if (aparelhos.length === 0) console.warn("Nenhum celular ligado ao adb; o APK não foi instalado.");
	await Promise.all(aparelhos.map((serial) => new Promise((resolver) => {
		const instalar = spawn(adb, ["-s", serial, "install", "-r", apk], { stdio: "inherit" });
		instalar.on("close", (codigoSaida) => {
			console.log(`${serial}: ${codigoSaida === 0 ? "instalado" : "falhou"}`);
			resolver();
		});
	})));
}

// O piloto de testes lê uma linha de argumentos de um arquivo, porque no celular não há linha de comando.
const piloto = valor("args");
if (piloto) {
	for (const serial of aparelhos) {
		adbCom(serial, "shell", "run-as", pacote, "sh", "-c", `'mkdir -p files && echo "${piloto}" > files/driver.args'`);
		adbCom(serial, "shell", "am", "start", "-n", atividade);
	}
}

if (flag("firebase")) {
	const app = process.env.FIREBASE_APP_ID;
	if (!app) {
		console.error("Defina FIREBASE_APP_ID (o ID do app Android no console do Firebase).");
		process.exit(1);
	}
	const notas = execFileSync("git", ["log", "-1", "--pretty=%s"], { cwd: raiz, encoding: "utf8" }).trim();
	execFileSync("npx", ["--yes", "firebase-tools", "appdistribution:distribute", apk, "--app", app, "--groups", process.env.FIREBASE_GRUPO ?? "testadores", "--release-notes", notas], { stdio: "inherit", shell: true });
}
