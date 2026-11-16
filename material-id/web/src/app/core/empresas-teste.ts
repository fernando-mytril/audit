import { Injectable, signal } from '@angular/core';

export interface EmpresaTeste { id: string; nome: string; }

const CHAVE = 'materialid.empresas-teste';
const BETA: EmpresaTeste = { id: '11111111-1111-1111-1111-111111111111', nome: 'Empresa Beta' };

/**
 * Empresas cadastradas para teste neste navegador. Em produção as empresas vêm do Mytril Identity (D27);
 * aqui o cadastro só gera o identificador que vai no token de teste.
 */
@Injectable({ providedIn: 'root' })
export class EmpresasTeste {
  readonly lista = signal<EmpresaTeste[]>(this.ler());

  cadastrar(nome: string): EmpresaTeste {
    const empresa = { id: crypto.randomUUID(), nome: nome.trim() };
    this.lista.update(l => [...l, empresa]);
    this.gravar();
    return empresa;
  }

  private ler(): EmpresaTeste[] {
    try {
      const salvas = JSON.parse(localStorage.getItem(CHAVE) ?? '[]') as EmpresaTeste[];
      return [BETA, ...salvas.filter(e => e.id !== BETA.id)];
    } catch { return [BETA]; }
  }

  private gravar() {
    try { localStorage.setItem(CHAVE, JSON.stringify(this.lista().filter(e => e.id !== BETA.id))); } catch { /* sem armazenamento: vale só nesta sessão */ }
  }
}
