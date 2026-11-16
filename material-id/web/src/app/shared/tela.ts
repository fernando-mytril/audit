import { Injectable, signal } from '@angular/core';

/** Largura da tela: cards até 767 px, grid a partir de 768 px (D22). */
@Injectable({ providedIn: 'root' })
export class Tela {
  private readonly consulta = typeof matchMedia === 'function' ? matchMedia('(min-width: 768px)') : null;
  readonly desktop = signal(this.consulta?.matches ?? true);

  constructor() {
    this.consulta?.addEventListener('change', e => this.desktop.set(e.matches));
  }
}
