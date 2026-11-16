import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef } from 'ag-grid-community';
import { Api } from '../../core/api';
import { mensagemDeErro } from '../../core/http';
import { Fabricante, Familia, ItemCatalogo } from '../../core/modelos';
import { FAMILIA } from '../../core/rotulos';
import { temaGrid, textosGrid } from '../../shared/grid';
import { Tela } from '../../shared/tela';

const PESO: Record<ItemCatalogo['tipoPeso'], string> = { KgPorPeca: 'kg/peça', KgPorMetro: 'kg/m', KgPorMetroQuadrado: 'kg/m²' };

/** Catálogo por fabricante (E2-01): consulta das tabelas técnicas carregadas. */
@Component({
  selector: 'app-catalogo',
  imports: [FormsModule, AgGridAngular],
  template: `
    <main class="container">
      <h1>Catálogo de produtos</h1>
      @if (erro()) { <p class="erro" role="alert">{{ erro() }}</p> }
      <form class="filtros" (ngSubmit)="carregar()">
        <div class="campo"><label for="fab">Fabricante</label>
          <select id="fab" name="fab" [(ngModel)]="fabricanteId"><option value="">Todos</option>
            @for (f of fabricantes(); track f.id) { <option [value]="f.id">{{ f.nome }}</option> }</select></div>
        <div class="campo"><label for="fam">Família</label>
          <select id="fam" name="fam" [(ngModel)]="familia"><option value="">Todas</option>
            @for (f of familias; track f) { <option [value]="f">{{ FAMILIA[f] }}</option> }</select></div>
        <div class="campo"><label for="busca">Medida</label>
          <input id="busca" name="busca" [(ngModel)]="busca" placeholder="ex.: 40x20 1,20"></div>
        <label class="revisar"><input type="checkbox" name="rev" [(ngModel)]="soRevisar"> Só itens a revisar</label>
        <button class="btn primario" type="submit">Filtrar</button>
      </form>
      <p class="muted" aria-live="polite">{{ itens().length }} itens</p>
      @if (tela.desktop()) {
        <ag-grid-angular class="grade" [theme]="tema" [localeText]="textos" [rowData]="itens()" [columnDefs]="colunas"
          [pagination]="true" [paginationPageSize]="50" />
      } @else {
        <ul class="lista">
          @for (i of itens().slice(0, 100); track i.id) {
            <li class="cartao"><strong>{{ i.descricao }}</strong>
              <div class="muted">{{ i.fabricante }} · {{ valor(i) }}{{ i.pecasPorAmarrado ? ' · ' + i.pecasPorAmarrado + ' peças/amarrado' : '' }}</div>
              @if (i.requerRevisao) { <div class="aviso">{{ i.observacao }}</div> }
            </li>
          }
        </ul>
      }
    </main>`,
  styles: `
    .filtros { display: grid; grid-template-columns: 1fr; gap: 0 12px; align-items: end; }
    @media (min-width: 768px) { .filtros { grid-template-columns: 1fr 1fr 1fr auto auto; } }
    .revisar { display: inline-flex; gap: 8px; align-items: center; min-height: 44px; margin-bottom: 12px; }
    .filtros .btn { margin-bottom: 12px; }
    .grade { height: 600px; display: block; }`,
})
export class Catalogo {
  private readonly api = inject(Api);
  protected readonly tela = inject(Tela);
  protected readonly FAMILIA = FAMILIA;
  protected readonly familias = Object.keys(FAMILIA) as Familia[];
  protected readonly tema = temaGrid;
  protected readonly textos = textosGrid;

  protected readonly fabricantes = signal<Fabricante[]>([]);
  protected readonly itens = signal<ItemCatalogo[]>([]);
  protected readonly erro = signal<string | null>(null);
  protected fabricanteId = '';
  protected familia: Familia | '' = '';
  protected busca = '';
  protected soRevisar = false;

  protected readonly colunas: ColDef<ItemCatalogo>[] = [
    { field: 'fabricante', headerName: 'Fabricante', width: 140 },
    { field: 'familia', headerName: 'Família', width: 160, valueFormatter: p => FAMILIA[p.value as Familia] },
    { field: 'descricao', headerName: 'Produto', flex: 1, minWidth: 320 },
    { field: 'valorPeso', headerName: 'Peso da tabela', width: 160, type: 'numericColumn', valueFormatter: p => p.data ? this.valor(p.data) : '' },
    { field: 'pecasPorAmarrado', headerName: 'Peças/amarrado', width: 150, type: 'numericColumn' },
    { field: 'requerRevisao', headerName: 'Revisar', width: 110, cellDataType: 'text', valueFormatter: p => (p.value ? 'Sim' : '—'), tooltipField: 'observacao' },
  ];

  constructor() {
    this.api.fabricantes().subscribe({ next: f => this.fabricantes.set(f), error: e => this.erro.set(mensagemDeErro(e)) });
    this.carregar();
  }

  carregar() {
    this.api.itens({ fabricanteId: this.fabricanteId || undefined, familia: this.familia || undefined, busca: this.busca, requerRevisao: this.soRevisar || undefined })
      .subscribe({ next: l => this.itens.set(l), error: e => this.erro.set(mensagemDeErro(e)) });
  }

  valor(i: ItemCatalogo) { return `${i.valorPeso.toLocaleString('pt-BR', { maximumFractionDigits: 3 })} ${PESO[i.tipoPeso]}`; }
}
