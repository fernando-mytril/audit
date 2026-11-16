import { Component, computed, input } from '@angular/core';
import { tom } from '../core/rotulos';

/** Etiqueta de status: texto + cor + borda. Nunca só cor. */
@Component({
  selector: 'app-etiqueta-status',
  template: `<span class="etiqueta" [attr.data-tom]="tom()">{{ rotulo() }}</span>`,
  styles: `
    .etiqueta { display: inline-block; padding: 2px 10px; border-radius: 999px; border: 1px solid currentColor;
      font-size: .8rem; font-weight: 700; line-height: 1.6; white-space: nowrap; }
    [data-tom="info"] { color: var(--info); } [data-tom="warn"] { color: var(--warn); }
    [data-tom="ok"] { color: var(--ok); } [data-tom="bad"] { color: var(--bad); } [data-tom="neutral"] { color: var(--neutral); }
  `,
})
export class EtiquetaStatus {
  readonly status = input.required<string>();
  readonly rotulo = input.required<string>();
  protected readonly tom = computed(() => tom(this.status()));
}
