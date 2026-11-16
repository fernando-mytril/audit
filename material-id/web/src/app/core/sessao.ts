import { Injectable, computed, signal } from '@angular/core';

const CHAVE = 'materialid.token';

/** Sessão a partir do JWT do Mytril Identity (D27). Lê empresa, usuário e permissões do token. */
@Injectable({ providedIn: 'root' })
export class Sessao {
  private readonly _token = signal<string | null>(ler());
  readonly token = this._token.asReadonly();

  private readonly payload = computed(() => decodificar(this._token()));
  readonly autenticado = computed(() => {
    const p = this.payload();
    return !!p && (typeof p['exp'] !== 'number' || p['exp'] * 1000 > Date.now());
  });
  readonly usuario = computed(() => (this.payload()?.['name'] as string) ?? '');
  readonly permissoes = computed<string[]>(() => {
    const perm = this.payload()?.['perm'];
    return Array.isArray(perm) ? perm : perm ? [perm as string] : [];
  });
  readonly podeCapturar = computed(() => this.permissoes().includes('materialid.conferencia.capturar'));
  readonly gestor = computed(() => this.permissoes().includes('materialid.conferencia.validar'));

  entrar(token: string) {
    try { sessionStorage.setItem(CHAVE, token); } catch { /* armazenamento indisponível: sessão só em memória */ }
    this._token.set(token);
  }

  sair() {
    try { sessionStorage.removeItem(CHAVE); } catch { /* idem */ }
    this._token.set(null);
  }
}

function ler(): string | null {
  try { return sessionStorage.getItem(CHAVE); } catch { return null; }
}

function decodificar(token: string | null): Record<string, unknown> | null {
  if (!token) return null;
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(decodeURIComponent(escape(atob(base64))));
  } catch { return null; }
}
