import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, RowClickedEvent } from 'ag-grid-community';
import { Api } from '../../core/api';
import { mensagemDeErro } from '../../core/http';
import { ConferenciaResumo, Operacao, StatusConferencia } from '../../core/modelos';
import { OPERACAO, STATUS_CONFERENCIA, formatarData, formatarKg, tom } from '../../core/rotulos';
import { Sessao } from '../../core/sessao';
import { EtiquetaStatus } from '../../shared/etiqueta-status';
import { temaGrid, textosGrid } from '../../shared/grid';
import { Tela } from '../../shared/tela';

type Filtro = StatusConferencia | 'Todas';

/** Lista de conferências (E6-01): cards no celular, AG Grid no desktop, filtros em chips. */
@Component({
  selector: 'app-conferencias',
  imports: [FormsModule, RouterLink, AgGridAngular, EtiquetaStatus],
  templateUrl: './conferencias.html',
  styleUrl: './conferencias.css',
})
export class Conferencias {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  protected readonly sessao = inject(Sessao);
  protected readonly tela = inject(Tela);

  protected readonly STATUS = STATUS_CONFERENCIA;
  protected readonly OPERACAO = OPERACAO;
  protected readonly kg = formatarKg;
  protected readonly data = formatarData;
  protected readonly filtros: Filtro[] = ['Todas', 'Aberta', 'EmValidacao', 'Concluida', 'Cancelada'];

  protected readonly todas = signal<ConferenciaResumo[]>([]);
  protected readonly filtro = signal<Filtro>('Todas');
  protected readonly busca = signal('');
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly abrindo = signal(false);
  protected novaOperacao: Operacao = 'Recebimento';
  protected novaContraparte = '';

  protected readonly visiveis = computed(() => {
    const termo = this.busca().trim().toLowerCase();
    return this.todas().filter(c =>
      (this.filtro() === 'Todas' || c.status === this.filtro()) &&
      (!termo || `${c.codigo} ${c.contraparte ?? ''}`.toLowerCase().includes(termo)));
  });

  protected readonly tema = temaGrid;
  protected readonly textos = textosGrid;
  protected readonly colunas: ColDef<ConferenciaResumo>[] = [
    { field: 'codigo', headerName: 'Código', pinned: 'left', width: 180 },
    { field: 'criadaEm', headerName: 'Data', width: 130, valueFormatter: p => formatarData(p.value), sort: 'desc' },
    { field: 'operacao', headerName: 'Operação', width: 130, valueFormatter: p => OPERACAO[p.value as Operacao] },
    { field: 'contraparte', headerName: 'Fornecedor / Cliente', flex: 1, minWidth: 180, valueFormatter: p => p.value ?? '—' },
    { field: 'quantidadeAmarrados', headerName: 'Amarrados', width: 120, type: 'numericColumn' },
    { field: 'quantidadePecas', headerName: 'Peças', width: 100, type: 'numericColumn' },
    { field: 'pesoMedidoKg', headerName: 'Peso medido', width: 150, type: 'numericColumn', valueFormatter: p => formatarKg(p.value) },
    {
      field: 'status', headerName: 'Status', width: 160,
      cellRenderer: (p: { value: StatusConferencia }) =>
        `<span class="etiqueta-grid" data-tom="${tom(p.value)}">${STATUS_CONFERENCIA[p.value]}</span>`,
    },
    { field: 'criadaPor', headerName: 'Operador', width: 160 },
  ];

  constructor() { this.carregar(); }

  carregar() {
    this.carregando.set(true);
    this.api.conferencias().subscribe({
      next: l => { this.todas.set(l); this.carregando.set(false); },
      error: e => { this.erro.set(mensagemDeErro(e)); this.carregando.set(false); },
    });
  }

  abrir() {
    this.abrindo.set(true);
    this.api.abrir(this.novaOperacao, this.novaContraparte.trim() || null).subscribe({
      next: c => this.router.navigate(['/conferencias', c.resumo.id]),
      error: e => { this.erro.set(mensagemDeErro(e)); this.abrindo.set(false); },
    });
  }

  rotuloFiltro(f: Filtro) { return f === 'Todas' ? 'Todas' : STATUS_CONFERENCIA[f]; }
  quantos(f: Filtro) { return f === 'Todas' ? this.todas().length : this.todas().filter(c => c.status === f).length; }
  aoClicarLinha(e: RowClickedEvent<ConferenciaResumo>) { if (e.data) this.router.navigate(['/conferencias', e.data.id]); }
}
