import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { mensagemDeErro } from '../../core/http';
import { Sessao } from '../../core/sessao';

/**
 * Entrada provisória de desenvolvimento. O login real é feito pelo Mytril Identity (D27);
 * esta tela usa o emissor de tokens de teste da API, que só existe em Development, até o DEP-01.
 */
@Component({
  selector: 'app-entrar',
  imports: [FormsModule],
  template: `
    <main class="container entrar">
      <h1>Entrar no Material ID</h1>
      <p class="aviso" role="note">Acesso de desenvolvimento. Em produção o login é feito pelo Mytril Identity.</p>
      @if (erro()) { <p class="erro" role="alert">{{ erro() }}</p> }
      <form (ngSubmit)="entrar()" class="cartao">
        <div class="campo">
          <label for="usuario">Nome do usuário</label>
          <input id="usuario" name="usuario" [(ngModel)]="usuario" required autocomplete="name">
        </div>
        <div class="campo">
          <label for="perfil">Perfil</label>
          <select id="perfil" name="perfil" [(ngModel)]="perfil">
            <option value="operador">Operador</option>
            <option value="gestor">Gestor (inclui dono e diretoria)</option>
          </select>
        </div>
        <div class="campo">
          <label for="empresa">Empresa (ID)</label>
          <input id="empresa" name="empresa" [(ngModel)]="empresa" required>
        </div>
        <button class="btn primario" type="submit" [disabled]="enviando()">Entrar</button>
      </form>
    </main>`,
  styles: `.entrar { max-width: 480px; } form { display: grid; }`,
})
export class Entrar {
  private readonly api = inject(Api);
  private readonly sessao = inject(Sessao);
  private readonly router = inject(Router);

  usuario = 'Operador do pátio';
  perfil = 'operador';
  empresa = '11111111-1111-1111-1111-111111111111';
  readonly erro = signal<string | null>(null);
  readonly enviando = signal(false);

  entrar() {
    this.enviando.set(true);
    this.erro.set(null);
    this.api.tokenDesenvolvimento(this.empresa.trim(), this.usuario.trim(), this.perfil).subscribe({
      next: r => { this.sessao.entrar(r.token); this.router.navigateByUrl('/conferencias'); },
      error: e => { this.erro.set(mensagemDeErro(e)); this.enviando.set(false); },
    });
  }
}
