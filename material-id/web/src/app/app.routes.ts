import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { Sessao } from './core/sessao';

const autenticado: CanActivateFn = () => inject(Sessao).autenticado() || inject(Router).parseUrl('/entrar');

export const routes: Routes = [
  { path: 'entrar', title: 'Entrar · Material ID', loadComponent: () => import('./paginas/entrar/entrar').then(m => m.Entrar) },
  { path: 'conferencias', title: 'Conferências · Material ID', canActivate: [autenticado],
    loadComponent: () => import('./paginas/conferencias/conferencias').then(m => m.Conferencias) },
  { path: 'conferencias/:id', title: 'Conferência · Material ID', canActivate: [autenticado],
    loadComponent: () => import('./paginas/conferencia/conferencia').then(m => m.ConferenciaPagina) },
  { path: 'catalogo', title: 'Catálogo · Material ID', canActivate: [autenticado],
    loadComponent: () => import('./paginas/catalogo/catalogo').then(m => m.Catalogo) },
  { path: '', pathMatch: 'full', redirectTo: 'conferencias' },
  { path: '**', redirectTo: 'conferencias' },
];
