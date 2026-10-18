// Gera um HTML único com os modelos embutidos, para abrir no navegador sem servidor: node visualizador.mjs <arquivo de saída>.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const aqui = path.dirname(fileURLToPath(import.meta.url));
const modelos = path.join(aqui, "..", "..", "game", "assets", "modelos");
const saida = process.argv[2];
if (!saida) throw new Error("Passe o arquivo de saída.");

const lugar = (arquivo) => {
	const nome = arquivo.replace(/^modelos\//, "").replace(/^novos\//, "").replace(/\.glb\.wasm$/, "");
	return ["", "criaturas", "cenario", "obstaculos", "itens"].map((d) => path.join(modelos, d, `${nome}.glb`)).find((p) => fs.existsSync(p));
};

const html = fs.readFileSync(path.join(aqui, "visualizador.modelo.html"), "utf8").replace(/file: "([^"]+\.glb\.wasm)"/g, (inteiro, arquivo) => {
	const origem = lugar(arquivo);
	if (!origem) throw new Error(`Modelo não encontrado: ${arquivo}`);
	return `file: "data:model/gltf-binary;base64,${fs.readFileSync(origem).toString("base64")}"`;
});
fs.writeFileSync(saida, html);
console.log(`Gerado ${saida} (${Math.round(fs.statSync(saida).size / 1024 / 1024)} MB).`);
