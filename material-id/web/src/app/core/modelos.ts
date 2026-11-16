export type Operacao = 'Recebimento' | 'Expedicao';
export type StatusConferencia = 'Aberta' | 'EmValidacao' | 'Concluida' | 'Cancelada';
export type GatilhoConferencia = 'Encerrar' | 'Validar' | 'Devolver' | 'Cancelar';
export type StatusAnalise = 'Capturando' | 'Processando' | 'Falha' | 'AguardandoConfirmacao' | 'Confirmada' | 'Corrigida' | 'Validada' | 'Cancelada';
export type GatilhoAnalise = 'EnviarFotos' | 'Concluir' | 'Falhar' | 'Reprocessar' | 'RefazerFotos' | 'Confirmar' | 'Corrigir' | 'Validar' | 'Devolver' | 'Cancelar';
export type Familia = 'TuboRedondo' | 'TuboQuadrado' | 'TuboRetangular' | 'PerfilUSimples' | 'PerfilUEnrijecido' | 'Chapa';

export interface Transicao { de: string; para: string; gatilho: string; usuario: string; em: string; motivo: string | null; }
export interface Medicao { itemCatalogoId: string; descricao: string | null; quantidade: number; comprimentoMm: number | null; larguraMm: number | null; pesoKg: number; }
export interface Analise {
  id: string; sequencia: number; status: StatusAnalise; resultado: Medicao | null; correcao: Medicao | null; medicaoAtual: Medicao | null;
  confianca: number | null; versaoModelo: string | null; motivoFalha: string | null; gatilhosValidos: GatilhoAnalise[]; transicoes: Transicao[];
}
export interface ConferenciaResumo {
  id: string; codigo: string; operacao: Operacao; contraparte: string | null; status: StatusConferencia; criadaEm: string; criadaPor: string;
  quantidadeAmarrados: number; quantidadePecas: number; pesoMedidoKg: number; gatilhosValidos: GatilhoConferencia[];
}
export interface ConferenciaDetalhe { resumo: ConferenciaResumo; analises: Analise[]; transicoes: Transicao[]; }
export interface Fabricante { id: string; nome: string; fonte: string | null; observacao: string | null; }
export interface ItemCatalogo {
  id: string; fabricanteId: string; fabricante: string; familia: Familia; acabamento: string; descricao: string; espessuraMm: number;
  diametroMm: number | null; larguraMm: number | null; alturaMm: number | null; abaMm: number | null; bitola: string | null;
  tipoPeso: 'KgPorPeca' | 'KgPorMetro' | 'KgPorMetroQuadrado'; valorPeso: number; comprimentoPadraoMm: number | null;
  pecasPorAmarrado: number | null; requerRevisao: boolean; observacao: string | null;
}
export interface Problema { title?: string; detail?: string; status?: number; gatilhosValidos?: string[]; }
