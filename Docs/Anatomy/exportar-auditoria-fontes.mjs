// Extração somente de leitura do YAML salvo do Unity e dos cabeçalhos OBJ.
// Não aplica vínculos na cena.
import fs from 'node:fs/promises';
import path from 'node:path';
import {createHash} from 'node:crypto';
const raiz=process.cwd();
const caminhoCena='Assets/Scenes/SampleScene.unity';
const cena=await fs.readFile(caminhoCena,'utf8');
const calcularHash=texto=>createHash('sha256').update(texto).digest('hex');
const valor=(corpo,chave)=>{
 const bruto=corpo.match(new RegExp(`^  ${chave}: *(.*)$`,'m'))?.[1]?.trim()??'';
 if(bruto.startsWith('"')) return JSON.parse(bruto);
 if(bruto.startsWith("'")) return bruto.slice(1,-1).replaceAll("''", "'");
 return bruto;
};
const referencia=(corpo,chave)=>corpo.match(new RegExp(`^  ${chave}: \\{fileID: (-?\\d+)`,'m'))?.[1]??'0';
const documentos=new Map([...cena.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\r?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(correspondencia=>[correspondencia[2],{type:correspondencia[1],id:correspondencia[2],body:correspondencia[3]}]));
const guidMeta=caminho=>fs.readFile(caminho+'.meta','utf8').then(texto=>texto.match(/^guid: (\w+)/m)[1]);
const guidCena=await guidMeta(caminhoCena);
const guidNo=await guidMeta('Assets/Scripts/Interacao/NoInteracaoAnatomica.cs');
const guidInformacao=await guidMeta('Assets/Scripts/Anatomia/InformacaoEstruturaAnatomica.cs');
const nos=[...documentos.values()].filter(documento=>documento.type==='114'&&documento.body.includes(`guid: ${guidNo},`));
const informacoes=[...documentos.values()].filter(documento=>documento.type==='114'&&documento.body.includes(`guid: ${guidInformacao},`));
const transformsObjetos=new Map([...documentos.values()].filter(documento=>documento.type==='4'&&!documento.body.includes('m_CorrespondingSourceObject: {fileID: -')).map(documento=>[referencia(documento.body,'m_GameObject'),documento]));
const idCena=id=>`sceneYaml:${guidCena}:${id}`;
function caminhoObjeto(idObjeto,visitados=new Set()){
 if(visitados.has(idObjeto)) throw new Error('Ciclo de Transform'); visitados.add(idObjeto);
 const objeto=documentos.get(idObjeto); if(!objeto) throw new Error(`GameObject ausente: ${idObjeto}`);
 const transform=transformsObjetos.get(idObjeto);
 const transformPai=transform?documentos.get(referencia(transform.body,'m_Father')):null;
 const objetoPai=transformPai?referencia(transformPai.body,'m_GameObject'):'0';
 return (objetoPai!=='0'?caminhoObjeto(objetoPai,visitados)+'/':'')+valor(objeto.body,'m_Name');
}
const textoAmigavel=await fs.readFile('Assets/Scripts/Interacao/NoInteracaoAnatomica.cs','utf8');
const nomesAmigaveis=new Map([...textoAmigavel.matchAll(/\{ "([^"]+)", "([^"]+)" \}/g)].map(correspondencia=>[correspondencia[1],correspondencia[2]]));
const corrigirGrafia=texto=>texto.replace(/\brigth\b/gi,correspondencia=>correspondencia[0]==='R'?'Right':'right');
const nomeAmigavel=texto=>nomesAmigaveis.get(texto)??texto.replaceAll('_',' ');
const grupos=nos.map(no=>{const objeto=referencia(no.body,'m_GameObject'); return {
 groupId:idCena(objeto),scenePath:caminhoObjeto(objeto),nodeNameRaw:valor(no.body,'nodeName'),
 displayName:nomeAmigavel(valor(no.body,'displayName')||valor(no.body,'nodeName')),
 parentGroupId:'',level:Number(valor(no.body,'nodeLevel')),directChildGroupIds:[],directRepresentationKeys:[],descendantRepresentationKeys:[],contentStatus:'pending_review',approvedDescription:'',
}}).sort((primeiro,segundo)=>primeiro.scenePath.localeCompare(segundo.scenePath));
const grupoPorCaminho=new Map(grupos.map(grupo=>[grupo.scenePath,grupo]));
for(const grupo of grupos){const pai=grupoPorCaminho.get(grupo.scenePath.slice(0,grupo.scenePath.lastIndexOf('/')));grupo.parentGroupId=pai?.groupId??'';pai?.directChildGroupIds.push(grupo.groupId);}
const ativos=new Map();
for(const arquivo of (await fs.readdir('Assets/Brain')).filter(nome=>nome.endsWith('.obj'))){
 const caminhoAtivo='Assets/Brain/'+arquivo, texto=await fs.readFile(caminhoAtivo,'utf8');
 const cabecalho=Object.fromEntries([...texto.matchAll(/^#[ \t]*([^:\r\n]+):[ \t]*([^\r\n]*)/gm)].map(correspondencia=>[correspondencia[1].trim(),correspondencia[2].trim()]));
 const guid=await guidMeta(caminhoAtivo);
 ativos.set(guid,{assetPath:caminhoAtivo,header:cabecalho,sha256:calcularHash(texto)});
}
const textoEnumeracoes=await fs.readFile('Assets/Scripts/Anatomy/BrainAnatomyEnums.cs','utf8');
const enumeracoes=Object.fromEntries([...textoEnumeracoes.matchAll(/public enum (\w+)\s*\{([^}]+)\}/g)].map(correspondencia=>[correspondencia[1],correspondencia[2].split(',').map(item=>item.trim()).filter(Boolean)]));
const enumeracao=(corpo,chave,tipo)=>enumeracoes[tipo][Number(valor(corpo,chave))]??valor(corpo,chave);
const problemas=[];
function registrarProblema(entrada,codigo,gravidade,mensagem){entrada.issues?.push(codigo);problemas.push({code:codigo,severity:gravidade,targetId:entrada.representationKey??entrada.groupId,message:mensagem});}
const representacoes=informacoes.map(informacao=>{
 const objeto=documentos.get(referencia(informacao.body,'m_GameObject'));
 const prefab=documentos.get(referencia(objeto.body,'m_PrefabInstance'));
 if(!prefab) throw new Error(`Prefab ausente para ${informacao.id}`);
 const guid=prefab.body.match(/m_SourcePrefab: \{fileID: [^,]+, guid: (\w+)/)[1];
 const ativo=ativos.get(guid);if(!ativo)throw new Error(`GUID de ativo desconhecido: ${guid}`);
 const idTransform=prefab.body.match(/m_TransformParent: \{fileID: (-?\d+)/)[1];
 const caminhoGrupo=caminhoObjeto(referencia(documentos.get(idTransform).body,'m_GameObject'));
 const grupo=grupoPorCaminho.get(caminhoGrupo);if(!grupo)throw new Error(`Pai desconhecido: ${caminhoGrupo}`);
 const nomeBruto=prefab.body.match(/propertyPath: m_Name\r?\n\s+value: ([^\r\n]*)/)?.[1]??'';
 const nome=nomeBruto.startsWith('"')?JSON.parse(nomeBruto):nomeBruto;
 const cabecalho=ativo.header;
 const representacao={representationKey:[guid,cabecalho['File ID'],cabecalho['Representation ID'],cabecalho['Concept ID']].join('|'),sceneObjectId:idCena(objeto.id),scenePath:caminhoGrupo+'/'+nome,gameObjectNameRaw:nome,
 anatomicalNameRaw:valor(informacao.body,'anatomicalName'),anatomicalNameDisplay:corrigirGrafia(valor(informacao.body,'anatomicalName')),portugueseNameRaw:valor(informacao.body,'portugueseName'),portugueseNameDisplay:corrigirGrafia(valor(informacao.body,'portugueseName')),
 sourceObjectNameRaw:valor(informacao.body,'sourceObjectName'),originalScenePath:valor(informacao.body,'originalScenePath'),originalParentGroup:valor(informacao.body,'originalParentGroup'),
 assetPath:ativo.assetPath,assetGuid:guid,objFileId:cabecalho['File ID'],objRepresentationId:cabecalho['Representation ID'],fmaConceptId:cabecalho['Concept ID'],objEnglishName:cabecalho['English name'],objBuildUpLogic:cabecalho['Build-up logic'],objCompatibilityVersion:cabecalho['Compatibility version'],
 groupId:grupo.groupId,groupPath:caminhoGrupo,serializedMajorDivision:enumeracao(informacao.body,'majorDivision','BrainMajorDivision'),serializedSubdivision:enumeracao(informacao.body,'subdivision','BrainAnatomicalSubdivision'),serializedAnatomicalGroup:enumeracao(informacao.body,'anatomicalGroup','BrainAnatomicalSystem'),serializedCorticalLobe:enumeracao(informacao.body,'corticalLobe','BrainCorticalLobe'),serializedSide:enumeracao(informacao.body,'hemisphereOrSpatialDivision','BrainSide'),serializedReviewStatus:enumeracao(informacao.body,'reviewStatus','BrainStructureReviewStatus'),
 sourceCatalogDivision:valor(informacao.body,'sourceCatalogDivision'),sourceCatalogSubdivision:valor(informacao.body,'sourceCatalogSubdivision'),legacyShortDescription:valor(informacao.body,'shortDescription'),legacyFunctionalDescription:valor(informacao.body,'functionalDescription'),contentStatus:'pending_review',issues:[]};
 if(representacao.anatomicalNameRaw!==representacao.anatomicalNameDisplay)registrarProblema(representacao,'DISPLAY_SPELLING_CORRECTED','info','Grafia rigth corrigida no catálogo de exibição; original preservado.');
 if(corrigirGrafia(nome).toLowerCase()!==cabecalho['English name'].toLowerCase())registrarProblema(representacao,'SCENE_NAME_DIFFERS_FROM_OBJ','warning','Nome da cena difere do nome inglês no OBJ referenciado. Associação preservada para revisão.');
 if(representacao.sourceObjectNameRaw.toLowerCase()!==path.basename(ativo.assetPath).toLowerCase())registrarProblema(representacao,'CATALOG_SOURCE_DIFFERS_FROM_MESH','warning','sourceObjectName difere do arquivo de malha realmente referenciado.');
 if(!representacao.portugueseNameRaw)registrarProblema(representacao,'PORTUGUESE_NAME_PENDING','warning','Nome português ainda não preenchido nem validado.');
 if(representacao.serializedSide==='Unspecified')registrarProblema(representacao,'SIDE_UNSPECIFIED','warning','Lateralidade serializada não especificada; nenhuma lateralidade foi inferida.');
 grupo.directRepresentationKeys.push(representacao.representationKey);
 for(const grupoAtual of grupos)if(representacao.scenePath.startsWith(grupoAtual.scenePath+'/'))grupoAtual.descendantRepresentationKeys.push(representacao.representationKey);
 return representacao;
}).sort((primeira,segunda)=>primeira.scenePath.localeCompare(segunda.scenePath));
for(const grupo of grupos)if(grupo.descendantRepresentationKeys.length===0)registrarProblema(grupo,'EMPTY_GROUP','warning','Grupo sem representações descendentes na cena atual; classificação preservada.');
const conceitos=[...new Set(representacoes.map(representacao=>representacao.fmaConceptId))].sort().map(id=>({conceptKey:id,fmaConceptId:id,objEnglishNames:[...new Set(representacoes.filter(representacao=>representacao.fmaConceptId===id).map(representacao=>representacao.objEnglishName))],representationKeys:representacoes.filter(representacao=>representacao.fmaConceptId===id).map(representacao=>representacao.representationKey)}));
if(representacoes.length!==240||grupos.length!==32||new Set(representacoes.map(representacao=>representacao.assetGuid)).size!==240||new Set(representacoes.map(representacao=>representacao.representationKey)).size!==240)throw new Error('Falha na invariante de contagem/identidade da fonte');
if(representacoes.some(representacao=>[representacao.objFileId,representacao.objRepresentationId,representacao.fmaConceptId,representacao.objEnglishName,representacao.objCompatibilityVersion,representacao.objBuildUpLogic].some(item=>!item)))throw new Error('Cabeçalho OBJ incompleto');
const resultado={schemaVersion:'1.0.0',generatedUtc:new Date().toISOString(),scenePath:caminhoCena,rootPath:'Sistema_Nervoso_Central',sourceUrl:'https://lifesciencedb.jp/bp3d/',applied:false,applicationBlocker:'UNITY_EDITOR_LICENSE_REQUIRED',identifierScheme:'identificadores sceneYaml: GUID da cena + fileID serializado; GlobalObjectId não verificado em runtime',representations:representacoes,groups:grupos,concepts:conceitos,issues:problemas,validation:{representationCount:240,groupCount:32,conceptCount:conceitos.length,interactionNodeCount:nos.length,directBindings:0,sourceIdentityValidated:true,graphValidated:false,idempotenceValidated:false,geometricInvariantsPreserved:null,errorCount:0,warningCount:problemas.filter(problema=>problema.severity==='warning').length,validationScope:'Somente identidade da fonte na cena salva e no OBJ. Sem execução do Unity Editor ou teste no headset.'},sourceSnapshot:{sceneSha256:calcularHash(cena),assetSha256:Object.fromEntries([...ativos.values()].map(ativo=>[ativo.assetPath,ativo.sha256]))}};
await fs.mkdir('Docs/Anatomy',{recursive:true});
await fs.writeFile('Docs/Anatomy/anatomical-mapping.json',JSON.stringify(resultado,null,2)+'\n');
function gerarCsv(linhas){const chaves=Object.keys(linhas[0]);const escapar=valor=>'"'+(Array.isArray(valor)?valor.join(';'):String(valor??'')).replaceAll('"','""')+'"';return '\uFEFF'+[chaves,...linhas.map(linha=>chaves.map(chave=>linha[chave]))].map(linha=>linha.map(escapar).join(',')).join('\r\n')+'\r\n';}
await fs.writeFile('Docs/Anatomy/anatomical-representations.csv',gerarCsv(representacoes));
await fs.writeFile('Docs/Anatomy/anatomical-groups.csv',gerarCsv(grupos));
console.log(JSON.stringify({applied:false,representations:240,groups:32,concepts:conceitos.length,issues:problemas.length,sceneSha256:resultado.sourceSnapshot.sceneSha256}));
