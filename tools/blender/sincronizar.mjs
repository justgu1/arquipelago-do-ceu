// Copia o bloco de comum.py para cada script de criatura (e o de comum_personagem.py para os do Personagem) e confere o que o modo seguro do MCP recusa.
// Uso: node tools/blender/sincronizar.mjs (atualiza) ou com --verificar (só confere; sai com 1 se algo estiver fora).
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const aqui = path.dirname(fileURLToPath(import.meta.url));
const ler = (nome) => fs.readFileSync(path.join(aqui, nome), "utf8").replace(/\r\n/g, "\n").trimEnd();
const comum = ler("comum.py");
// O Personagem usa o bloco das criaturas mais o do esqueleto, dos pesos e da animação.
const comumPersonagem = comum + "\n\n" + ler("comum_personagem.py");
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

// O contrato de toda criatura: raiz com o nome do arquivo, uma origem própria e a construção chamada no fim.
function conferirContrato(arquivo, texto, origens) {
	const base = arquivo.replace(/\.py$/, "");
	const raiz = base[0].toUpperCase() + base.slice(1);
	const erros = [];
	if (!new RegExp(`^def construir_${base}\\(ox\\):`, "m").test(texto)) erros.push(`falta def construir_${base}(ox)`);
	if (!texto.includes(`apagar("${raiz}")`)) erros.push(`falta apagar("${raiz}") para refazer sem duplicar`);
	if (!texto.includes(`vazio("${raiz}", (ox, 0, 0), None)`)) erros.push(`a raiz precisa ser vazio("${raiz}", (ox, 0, 0), None)`);
	if (!new RegExp(`^construir_${base}\\(ORIGEM_X\\)\\s*$`, "m").test(texto)) erros.push(`falta a chamada construir_${base}(ORIGEM_X) no fim`);
	const origem = /^ORIGEM_X = (-?[\d.]+)$/m.exec(texto)?.[1];
	if (origem === undefined) erros.push("falta ORIGEM_X = <número>");
	else if (origens.has(origem)) erros.push(`ORIGEM_X ${origem} já é de ${origens.get(origem)}`);
	else origens.set(origem, arquivo);
	for (const e of erros) console.error(`${arquivo}: ${e}.`);
	return erros.length === 0;
}

let ok = true;
const origens = new Map();
const lista = (pasta, bloco) => fs.readdirSync(path.join(aqui, pasta)).filter((f) => f.endsWith(".py")).sort().map((arquivo) => ({ pasta, arquivo, bloco }));
for (const { pasta, arquivo, bloco } of [...lista("criaturas", comum), ...lista("personagem", comumPersonagem), ...lista("lobby", comum)]) {
	const caminho = path.join(aqui, pasta, arquivo);
	const texto = fs.readFileSync(caminho, "utf8").replace(/\r\n/g, "\n");
	if (pasta === "criaturas" && !conferirContrato(arquivo, texto, origens)) ok = false;
	const i = texto.indexOf(INICIO);
	const j = texto.indexOf(FIM);
	if (i < 0 || j < 0 || j < i) {
		console.error(`${arquivo}: faltam os marcadores do bloco comum.`);
		ok = false;
		continue;
	}
	const novo = texto.slice(0, i) + INICIO + "\n" + bloco + "\n" + texto.slice(j);
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
