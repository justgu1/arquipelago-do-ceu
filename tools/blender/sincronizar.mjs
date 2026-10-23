// Copia o bloco de comum.py para cada script de criatura e confere o que o modo seguro do MCP recusa.
// Uso: node tools/blender/sincronizar.mjs (atualiza) ou com --verificar (só confere; sai com 1 se algo estiver fora).
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const aqui = path.dirname(fileURLToPath(import.meta.url));
const pasta = path.join(aqui, "criaturas");
const comum = fs.readFileSync(path.join(aqui, "comum.py"), "utf8").replace(/\r\n/g, "\n").trimEnd();
// O modo seguro só roda script autocontido, por isso o bloco comum é repetido em cada criatura.
const INICIO = "# >>> comum (copiado de tools/blender/comum.py por sincronizar.mjs; não edite aqui)";
const FIM = "# <<< comum";
const verificar = process.argv.includes("--verificar");

// O que o modo seguro do MCP recusa: funções passadas como valor, chamadas calculadas, lambda e acesso ao sistema.
const PROIBIDO = [
	[/\blambda\b/, "lambda"],
	[/\b(globals|eval|exec|open|getattr|setattr|__import__)\s*\(/, "chamada de sistema"],
	[/^\s*(import|from)\s+(os|sys|subprocess|pathlib|socket|shutil)\b/m, "import de sistema"],
	[/[\])]\s*\(/, "chamada calculada, como d[x](...) ou f()(...)"],
];

let ok = true;
for (const arquivo of fs.readdirSync(pasta).filter((f) => f.endsWith(".py")).sort()) {
	const caminho = path.join(pasta, arquivo);
	const texto = fs.readFileSync(caminho, "utf8").replace(/\r\n/g, "\n");
	const i = texto.indexOf(INICIO);
	const j = texto.indexOf(FIM);
	if (i < 0 || j < 0 || j < i) {
		console.error(`${arquivo}: faltam os marcadores do bloco comum.`);
		ok = false;
		continue;
	}
	const novo = texto.slice(0, i) + INICIO + "\n" + comum + "\n" + texto.slice(j);
	if (novo !== texto) {
		if (verificar) {
			console.error(`${arquivo}: bloco comum desatualizado; rode node tools/blender/sincronizar.mjs.`);
			ok = false;
		} else {
			fs.writeFileSync(caminho, novo);
			console.log(`${arquivo}: bloco comum atualizado.`);
		}
	}
	for (const [regex, motivo] of PROIBIDO) {
		const linhas = novo.split("\n");
		linhas.forEach((linha, n) => {
			if (linha.trimStart().startsWith("#")) return;
			if (regex.test(linha)) {
				console.error(`${arquivo}:${n + 1}: o modo seguro recusa (${motivo}).`);
				ok = false;
			}
		});
	}
}
process.exit(ok ? 0 : 1);
