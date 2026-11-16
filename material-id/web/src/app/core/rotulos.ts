import { Familia, GatilhoAnalise, GatilhoConferencia, Operacao, StatusAnalise, StatusConferencia } from './modelos';

export const OPERACAO: Record<Operacao, string> = { Recebimento: 'Recebimento', Expedicao: 'Expedição' };

export const STATUS_CONFERENCIA: Record<StatusConferencia, string> = {
  Aberta: 'Aberta', EmValidacao: 'Em validação', Concluida: 'Concluída', Cancelada: 'Cancelada',
};

export const STATUS_ANALISE: Record<StatusAnalise, string> = {
  Capturando: 'Capturando', Processando: 'Processando', Falha: 'Falha', AguardandoConfirmacao: 'Aguardando confirmação',
  Confirmada: 'Confirmada', Corrigida: 'Corrigida', Validada: 'Validada', Cancelada: 'Cancelada',
};

export const GATILHO_CONFERENCIA: Record<GatilhoConferencia, string> = {
  Encerrar: 'Encerrar e enviar para validação', Validar: 'Concluir conferência', Devolver: 'Devolver ao operador', Cancelar: 'Cancelar conferência',
};

export const GATILHO_ANALISE: Partial<Record<GatilhoAnalise, string>> = {
  EnviarFotos: 'Enviar fotos', Reprocessar: 'Reprocessar', RefazerFotos: 'Refazer fotos', Confirmar: 'Confirmar',
  Corrigir: 'Corrigir', Validar: 'Validar', Devolver: 'Devolver', Cancelar: 'Cancelar',
};

export const FAMILIA: Record<Familia, string> = {
  TuboRedondo: 'Tubo redondo', TuboQuadrado: 'Tubo quadrado', TuboRetangular: 'Tubo retangular',
  PerfilUSimples: 'Perfil U simples', PerfilUEnrijecido: 'Perfil U enrijecido', Chapa: 'Chapa',
};

/** Tom visual do status. A cor nunca vem sozinha: a etiqueta sempre leva o texto. */
export function tom(status: string): 'info' | 'warn' | 'ok' | 'bad' | 'neutral' {
  switch (status) {
    case 'Aberta': case 'Capturando': case 'Processando': return 'info';
    case 'EmValidacao': case 'AguardandoConfirmacao': case 'Corrigida': return 'warn';
    case 'Concluida': case 'Validada': case 'Confirmada': return 'ok';
    case 'Falha': return 'bad';
    default: return 'neutral';
  }
}

const kg = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });
const dataHora = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
export const formatarKg = (v: number | null | undefined) => (v == null ? '—' : `${kg.format(v)} kg`);
export const formatarData = (iso: string) => dataHora.format(new Date(iso));
