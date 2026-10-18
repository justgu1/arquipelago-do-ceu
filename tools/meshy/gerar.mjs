// Folha de pedidos do Meshy: --lista mostra custo e saldo; --gerar só roda com --aprovado=<total> e --saida=<pasta fora do repositório>.
// A chave vem de MESHY_API_KEY e nunca é escrita em arquivo nem em log.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const aqui = path.dirname(fileURLToPath(import.meta.url));
const dados = JSON.parse(fs.readFileSync(path.join(aqui, "pedidos.json"), "utf8"));
const args = Object.fromEntries(process.argv.slice(2).map((a) => { const [k, v = true] = a.replace(/^--/, "").split("="); return [k, v]; }));

// Tabela de preços do Meshy (docs.meshy.ai/en/api/pricing): prévia da malha 20 + textura 10; a malha já sai reduzida ao alvo, sem remesh à parte.
const CUSTO = { previa: 20, textura: 10 };
const custoDe = (p) => CUSTO.previa + CUSTO.textura;
// Com --saida, o que já foi baixado não entra de novo: uma segunda rodada não cobra duas vezes.
const jaBaixado = (p) => args.saida && fs.existsSync(path.join(args.saida, `${p.arquivo}.glb`));
const escolhidos = () => dados.pedidos.filter((p) => (!args.prioridade || p.prioridade === args.prioridade) && (!args.so || p.arquivo === args.so) && !jaBaixado(p));
const total = (lista) => lista.reduce((soma, p) => soma + custoDe(p), 0);

const api = "https://api.meshy.ai/openapi";
const chave = () => {
	if (!process.env.MESHY_API_KEY) throw new Error("Defina MESHY_API_KEY no ambiente.");
	return { Authorization: `Bearer ${process.env.MESHY_API_KEY}`, "Content-Type": "application/json" };
};

async function saldo() {
	const r = await fetch(`${api}/v1/balance`, { headers: chave() });
	if (!r.ok) throw new Error(`Saldo: HTTP ${r.status}`);
	return (await r.json()).balance;
}

function tabela(lista) {
	const linhas = lista.map((p) => `| ${p.prioridade} | \`${p.arquivo}.glb\` | ${p.grupo} | ${p.triangulos} | ${p.tamanho} | ${p.clipes ?? "nenhum no modelo (movimento feito no Godot)"} | ${custoDe(p)} | ${p.prompt} |`);
	return ["| Prior. | Arquivo | Grupo | Triângulos | Tamanho | Clipes | Créditos | Prompt |", "| --- | --- | --- | --- | --- | --- | --- | --- |", ...linhas].join("\n");
}

async function criar(corpo, caminho) {
	const r = await fetch(`${api}${caminho}`, { method: "POST", headers: chave(), body: JSON.stringify(corpo) });
	if (!r.ok) throw new Error(`${caminho}: HTTP ${r.status} ${await r.text()}`);
	return (await r.json()).result;
}

async function esperar(id) {
	for (;;) {
		const r = await fetch(`${api}/v2/text-to-3d/${id}`, { headers: chave() });
		const t = await r.json();
		if (t.status === "SUCCEEDED") return t;
		if (t.status === "FAILED" || t.status === "CANCELED") throw new Error(`Tarefa ${id}: ${t.status}`);
		await new Promise((ok) => setTimeout(ok, 8000));
	}
}

async function gerarUm(p, saida) {
	const previa = await criar({ mode: "preview", prompt: `${p.prompt}, ${dados.estilo}`.slice(0, 600), negative_prompt: dados.negativo, ai_model: "latest", topology: "triangle", target_polycount: p.triangulos, should_remesh: true }, "/v2/text-to-3d");
	await esperar(previa);
	const final = await criar({ mode: "refine", preview_task_id: previa, enable_pbr: false }, "/v2/text-to-3d");
	const pronta = await esperar(final);
	const arquivo = await fetch(pronta.model_urls.glb);
	const destino = path.join(saida, `${p.arquivo}.glb`);
	fs.mkdirSync(path.dirname(destino), { recursive: true });
	fs.writeFileSync(destino, Buffer.from(await arquivo.arrayBuffer()));
	console.log(`OK ${p.arquivo}`);
}

const lista = escolhidos();
if (args.lista) {
	console.log(args.md ? tabela(lista) : lista.map((p) => `${p.prioridade} ${p.arquivo} (${custoDe(p)})`).join("\n"));
	console.log(`\nTotal: ${total(lista)} créditos em ${lista.length} modelos.`);
	if (process.env.MESHY_API_KEY) console.log(`Saldo atual: ${await saldo()} créditos.`);
} else if (args.gerar) {
	const custo = total(lista);
	if (Number(args.aprovado) !== custo) throw new Error(`Sem aprovação: passe --aprovado=${custo} depois de o usuário aprovar a lista e o custo.`);
	if (!args.saida) throw new Error("Passe --saida=<pasta fora do repositório>.");
	if (custo > (await saldo())) throw new Error("Saldo insuficiente.");
	for (const p of lista) await gerarUm(p, args.saida);
	console.log("Pronto. Otimize com tools/otimizar-modelos antes de copiar para game/assets/modelos.");
} else {
	console.log("Use --lista ou --gerar (ver o início do arquivo).");
}
