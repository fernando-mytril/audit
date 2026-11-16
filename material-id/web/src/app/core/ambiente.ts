import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';

export interface InfoAmbiente { ambiente: string; iaSimulada: boolean; loginDeTeste: boolean; }

/** Informações do ambiente (teste ou produção) para avisos na tela. */
@Injectable({ providedIn: 'root' })
export class Ambiente {
  readonly info = signal<InfoAmbiente | null>(null);

  constructor() {
    inject(HttpClient).get<InfoAmbiente>('/api/ambiente').subscribe({ next: i => this.info.set(i), error: () => this.info.set(null) });
  }
}
