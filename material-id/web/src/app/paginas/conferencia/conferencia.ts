import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Api } from '../../core/api';
import { mensagemDeErro } from '../../core/http';
import { Analise, ConferenciaDetalhe, GatilhoAnalise, GatilhoConferencia, ItemCatalogo } from '../../core/modelos';
import { GATILHO_ANALISE, GATILHO_CONFERENCIA, OPERACAO, STATUS_ANALISE, STATUS_CONFERENCIA, formatarData, formatarKg } from '../../core/rotulos';
import { Sessao } from '../../core/sessao';
import { EtiquetaStatus } from '../../shared/etiqueta-status';

const DO_OPERADOR: GatilhoAnalise[] = ['EnviarFotos', 'Reprocessar', 'RefazerFotos', 'Confirmar', 'Corrigir', 'Cancelar'];
const DO_GESTOR: GatilhoAnalise[] = ['Validar', 'Devolver'];
const ROTA_ANALISE: Partial<Record<GatilhoAnalise, string>> = {
  EnviarFotos: 'enviar-fotos', Reprocessar: 'reprocessar', RefazerFotos: 'refazer-fotos', Confirmar: 'confirmar',
  Validar: 'validar', Devolver: 'devolver', Cancelar: 'cancelar',
};
const EXIGE_MOTIVO = new Set(['Devolver', 'Cancelar']);

interface Pendente { alvo: 'conferencia' | 'analise'; gatilho: string; analiseId?: string; titulo: string; }

/** Detalhe da conferência (E6-02): amarrados, ações permitidas pela máquina de estados e linha do tempo. */
@Component({
  selector: 'app-conferencia',
  imports: [FormsModule, RouterLink, EtiquetaStatus],
  templateUrl: './conferencia.html',
  styleUrl: './conferencia.css',
})
export class ConferenciaPagina {
  private readonly api = inject(Api);
  protected readonly sessao = inject(Sessao);

  readonly id = input.required<string>();

  protected readonly STATUS_C = STATUS_CONFERENCIA;
  protected readonly STATUS_A = STATUS_ANALISE;
  protected readonly OPERACAO = OPERACAO;
  protected readonly kg = formatarKg;
  protected readonly data = formatarData;

  protected readonly c = signal<ConferenciaDetalhe | null>(null);
  protected readonly erro = signal<string | null>(null);
  protected readonly ocupado = signal(false);
  protected readonly pendente = signal<Pendente | null>(null);
  protected motivo = '';

  // Correção (D6): o operador escolhe o produto certo no catálogo e informa a quantidade.
  protected readonly corrigindo = signal<Analise | null>(null);
  protected readonly opcoes = signal<ItemCatalogo[]>([]);
  protected buscaItem = '';
  protected itemCorreto = '';
  protected quantidadeCorreta: number | null = null;
  protected comprimentoCorreto: number | null = null;
  protected larguraCorreta: number | null = null;

  protected readonly linhaDoTempo = computed(() => {
    const d = this.c();
    if (!d) return [];
    const nome = (s: string) => (STATUS_CONFERENCIA as Record<string, string>)[s] ?? (STATUS_ANALISE as Record<string, string>)[s] ?? s;
    const conf = d.transicoes.map(t => ({ ...t, de: nome(t.de), para: nome(t.para), onde: 'Conferência' }));
    const amarr = d.analises.flatMap(a => a.transicoes.map(t => ({ ...t, de: nome(t.de), para: nome(t.para), onde: `Amarrado ${a.sequencia}` })));
    return [...conf, ...amarr].sort((x, y) => y.em.localeCompare(x.em));
  });

  // Enquanto algum amarrado está em processamento, atualiza a cada 2 s (o tempo real via SignalR chega no E1-04).
  private readonly temporizador = setInterval(() => {
    if (!this.ocupado() && this.c()?.analises.some(a => a.status === 'Processando')) this.carregar();
  }, 2000);

  constructor() { inject(DestroyRef).onDestroy(() => clearInterval(this.temporizador)); }

  ngOnInit() { this.carregar(); }

  carregar() {
    this.api.conferencia(this.id()).subscribe({ next: d => this.c.set(d), error: e => this.erro.set(mensagemDeErro(e)) });
  }

  simulada(a: Analise) { return a.versaoModelo === 'simulador-dev'; }

