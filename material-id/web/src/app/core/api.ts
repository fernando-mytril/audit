import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ConferenciaDetalhe, ConferenciaResumo, Fabricante, Familia, ItemCatalogo, Operacao, StatusConferencia } from './modelos';

@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  tokenDesenvolvimento(empresaId: string, usuario: string, perfil: string) {
    return this.http.post<{ token: string }>('/dev/token', { empresaId, usuario, perfil });
  }

  conferencias(status?: StatusConferencia) {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<ConferenciaResumo[]>('/api/conferencias', { params });
  }
  conferencia(id: string) { return this.http.get<ConferenciaDetalhe>(`/api/conferencias/${id}`); }
  abrir(operacao: Operacao, contraparte: string | null) { return this.http.post<ConferenciaDetalhe>('/api/conferencias', { operacao, contraparte }); }
  acaoConferencia(id: string, acao: string, motivo?: string) {
    return this.http.post<ConferenciaDetalhe>(`/api/conferencias/${id}/${acao}`, motivo === undefined ? null : { motivo });
  }
  adicionarAmarrado(id: string) { return this.http.post<ConferenciaDetalhe>(`/api/conferencias/${id}/analises`, null); }
  acaoAnalise(id: string, acao: string, corpo?: unknown) { return this.http.post<ConferenciaDetalhe>(`/api/analises/${id}/${acao}`, corpo ?? null); }

  fabricantes() { return this.http.get<Fabricante[]>('/api/catalogo/fabricantes'); }
  itens(filtro: { fabricanteId?: string; familia?: Familia; busca?: string; requerRevisao?: boolean }) {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(filtro)) if (v !== undefined && v !== '' && v !== null) params = params.set(k, String(v));
    return this.http.get<ItemCatalogo[]>('/api/catalogo/itens', { params });
  }
}
