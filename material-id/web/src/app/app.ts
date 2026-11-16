import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Sessao } from './core/sessao';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <a class="pular" href="#conteudo">Pular para o conteúdo</a>
    @if (sessao.autenticado()) {
      <header class="barra">
        <strong class="marca">Material ID</strong>
        <nav aria-label="Principal" class="row">
          <a routerLink="/conferencias" routerLinkActive="ativo" ariaCurrentWhenActive="page">Conferências</a>
          <a routerLink="/catalogo" routerLinkActive="ativo" ariaCurrentWhenActive="page">Catálogo</a>
        </nav>
        <span class="spacer"></span>
        <span class="muted usuario">{{ sessao.usuario() }} · {{ sessao.gestor() ? 'Gestor' : 'Operador' }}</span>
        <button class="btn" type="button" (click)="sair()">Sair</button>
      </header>
    }
    <div id="conteudo" tabindex="-1"><router-outlet /></div>`,
  styles: `
    .barra { display: flex; flex-wrap: wrap; align-items: center; gap: 8px 16px; padding: 8px 16px;
      background: var(--surface); border-bottom: 1px solid var(--line); }
    .marca { font-size: 1.05rem; }
    nav a { display: inline-flex; align-items: center; min-height: 44px; padding: 0 8px; color: var(--text); text-decoration: none; font-weight: 600; }
    nav a.ativo { box-shadow: inset 0 -3px 0 var(--accent); }
    .usuario { font-size: .9rem; }
    .pular { position: absolute; left: -999px; }
    .pular:focus { left: 8px; top: 8px; z-index: 10; background: var(--surface); padding: 8px; }
    .spacer { flex: 1; }`,
})
export class App {
  protected readonly sessao = inject(Sessao);
  private readonly router = inject(Router);
  sair() { this.sessao.sair(); this.router.navigateByUrl('/entrar'); }
}
