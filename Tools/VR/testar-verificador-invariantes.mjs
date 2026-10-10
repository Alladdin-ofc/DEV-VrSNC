import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const pastaFerramentas = path.dirname(fileURLToPath(import.meta.url));
const verificador = path.join(pastaFerramentas, 'verificar-invariantes-cena-anatomica.mjs');
const referencia = path.join(pastaFerramentas, 'Referencias', 'CenaAnatomica.unity');
const temporario = fs.mkdtempSync(path.join(os.tmpdir(), 'vr-snc-invariantes-'));
function executar(...argumentos) {
  const execucao = spawnSync(process.execPath, [verificador, ...argumentos], { encoding: 'utf8' });
  assert.equal(execucao.error, undefined);
  return { codigo: execucao.status, resultado: JSON.parse(execucao.stdout) };
}
try {
  const atual = executar();
  assert.equal(atual.codigo, 0, JSON.stringify(atual.resultado.errors));
  assert.equal(atual.resultado.status, 'PASS');
  const texto = fs.readFileSync(referencia, 'utf8');
  const alterada = path.join(temporario, 'CenaAlterada.unity');
  const raiz = texto.indexOf('--- !u!4 &6000000000000000002');
  assert.ok(raiz >= 0);
  const inicioPosicao = texto.indexOf('  m_LocalPosition:', raiz);
  assert.ok(inicioPosicao > raiz);
  const fimPosicao = texto.indexOf('\n', inicioPosicao);
  fs.writeFileSync(alterada, texto.slice(0, inicioPosicao) + '  m_LocalPosition: {x: 123, y: 456, z: 789}' + texto.slice(fimPosicao));
  const mutacao = executar(referencia, alterada);
  assert.equal(mutacao.codigo, 1);
  assert.equal(mutacao.resultado.status, 'FAIL');
  assert.ok(mutacao.resultado.diffCount > 0);
  const ausente = executar(path.join(temporario, 'Ausente.unity'));
  assert.equal(ausente.codigo, 1);
  assert.equal(ausente.resultado.status, 'BLOCKED');
  const adulterada = executar(alterada, referencia);
  assert.equal(adulterada.codigo, 1);
  assert.ok(adulterada.resultado.errors.some(erro => erro.includes('SHA256')));
  console.log('APROVADO: cena atual íntegra; alteração anatômica detectada; referência ausente recusada; referência adulterada recusada.');
} finally {
  // Diretório criado exclusivamente por este teste, fora do projeto.
  fs.rmSync(temporario, { recursive: true, force: true });
}