  acoesConferencia(): GatilhoConferencia[] {
    const d = this.c();
    if (!d) return [];
    return d.resumo.gatilhosValidos.filter(g =>
      g === 'Encerrar' ? this.sessao.podeCapturar() : g === 'Cancelar' ? this.sessao.podeCapturar() || this.sessao.gestor() : this.sessao.gestor());
  }

  acoesAnalise(a: Analise): GatilhoAnalise[] {
    const status = this.c()?.resumo.status;
    return a.gatilhosValidos.filter(g =>
      (DO_OPERADOR.includes(g) && this.sessao.podeCapturar() && status === 'Aberta') ||
      (DO_GESTOR.includes(g) && this.sessao.gestor() && status === 'EmValidacao'));
  }

  rotuloConferencia(g: GatilhoConferencia) { return GATILHO_CONFERENCIA[g]; }
  rotuloAnalise(g: GatilhoAnalise) { return GATILHO_ANALISE[g] ?? g; }

  executarConferencia(g: GatilhoConferencia) {
    if (EXIGE_MOTIVO.has(g)) return this.pedirMotivo({ alvo: 'conferencia', gatilho: g, titulo: GATILHO_CONFERENCIA[g] });
    this.rodar(this.api.acaoConferencia(this.id(), g.toLowerCase()));
  }

  executarAnalise(a: Analise, g: GatilhoAnalise) {
    if (g === 'Corrigir') return this.iniciarCorrecao(a);
    if (EXIGE_MOTIVO.has(g)) return this.pedirMotivo({ alvo: 'analise', gatilho: g, analiseId: a.id, titulo: `${GATILHO_ANALISE[g]} amarrado ${a.sequencia}` });
    this.rodar(this.api.acaoAnalise(a.id, ROTA_ANALISE[g]!));
  }

  adicionarAmarrado() { this.rodar(this.api.adicionarAmarrado(this.id())); }

  confirmarMotivo() {
    const p = this.pendente();
    if (!p) return;
    const motivo = this.motivo.trim();
    if (!motivo) { this.erro.set('Informe o motivo.'); return; }
    const req = p.alvo === 'conferencia'
      ? this.api.acaoConferencia(this.id(), p.gatilho.toLowerCase(), motivo)
      : this.api.acaoAnalise(p.analiseId!, ROTA_ANALISE[p.gatilho as GatilhoAnalise]!, { motivo });
    this.rodar(req, () => this.pendente.set(null));
  }

  iniciarCorrecao(a: Analise) {
    this.corrigindo.set(a);
    this.itemCorreto = a.medicaoAtual?.itemCatalogoId ?? '';
    this.quantidadeCorreta = a.medicaoAtual?.quantidade ?? null;
    this.comprimentoCorreto = a.medicaoAtual?.comprimentoMm ?? null;
    this.larguraCorreta = a.medicaoAtual?.larguraMm ?? null;
    this.buscaItem = '';
    this.opcoes.set([]);
  }

  buscarItens() {
    if (this.buscaItem.trim().length < 2) return;
    this.api.itens({ busca: this.buscaItem.trim() }).subscribe({
      next: l => this.opcoes.set(l.slice(0, 50)), error: e => this.erro.set(mensagemDeErro(e)),
    });
  }

  salvarCorrecao() {
    const a = this.corrigindo();
    if (!a) return;
    if (!this.itemCorreto || !this.quantidadeCorreta) { this.erro.set('Informe o produto e a quantidade corretos.'); return; }
    this.rodar(this.api.acaoAnalise(a.id, 'corrigir', {
      itemCatalogoId: this.itemCorreto, quantidade: this.quantidadeCorreta,
      comprimentoMm: this.comprimentoCorreto || null, larguraMm: this.larguraCorreta || null,
    }), () => this.corrigindo.set(null));
  }

  confianca(v: number | null) { return v == null ? '—' : `${Math.round(v * 100)}%`; }

  private pedirMotivo(p: Pendente) { this.motivo = ''; this.pendente.set(p); }

  private rodar(req: Observable<ConferenciaDetalhe>, depois?: () => void) {
    this.ocupado.set(true);
    this.erro.set(null);
    req.subscribe({
      next: d => { this.c.set(d); this.ocupado.set(false); depois?.(); },
      error: e => { this.erro.set(mensagemDeErro(e)); this.ocupado.set(false); },
    });
  }
}
