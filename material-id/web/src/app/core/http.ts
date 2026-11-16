import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Sessao } from './sessao';
import { Problema } from './modelos';

export const autenticacao: HttpInterceptorFn = (req, next) => {
  const sessao = inject(Sessao);
  const router = inject(Router);
  const token = sessao.token();
  const r = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(r).pipe(catchError((e: HttpErrorResponse) => {
    if (e.status === 401) { sessao.sair(); router.navigateByUrl('/entrar'); }
    return throwError(() => e);
  }));
};

/** Mensagem legível a partir do Problem Details da API. */
export function mensagemDeErro(e: unknown): string {
  if (e instanceof HttpErrorResponse) {
    const p = e.error as Problema | null;
    if (p?.detail) return p.detail;
    if (e.status === 0) return 'Sem conexão com o servidor.';
    if (e.status === 403) return 'Seu perfil não tem permissão para esta ação.';
  }
  return 'Não foi possível concluir a operação.';
}
