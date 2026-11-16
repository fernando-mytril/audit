import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Ambiente } from '../../core/ambiente';
import { EmpresasTeste } from '../../core/empresas-teste';
import { mensagemDeErro } from '../../core/http';
import { Sessao } from '../../core/sessao';

/**
 * Entrada do ambiente de teste. O login real é feito pelo Mytril Identity (D27, DEP-01);
 * aqui o token é emitido pelo emissor de teste da API, que só existe em Development.
 */
@Component({
  selector: 'app-entrar',
  imports: [FormsModule],
  template: `
    <main class="container entrar">
      <h1>Material ID</h1>
      <p class="muted">Conferência de peso de tubos, perfis e chapas pela câmera do celular.</p>

      @if (ambiente.info()?.loginDeTeste === false) {
        <p class="aviso" role="note">O login é feito pelo Mytril Identity.</p>
      } @else {
        <p class="aviso" role="note">Ambiente de teste: o login abaixo substitui o Mytril Identity até a integração.</p>
        @if (erro()) { <p class="erro" role="alert">{{ erro() }}</p> }

        <div class="row abas" role="tablist" aria-label="Acesso">
          <button class="chip" role="tab" type="button" [attr.aria-selected]="aba() === 'entrar'" [attr.aria-pressed]="aba() === 'entrar'" (click)="aba.set('entrar')">Entrar</button>
          <button class="chip" role="tab" type="button" [attr.aria-selected]="aba() === 'cadastro'" [attr.aria-pressed]="aba() === 'cadastro'" (click)="aba.set('cadastro')">Cadastrar empresa</button>
        </div>

        @if (aba() === 'cadastro') {
          <form (ngSubmit)="cadastrar()" class="cartao">
            <div class="campo">
              <label for="nome-empresa">Nome da empresa</label>
              <input id="nome-empresa" name="nomeEmpresa" [(ngModel)]="nomeEmpresa" required autocomplete="organization">
            </div>
            <p class="muted">Cada empresa vê só os próprios dados. Cadastre duas para testar o isolamento.</p>
            <button class="btn primario" type="submit">Cadastrar e continuar</button>
          </form>
        } @else {
          <form (ngSubmit)="entrar()" class="cartao">
            <div class="campo">
              <label for="empresa">Empresa</label>
              <select id="empresa" name="empresa" [(ngModel)]="empresaId">
                @for (e of empresas.lista(); track e.id) { <option [value]="e.id">{{ e.nome }}</option> }
              </select>
            </div>
            <div class="campo">
              <label for="usuario">Seu nome</label>
              <input id="usuario" name="usuario" [(ngModel)]="usuario" required autocomplete="name">
            </div>
            <fieldset class="perfis">
              <legend>Perfil</legend>
              <label class="opcao"><input type="radio" name="perfil" value="operador" [(ngModel)]="perfil"> Operador (pátio)</label>
              <label class="opcao"><input type="radio" name="perfil" value="gestor" [(ngModel)]="perfil"> Gestor (inclui dono e diretoria)</label>
            </fieldset>
            <button class="btn primario" type="submit" [disabled]="enviando()">Entrar</button>
          </form>
        }
      }
    </main>`,
  styles: `
    .entrar { max-width: 480px; }
    form { display: grid; }
    .abas { margin-bottom: 12px; }
    .perfis { border: 0; padding: 0; margin: 0 0 12px; display: grid; }
    .perfis legend { font-weight: 600; font-size: .9rem; margin-bottom: 4px; }
    .opcao { display: inline-flex; gap: 8px; align-items: center; min-height: 44px; }
    .opcao input { width: 20px; height: 20px; }`,
})
export class Entrar {
  private readonly api = inject(Api);
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);
  protected readonly empresas = inject(EmpresasTeste);
  protected readonly ambiente = inject(Ambiente);

  protected readonly aba = signal<'entrar' | 'cadastro'>('entrar');
  protected empresaId = this.empresas.lista()[0].id;
  protected usuario = '';
  protected perfil = 'operador';
  protected nomeEmpresa = '';
  protected readonly erro = signal<string | null>(null);
  protected readonly enviando = signal(false);

  cadastrar() {
    if (!this.nomeEmpresa.trim()) { this.erro.set('Informe o nome da empresa.'); return; }
    this.empresaId = this.empresas.cadastrar(this.nomeEmpresa).id;
    this.nomeEmpresa = '';
    this.erro.set(null);
    this.aba.set('entrar');
  }

  entrar() {
    if (!this.usuario.trim()) { this.erro.set('Informe seu nome.'); return; }
    this.enviando.set(true);
    this.erro.set(null);
    this.api.tokenDesenvolvimento(this.empresaId, this.usuario.trim(), this.perfil).subscribe({
      next: r => { this.sessao.entrar(r.token); this.router.navigateByUrl('/conferencias'); },
      error: e => { this.erro.set(mensagemDeErro(e)); this.enviando.set(false); },
    });
  }
}
